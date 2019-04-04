using Core.Database;
using Std.Database.Core;
using Std.Database.Wal;

namespace Std.Database.Transaction;

/// <summary>
///     事务管理器，负责 MVCC 和并发控制
/// </summary>
internal sealed class TransactionManager
{
    #region 构造函数

    /// <summary>
    ///     创建事务管理器
    /// </summary>
    /// <param name="walWriter">WAL 写入器</param>
    public TransactionManager(IWalWriter walWriter)
    {
        _wal_writer = walWriter;
        _active_transactions = new Dictionary<TransactionId, LightTransaction>();
        _current_sequence_value = 0;
        _key_lock_table = new Dictionary<DatabaseKey, TransactionId>();
        _key_write_time = new Dictionary<DatabaseKey, long>();
    }

    #endregion

    #region 序列号

    /// <summary>
    ///     分配序列号（线程安全，使用原子操作）
    /// </summary>
    /// <returns>新序列号</returns>
    public SequenceNumber allocate_sequence()
    {
        var newValue = Interlocked.Increment(ref _current_sequence_value);
        return new SequenceNumber((ulong)newValue);
    }

    #endregion

    #region 字段

    private readonly IWalWriter _wal_writer;
    private readonly Dictionary<TransactionId, LightTransaction> _active_transactions;
    private readonly ReaderWriterLockSlim _lock = new();
    private long _current_sequence_value;
    private readonly Dictionary<DatabaseKey, TransactionId> _key_lock_table;
    private readonly Dictionary<DatabaseKey, long> _key_write_time;

    #endregion

    #region 属性

    /// <summary>
    ///     当前序列号
    /// </summary>
    public SequenceNumber current_sequence => new((ulong)Interlocked.Read(ref _current_sequence_value));

    /// <summary>
    ///     活跃事务数
    /// </summary>
    public int active_transaction_count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _active_transactions.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    #endregion

    #region 事务生命周期

    /// <summary>
    ///     异步开启事务
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    /// <param name="writeCoordinator">写入协调器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事务实例</returns>
    public async ValueTask<LightTransaction> begin_transaction(IsolationLevel isolationLevel,
        WriteCoordinator writeCoordinator, CancellationToken cancellationToken = default)
    {
        var txId = TransactionId.@new();
        var sequence = allocate_sequence();

        var transaction = new LightTransaction(txId, isolationLevel, sequence, _wal_writer, this, writeCoordinator);

        _lock.EnterWriteLock();
        try
        {
            _active_transactions[txId] = transaction;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        await _wal_writer.append(new WalRecord
        {
            sequence = sequence,
            transaction_id = txId,
            operation_type = WalOperationType.begin_transaction,
            key = DatabaseKey.empty
        }, cancellationToken);

        return transaction;
    }

    /// <summary>
    ///     同步开启事务（仅用于向后兼容，推荐使用异步版本）
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    /// <param name="writeCoordinator">写入协调器</param>
    /// <returns>事务实例</returns>
    public LightTransaction begin_transaction(IsolationLevel isolationLevel, WriteCoordinator writeCoordinator)
    {
        return begin_transaction(isolationLevel, writeCoordinator, CancellationToken.None).AsTask().GetAwaiter()
            .GetResult();
    }

    /// <summary>
    ///     提交事务，将写集合应用到索引
    /// </summary>
    /// <param name="txId">事务 ID</param>
    /// <param name="writeSet">插入/更新的键值对</param>
    /// <param name="deleteSet">删除的键值对</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask commit(
        TransactionId txId,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> writeSet,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> deleteSet,
        IReadOnlyCollection<DatabaseKey> readSet,
        CancellationToken cancellationToken = default)
    {
        WriteCoordinator? writeCoordinator = null;
        LightTransaction? transaction = null;
        SequenceNumber commitSequence;
        IsolationLevel isolationLevel;
        SequenceNumber startSequence;

        _lock.EnterWriteLock();
        try
        {
            if (!_active_transactions.TryGetValue(txId, out transaction))
                throw new InvalidOperationException($"事务 {txId} 不存在或已结束");

            isolationLevel = transaction.isolation_level;
            startSequence = transaction.start_sequence;
            writeCoordinator = transaction._write_coordinator;

            if (isolationLevel == IsolationLevel.Serializable)
            {
                validate_no_write_conflicts(txId, writeSet, deleteSet);
                validate_serializable_conflict(txId, startSequence, writeSet, deleteSet, readSet);
            }

            commitSequence = allocate_sequence();

            acquire_key_locks(txId, writeSet, deleteSet);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        try
        {
            await _wal_writer.append(new WalRecord
            {
                sequence = commitSequence,
                transaction_id = txId,
                operation_type = WalOperationType.commit_transaction,
                key = DatabaseKey.empty
            }, cancellationToken);

            await _wal_writer.flush(cancellationToken);

            if (writeCoordinator is not null)
            {
                await writeCoordinator.apply_write_set(txId, writeSet, commitSequence, cancellationToken);

                foreach (var key in deleteSet.Keys)
                    await writeCoordinator.apply_write_set(txId,
                        new Dictionary<DatabaseKey, DatabaseValue> { { key, DatabaseValue.empty } },
                        commitSequence, cancellationToken);
            }

            transaction?.mark_committed();

            _lock.EnterWriteLock();
            try
            {
                release_key_locks(txId, writeSet, deleteSet);

                record_key_write_time(writeSet, deleteSet, commitSequence);

                _active_transactions.Remove(txId);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        catch
        {
            _lock.EnterWriteLock();
            try
            {
                release_key_locks(txId, writeSet, deleteSet);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            throw;
        }
    }

    /// <summary>
    ///     异步回滚事务
    /// </summary>
    /// <param name="txId">事务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask rollback(TransactionId txId, CancellationToken cancellationToken = default)
    {
        LightTransaction? transaction = null;
        SequenceNumber sequence;

        _lock.EnterWriteLock();
        try
        {
            if (!_active_transactions.TryGetValue(txId, out transaction)) return;

            sequence = allocate_sequence();

            release_key_locks(txId, transaction.write_set, transaction.delete_set);

            _active_transactions.Remove(txId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        await _wal_writer.append(new WalRecord
        {
            sequence = sequence,
            transaction_id = txId,
            operation_type = WalOperationType.rollback_transaction,
            key = DatabaseKey.empty
        }, cancellationToken);

        transaction?.mark_rolled_back();
    }

    /// <summary>
    ///     同步回滚事务（仅用于向后兼容，推荐使用异步版本）
    /// </summary>
    /// <param name="txId">事务 ID</param>
    public void rollback(TransactionId txId)
    {
        rollback(txId, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    #endregion

    #region 可见性

    /// <summary>
    ///     获取事务的可见序列号
    /// </summary>
    /// <param name="txId">事务 ID</param>
    /// <returns>可见序列号</returns>
    public SequenceNumber get_visible_sequence(TransactionId txId)
    {
        _lock.EnterReadLock();
        try
        {
            if (_active_transactions.TryGetValue(txId, out var transaction)) return transaction.start_sequence;

            return current_sequence;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     获取最小活跃事务的序列号，用于版本清理
    /// </summary>
    /// <returns>最小活跃序列号，若无活跃事务则返回当前序列号</returns>
    public SequenceNumber get_min_active_sequence()
    {
        _lock.EnterReadLock();
        try
        {
            if (_active_transactions.Count == 0) return current_sequence;

            var minSequence = current_sequence;
            foreach (var tx in _active_transactions.Values)
                if (tx.start_sequence.value < minSequence.value)
                    minSequence = tx.start_sequence;

            return minSequence;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    #endregion

    #region 写写冲突检测

    /// <summary>
    ///     验证无写写冲突（Serializable 隔离级别）
    /// </summary>
    /// <param name="txId">当前事务 ID</param>
    /// <param name="writeSet">写集合</param>
    /// <param name="deleteSet">删除集合</param>
    /// <exception cref="InvalidOperationException">检测到写写冲突时抛出</exception>
    private void validate_no_write_conflicts(
        TransactionId txId,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> writeSet,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> deleteSet)
    {
        foreach (var key in writeSet.Keys)
            if (_key_lock_table.TryGetValue(key, out var lockOwner) && lockOwner != txId)
                throw new InvalidOperationException(
                    $"写写冲突：键 {key} 已被事务 {lockOwner} 锁定");

        foreach (var key in deleteSet.Keys)
            if (_key_lock_table.TryGetValue(key, out var lockOwner) && lockOwner != txId)
                throw new InvalidOperationException(
                    $"写写冲突：键 {key} 已被事务 {lockOwner} 锁定");
    }

    /// <summary>
    ///     验证无串行化冲突（检查是否有其他事务在快照建立后修改了相同键）
    /// </summary>
    /// <param name="txId">当前事务 ID</param>
    /// <param name="startSequence">事务开始序列号</param>
    /// <param name="writeSet">写集合</param>
    /// <param name="deleteSet">删除集合</param>
    /// <exception cref="InvalidOperationException">检测到串行化冲突时抛出</exception>
    private void validate_serializable_conflict(
        TransactionId txId,
        SequenceNumber startSequence,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> writeSet,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> deleteSet,
        IReadOnlyCollection<DatabaseKey> readSet)
    {
        foreach (var key in writeSet.Keys)
            if (_key_write_time.TryGetValue(key, out var writeSeq) && (ulong)writeSeq > startSequence.value)
                throw new InvalidOperationException(
                    $"串行化冲突：键 {key} 在事务开始后被其他事务修改（序列号 {writeSeq}）");

        foreach (var key in deleteSet.Keys)
            if (_key_write_time.TryGetValue(key, out var writeSeq) && (ulong)writeSeq > startSequence.value)
                throw new InvalidOperationException(
                    $"串行化冲突：键 {key} 在事务开始后被其他事务修改（序列号 {writeSeq}）");

        foreach (var key in readSet)
            if (_key_write_time.TryGetValue(key, out var writeSeq) && (ulong)writeSeq > startSequence.value)
                throw new InvalidOperationException(
                    $"串行化冲突：读取的键 {key} 在事务开始后被其他事务修改（序列号 {writeSeq}），违反可序列化");
    }

    /// <summary>
    ///     记录键的写入时间戳（用于串行化冲突检测）
    /// </summary>
    private void record_key_write_time(
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> writeSet,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> deleteSet,
        SequenceNumber commitSequence)
    {
        foreach (var key in writeSet.Keys) _key_write_time[key] = (long)commitSequence.value;

        foreach (var key in deleteSet.Keys) _key_write_time[key] = (long)commitSequence.value;
    }

    /// <summary>
    ///     获取写键的锁（需在写锁内调用）
    /// </summary>
    private void acquire_key_locks(
        TransactionId txId,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> writeSet,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> deleteSet)
    {
        foreach (var key in writeSet.Keys) _key_lock_table[key] = txId;

        foreach (var key in deleteSet.Keys) _key_lock_table[key] = txId;
    }

    /// <summary>
    ///     释放写键的锁（需在写锁内调用）
    /// </summary>
    private void release_key_locks(
        TransactionId txId,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> writeSet,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> deleteSet)
    {
        foreach (var key in writeSet.Keys)
            if (_key_lock_table.TryGetValue(key, out var owner) && owner == txId)
                _key_lock_table.Remove(key);

        foreach (var key in deleteSet.Keys)
            if (_key_lock_table.TryGetValue(key, out var owner) && owner == txId)
                _key_lock_table.Remove(key);
    }

    #endregion
}