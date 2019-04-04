using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class BTreeIndexTests : IDisposable
{
    private readonly string _tempPath;
    private BTreeIndex? _index;
    private PageCache? _pageCache;
    private FileStorageEngine? _storage;

    public BTreeIndexTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_btree_test_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        _index = null;
        _pageCache = null;
        _storage?.Dispose();

        try
        {
            if (Directory.Exists(_tempPath)) Directory.Delete(_tempPath, true);
        }
        catch
        {
        }
    }

    private async Task<BTreeIndex> CreateIndexAsync()
    {
        _storage = new FileStorageEngine(_tempPath);
        _pageCache = new PageCache(_storage, 1024);
        _index = new BTreeIndex(_pageCache, "test", 4);
        return _index;
    }

    [Fact]
    public async Task InsertAndSearch_ShouldReturnValue()
    {
        var index = await CreateIndexAsync();
        var key = DatabaseKey.from_string("hello");
        var value = DatabaseValue.from_string("world");

        await index.insert(key, value);
        var result = await index.search(key);

        Assert.NotNull(result);
        Assert.Equal("world", result.Value.ToString());
    }

    [Fact]
    public async Task Search_NonexistentKey_ShouldReturnNull()
    {
        var index = await CreateIndexAsync();
        var key = DatabaseKey.from_string("missing");

        var result = await index.search(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task Insert_DuplicateKey_ShouldUpdateValue()
    {
        var index = await CreateIndexAsync();
        var key = DatabaseKey.from_string("key1");

        await index.insert(key, DatabaseValue.from_string("v1"));
        await index.insert(key, DatabaseValue.from_string("v2"));

        var result = await index.search(key);

        Assert.NotNull(result);
        Assert.Equal("v2", result.Value.ToString());
    }

    [Fact]
    public async Task Delete_ExistingKey_ShouldRemove()
    {
        var index = await CreateIndexAsync();
        var key = DatabaseKey.from_string("key1");

        await index.insert(key, DatabaseValue.from_string("value"));
        var deleted = await index.DeleteAsync(key);
        var result = await index.search(key);

        Assert.True(deleted);
        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_NonexistentKey_ShouldReturnFalse()
    {
        var index = await CreateIndexAsync();
        var key = DatabaseKey.from_string("missing");

        var deleted = await index.DeleteAsync(key);

        Assert.False(deleted);
    }

    [Fact]
    public async Task Delete_MultipleKeys_ShouldRemoveCorrectly()
    {
        var index = await CreateIndexAsync();

        for (var i = 0; i < 10; i++)
            await index.insert(DatabaseKey.from_string($"key{i}"), DatabaseValue.from_string($"value{i}"));

        var deleted = await index.DeleteAsync(DatabaseKey.from_string("key3"));
        Assert.True(deleted);

        var result = await index.search(DatabaseKey.from_string("key3"));
        Assert.Null(result);

        for (var i = 0; i < 10; i++)
        {
            if (i == 3) continue;

            var value = await index.search(DatabaseKey.from_string($"key{i}"));
            Assert.NotNull(value);
            Assert.Equal($"value{i}", value.Value.ToString());
        }
    }

    [Fact]
    public async Task RangeScan_ShouldReturnOrderedResults()
    {
        var index = await CreateIndexAsync();

        for (var i = 5; i < 15; i++)
            await index.insert(DatabaseKey.from_string($"key{i:D2}"), DatabaseValue.from_string($"value{i}"));

        var results = new List<DatabaseEntry>();
        await foreach (var entry in index.RangeScanAsync(DatabaseKey.from_string("key07"),
                           DatabaseKey.from_string("key12"))) results.Add(entry);

        Assert.Equal(6, results.Count);
        Assert.Equal("key07", results[0].Key.ToString());
        Assert.Equal("key12", results[^1].Key.ToString());
    }

    [Fact]
    public async Task PrefixScan_ShouldReturnMatchingResults()
    {
        var index = await CreateIndexAsync();

        await index.insert(DatabaseKey.from_string("user:alice"), DatabaseValue.from_string("Alice"));
        await index.insert(DatabaseKey.from_string("user:bob"), DatabaseValue.from_string("Bob"));
        await index.insert(DatabaseKey.from_string("product:1"), DatabaseValue.from_string("Product"));

        var results = new List<DatabaseEntry>();
        await foreach (var entry in index.PrefixScanAsync(DatabaseKey.from_string("user:"))) results.Add(entry);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task LargeNumberOfInserts_ShouldWork()
    {
        var index = await CreateIndexAsync();
        const int count = 1000;

        for (var i = 0; i < count; i++)
            await index.insert(DatabaseKey.from_string($"key{i:D6}"), DatabaseValue.from_string($"value{i}"));

        for (var i = 0; i < count; i++)
        {
            var result = await index.search(DatabaseKey.from_string($"key{i:D6}"));
            Assert.NotNull(result);
            Assert.Equal($"value{i}", result.Value.ToString());
        }
    }

    [Fact]
    public async Task Insert_AfterDelete_ShouldWork()
    {
        var index = await CreateIndexAsync();
        var key = DatabaseKey.from_string("key1");

        await index.insert(key, DatabaseValue.from_string("v1"));
        await index.DeleteAsync(key);
        await index.insert(key, DatabaseValue.from_string("v2"));

        var result = await index.search(key);

        Assert.NotNull(result);
        Assert.Equal("v2", result.Value.ToString());
    }

    [Fact]
    public async Task Delete_AllKeys_ShouldEmptyTree()
    {
        var index = await CreateIndexAsync();

        for (var i = 0; i < 20; i++)
            await index.insert(DatabaseKey.from_string($"key{i}"), DatabaseValue.from_string($"value{i}"));

        for (var i = 0; i < 20; i++)
        {
            var deleted = await index.DeleteAsync(DatabaseKey.from_string($"key{i}"));
            Assert.True(deleted);
        }

        for (var i = 0; i < 20; i++)
        {
            var result = await index.search(DatabaseKey.from_string($"key{i}"));
            Assert.Null(result);
        }
    }
}