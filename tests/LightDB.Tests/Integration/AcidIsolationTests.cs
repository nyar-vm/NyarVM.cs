using Core.Database;
using Std.Database.Core;

namespace LightDB.Tests.Integration;

/// <summary>
///     ACID 隔离级别与并发事务正确性测试套件
///     覆盖 ReadUncommitted / ReadCommitted / Snapshot / Serializable
///     四种隔离级别，含并发读写、写写冲突、回滚可见性等场景
/// </summary>
public sealed class AcidIsolationTests : IDisposable
{
    private readonly LightDatabase _db;

    public AcidIsolationTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_acid_{Guid.NewGuid():N}");
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

    #region ReadUncommitted 隔离级别

    [Fact]
    public async Task ReadUncommitted_ReadsLatestCommitted()
    {
        await _db.put(DatabaseKey.from_string("k1"), "before_tx");

        var tx = await _db.begin_transaction(IsolationLevel.ReadUncommitted);
        var result = await tx.get<string>("k1");

        Assert.Equal("before_tx", result);
        await tx.rollback();
    }

    [Fact]
    public async Task ReadUncommitted_NoDirtyReadFromUncommittedTx()
    {
        await _db.put(DatabaseKey.from_string("k1"), "committed");

        var writer = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await writer.put("k1", "uncommitted_dirty");

        var reader = await _db.begin_transaction(IsolationLevel.ReadUncommitted);
        var result = await reader.get<string>("k1");

        Assert.Equal("committed", result);
        await reader.rollback();
        await writer.rollback();
    }

    #endregion

    #region ReadCommitted 隔离级别

    [Fact]
    public async Task ReadCommitted_SeesCommittedData()
    {
        await _db.put(DatabaseKey.from_string("k1"), "initial");

        var tx = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await tx.put("k1", "updated");
        await tx.commit();

        var reader = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        var result = await reader.get<string>("k1");
        Assert.Equal("updated", result);
        await reader.rollback();
    }

    [Fact]
    public async Task ReadCommitted_PreventsDirtyRead()
    {
        await _db.put(DatabaseKey.from_string("k1"), "committed");

        var writer = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await writer.put("k1", "dirty_value");

        var reader = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        var result = await reader.get<string>("k1");

        Assert.Equal("committed", result);
        await reader.rollback();
        await writer.rollback();
    }

    [Fact]
    public async Task ReadCommitted_SeesNewlyCommittedAfterRead()
    {
        var key = DatabaseKey.from_string("k1");
        await _db.put(key, "initial");

        var reader = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        var firstRead = await reader.get<string>("k1");
        Assert.Equal("initial", firstRead);

        var writer = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await writer.put("k1", "newly_committed");
        await writer.commit();

        var secondRead = await reader.get<string>("k1");
        Assert.Equal("newly_committed", secondRead);
        await reader.rollback();
    }

    #endregion

    #region Snapshot 隔离级别

    [Fact]
    public async Task Snapshot_PreventsNonRepeatableRead()
    {
        var key = DatabaseKey.from_string("k1");
        await _db.put(key, "initial");

        var reader = await _db.begin_transaction(IsolationLevel.Snapshot);
        var firstRead = await reader.get<string>("k1");
        Assert.Equal("initial", firstRead);

        var writer = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await writer.put("k1", "changed");
        await writer.commit();

        var secondRead = await reader.get<string>("k1");
        Assert.Equal("initial", secondRead);
        await reader.rollback();
    }

    [Fact]
    public async Task Snapshot_ProvidesConsistentView()
    {
        var k1 = DatabaseKey.from_string("k1");
        var k2 = DatabaseKey.from_string("k2");
        await _db.put(k1, "v1");
        await _db.put(k2, "v2");

        var reader = await _db.begin_transaction(IsolationLevel.Snapshot);
        var a = await reader.get<string>("k1");
        var b = await reader.get<string>("k2");

        var writer = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await writer.put("k1", "v1_modified");
        await writer.put("k2", "v2_modified");
        await writer.commit();

        var aAgain = await reader.get<string>("k1");
        var bAgain = await reader.get<string>("k2");

        Assert.Equal("v1", a);
        Assert.Equal("v2", b);
        Assert.Equal("v1", aAgain);
        Assert.Equal("v2", bAgain);
        await reader.rollback();
    }

    [Fact]
    public async Task Snapshot_HidesCommittedDeletes()
    {
        var key = DatabaseKey.from_string("k1");
        await _db.put(key, "value");

        var reader = await _db.begin_transaction(IsolationLevel.Snapshot);
        var beforeDelete = await reader.get<string>("k1");
        Assert.Equal("value", beforeDelete);

        var deleter = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await deleter.delete("k1");
        await deleter.commit();

        var afterDelete = await reader.get<string>("k1");
        Assert.Equal("value", afterDelete);
        await reader.rollback();
    }

    [Fact]
    public async Task Snapshot_IsDefaultTransactionIsolation()
    {
        var tx = await _db.begin_transaction();
        Assert.Equal(IsolationLevel.Snapshot, tx.isolation_level);
        await tx.rollback();
    }

    #endregion

    #region Serializable 隔离级别

    [Fact]
    public async Task Serializable_PreventsPhantomRead()
    {
        var k1 = DatabaseKey.from_string("k1");
        var k2 = DatabaseKey.from_string("k2");
        await _db.put(k1, "v1");

        var reader = await _db.begin_transaction(IsolationLevel.Serializable);
        var initialRead = await reader.get<string>("k1");
        var initialMissing = await reader.get<string>("k2");
        Assert.Equal("v1", initialRead);
        Assert.Null(initialMissing);

        var inserter = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await inserter.put("k2", "v2_phantom");
        await inserter.commit();

        var laterRead = await reader.get<string>("k2");
        Assert.Null(laterRead);
        await reader.rollback();
    }

    [Fact]
    public async Task Serializable_DetectsWriteConflict()
    {
        var key = DatabaseKey.from_string("shared_key");
        await _db.put(key, "initial");

        var tx1 = await _db.begin_transaction(IsolationLevel.Serializable);
        await tx1.put("shared_key", "tx1_value");

        var tx2 = await _db.begin_transaction(IsolationLevel.Serializable);
        await tx2.put("shared_key", "tx2_value");

        await tx1.commit();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tx2.commit().AsTask());
    }

    #endregion

    #region 回滚与可见性

    [Fact]
    public async Task Rollback_MakesWritesInvisible()
    {
        var key = DatabaseKey.from_string("k1");
        await _db.put(key, "original");

        var tx = await _db.begin_transaction();
        await tx.put("k1", "rolled_back_value");
        await tx.rollback();

        var result = await _db.get<string>(key);
        Assert.Equal("original", result);
    }

    [Fact]
    public async Task Rollback_DeletesNotApplied()
    {
        var key = DatabaseKey.from_string("k1");
        await _db.put(key, "original");

        var tx = await _db.begin_transaction();
        await tx.delete("k1");
        await tx.rollback();

        var result = await _db.get<string>(key);
        Assert.Equal("original", result);
    }

    [Fact]
    public async Task Commit_AfterPartialWrites_PreservesState()
    {
        var k1 = DatabaseKey.from_string("k1");
        var k2 = DatabaseKey.from_string("k2");
        await _db.put(k1, "v1");
        await _db.put(k2, "v2");

        var tx = await _db.begin_transaction();
        await tx.put("k1", "v1_new");
        await tx.commit();

        var result1 = await _db.get<string>(k1);
        var result2 = await _db.get<string>(k2);

        Assert.Equal("v1_new", result1);
        Assert.Equal("v2", result2);
    }

    [Fact]
    public async Task ReadOnlyTransaction_NoWriteDoesNotAffectStorage()
    {
        var key = DatabaseKey.from_string("k1");
        await _db.put(key, "value");

        var tx = await _db.begin_transaction();
        var result = await tx.get<string>("k1");
        Assert.Equal("value", result);
        await tx.commit();

        var verify = await _db.get<string>(key);
        Assert.Equal("value", verify);
    }

    [Fact]
    public async Task MultipleUpdates_InSameTransaction_KeepLastValue()
    {
        var key = DatabaseKey.from_string("k1");

        var tx = await _db.begin_transaction();
        await tx.put("k1", "first");
        await tx.put("k1", "second");
        await tx.put("k1", "final");
        await tx.commit();

        var result = await _db.get<string>(key);
        Assert.Equal("final", result);
    }

    [Fact]
    public async Task DeleteThenReinsert_InSameTransaction()
    {
        var key = DatabaseKey.from_string("k1");
        await _db.put(key, "original");

        var tx = await _db.begin_transaction();
        await tx.delete("k1");

        var afterDelete = await tx.get<string>("k1");
        Assert.Null(afterDelete);

        await tx.put("k1", "reinserted");

        var afterReinsert = await tx.get<string>("k1");
        Assert.Equal("reinserted", afterReinsert);

        await tx.commit();

        var final = await _db.get<string>(key);
        Assert.Equal("reinserted", final);
    }

    #endregion

    #region 并发读写

    [Fact]
    public async Task ConcurrentReaders_OnSameKey_Consistent()
    {
        var key = DatabaseKey.from_string("concurrent_key");
        await _db.put(key, "shared_value");

        var tasks = new List<Task<string?>>();
        for (var i = 0; i < 20; i++) tasks.Add(ReadWithSnapshotAsync(key));

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal("shared_value", r));
    }

    [Fact]
    public async Task ConcurrentWriters_DifferentKeys_NoConflict()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 10; i++)
        {
            var idx = i;
            tasks.Add(WriteAndCommitAsync($"key_{idx}", $"value_{idx}"));
        }

        await Task.WhenAll(tasks);

        for (var i = 0; i < 10; i++)
        {
            var result = await _db.get<string>(DatabaseKey.from_string($"key_{i}"));
            Assert.Equal($"value_{i}", result);
        }
    }

    [Fact]
    public async Task WriterSeesOwnWrites_EvenBeforeCommit()
    {
        var tx = await _db.begin_transaction();
        await tx.put("self_key", "self_value");

        var selfRead = await tx.get<string>("self_key");
        Assert.Equal("self_value", selfRead);

        await tx.put("self_key", "self_modified");
        var selfRead2 = await tx.get<string>("self_key");
        Assert.Equal("self_modified", selfRead2);

        await tx.commit();
    }

    [Fact]
    public async Task SnapshotReader_DoesNotBlockWriter()
    {
        var key = DatabaseKey.from_string("block_key");
        await _db.put(key, "initial");

        var reader = await _db.begin_transaction(IsolationLevel.Snapshot);
        await reader.get<string>("block_key");

        var writer = await _db.begin_transaction(IsolationLevel.ReadCommitted);
        await writer.put("block_key", "updated");
        await writer.commit();

        var readerResult = await reader.get<string>("block_key");
        Assert.Equal("initial", readerResult);
        await reader.rollback();
    }

    [Fact]
    public async Task CommittedData_VisibleToNextTransaction()
    {
        var key = DatabaseKey.from_string("visibility_key");

        var tx1 = await _db.begin_transaction();
        await tx1.put("visibility_key", "committed_data");
        await tx1.commit();

        var tx2 = await _db.begin_transaction();
        var result = await tx2.Get<string>("visibility_key");
        Assert.Equal("committed_data", result);
        await tx2.commit();
    }

    #endregion

    #region 事务生命周期

    [Fact]
    public async Task Transaction_DisposeWithoutAction_DoesNotThrow()
    {
        var tx = await _db.begin_transaction();
        await tx.DisposeAsync();
        Assert.True(tx.is_rolled_back);
    }

    [Fact]
    public async Task DoubleCommit_Throws()
    {
        var tx = await _db.begin_transaction();
        await tx.commit();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tx.commit().AsTask());
    }

    [Fact]
    public async Task DoubleRollback_IsIdempotent()
    {
        var tx = await _db.begin_transaction();
        await tx.rollback();
        await tx.rollback();
        Assert.True(tx.is_rolled_back);
    }

    [Fact]
    public async Task Transaction_AfterDispose_Throws()
    {
        var tx = await _db.begin_transaction();
        await tx.DisposeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tx.get<string>("any_key").AsTask());
    }

    #endregion

    #region 辅助方法

    private async Task<string?> ReadWithSnapshotAsync(DatabaseKey key)
    {
        var tx = await _db.begin_transaction(IsolationLevel.Snapshot);
        var result = await tx.get<string>(key);
        await tx.rollback();
        return result;
    }

    private async Task WriteAndCommitAsync(string key, string value)
    {
        var tx = await _db.begin_transaction();
        await tx.put(key, value);
        await tx.commit();
    }

    #endregion
}