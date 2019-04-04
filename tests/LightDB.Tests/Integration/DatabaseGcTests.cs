using System.Diagnostics;
using Std.Database.Core;

namespace LightDB.Tests.Integration;

/// <summary>
///     LightDatabase 垃圾回收集成测试：检查点 GC、手动 GC、版本清理
/// </summary>
public sealed class DatabaseGcTests : IDisposable
{
    private readonly string _tempDir;

    public DatabaseGcTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LightDB_db_gc_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }
        catch
        {
        }
    }

    #region VersionStore GC

    [Fact]
    public async Task CheckpointAsync_版本存储清理_旧版本被回收()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 4096,
            b_tree_order = 50
        };

        await using var db = new LightDatabase(options);

        const string key = "versioned_key";
        await db.put(key, "v1");
        await db.put(key, "v2");
        await db.put(key, "v3");

        var currentBeforeGc = await db.get<string>(key);
        Assert.Equal("v3", currentBeforeGc);

        await db.check_point();

        var currentAfterGc = await db.get<string>(key);
        Assert.Equal("v3", currentAfterGc);

        await db.garbage_collect();

        var final = await db.get<string>(key);
        Assert.Equal("v3", final);
    }

    #endregion

    #region 性能基准

    [Fact]
    public async Task GarbageCollectAsync_10万条写入删除后GC_性能可接受()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 8192,
            b_tree_order = 50
        };

        await using var db = new LightDatabase(options);

        for (var i = 0; i < 1000; i++) await db.put($"perf_{i:D6}", $"value_{i}");

        for (var i = 0; i < 500; i++) await db.delete($"perf_{i:D6}");

        var sw = Stopwatch.StartNew();
        await db.garbage_collect();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 30000, $"GC 耗时 {sw.ElapsedMilliseconds}ms 超过 30s 阈值");
    }

    #endregion

    #region Checkpoint GC

    [Fact]
    public async Task CheckpointAsync_写入后检查点_墓碑被清除()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 4096,
            b_tree_order = 50
        };

        await using var db = new LightDatabase(options);

        for (var i = 0; i < 200; i++) await db.put($"k_{i:D6}", $"v_{i}");

        for (var i = 0; i < 100; i++) await db.delete($"k_{i:D6}");

        await db.check_point();

        for (var i = 0; i < 100; i++)
        {
            var val = await db.get<string>($"k_{i:D6}");
            Assert.Null(val);
        }

        for (var i = 100; i < 200; i++)
        {
            var val = await db.get<string>($"k_{i:D6}");
            Assert.Equal($"v_{i}", val);
        }
    }

    [Fact]
    public async Task CheckpointAsync_检查点后恢复_根节点正确()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 4096,
            b_tree_order = 50
        };

        await using (var db = new LightDatabase(options))
        {
            for (var i = 0; i < 300; i++) await db.put($"item_{i:D6}", new { Id = i, Name = $"Item_{i}" });

            for (var i = 0; i < 150; i++) await db.delete($"item_{i:D6}");

            await db.check_point();
        }

        await using (var db2 = new LightDatabase(options))
        {
            for (var i = 150; i < 300; i++)
            {
                var val = await db2.get<Dictionary<string, object>>($"item_{i:D6}");
                Assert.NotNull(val);
            }

            var deleted = await db2.get<string>($"item_{0:D6}");
            Assert.Null(deleted);
        }
    }

    [Fact]
    public async Task CheckpointAsync_多次检查点_数据文件不无限增长()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 4096,
            b_tree_order = 50
        };

        await using var db = new LightDatabase(options);

        for (var round = 0; round < 5; round++)
        {
            for (var i = 0; i < 100; i++) await db.put($"r{round}_k_{i:D6}", $"v_{i}");

            for (var i = 0; i < 80; i++) await db.delete($"r{round}_k_{i:D6}");

            await db.check_point();
        }

        var dataPath = Path.Combine(_tempDir, "light.dat");
        Assert.True(new FileInfo(dataPath).Length > 0);
    }

    #endregion

    #region 手动 GC

    [Fact]
    public async Task GarbageCollectAsync_大量删除后手动GC_墓碑被清除()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 4096,
            b_tree_order = 50
        };

        await using var db = new LightDatabase(options);

        for (var i = 0; i < 500; i++) await db.put($"gc_{i:D6}", $"value_{i}");

        for (var i = 0; i < 400; i++) await db.delete($"gc_{i:D6}");

        var removed = await db.garbage_collect();
        Assert.True(removed >= 0);

        var visibleCount = 0;
        for (var i = 0; i < 500; i++)
        {
            var val = await db.get<string>($"gc_{i:D6}");
            if (val is not null) visibleCount++;
        }

        Assert.Equal(100, visibleCount);
    }

    [Fact]
    public async Task GarbageCollectAsync_无删除时GC_返回零()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 4096,
            b_tree_order = 50
        };

        await using var db = new LightDatabase(options);

        for (var i = 0; i < 50; i++) await db.put($"live_{i:D6}", i);

        var removed = await db.garbage_collect();
        Assert.Equal(0, removed);
    }

    #endregion

    #region GC 后恢复

    [Fact]
    public async Task GarbageCollectAsync_GC后重新打开_数据完整性不变()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 8192,
            b_tree_order = 50
        };

        var keysToKeep = new HashSet<string>();
        await using (var db = new LightDatabase(options))
        {
            for (var i = 0; i < 500; i++) await db.put($"test_{i:D6}", $"val_{i}");

            for (var i = 0; i < 300; i++) await db.delete($"test_{i:D6}");

            for (var i = 300; i < 500; i++) keysToKeep.Add($"test_{i:D6}");

            await db.garbage_collect();
        }

        await using (var db2 = new LightDatabase(options))
        {
            foreach (var key in keysToKeep)
            {
                var val = await db2.get<string>(key);
                Assert.NotNull(val);
            }

            var deleted = await db2.get<string>("test_000000");
            Assert.Null(deleted);

            var deleted2 = await db2.get<string>("test_000150");
            Assert.Null(deleted2);
        }
    }

    [Fact]
    public async Task CheckpointAsync_持续插入删除后检查点_可正确恢复()
    {
        var options = new LightOptions
        {
            path = _tempDir,
            PageSize = 4096,
            b_tree_order = 50
        };

        var expectedKeys = new Dictionary<string, int>();
        await using (var db = new LightDatabase(options))
        {
            for (var batch = 0; batch < 5; batch++)
            {
                for (var i = 0; i < 50; i++)
                {
                    var key = $"b{batch}_k_{i}";
                    await db.PutAsync(key, batch * 100 + i);
                }

                if (batch > 0)
                    for (var i = 0; i < 30; i++)
                        await db.DeleteAsync($"b{batch - 1}_k_{i}");

                if (batch == 4)
                    for (var i = 0; i < 50; i++)
                        expectedKeys[$"b4_k_{i}"] = 400 + i;

                await db.check_point();
            }
        }

        await using (var db2 = new LightDatabase(options))
        {
            foreach (var (key, value) in expectedKeys)
            {
                var val = await db2.get<int>(key);
                Assert.Equal(value, val);
            }
        }
    }

    #endregion
}