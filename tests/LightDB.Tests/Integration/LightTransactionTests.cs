using Core.Database;
using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class LightTransactionTests : IDisposable
{
    private readonly LightDatabase _db;

    public LightTransactionTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_test_{Guid.NewGuid():N}");
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
    public async Task BeginTransactionAsync_ShouldReturnTransaction()
    {
        var tx = await _db.begin_transaction();

        Assert.NotNull(tx);
        Assert.False(tx.is_committed);
        Assert.False(tx.is_rolled_back);
        Assert.Equal(IsolationLevel.Snapshot, tx.isolation_level);

        await tx.rollback();
    }

    [Fact]
    public async Task BeginTransactionAsync_WithCustomIsolationLevel_ShouldRespectLevel()
    {
        var tx = await _db.begin_transaction(IsolationLevel.ReadCommitted);

        Assert.Equal(IsolationLevel.ReadCommitted, tx.isolation_level);

        await tx.rollback();
    }

    [Fact]
    public async Task PutAndGet_InTransaction_ShouldWork()
    {
        var tx = await _db.begin_transaction();

        await tx.put("key1", "value1");
        var result = await tx.get<string>("key1");

        Assert.Equal("value1", result);

        await tx.commit();
    }

    [Fact]
    public async Task CommitAsync_ShouldMarkAsCommitted()
    {
        var tx = await _db.begin_transaction();

        await tx.put("key1", "value1");
        await tx.commit();

        Assert.True(tx.is_committed);
        Assert.False(tx.is_rolled_back);
    }

    [Fact]
    public async Task RollbackAsync_ShouldMarkAsRolledBack()
    {
        var tx = await _db.begin_transaction();

        await tx.put("key1", "value1");
        await tx.rollback();

        Assert.True(tx.is_rolled_back);
        Assert.False(tx.is_committed);
    }

    [Fact]
    public async Task Get_AfterCommit_ShouldBeVisibleInDatabase()
    {
        var tx = await _db.begin_transaction();

        await tx.put("key1", "committed_value");
        await tx.commit();

        var result = await _db.get<string>(DatabaseKey.from_string("key1"));

        Assert.Equal("committed_value", result);
    }

    [Fact]
    public async Task Delete_InTransaction_ShouldHideKey()
    {
        var key = DatabaseKey.from_string("key1");
        await _db.put(key, "original");

        var tx = await _db.begin_transaction();
        await tx.delete("key1");

        var result = await tx.get<string>("key1");
        Assert.Null(result);

        await tx.commit();
    }

    [Fact]
    public async Task Operations_AfterCommit_ShouldThrow()
    {
        var tx = await _db.begin_transaction();
        await tx.put("key1", "value1");
        await tx.commit();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tx.put("key2", "value2").AsTask());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tx.get<string>("key1").AsTask());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tx.delete("key1").AsTask());
    }

    [Fact]
    public async Task Operations_AfterRollback_ShouldThrow()
    {
        var tx = await _db.begin_transaction();
        await tx.rollback();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tx.put("key2", "value2").AsTask());
    }

    [Fact]
    public async Task IsReadOnly_WithNoWrites_ShouldReturnTrue()
    {
        var tx = await _db.begin_transaction();

        Assert.True(tx.is_read_only);

        await tx.rollback();
    }

    [Fact]
    public async Task IsReadOnly_WithWrites_ShouldReturnFalse()
    {
        var tx = await _db.begin_transaction();
        await tx.put("key1", "value1");

        Assert.False(tx.is_read_only);

        await tx.rollback();
    }

    [Fact]
    public async Task Dispose_WithoutCommit_ShouldRollback()
    {
        var tx = await _db.begin_transaction();
        await tx.put("key1", "value1");

        await tx.DisposeAsync();

        Assert.True(tx.is_rolled_back);
    }

    [Fact]
    public async Task StartTime_ShouldBeRecent()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var tx = await _db.begin_transaction();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.True(tx.start_time >= before);
        Assert.True(tx.start_time <= after);

        await tx.rollback();
    }
}