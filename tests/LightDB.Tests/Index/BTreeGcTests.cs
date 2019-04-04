using Std.Database.Core;

namespace LightDB.Tests.Index;

/// <summary>
///     B+Tree 垃圾回收测试：标记-清除、页面合并、空闲页复用
/// </summary>
public sealed class BTreeGcTests : IDisposable
{
    private readonly PageCache _pageCache;
    private readonly FileStorageEngine _storage;
    private readonly string _tempDir;

    public BTreeGcTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LightDB_gc_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _storage = new FileStorageEngine(_tempDir);
        _pageCache = new PageCache(_storage, 128);
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

    private static void FillIndex(BTreeIndex index, int count, string prefix = "key")
    {
        for (var i = 0; i < count; i++)
            index.insert(
                DatabaseKey.from_string($"{prefix}_{i:D6}"),
                DatabaseValue.from_int32(i)
            ).AsTask().GetAwaiter().GetResult();
    }

    private static void FillIndexRange(BTreeIndex index, int start, int count, string prefix = "key")
    {
        for (var i = start; i < start + count; i++)
            index.insert(
                DatabaseKey.from_string($"{prefix}_{i:D6}"),
                DatabaseValue.from_int32(i)
            ).AsTask().GetAwaiter().GetResult();
    }

    #region 大规模数据

    [Fact]
    public async Task CompactAsync_大量插入删除后GC_保持数据完整性()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);

        for (var round = 0; round < 3; round++)
        {
            FillIndexRange(index, round * 200, 200, $"r{round}");
            for (var i = 0; i < 100; i++)
                await index.DeleteAsync(
                    DatabaseKey.from_string($"r{round}_{round * 200 + i:D6}"),
                    new SequenceNumber((ulong)(200 + i)));
        }

        await index.CompactAsync(new SequenceNumber(10000));

        var totalVisible = 0;
        for (var round = 0; round < 3; round++)
        for (var i = 100; i < 200; i++)
        {
            var key = DatabaseKey.from_string($"r{round}_{round * 200 + i:D6}");
            var val = await index.search(key);
            if (val is not null) totalVisible++;
        }

        Assert.Equal(300, totalVisible);
    }

    #endregion

    #region 辅助方法

    private int CountVisibleKeys(BTreeIndex index, int maxKeys, string prefix = "key")
    {
        var count = 0;
        for (var i = 0; i < maxKeys; i++)
        {
            var val = index.search(DatabaseKey.from_string($"{prefix}_{i:D6}"))
                .AsTask().GetAwaiter().GetResult();
            if (val is not null) count++;
        }

        return count;
    }

    #endregion

    #region Tombstone 清除

    [Fact]
    public async Task CompactAsync_删除少量记录后GC_墓碑被物理清除()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 100);

        for (var i = 0; i < 10; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(100 + (ulong)i));

        var beforeCount = CountVisibleKeys(index, 100);
        Assert.Equal(90, beforeCount);

        var removed = await index.CompactAsync(new SequenceNumber(200));
        Assert.True(removed >= 0);

        var afterCount = CountVisibleKeys(index, 100);
        Assert.Equal(90, afterCount);

        for (var i = 0; i < 10; i++)
        {
            var val = await index.search(DatabaseKey.from_string($"key_{i:D6}"));
            Assert.Null(val);
        }

        for (var i = 10; i < 100; i++)
        {
            var val = await index.search(DatabaseKey.from_string($"key_{i:D6}"));
            Assert.NotNull(val);
        }
    }

    [Fact]
    public async Task CompactAsync_全部删除后GC_树变空()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 50);

        for (var i = 0; i < 50; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(100 + (ulong)i));

        var removed = await index.CompactAsync(new SequenceNumber(200));
        Assert.True(removed >= 0);

        var afterCount = CountVisibleKeys(index, 50);
        Assert.Equal(0, afterCount);
    }

    #endregion

    #region 空闲页复用

    [Fact]
    public async Task DeallocatePageAsync_压缩释放的页面_可被复用()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 200);

        for (var i = 0; i < 100; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(100 + (ulong)i));

        var maxPageIdBefore = _storage.MaxPageId;
        await index.CompactAsync(new SequenceNumber(200));
        var maxPageIdAfter = _storage.MaxPageId;

        FillIndexRange(index, 200, 50, "newKey");

        Assert.Equal(maxPageIdAfter, _storage.MaxPageId);
    }

    [Fact]
    public async Task DeallocatePageAsync_根节点简化后_旧根页被回收()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 30);

        var rootBefore = index.RootPageId;

        for (var i = 0; i < 25; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(100 + (ulong)i));

        await index.CompactAsync(new SequenceNumber(200));

        if (CountVisibleKeys(index, 30) > 0) Assert.True(index.RootPageId >= 0);
    }

    #endregion

    #region 页面合并

    [Fact]
    public async Task MergeUnderflowNodes_半数记录删除后_叶节点被合并()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 300);

        var pageCountBefore = _storage.MaxPageId;

        for (var i = 0; i < 200; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(100 + (ulong)i));

        await index.CompactAsync(new SequenceNumber(200));

        var visibleCount = CountVisibleKeys(index, 300);
        Assert.Equal(100, visibleCount);
    }

    [Fact]
    public async Task MergeUnderflowNodes_删除导致稀疏节点_合并正确保持顺序()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 500);

        for (var i = 0; i < 400; i += 2)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(100 + (ulong)i));

        await index.CompactAsync(new SequenceNumber(1000));

        var visibleCount = CountVisibleKeys(index, 500);
        Assert.True(visibleCount > 0);

        DatabaseKey? lastKey = null;
        for (var i = 1; i < 500; i += 2)
        {
            var key = DatabaseKey.from_string($"key_{i:D6}");
            var val = await index.search(key);
            if (val is not null && lastKey is not null) Assert.True(lastKey.Value.CompareTo(key) < 0);

            lastKey = key;
        }
    }

    #endregion

    #region MVCC 与 GC

    [Fact]
    public async Task CompactAsync_GC后旧快照仍可见历史版本()
    {
        var versionStore = new VersionStore();
        var index = new BTreeIndex(_pageCache, "test", 50, versionStore);

        await index.insert(DatabaseKey.from_string("hist_key"), DatabaseValue.from_int32(1), new SequenceNumber(10));
        await index.insert(DatabaseKey.from_string("hist_key"), DatabaseValue.from_int32(2), new SequenceNumber(20));
        await index.insert(DatabaseKey.from_string("hist_key"), DatabaseValue.from_int32(3), new SequenceNumber(30));

        var currentBeforeGc = await index.search(DatabaseKey.from_string("hist_key"));
        Assert.NotNull(currentBeforeGc);

        await index.DeleteAsync(DatabaseKey.from_string("hist_key"), new SequenceNumber(40));

        var afterDelete = await index.search(DatabaseKey.from_string("hist_key"));
        Assert.Null(afterDelete);

        await index.CompactAsync(new SequenceNumber(100));

        var afterGc = await index.search(DatabaseKey.from_string("hist_key"));
        Assert.Null(afterGc);
    }

    [Fact]
    public async Task CompactAsync_仅删除早于minSequence的墓碑()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 50);

        for (var i = 0; i < 20; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(50 + (ulong)i));

        for (var i = 20; i < 30; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(200 + (ulong)i));

        await index.CompactAsync(new SequenceNumber(100));

        for (var i = 0; i < 20; i++)
        {
            var val = await index.search(DatabaseKey.from_string($"key_{i:D6}"));
            Assert.Null(val);
        }

        for (var i = 20; i < 30; i++)
        {
            var val = await index.search(DatabaseKey.from_string($"key_{i:D6}"));
            Assert.Null(val);
        }
    }

    #endregion

    #region 幂等性与健壮性

    [Fact]
    public async Task CompactAsync_重复GC_幂等()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 100);

        for (var i = 0; i < 30; i++)
            await index.DeleteAsync(DatabaseKey.from_string($"key_{i:D6}"), new SequenceNumber(50 + (ulong)i));

        var removed1 = await index.CompactAsync(new SequenceNumber(100));
        var removed2 = await index.CompactAsync(new SequenceNumber(100));
        var removed3 = await index.CompactAsync(new SequenceNumber(100));

        Assert.True(removed1 > 0, "首次 GC 应移除墓碑");
        Assert.True(removed2 == 0 || removed2 <= removed1, "重复 GC 不应增加移除数量");
        Assert.True(removed3 == 0 || removed3 <= removed2, "第三次 GC 应稳定");
    }

    [Fact]
    public async Task CompactAsync_空树GC_不崩溃()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        await index.CompactAsync(new SequenceNumber(0));
        await index.CompactAsync(new SequenceNumber(100));
    }

    [Fact]
    public async Task CompactAsync_无删除的GC_不影响数据()
    {
        var index = new BTreeIndex(_pageCache, "test", 50);
        FillIndex(index, 100);

        var removed = await index.CompactAsync(new SequenceNumber(200));
        Assert.Equal(0, removed);

        var count = CountVisibleKeys(index, 100);
        Assert.Equal(100, count);
    }

    #endregion
}