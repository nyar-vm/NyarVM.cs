using System.Collections.Concurrent;
using Core.Database;

namespace Std.Database;

/// <summary>
///     纯内存数据库，数据仅存于内存，进程退出后丢失
/// </summary>
public sealed class MemoryDb : IDatabase
{
    private readonly ConcurrentDictionary<byte[], byte[]> _data;
    private readonly MemoryIndexManager _index_manager;
    private readonly byte[] _lock = [];
    private readonly SortedSet<byte[]> _sorted_keys;
    private bool _disposed;

    /// <summary>
    ///     创建内存数据库
    /// </summary>
    /// <param name="name">数据库名称</param>
    public MemoryDb(string name = "memory")
    {
        this.name = name;
        _data = new ConcurrentDictionary<byte[], byte[]>(new ByteArrayComparer());
        _sorted_keys = new(new ByteArrayComparer());
        _index_manager = new MemoryIndexManager();
    }

    /// <summary>
    ///     数据库名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     索引管理器
    /// </summary>
    public IIndexManager IndexManager => _index_manager;

    /// <summary>
    ///     异步获取键值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>值，若不存在则返回 null</returns>
    public Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();
        if (_data.TryGetValue(keyArray, out var value)) return Task.FromResult<ReadOnlyMemory<byte>?>(value);
        return Task.FromResult<ReadOnlyMemory<byte>?>(null);
    }

    /// <summary>
    ///     异步存储键值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();
        var valueArray = value.ToArray();
        _data[keyArray] = valueArray;
        lock (_lock)
        {
            _sorted_keys.Add(keyArray);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     异步删除键
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DeleteAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();
        _data.TryRemove(keyArray, out _);
        lock (_lock)
        {
            _sorted_keys.Remove(keyArray);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     异步判断键是否存在
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    public Task<bool> ContainsKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();
        return Task.FromResult(_data.ContainsKey(keyArray));
    }

    /// <summary>
    ///     开启事务
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    /// <returns>事务实例</returns>
    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot)
    {
        throw_if_disposed();
        return new MemoryTransaction(this, isolationLevel);
    }

    /// <summary>
    ///     创建快照
    /// </summary>
    /// <returns>快照实例</returns>
    public ISnapshot CreateSnapshot()
    {
        throw_if_disposed();
        return new MemorySnapshot(_data, _sorted_keys, _lock);
    }

    /// <summary>
    ///     创建游标
    /// </summary>
    /// <returns>游标实例</returns>
    public ICursor CreateCursor()
    {
        throw_if_disposed();
        return new MemoryCursor(_data, _sorted_keys, _lock);
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _data.Clear();
        lock (_lock)
        {
            _sorted_keys.Clear();
        }
    }

    /// <summary>
    ///     异步释放资源
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void throw_if_disposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MemoryDb));
    }

    /// <summary>
    ///     字节数组比较器，同时实现 IComparer 和 IEqualityComparer
    /// </summary>
    internal sealed class ByteArrayComparer : IComparer<byte[]>, IEqualityComparer<byte[]>
    {
        /// <summary>
        ///     比较两个字节数组的大小
        /// </summary>
        /// <param name="x">第一个字节数组</param>
        /// <param name="y">第二个字节数组</param>
        /// <returns>比较结果</returns>
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x is null && y is null) return 0;

            if (x is null) return -1;

            if (y is null) return 1;

            return ((ReadOnlySpan<byte>)x).SequenceCompareTo(y);
        }

        /// <summary>
        ///     判断两个字节数组是否相等
        /// </summary>
        /// <param name="x">第一个字节数组</param>
        /// <param name="y">第二个字节数组</param>
        /// <returns>是否相等</returns>
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x is null && y is null) return true;

            if (x is null || y is null) return false;

            return x.AsSpan().SequenceEqual(y);
        }

        /// <summary>
        ///     计算字节数组的哈希值
        /// </summary>
        /// <param name="obj">字节数组</param>
        /// <returns>哈希值</returns>
        public int GetHashCode(byte[] obj)
        {
            var hash = new HashCode();
            hash.AddBytes(obj);
            return hash.ToHashCode();
        }
    }
}