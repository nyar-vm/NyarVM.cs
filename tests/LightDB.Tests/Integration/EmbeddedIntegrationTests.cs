using Core.Database;
using Std.Database.Core;
using Std.Database.Wal;

namespace LightDB.Tests.Integration;

public class EmbeddedIntegrationTests : IDisposable
{
    private readonly string _tempDir;

    public EmbeddedIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lightdb-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    private LightOptions CreateOptions(WalFlushPolicy flushPolicy = WalFlushPolicy.batch)
    {
        return new LightOptions
        {
            path = _tempDir,
            wal_flush_policy = flushPolicy
        };
    }

    private class TestUser
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }

    private class TestProduct
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }

    #region 单文件数据库

    [Fact]
    public async Task SingleFileDatabase_ShouldCreateDataFile()
    {
        await using var db = new LightDatabase(CreateOptions());
        Assert.True(File.Exists(Path.Combine(_tempDir, "light.dat")));
    }

    [Fact]
    public async Task SingleFileDatabase_ShouldCreateWalFile()
    {
        await using var db = new LightDatabase(CreateOptions());
        await db.put("key1", "value1");
        Assert.True(File.Exists(Path.Combine(_tempDir, "light.wal")));
    }

    [Fact]
    public async Task SingleFileDatabase_ShouldPersistAndReopen()
    {
        var options = CreateOptions(WalFlushPolicy.every_write);

        await using (var db = new LightDatabase(options))
        {
            await db.put("persist-key", "persist-value");
        }

        await using (var db = new LightDatabase(options))
        {
            var value = await db.get<string>("persist-key");
            Assert.Equal("persist-value", value);
        }
    }

    [Fact]
    public async Task SingleFileDatabase_ShouldHandleMultipleCollections()
    {
        await using var db = new LightDatabase(CreateOptions());

        var users = db.get_collection<TestUser>("users");
        var products = db.get_collection<TestProduct>("products");

        var userKey = await users.InsertAsync(new TestUser { Id = 1, Name = "Alice" });
        var productKey = await products.InsertAsync(new TestProduct { Id = 101, Name = "Widget" });

        var user = await users.FindAsync(userKey);
        var product = await products.FindAsync(productKey);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
        Assert.NotNull(product);
        Assert.Equal("Widget", product.Name);
    }

    #endregion

    #region 跨平台文件锁

    [Fact]
    public async Task FileLock_ShouldPreventConcurrentWriteAccess()
    {
        var options = CreateOptions();
        await using var db1 = new LightDatabase(options);

        var ex = await Assert.ThrowsAsync<IOException>(async () =>
        {
            var db2 = new LightDatabase(options);
            await db2.DisposeAsync();
        });

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task FileLock_ShouldReleaseOnDispose()
    {
        var options = CreateOptions();

        await using (var db1 = new LightDatabase(options))
        {
            await db1.put("key1", "value1");
        }

        await using (var db2 = new LightDatabase(options))
        {
            var value = await db2.get<string>("key1");
            Assert.Equal("value1", value);
        }
    }

    #endregion

    #region 崩溃恢复

    [Fact]
    public async Task CrashRecovery_ShouldRecoverFromWal()
    {
        var options = CreateOptions(WalFlushPolicy.every_write);

        await using (var db = new LightDatabase(options))
        {
            await db.put("crash-key1", "crash-value1");
            await db.put("crash-key2", "crash-value2");
        }

        await using (var db = new LightDatabase(options))
        {
            var value1 = await db.get<string>("crash-key1");
            var value2 = await db.get<string>("crash-key2");

            Assert.Equal("crash-value1", value1);
            Assert.Equal("crash-value2", value2);
        }
    }

    [Fact]
    public async Task CrashRecovery_ShouldRecoverCommittedTransaction()
    {
        var options = CreateOptions(WalFlushPolicy.every_write);

        await using (var db = new LightDatabase(options))
        {
            await using var tx = await db.begin_transaction(IsolationLevel.Serializable);
            await tx.put("tx-key1", "tx-value1");
            await tx.put("tx-key2", "tx-value2");
            await tx.commit();
        }

        await using (var db = new LightDatabase(options))
        {
            var value1 = await db.get<string>("tx-key1");
            var value2 = await db.get<string>("tx-key2");

            Assert.Equal("tx-value1", value1);
            Assert.Equal("tx-value2", value2);
        }
    }

    [Fact]
    public async Task CrashRecovery_ShouldNotRecoverUncommittedTransaction()
    {
        var options = CreateOptions(WalFlushPolicy.every_write);

        await using (var db = new LightDatabase(options))
        {
            await db.put("before-key", "before-value");

            await using var tx = await db.begin_transaction(IsolationLevel.Serializable);
            await tx.put("uncommitted-key", "uncommitted-value");
        }

        await using (var db = new LightDatabase(options))
        {
            var beforeValue = await db.get<string>("before-key");
            Assert.Equal("before-value", beforeValue);

            var uncommittedValue = await db.get<string>("uncommitted-key");
            Assert.Null(uncommittedValue);
        }
    }

    [Fact]
    public async Task CrashRecovery_ShouldHandleBatchWalRecovery()
    {
        var options = CreateOptions();

        await using (var db = new LightDatabase(options))
        {
            for (var i = 0; i < 100; i++) await db.put($"batch-key-{i}", $"batch-value-{i}");

            await db.check_point();
        }

        await using (var db = new LightDatabase(options))
        {
            for (var i = 0; i < 100; i++)
            {
                var value = await db.get<string>($"batch-key-{i}");
                Assert.Equal($"batch-value-{i}", value);
            }
        }
    }

    #endregion

    #region 并发访问

    [Fact]
    public async Task ConcurrentAccess_ShouldHandleConcurrentReads()
    {
        await using var db = new LightDatabase(CreateOptions());

        for (var i = 0; i < 50; i++) await db.put($"concurrent-key-{i}", $"concurrent-value-{i}");

        var tasks = new List<Task<string?>>();
        for (var i = 0; i < 50; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () => await db.get<string>($"concurrent-key-{index}")));
        }

        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < 50; i++) Assert.Equal($"concurrent-value-{i}", results[i]);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldHandleConcurrentWrites()
    {
        await using var db = new LightDatabase(CreateOptions());

        var tasks = new List<Task>();
        for (var i = 0; i < 50; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () => await db.put($"write-key-{index}", $"write-value-{index}")));
        }

        await Task.WhenAll(tasks);

        for (var i = 0; i < 50; i++)
        {
            var value = await db.get<string>($"write-key-{i}");
            Assert.Equal($"write-value-{i}", value);
        }
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldHandleMixedReadWrite()
    {
        await using var db = new LightDatabase(CreateOptions());

        for (var i = 0; i < 20; i++) await db.put($"mixed-key-{i}", $"initial-value-{i}");

        var tasks = new List<Task>();
        for (var i = 0; i < 20; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await db.put($"mixed-key-{index}", $"updated-value-{index}");
                var value = await db.get<string>($"mixed-key-{index}");
                Assert.NotNull(value);
            }));
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConcurrentAccess_SnapshotIsolation_ShouldSeeConsistentView()
    {
        await using var db = new LightDatabase(CreateOptions());

        for (var i = 0; i < 10; i++) await db.put($"snapshot-key-{i}", $"snapshot-value-{i}");

        using var snapshot = db.create_snapshot();

        var updateTasks = new List<Task>();
        for (var i = 0; i < 10; i++)
        {
            var index = i;
            updateTasks.Add(Task.Run(async () =>
                await db.put($"snapshot-key-{index}", $"modified-value-{index}")));
        }

        await Task.WhenAll(updateTasks);

        for (var i = 0; i < 10; i++)
        {
            var snapshotValue = await snapshot.get<string>($"snapshot-key-{i}");
            Assert.Equal($"snapshot-value-{i}", snapshotValue);

            var currentValue = await db.get<string>($"snapshot-key-{i}");
            Assert.Equal($"modified-value-{i}", currentValue);
        }
    }

    [Fact]
    public async Task ConcurrentAccess_HighThroughput_StressTest()
    {
        var options = CreateOptions();
        await using var db = new LightDatabase(options);

        const int operationCount = 500;
        var tasks = new List<Task>();

        for (var i = 0; i < operationCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () => { await db.put($"stress-key-{index}", $"stress-value-{index}"); }));
        }

        await Task.WhenAll(tasks);

        var readTasks = new List<Task<bool>>();
        for (var i = 0; i < operationCount; i++)
        {
            var index = i;
            readTasks.Add(Task.Run(async () =>
            {
                var value = await db.get<string>($"stress-key-{index}");
                return value == $"stress-value-{index}";
            }));
        }

        var results = await Task.WhenAll(readTasks);
        Assert.All(results, Assert.True);
    }

    #endregion
}