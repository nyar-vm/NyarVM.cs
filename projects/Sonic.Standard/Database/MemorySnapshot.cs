using System.Collections.Concurrent;
using Core.Database;

namespace Std.Database;

/// <summary>
///     内存数据库快照实现，基于创建时的字典副本
/// </summary>
internal sealed class MemorySnapshot : ISnapshot
{
    private readonly Dictionary<byte[], byte[]> _snapshot_data;
    private bool _disposed;

    /// <summary>
    ///     创建内存快照
    /// </summary>
    /// <param name="data">数据字典</param>
    /// <param name="sortedKeys">有序键集合</param>
    /// <param name="lockObj">同步锁对象</param>
    public MemorySnapshot(ConcurrentDictionary<byte[], byte[]> data, SortedSet<byte[]> sortedKeys, byte[] lockObj)
    {
        lock (lockObj)
        {
            _snapshot_data = new Dictionary<byte[], byte[]>(data, new MemoryDb.ByteArrayComparer());
        }
    }

    /// <summary>
    ///     获取键值
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>值，若不存在则返回 null</returns>
    public ReadOnlyMemory<byte>? Get(ReadOnlyMemory<byte> key)
    {
        throw_if_disposed();
        var keyArray = key.ToArray();
        if (_snapshot_data.TryGetValue(keyArray, out var value)) return value;
        return null;
    }

    /// <summary>
    ///     创建游标
    /// </summary>
    /// <returns>游标实例</returns>
    public ICursor CreateCursor()
    {
        throw_if_disposed();
        var sortedKeys = new SortedSet<byte[]>(_snapshot_data.Keys, new MemoryDb.ByteArrayComparer());
        var data = new ConcurrentDictionary<byte[], byte[]>(_snapshot_data, new MemoryDb.ByteArrayComparer());
        return new MemoryCursor(data, sortedKeys, []);
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _snapshot_data.Clear();
    }

    private void throw_if_disposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MemorySnapshot));
    }
}