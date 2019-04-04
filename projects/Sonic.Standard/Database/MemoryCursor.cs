using System.Collections.Concurrent;
using Core.Database;

namespace Std.Database;

/// <summary>
///     内存数据库游标实现，基于有序键列表遍历
/// </summary>
internal sealed class MemoryCursor : ICursor
{
    private readonly ConcurrentDictionary<byte[], byte[]> _data;
    private readonly byte[] _lock;
    private readonly SortedSet<byte[]> _sorted_keys;
    private bool _disposed;
    private int _index = -1;
    private List<byte[]> _key_list;

    /// <summary>
    ///     创建内存游标
    /// </summary>
    /// <param name="data">数据字典</param>
    /// <param name="sortedKeys">有序键集合</param>
    /// <param name="lockObj">同步锁对象</param>
    public MemoryCursor(ConcurrentDictionary<byte[], byte[]> data, SortedSet<byte[]> sortedKeys, byte[] lockObj)
    {
        _data = data;
        _sorted_keys = sortedKeys;
        _lock = lockObj;
        _key_list = [];
        refresh_key_list();
    }

    /// <summary>
    ///     当前键值对
    /// </summary>
    public (ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value) Current
    {
        get
        {
            if (_index < 0 || _index >= _key_list.Count) throw new InvalidOperationException("游标未定位到有效位置");

            var key = _key_list[_index];
            var value = _data[key];
            return (key, value);
        }
    }

    /// <summary>
    ///     向后移动
    /// </summary>
    /// <returns>是否成功移动</returns>
    public ValueTask<bool> MoveNextAsync()
    {
        if (_disposed) return ValueTask.FromResult(false);

        _index++;
        return ValueTask.FromResult(_index < _key_list.Count);
    }

    /// <summary>
    ///     向前移动
    /// </summary>
    /// <returns>是否成功移动</returns>
    public ValueTask<bool> MovePreviousAsync()
    {
        if (_disposed) return ValueTask.FromResult(false);

        if (_index <= 0) return ValueTask.FromResult(false);

        _index--;
        return ValueTask.FromResult(true);
    }

    /// <summary>
    ///     定位到指定键
    /// </summary>
    /// <param name="key">目标键</param>
    public ValueTask SeekAsync(ReadOnlyMemory<byte> key)
    {
        if (_disposed) return ValueTask.CompletedTask;

        refresh_key_list();
        var keyArray = key.ToArray();
        for (var i = 0; i < _key_list.Count; i++)
            if (_key_list[i].AsSpan().SequenceCompareTo(keyArray) >= 0)
            {
                _index = i;
                return ValueTask.CompletedTask;
            }

        _index = _key_list.Count;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _key_list.Clear();
    }

    private void refresh_key_list()
    {
        lock (_lock)
        {
            _key_list = [.. _sorted_keys];
        }
    }
}