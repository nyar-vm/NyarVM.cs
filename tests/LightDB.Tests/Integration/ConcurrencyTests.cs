using Core.Database;
using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class ConcurrencyTests : IDisposable
{
    private readonly LightDatabase _db;

    public ConcurrencyTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_concurrency_test_{Guid.NewGuid():N}");
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

    [Fact]
    public async Task MultipleParallelWrites_ShouldSucceed()
    {
        const int taskCount = 10;
        const int writesPerTask = 50;

        var tasks = new List<Task>();
        for (var t = 0; t < taskCount; t++)
        {
            var taskId = t;
            tasks.Add(Task.Run(async () =>
            {
                for (var i = 0; i < writesPerTask; i++) await _db.put($"task{taskId}:key{i}", $"value{taskId}:{i}");
            }));
        }

        await Task.WhenAll(tasks);

        for (var t = 0; t < taskCount; t++)
        for (var i = 0; i < writesPerTask; i++)
        {
            var result = await _db.get<string>($"task{t}:key{i}");
            Assert.Equal($"value{t}:{i}", result);
        }
    }

    [Fact]
    public async Task MultipleTransactions_ShouldBeIsolated()
    {
        await _db.put("shared", "initial");

        var tx1 = await _db.begin_transaction();
        var tx2 = await _db.begin_transaction();

        await tx1.put("shared", "tx1_value");
        await tx2.put("shared", "tx2_value");

        var tx1Result = await tx1.get<string>("shared");
        var tx2Result = await tx2.get<string>("shared");

        Assert.Equal("tx1_value", tx1Result);
        Assert.Equal("tx2_value", tx2Result);

        await tx1.commit();
        await tx2.rollback();

        var finalResult = await _db.get<string>("shared");
        Assert.Equal("tx1_value", finalResult);
    }

    [Fact]
    public async Task ReadWriteConcurrently_ShouldNotCorruptData()
    {
        const int iterations = 100;

        var writeTask = Task.Run(async () =>
        {
            for (var i = 0; i < iterations; i++)
            {
                await _db.put("counter", i);
                await Task.Yield();
            }
        });

        var readTask = Task.Run(async () =>
        {
            for (var i = 0; i < iterations; i++)
            {
                var value = await _db.get<int>("counter");
                Assert.True((value >= 0 && value < iterations) || value == default);
                await Task.Yield();
            }
        });

        await Task.WhenAll(writeTask, readTask);

        var finalValue = await _db.get<int>("counter");
        Assert.Equal(iterations - 1, finalValue);
    }

    [Fact]
    public async Task MultipleSnapshotsConcurrently_ShouldBeConsistent()
    {
        for (var i = 0; i < 20; i++) await _db.put($"key{i}", $"value{i}");

        var snapshots = new List<ISnapshot>();
        for (var i = 0; i < 5; i++) snapshots.Add(_db.create_snapshot());

        for (var i = 0; i < 20; i++) await _db.put($"key{i}", $"updated{i}");

        foreach (var snapshot in snapshots)
            for (var i = 0; i < 20; i++)
            {
                var value = await snapshot.get<string>($"key{i}");
                Assert.Equal($"value{i}", value);
            }

        foreach (var snapshot in snapshots) snapshot.Dispose();
    }
}