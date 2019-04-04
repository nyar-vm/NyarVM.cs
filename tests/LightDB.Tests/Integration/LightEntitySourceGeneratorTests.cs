using Std.Database.Core;

namespace LightDB.Tests.Integration;

/// <summary>
///     测试 Source Generator 生成的 DatabaseKeyBuilder 及显式 DatabaseKey API
/// </summary>
public sealed class LightEntitySourceGeneratorTests : IDisposable
{
    private readonly LightDatabase _db;

    public LightEntitySourceGeneratorTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_sg_test_{Guid.NewGuid():N}");
        _db = new LightDatabase(new LightOptions { path = tempPath });
    }

    public void Dispose()
    {
        _db.Dispose();
        try
        {
            if (Directory.Exists(_db.options.path)) Directory.Delete(_db.options.path, true);
        }
        catch
        {
        }
    }

    #region 原始 DatabaseKey 直接 API 测试

    [Fact]
    public async Task RawKey_ShouldRoundtrip()
    {
        var key = DatabaseKey.from_string("raw-key");

        await _db.put(key, "raw-value");

        var result = await _db.get<string>(key);
        Assert.Equal("raw-value", result);
    }

    #endregion

    #region DbUser（string 主键）实体测试

    [Fact]
    public async Task PutAndGet_WithStringKey_ShouldRoundtrip()
    {
        var user = new DbUser { Name = "张三", Age = 28 };
        var key = DatabaseKeyBuilder.Build<DbUser>("张三");

        await _db.put(key, user);

        var result = await _db.get<DbUser>(key);
        Assert.NotNull(result);
        Assert.Equal("张三", result.Name);
        Assert.Equal(28, result.Age);
    }

    [Fact]
    public async Task PutAndGet_AnotherUser_ShouldRoundtrip()
    {
        var key = DatabaseKeyBuilder.Build<DbUser>("李四");

        await _db.put(key, new DbUser { Name = "李四", Age = 35 });

        var result = await _db.get<DbUser>(key);
        Assert.NotNull(result);
        Assert.Equal("李四", result.Name);
        Assert.Equal(35, result.Age);
    }

    [Fact]
    public async Task Get_WithNonexistentKey_ShouldReturnNull()
    {
        var key = DatabaseKeyBuilder.Build<DbUser>("不存在的用户");

        var result = await _db.get<DbUser>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task Put_Overwrite_ShouldUpdateValue()
    {
        var key = DatabaseKeyBuilder.Build<DbUser>("王五");

        await _db.put(key, new DbUser { Name = "王五", Age = 20 });
        await _db.put(key, new DbUser { Name = "王五", Age = 25 });

        var result = await _db.get<DbUser>(key);
        Assert.NotNull(result);
        Assert.Equal(25, result.Age);
    }

    #endregion

    #region DbOrder（int 主键）实体测试

    [Fact]
    public async Task PutAndGet_WithIntKey_ShouldRoundtrip()
    {
        var order = new DbOrder { Id = 100, Amount = 99.9m };
        var key = DatabaseKeyBuilder.Build<DbOrder>(100);

        await _db.put(key, order);

        var result = await _db.get<DbOrder>(key);
        Assert.NotNull(result);
        Assert.Equal(100, result.Id);
        Assert.Equal(99.9m, result.Amount);
    }

    [Fact]
    public async Task Delete_WithIntKey_ShouldRemoveEntity()
    {
        var key = DatabaseKeyBuilder.Build<DbOrder>(300);

        await _db.put(key, new DbOrder { Id = 300, Amount = 10m });

        var deleted = await _db.delete(key);
        Assert.True(deleted);

        var result = await _db.get<DbOrder>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task Contains_WithIntKey_ShouldReturnCorrectResult()
    {
        var key = DatabaseKeyBuilder.Build<DbOrder>(400);

        await _db.put(key, new DbOrder { Id = 400, Amount = 50m });

        var exists = await _db.contains(key);
        Assert.True(exists);
    }

    [Fact]
    public async Task Contains_WithMissingKey_ShouldReturnFalse()
    {
        var key = DatabaseKeyBuilder.Build<DbOrder>(99999);

        var exists = await _db.contains(key);
        Assert.False(exists);
    }

    #endregion

    #region SgUser（外部程序集实体）测试

    [Fact]
    public async Task PutAndGet_WithSgUser_ShouldRoundtrip()
    {
        var key = DatabaseKeyBuilder.Build<SgUser>("赵六");

        await _db.put(key, new SgUser { Name = "赵六", Age = 40 });

        var result = await _db.get<SgUser>(key);
        Assert.NotNull(result);
        Assert.Equal(40, result.Age);
    }

    [Fact]
    public async Task Get_WithSgUser_ShouldReturnCorrectAge()
    {
        var key = DatabaseKeyBuilder.Build<SgUser>("钱七");

        await _db.put(key, new SgUser { Name = "钱七", Age = 50 });

        var result = await _db.get<SgUser>(key);
        Assert.NotNull(result);
        Assert.Equal(50, result.Age);
    }

    [Fact]
    public async Task Delete_WithSgUser_ShouldRemoveEntity()
    {
        var key = DatabaseKeyBuilder.Build<SgUser>("待删用户");

        await _db.put(key, new SgUser { Name = "待删用户", Age = 60 });

        var deleted = await _db.delete(key);
        Assert.True(deleted);

        var result = await _db.get<SgUser>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task Contains_WithSgUser_ShouldReturnCorrectResult()
    {
        var key = DatabaseKeyBuilder.Build<SgUser>("存在");

        await _db.put(key, new SgUser { Name = "存在", Age = 30 });

        var exists = await _db.contains(key);
        Assert.True(exists);
    }

    #endregion
}