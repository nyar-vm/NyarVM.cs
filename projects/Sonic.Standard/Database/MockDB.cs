using Core.Database;

namespace Std.Database;

/// <summary>
///     可编程模拟数据库，用于单元测试中模拟数据库行为
/// </summary>
public sealed class MockDb : IDatabase
{
    private readonly Dictionary<byte[], byte[]> _data;
    private readonly List<Action<ReadOnlyMemory<byte>>> _delete_handlers;
    private readonly Dictionary<byte[], Func<ReadOnlyMemory<byte>, Task<ReadOnlyMemory<byte>?>>> _get_handlers;
    private readonly MockIndexManager _index_manager;
    private readonly List<Func<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, Task>> _put_handlers;
    private bool _disposed;
    private Type? _exception_type;

    public MockDb(string name = "mock")
    {
        name = name;
        _data = new Dictionary<byte[], byte[]>(new ByteArrayComparer());
        _get_handlers =
            new Dictionary<byte[], Func<ReadOnlyMemory<byte>, Task<ReadOnlyMemory<byte>?>>>(new ByteArrayComparer());
        _put_handlers = [];
        _delete_handlers = [];
        _index_manager = new MockIndexManager();
    }

    public string name { get; }

    public IIndexManager IndexManager => _index_manager;

    public Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        throw_if_configured();
        var keyArray = key.ToArray();
        if (_get_handlers.TryGetValue(keyArray, out var handler)) return handler(key);
        if (_data.TryGetValue(keyArray, out var value)) return Task.FromResult<ReadOnlyMemory<byte>?>(value);
        return Task.FromResult<ReadOnlyMemory<byte>?>(null);
    }

    public async Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        throw_if_configured();
        var keyArray = key.ToArray();
        var valueArray = value.ToArray();
        _data[keyArray] = valueArray;
        foreach (var handler in _put_handlers) await handler(key, value);
    }

    public Task DeleteAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        throw_if_configured();
        var keyArray = key.ToArray();
        _data.Remove(keyArray);
        foreach (var handler in _delete_handlers) handler(key);
        return Task.CompletedTask;
    }

    public Task<bool> ContainsKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        throw_if_disposed();
        throw_if_configured();
        var keyArray = key.ToArray();
        return Task.FromResult(_data.ContainsKey(keyArray));
    }

    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot)
    {
        throw_if_disposed();
        throw_if_configured();
        return new MockTransaction(this, isolationLevel);
    }

    public ISnapshot CreateSnapshot()
    {
        throw_if_disposed();
        throw_if_configured();
        return new MockSnapshot(_data);
    }

    public ICursor CreateCursor()
    {
        throw_if_disposed();
        throw_if_configured();
        return new MockCursor(_data);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _data.Clear();
        _get_handlers.Clear();
        _put_handlers.Clear();
        _delete_handlers.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     预设指定键的返回值
    /// </summary>
    public void setup_get(byte[] key, byte[]? value)
    {
        var keyCopy = key.ToArray();
        if (value is not null)
        {
            var valueCopy = value.ToArray();
            _get_handlers[keyCopy] = _ => Task.FromResult<ReadOnlyMemory<byte>?>(valueCopy);
        }
        else
        {
            _get_handlers[keyCopy] = _ => Task.FromResult<ReadOnlyMemory<byte>?>(null);
        }
    }

    /// <summary>
    ///     注入 Put 操作处理器
    /// </summary>
    public void setup_put(Func<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, Task> handler)
    {
        _put_handlers.Add(handler);
    }

    /// <summary>
    ///     注入 Delete 操作处理器
    /// </summary>
    public void setup_delete(Action<ReadOnlyMemory<byte>> handler)
    {
        _delete_handlers.Add(handler);
    }

    /// <summary>
    ///     注入异常类型，所有操作将抛出该异常
    /// </summary>
    public void setup_exception(Type exceptionType)
    {
        _exception_type = exceptionType;
    }

    private void throw_if_disposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MockDb));
    }

    private void throw_if_configured()
    {
        if (_exception_type is not null) throw (Exception)Activator.CreateInstance(_exception_type)!;
    }

    internal sealed class ByteArrayComparer : IComparer<byte[]>, IEqualityComparer<byte[]>
    {
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x is null && y is null) return 0;

            if (x is null) return -1;

            if (y is null) return 1;

            return ((ReadOnlySpan<byte>)x).SequenceCompareTo(y);
        }

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x is null && y is null) return true;

            if (x is null || y is null) return false;

            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            var hash = new HashCode();
            hash.AddBytes(obj);
            return hash.ToHashCode();
        }
    }
}