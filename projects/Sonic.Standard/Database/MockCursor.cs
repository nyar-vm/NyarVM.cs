using Core.Database;

namespace Std.Database;

/// <summary>
///     模拟游标实现，基于有序键列表遍历
/// </summary>
internal sealed class MockCursor : ICursor
{
    private readonly Dictionary<byte[], byte[]> _data;
    private bool _disposed;
    private int _index = -1;
    private List<byte[]> _key_list;

    public MockCursor(Dictionary<byte[], byte[]> data)
    {
        _data = data;
        _key_list = [];
        refresh_key_list();
    }

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

    public ValueTask<bool> MoveNextAsync()
    {
        if (_disposed) return ValueTask.FromResult(false);

        _index++;
        return ValueTask.FromResult(_index < _key_list.Count);
    }

    public ValueTask<bool> MovePreviousAsync()
    {
        if (_disposed) return ValueTask.FromResult(false);

        if (_index <= 0) return ValueTask.FromResult(false);

        _index--;
        return ValueTask.FromResult(true);
    }

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

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _key_list.Clear();
    }

    private void refresh_key_list()
    {
        _key_list = [.. _data.Keys.OrderBy(k => k, new MockDb.ByteArrayComparer())];
    }
}