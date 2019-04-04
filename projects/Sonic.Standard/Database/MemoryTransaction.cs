using Core.Database;

namespace Std.Database;

/// <summary>
///     内存数据库事务实现，通过 Copy-on-Write 快照提供事务语义
/// </summary>
internal sealed class MemoryTransaction : ITransaction
{
    private readonly MemoryDb _db;
    private readonly HashSet<byte[]> _delete_set;
    private readonly Dictionary<byte[], byte[]> _write_set;
    private bool _committed;
    private bool _disposed;
    private bool _rolled_back;

    /// <summary>
    ///     创建内存事务
    /// </summary>
    /// <param name="db">所属内存数据库</param>
    /// <param name="isolationLevel">隔离级别</param>
    public MemoryTransaction(MemoryDb db, IsolationLevel isolationLevel)
    {
        _db = db;
        IsolationLevel = isolationLevel;
        _write_set = new Dictionary<byte[], byte[]>(new MemoryDb.ByteArrayComparer());
        _delete_set = new(new MemoryDb.ByteArrayComparer());
    }

    /// <summary>
    ///     事务隔离级别
    /// </summary>
    public IsolationLevel IsolationLevel { get; }

    /// <summary>
    ///     事务内获取键值
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>值，若不存在则返回 null</returns>
    public Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();

        if (_delete_set.Contains(keyArray)) return Task.FromResult<ReadOnlyMemory<byte>?>(null);

        if (_write_set.TryGetValue(keyArray, out var value)) return Task.FromResult<ReadOnlyMemory<byte>?>(value);

        return _db.GetAsync(key);
    }

    /// <summary>
    ///     事务内存储键值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();
        var valueArray = value.ToArray();
        _write_set[keyArray] = valueArray;
        _delete_set.Remove(keyArray);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     事务内删除键
    /// </summary>
    /// <param name="key">键</param>
    public Task DeleteAsync(ReadOnlyMemory<byte> key)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();
        _delete_set.Add(keyArray);
        _write_set.Remove(keyArray);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     提交事务
    /// </summary>
    public async Task CommitAsync()
    {
        throw_if_disposed();
        if (_committed || _rolled_back) throw new InvalidOperationException("事务已经完成");

        foreach (var kvp in _write_set) await _db.PutAsync(kvp.Key, kvp.Value);

        foreach (var key in _delete_set) await _db.DeleteAsync(key);

        _committed = true;
    }

    /// <summary>
    ///     回滚事务
    /// </summary>
    public Task RollbackAsync()
    {
        throw_if_disposed();
        if (_committed || _rolled_back) throw new InvalidOperationException("事务已经完成");

        _write_set.Clear();
        _delete_set.Clear();
        _rolled_back = true;
        return Task.CompletedTask;
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        if (!_committed && !_rolled_back)
        {
            _write_set.Clear();
            _delete_set.Clear();
            _rolled_back = true;
        }
    }

    private void throw_if_disposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MemoryTransaction));
    }
}