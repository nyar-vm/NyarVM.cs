using Nyar.Database.Index;

namespace Nyar.Tests.Database;

public sealed class FileIndexTests
{
    #region 辅助方法

    private static FileRecord create_file(string uri, string hash, string[]? deps = null)
    {
        var file = new FileRecord();
        file.Uri = uri;
        file.ContentHash = hash;
        file.LastModified = DateTime.UtcNow;
        file.DependencyUris = deps ?? [];
        file.LanguageId = "typescript";
        file.AnalysisStatus = FileAnalysisStatus.Completed;
        return file;
    }

    #endregion

    #region Clear 测试

    [Fact]
    public void Clear_清空所有数�?)
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));
        index.AddOrUpdate(create_file("file:///b.ts", "hash2"));

        index.Clear();

        Assert.Equal(0, index.Count);
        Assert.Empty(index.All);
    }

    #endregion

    #region AddOrUpdate 测试

    [Fact]
    public void AddOrUpdate_添加新文件_计数增加()
    {
        var index = new FileIndex();
        var file = create_file("file:///a.ts", "hash1");

        index.AddOrUpdate(file);

        Assert.Equal(1, index.Count);
        Assert.True(index.Contains("file:///a.ts"));
    }

    [Fact]
    public void AddOrUpdate_更新已有文件_计数不变()
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));
        index.AddOrUpdate(create_file("file:///a.ts", "hash2"));

        Assert.Equal(1, index.Count);
        var found = index.Find("file:///a.ts");
        Assert.NotNull(found);
        Assert.Equal("hash2", found.ContentHash);
    }

    #endregion

    #region 依赖图测�?
    [Fact]
    public void AddOrUpdate_依赖关系正确建立()
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));
        index.AddOrUpdate(create_file("file:///b.ts", "hash2", ["file:///a.ts"]));

        var deps = index.GetDependencies("file:///b.ts");
        Assert.Contains("file:///a.ts", deps);
    }

    [Fact]
    public void AddOrUpdate_反向依赖正确建立()
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));
        index.AddOrUpdate(create_file("file:///b.ts", "hash2", ["file:///a.ts"]));

        var dependents = index.GetDependents("file:///a.ts");
        Assert.Contains("file:///b.ts", dependents);
    }

    [Fact]
    public void GetTransitiveDependents_传递依赖正确计�?)
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));
        index.AddOrUpdate(create_file("file:///b.ts", "hash2", ["file:///a.ts"]));
        index.AddOrUpdate(create_file("file:///c.ts", "hash3", ["file:///b.ts"]));

        var transitive = index.GetTransitiveDependents("file:///a.ts");

        Assert.Contains("file:///b.ts", transitive);
        Assert.Contains("file:///c.ts", transitive);
    }

    [Fact]
    public void AddOrUpdate_更新文件时旧依赖被移�?)
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));
        index.AddOrUpdate(create_file("file:///b.ts", "hash2", ["file:///a.ts"]));
        index.AddOrUpdate(create_file("file:///b.ts", "hash3"));

        var deps = index.GetDependencies("file:///b.ts");
        Assert.Empty(deps);
        var dependents = index.GetDependents("file:///a.ts");
        Assert.Empty(dependents);
    }

    #endregion

    #region HasChanged 测试

    [Fact]
    public void HasChanged_哈希相同_返回false()
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));

        Assert.False(index.HasChanged("file:///a.ts", "hash1"));
    }

    [Fact]
    public void HasChanged_哈希不同_返回true()
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));

        Assert.True(index.HasChanged("file:///a.ts", "hash2"));
    }

    [Fact]
    public void HasChanged_不存在的文件_返回true()
    {
        var index = new FileIndex();

        Assert.True(index.HasChanged("file:///nonexistent.ts", "any"));
    }

    #endregion

    #region Remove 测试

    [Fact]
    public void Remove_存在的文件_返回true并移�?)
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));

        var result = index.Remove("file:///a.ts");

        Assert.True(result);
        Assert.Equal(0, index.Count);
    }

    [Fact]
    public void Remove_不存在的文件_返回false()
    {
        var index = new FileIndex();

        var result = index.Remove("file:///nonexistent.ts");

        Assert.False(result);
    }

    [Fact]
    public void Remove_依赖关系被清�?)
    {
        var index = new FileIndex();
        index.AddOrUpdate(create_file("file:///a.ts", "hash1"));
        index.AddOrUpdate(create_file("file:///b.ts", "hash2", ["file:///a.ts"]));

        index.Remove("file:///b.ts");

        var dependents = index.GetDependents("file:///a.ts");
        Assert.Empty(dependents);
    }

    #endregion
}
