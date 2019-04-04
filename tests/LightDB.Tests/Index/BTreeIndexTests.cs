using Std.Database.Core;

namespace LightDB.Tests.Index;

public sealed class BTreeIndexTests : IDisposable
{
    private readonly PageCache _pageCache;
    private readonly FileStorageEngine _storage;
    private readonly string _tempDir;

    public BTreeIndexTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LightDB_btree_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var dataPath = Path.Combine(_tempDir, "test.dat");
        _storage = new FileStorageEngine(_tempDir);
        _pageCache = new PageCache(_storage, 1024);
    }

    public void Dispose()
    {
        _pageCache.flush().AsTask().Wait();
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
    public async Task InsertAndSearch_ShouldFindKey()
    {
        var index = new BTreeIndex(_pageCache, "test");
        var key = DatabaseKey.from_string("key1");
        var value = DatabaseValue.from_string("value1");

        await index.insert(key, value);
        var result = await index.search(key);

        Assert.NotNull(result);
        Assert.Equal("value1", result.Value.ToString());
    }

    [Fact]
    public async Task Search_WithNonexistentKey_ShouldReturnNull()
    {
        var index = new BTreeIndex(_pageCache, "test");

        var result = await index.search(DatabaseKey.from_string("nonexistent"));

        Assert.Null(result);
    }

    [Fact]
    public async Task Insert_MultipleKeys_ShouldFindAll()
    {
        var index = new BTreeIndex(_pageCache, "test");

        await index.insert(DatabaseKey.from_string("a"), DatabaseValue.from_string("va"));
        await index.insert(DatabaseKey.from_string("b"), DatabaseValue.from_string("vb"));
        await index.insert(DatabaseKey.from_string("c"), DatabaseValue.from_string("vc"));

        var a = await index.search(DatabaseKey.from_string("a"));
        var b = await index.search(DatabaseKey.from_string("b"));
        var c = await index.search(DatabaseKey.from_string("c"));

        Assert.NotNull(a);
        Assert.Equal("va", a.Value.ToString());
        Assert.NotNull(b);
        Assert.Equal("vb", b.Value.ToString());
        Assert.NotNull(c);
        Assert.Equal("vc", c.Value.ToString());
    }

    [Fact]
    public async Task Insert_Overwrite_ShouldUpdateValue()
    {
        var index = new BTreeIndex(_pageCache, "test");
        var key = DatabaseKey.from_string("key1");

        await index.insert(key, DatabaseValue.from_string("v1"));
        await index.insert(key, DatabaseValue.from_string("v2"));

        var result = await index.search(key);

        Assert.NotNull(result);
        Assert.Equal("v2", result.Value.ToString());
    }
}