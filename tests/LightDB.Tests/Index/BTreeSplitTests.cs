using Std.Database.Core;

namespace LightDB.Tests.Index;

public sealed class BTreeSplitTests : IDisposable
{
    private readonly PageCache _pageCache;
    private readonly FileStorageEngine _storage;
    private readonly string _tempDir;

    public BTreeSplitTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LightDB_split_test_{Guid.NewGuid():N}");
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
    public async Task Insert_TriggerSingleSplit_ShouldFindAllKeys()
    {
        var index = new BTreeIndex(_pageCache, "test", 4);

        for (var i = 0; i < 5; i++)
            await index.insert(DatabaseKey.from_string($"key{i}"), DatabaseValue.from_string($"value{i}"));

        for (var i = 0; i < 5; i++)
        {
            var result = await index.search(DatabaseKey.from_string($"key{i}"));
            Assert.NotNull(result);
            Assert.Equal($"value{i}", result.Value.ToString());
        }
    }

    [Fact]
    public async Task Insert_TriggerMultipleSplits_ShouldFindAllKeys()
    {
        var index = new BTreeIndex(_pageCache, "test", 4);

        for (var i = 0; i < 20; i++)
            await index.insert(DatabaseKey.from_string($"key{i:D4}"), DatabaseValue.from_string($"value{i}"));

        for (var i = 0; i < 20; i++)
        {
            var result = await index.search(DatabaseKey.from_string($"key{i:D4}"));
            Assert.NotNull(result);
            Assert.Equal($"value{i}", result.Value.ToString());
        }
    }

    [Fact]
    public async Task Insert_100Keys_ShouldFindAll()
    {
        var index = new BTreeIndex(_pageCache, "test", 4);

        for (var i = 0; i < 100; i++)
            await index.insert(DatabaseKey.from_string($"key{i:D6}"), DatabaseValue.from_string($"value{i}"));

        var failedKeys = new List<int>();
        for (var i = 0; i < 100; i++)
        {
            var result = await index.search(DatabaseKey.from_string($"key{i:D6}"));
            if (result is null) failedKeys.Add(i);
        }

        Assert.Empty(failedKeys);
    }

    [Fact]
    public async Task Insert_500Keys_ShouldFindAll()
    {
        var index = new BTreeIndex(_pageCache, "test", 4);

        for (var i = 0; i < 500; i++)
            await index.insert(DatabaseKey.from_string($"key{i:D6}"), DatabaseValue.from_string($"value{i}"));

        var failedKeys = new List<int>();
        for (var i = 0; i < 500; i++)
        {
            var result = await index.search(DatabaseKey.from_string($"key{i:D6}"));
            if (result is null) failedKeys.Add(i);
        }

        Assert.Empty(failedKeys);
    }
}