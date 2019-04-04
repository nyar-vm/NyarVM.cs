using Core.Database;
using Std.Database.Core;
using Std.Database.Wal;

namespace Std.Database.Transaction;

/// <summary>
///     LightDB 事务实现
/// </summary>
internal sealed class LightTransaction : IDisposable, IAsyncDisposable
{
    private readonly Dictionary<DatabaseKey, DatabaseValue> _delete_set;
    private readonly TransactionManager _manager;
    private readonly HashSet<DatabaseKey> _read_set;
    private readonly IWalWriter _wal_writer;
    private readonly Dictionary<DatabaseKey, DatabaseValue> _write_set;
    private bool _disposed;

    /// <summary>
    ///     创建事务
    /// </summary>
    /// <param name="id">事务 ID</param>
    /// <param name="isolationLevel">隔离级别</param>
    /// <param name="startSequence">开始序列号</param>
    /// <param name="walWriter">WAL 写入器</param>
    /// <param name="manager">事务管理器</param>
    /// <param name="writeCoordinator">写入协调器</param>
    internal LightTransaction(TransactionId id, IsolationLevel isolationLevel, SequenceNumber startSequence,
        IWalWriter walWriter, TransactionManager manager, WriteCoordinator writeCoordinator)
    {
        this.id = id;
        isolation_level = isolationLevel;
        start_sequence = startSequence;
        start_time = DateTime.UtcNow;
        _wal_writer = walWriter;
        _manager = manager;
        _write_coordinator = writeCoordinator;
        _write_set = new Dictionary<DatabaseKey, DatabaseValue>();
        _delete_set = new Dictionary<DatabaseKey, DatabaseValue>();
        _read_set = [];
    }

    /// <summary>
    ///     事务开始时的序列号
    /// </summary>
    public SequenceNumber start_sequence { get; }

    /// <summary>
    ///     写集合（包含插入和更新）
    /// </summary>
    public IReadOnlyDictionary<DatabaseKey, DatabaseValue> write_set => _write_set;

    /// <summary>
    ///     删除集合
    /// </summary>
    public IReadOnlyDictionary<DatabaseKey, DatabaseValue> delete_set => _delete_set;

    /// <summary>
    ///     读取集合（Serializable 隔离级别下追踪读取过的键，用于防止 write skew）
    /// </summary>
    public IReadOnlyCollection<DatabaseKey> read_set => _read_set;

    /// <summary>
    ///     写入协调器引用，供事务管理器提交时使用
    /// </summary>
    internal WriteCoordinator _write_coordinator { get; }

    /// <summary>
    ///     事务 ID
    /// </summary>
    public TransactionId id { get; }

    /// <summary>
    ///     隔离级别
    /// </summary>
    public IsolationLevel isolation_level { get; }

    /// <summary>
    ///     事务开始时间
    /// </summary>
    public DateTime start_time { get; }

    /// <summary>
    ///     是否只读
    /// </summary>
    public bool is_read_only => _write_set.Count == 0 && _delete_set.Count == 0;

    /// <summary>
    ///     是否已提交
    /// </summary>
    public bool is_committed { get; private set; }

    /// <summary>
    ///     是否已回滚
    /// </summary>
    public bool is_rolled_back { get; private set; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        if (!is_committed && !is_rolled_back) await _manager.rollback(id, CancellationToken.None);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (!is_committed && !is_rolled_back) _manager.rollback(id);
    }

    /// <summary>
    ///     事务内获取键值
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>值</returns>
    public async ValueTask<TValue?> get<TValue>(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        if (is_committed || is_rolled_back) throw new InvalidOperationException("事务已结束");

        if (_delete_set.ContainsKey(key)) return default;

        if (_write_set.TryGetValue(key, out var writeValue)) return writeValue.to_object<TValue>();

        SequenceNumber? asOfSequence = null;
        if (isolation_level is IsolationLevel.Snapshot or IsolationLevel.Serializable) asOfSequence = start_sequence;

        var value = await _write_coordinator.get(key, asOfSequence, cancellationToken);
        if (value is null) return default;

        if (isolation_level is IsolationLevel.Snapshot or IsolationLevel.Serializable) _read_set.Add(key);

        return value.Value.to_object<TValue>();
    }

    /// <summary>
    ///     事务内存储键值
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask put<TValue>(DatabaseKey key, TValue value, CancellationToken cancellationToken = default)
    {
        if (is_committed || is_rolled_back) throw new InvalidOperationException("事务已结束");

        var lightValue = DatabaseValue.from_object(value);
        _write_set[key] = lightValue;
        _delete_set.Remove(key);

        var sequence = _manager.allocate_sequence();

        await _wal_writer.append(new WalRecord
        {
            sequence = sequence,
            transaction_id = id,
            operation_type = WalOperationType.update,
            key = key,
            new_value = lightValue
        }, cancellationToken);
    }

    /// <summary>
    ///     事务内删除键
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功删除</returns>
    public async ValueTask<bool> delete(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        if (is_committed || is_rolled_back) throw new InvalidOperationException("事务已结束");

        _delete_set[key] = DatabaseValue.empty;
        _write_set.Remove(key);

        var sequence = _manager.allocate_sequence();

        await _wal_writer.append(new WalRecord
        {
            sequence = sequence,
            transaction_id = id,
            operation_type = WalOperationType.delete,
            key = key
        }, cancellationToken);

        return true;
    }

    /// <summary>
    ///     提交事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask commit(CancellationToken cancellationToken = default)
    {
        if (is_committed || is_rolled_back) throw new InvalidOperationException("事务已结束");

        await _manager.commit(id, _write_set, _delete_set, _read_set, cancellationToken);
    }

    /// <summary>
    ///     回滚事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask rollback(CancellationToken cancellationToken = default)
    {
        if (is_committed || is_rolled_back) return;

        await _manager.rollback(id, cancellationToken);
    }

    /// <summary>
    ///     标记为已提交
    /// </summary>
    internal void mark_committed()
    {
        is_committed = true;
    }

    /// <summary>
    ///     标记为已回滚
    /// </summary>
    internal void mark_rolled_back()
    {
        is_rolled_back = true;
    }
}