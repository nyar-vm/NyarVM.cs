using Std.Database.Core;
using Std.Database.Index;
using Std.Database.Transaction;
using Std.Database.Wal;

namespace Std.Database;

/// <summary>
///     写入协调器，统一管理索引更新、WAL 写入和序列号分配
/// </summary>
internal sealed class WriteCoordinator
{
    private readonly TransactionManager _transaction_manager;
    private readonly IWalWriter _wal_writer;

    /// <summary>
    ///     创建写入协调器
    /// </summary>
    /// <param name="primaryIndex">主索引</param>
    /// <param name="walWriter">WAL 写入器</param>
    /// <param name="transactionManager">事务管理器</param>
    public WriteCoordinator(
        IBTreeIndex primaryIndex,
        IWalWriter walWriter,
        TransactionManager transactionManager)
    {
        primary_index = primaryIndex;
        _wal_writer = walWriter;
        _transaction_manager = transactionManager;
    }

    /// <summary>
    ///     主索引引用（供读取操作使用）
    /// </summary>
    public IBTreeIndex primary_index { get; }

    /// <summary>
    ///     非事务写入：插入键值对
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask put(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken)
    {
        var sequence = _transaction_manager.allocate_sequence();

        await _wal_writer.append(new WalRecord
        {
            sequence = sequence,
            transaction_id = TransactionId.min,
            operation_type = WalOperationType.update,
            key = key,
            new_value = value
        }, cancellationToken);

        await _wal_writer.flush(cancellationToken);

        await primary_index.insert(key, value, sequence);
    }

    /// <summary>
    ///     非事务写入：删除键
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask<bool> delete(DatabaseKey key, CancellationToken cancellationToken)
    {
        var oldValue = await primary_index.search(key);
        if (oldValue is null) return false;

        var sequence = _transaction_manager.allocate_sequence();

        await _wal_writer.append(new WalRecord
        {
            sequence = sequence,
            transaction_id = TransactionId.min,
            operation_type = WalOperationType.delete,
            key = key,
            old_value = oldValue
        }, cancellationToken);

        await _wal_writer.flush(cancellationToken);

        var result = await primary_index.delete(key, sequence);

        return result;
    }

    /// <summary>
    ///     事务提交时：将 writeSet 应用到索引
    /// </summary>
    /// <param name="txId">事务 ID</param>
    /// <param name="writeSet">事务的写集合</param>
    /// <param name="commitSequence">提交序列号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask apply_write_set(
        TransactionId txId,
        IReadOnlyDictionary<DatabaseKey, DatabaseValue> writeSet,
        SequenceNumber commitSequence,
        CancellationToken cancellationToken)
    {
        foreach (var (key, value) in writeSet)
            if (value.is_empty)
                await primary_index.delete(key, commitSequence);
            else
                await primary_index.insert(key, value, commitSequence);
    }

    /// <summary>
    ///     读取键值（支持 MVCC 快照可见性）
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="asOfSequence">快照序列号，为 null 时读取最新版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask<DatabaseValue?> get(DatabaseKey key, SequenceNumber? asOfSequence = null,
        CancellationToken cancellationToken = default)
    {
        return await primary_index.search(key, asOfSequence);
    }
}