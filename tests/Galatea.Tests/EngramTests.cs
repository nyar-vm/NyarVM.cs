namespace Galatea.Tests;

/// <summary>
///     Engram 模型存储集成测试 —— 验证 M8 里程碑：
///     模型保存/加载、版本管理、分支、回滚、元数据
/// </summary>
public class EngramTests : GradientTestBase
{
    #region 辅助方法

    /// <summary>
    ///     创建测试用的 EngramData
    /// </summary>
    private static EngramData CreateEngramData(
        string id = "",
        string branchId = "main",
        byte[]? modelData = null,
        Dictionary<string, string>? metadata = null)
    {
        return new EngramData
        {
            Id = id,
            BranchId = branchId,
            ModelData = modelData ?? new byte[] { 1, 2, 3, 4 },
            Metadata = metadata ?? new Dictionary<string, string>(),
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region ModelSerializer 序列化

    [Fact]
    public void ModelSerializer_Serialize_ProducesValidData()
    {
        var model = new TestLinearModel(4, 3);
        var parameters = model.Parameters().ToList();

        var data = ModelSerializer.Serialize(parameters);

        Assert.True(data.Length > 0, "序列化数据不应为空");
        Assert.True(data.Length > 16, "序列化数据应包含头部和参数");
    }

    [Fact]
    public void ModelSerializer_RoundTrip_PreservesWeights()
    {
        var model = new TestLinearModel(5, 4);
        var parameters = model.Parameters().ToList();
        var originalCopy = parameters.Select(p => p.Value.Clone()).ToArray();

        var data = ModelSerializer.Serialize(parameters);
        var deserialized = ModelSerializer.Deserialize(data);

        var error = ModelSerializer.VerifyRoundTrip(parameters, deserialized);
        Assert.True(error < 1e-6f, $"往返序列化应无损，误差={error}");
    }

    [Fact]
    public void ModelSerializer_Deserialize_CorrectCount()
    {
        var model = new TestLinearModel(6, 3);
        var parameters = model.Parameters().ToList();
        var expectedCount = parameters.Count;

        var data = ModelSerializer.Serialize(parameters);
        var deserialized = ModelSerializer.Deserialize(data);

        Assert.Equal(expectedCount, deserialized.Count);
    }

    [Fact]
    public void ModelSerializer_LoadInto_RestoresParameters()
    {
        var model = new TestLinearModel(3, 2);
        var parameters = model.Parameters().ToList();

        var data = ModelSerializer.Serialize(parameters);
        var deserialized = ModelSerializer.Deserialize(data);

        var outputBefore = model.Forward(RandomInput(2, 3));
        ModelSerializer.LoadInto(parameters, deserialized);
        var outputAfter = model.Forward(RandomInput(2, 3));

        var error = MaxAbsoluteError(outputBefore, outputAfter);
        Assert.True(error < 1e-6f, $"加载参数后输出应一致，误差={error}");
    }

    [Fact]
    public void ModelSerializer_InvalidMagic_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ModelSerializer.Deserialize(new byte[] { 0, 0, 0, 0 }));
    }

    [Fact]
    public void ModelSerializer_LoadInto_ShapeMismatch_Throws()
    {
        var model = new TestLinearModel(4, 3);
        var parameters = model.Parameters().ToList();

        var shapeMismatch = new[] { ArrayND.Zeros(10, 10) };

        Assert.Throws<InvalidOperationException>(() =>
            ModelSerializer.LoadInto(parameters, shapeMismatch));
    }

    #endregion

    #region InMemoryEngramStore 保存/加载

    [Fact]
    public async Task EngramStore_Save_GeneratesId_WhenEmpty()
    {
        var store = new InMemoryEngramStore();
        var engram = CreateEngramData();

        var id = await store.SaveAsync(engram);

        Assert.False(string.IsNullOrEmpty(id), "保存应生成非空 ID");
        Assert.Equal(1, store.VersionCount);
    }

    [Fact]
    public async Task EngramStore_SaveAndGet_RoundTrip()
    {
        var store = new InMemoryEngramStore();
        var engram = CreateEngramData(metadata: new Dictionary<string, string>
        {
            { "epoch", "10" },
            { "accuracy", "0.95" }
        });

        var id = await store.SaveAsync(engram);
        var loaded = await store.GetAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(engram.BranchId, loaded!.BranchId);
        Assert.Equal(engram.Metadata["epoch"], loaded.Metadata["epoch"]);
    }

    [Fact]
    public async Task EngramStore_Get_NonExistent_ReturnsNull()
    {
        var store = new InMemoryEngramStore();
        var result = await store.GetAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task EngramStore_MultipleSaves_IncrementsVersionCount()
    {
        var store = new InMemoryEngramStore();

        await store.SaveAsync(CreateEngramData("v1"));
        await store.SaveAsync(CreateEngramData("v2"));
        await store.SaveAsync(CreateEngramData("v3"));

        Assert.Equal(3, store.VersionCount);
        Assert.Equal(3, store.DataCount);
    }

    #endregion

    #region 版本管理

    [Fact]
    public async Task EngramStore_ListAsync_ReturnsAllVersions()
    {
        var store = new InMemoryEngramStore();

        await store.SaveAsync(CreateEngramData("v1"));
        await store.SaveAsync(CreateEngramData("v2"));

        var versions = await store.ListAsync().ToListAsync();

        Assert.Equal(2, versions.Count);
        Assert.Contains(versions, v => v.Id == "v1");
        Assert.Contains(versions, v => v.Id == "v2");
    }

    [Fact]
    public async Task EngramStore_GetBranchHead_ReturnsLatest()
    {
        var store = new InMemoryEngramStore();

        await store.SaveAsync(CreateEngramData("v1", "main"));
        await store.SaveAsync(CreateEngramData("v2", "main"));
        await store.SaveAsync(CreateEngramData("v3", "main"));

        var head = store.GetBranchHead("main");
        Assert.NotNull(head);
        Assert.Equal("v3", head!.Id);
    }

    [Fact]
    public async Task EngramStore_ListBranchVersions_Chronological()
    {
        var store = new InMemoryEngramStore();

        await store.SaveAsync(CreateEngramData("a", "main"));
        await store.SaveAsync(CreateEngramData("b", "main"));
        await store.SaveAsync(CreateEngramData("c", "main"));

        var versions = await store.ListBranchVersionsAsync("main").ToListAsync();

        Assert.Equal(3, versions.Count);
        Assert.Equal("a", versions[0].Id);
        Assert.Equal("b", versions[1].Id);
        Assert.Equal("c", versions[2].Id);
    }

    #endregion

    #region 分支管理

    [Fact]
    public void EngramStore_CreateBranch_CreatesIndependentLine()
    {
        var store = new InMemoryEngramStore();
        var branch = store.CreateBranch("experiment", "main");

        Assert.NotNull(branch);
        Assert.Equal("experiment", branch.Name);
        Assert.Equal("main", branch.ParentId);
        Assert.NotNull(store.GetBranch(branch.Id));
    }

    [Fact]
    public async Task EngramStore_Branches_IndependentVersions()
    {
        var store = new InMemoryEngramStore();

        await store.SaveAsync(CreateEngramData("m1", "main"));

        var expBranch = store.CreateBranch("experiment", "main");
        await store.SaveAsync(CreateEngramData("e1", expBranch.Id));
        await store.SaveAsync(CreateEngramData("e2", expBranch.Id));

        await store.SaveAsync(CreateEngramData("m2", "main"));

        var mainVersions = await store.ListBranchVersionsAsync("main").ToListAsync();
        var expVersions = await store.ListBranchVersionsAsync(expBranch.Id).ToListAsync();

        Assert.Equal(2, mainVersions.Count);
        Assert.Equal(2, expVersions.Count);
        Assert.Equal("m2", mainVersions[^1].Id);
        Assert.Equal("e2", expVersions[^1].Id);
    }

    [Fact]
    public void EngramStore_FindBranchByName_Works()
    {
        var store = new InMemoryEngramStore();
        var created = store.CreateBranch("lora-tuning", "main");

        var found = store.FindBranchByName("lora-tuning");
        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);

        var notFound = store.FindBranchByName("nonexistent");
        Assert.Null(notFound);
    }

    [Fact]
    public void EngramStore_ListBranches_IncludesDefaultMain()
    {
        var store = new InMemoryEngramStore();

        store.CreateBranch("dev", "main");
        store.CreateBranch("staging", "main");

        var branches = store.ListBranches();
        Assert.True(branches.Count >= 3, $"至少 3 个分支，实际={branches.Count}");
        Assert.Contains(branches, b => b.Name == "main");
        Assert.Contains(branches, b => b.Name == "dev");
        Assert.Contains(branches, b => b.Name == "staging");
    }

    #endregion

    #region 回滚

    [Fact]
    public async Task EngramStore_Rollback_CreatesNewVersion()
    {
        var store = new InMemoryEngramStore();

        await store.SaveAsync(CreateEngramData("v1", "main",
            new byte[] { 1, 2, 3 }));
        await store.SaveAsync(CreateEngramData("v2", "main",
            new byte[] { 4, 5, 6 }));

        var rollbackId = await store.RollbackAsync("v1", "main", "回滚到 v1");

        Assert.False(string.IsNullOrEmpty(rollbackId));

        var head = store.GetBranchHead("main");
        Assert.NotNull(head);
        Assert.Equal(new byte[] { 1, 2, 3 }, head!.ModelData);
        Assert.Contains("回滚到 v1", head.Metadata["message"]);
        Assert.Equal("v1", head.Metadata["rollback_source"]);
    }

    [Fact]
    public async Task EngramStore_Rollback_InvalidVersion_Throws()
    {
        var store = new InMemoryEngramStore();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.RollbackAsync("nonexistent", "main"));
    }

    #endregion

    #region 元数据管理

    [Fact]
    public async Task EngramStore_Metadata_PreservedOnSaveGet()
    {
        var store = new InMemoryEngramStore();

        var metadata = new Dictionary<string, string>
        {
            { "model_type", "Linear" },
            { "input_size", "4" },
            { "output_size", "3" },
            { "training_samples", "1000" },
            { "val_accuracy", "0.92" },
            { "framework", "Galatea" }
        };

        var engram = CreateEngramData(metadata: metadata);
        var id = await store.SaveAsync(engram);
        var loaded = await store.GetAsync(id);

        Assert.NotNull(loaded);
        foreach (var kv in metadata) Assert.Equal(kv.Value, loaded!.Metadata[kv.Key]);
    }

    [Fact]
    public void EngramStore_Reset_ClearsAll()
    {
        var store = new InMemoryEngramStore();
        store.CreateBranch("test", "main");

        store.Reset();

        Assert.Equal(0, store.VersionCount);
        Assert.Equal(1, store.BranchCount);
        Assert.NotNull(store.FindBranchByName("main"));
    }

    #endregion
}