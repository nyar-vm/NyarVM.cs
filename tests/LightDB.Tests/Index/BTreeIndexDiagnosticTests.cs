using Std.Database.Core;

namespace LightDB.Tests.Index;

public sealed class BTreeIndexDiagnosticTests : IDisposable
{
    private readonly PageCache _pageCache;
    private readonly FileStorageEngine _storage;
    private readonly string _tempDir;

    public BTreeIndexDiagnosticTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LightDB_btree_diag_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _storage = new FileStorageEngine(_tempDir);
        _pageCache = new PageCache(_storage, 1024);
    }

    public void Dispose()
    {
        _storage.Dispose();
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task PageCache_AllocateAndGet_ShouldReturnSamePage()
    {
        var page = await _pageCache.AllocatePageAsync();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        data.CopyTo(page.Data, 0);

        var retrieved = await _pageCache.GetPageAsync(page.Id);

        Assert.Equal(page.Id, retrieved.Id);
        Assert.Equal(data[0], retrieved.Data[0]);
        Assert.Equal(data[4], retrieved.Data[4]);
    }

    [Fact]
    public async Task BTreeNode_BinarySearch_ShouldFindKey()
    {
        var node = new BTreeNode { IsLeaf = true };
        var key1 = DatabaseKey.from_string("key1");
        node.Keys.Add(key1);
        node.Values.Add(DatabaseValue.from_string("value1"));

        var searchKey = DatabaseKey.from_string("key1");
        var index = node.Keys.BinarySearch(searchKey);

        Assert.True(index >= 0, $"BinarySearch should find key, got index={index}");
    }

    [Fact]
    public async Task InsertAndSearch_DirectPageCheck_ShouldFindData()
    {
        var index = new BTreeIndex(_pageCache, "test");
        var key = DatabaseKey.from_string("key1");
        var value = DatabaseValue.from_string("value1");

        await index.insert(key, value);

        var result = await index.search(key);
        Assert.NotNull(result);
        Assert.Equal("value1", result.Value.ToString());
    }
}