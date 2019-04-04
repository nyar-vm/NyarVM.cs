namespace Std.Database.Core;

/// <summary>
///     支持异步操作的读写锁，允许并发读取和独占写入
/// </summary>
internal sealed class AsyncReaderWriterLock : IDisposable
{
    private readonly SemaphoreSlim _read_lock = new(1, 1);
    private readonly SemaphoreSlim _write_lock = new(1, 1);
    private bool _disposed;
    private int _reader_count;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _read_lock.Dispose();
        _write_lock.Dispose();
    }

    /// <summary>
    ///     异步获取读锁
    /// </summary>
    public async ValueTask enter_read_lock(CancellationToken cancellationToken = default)
    {
        await _write_lock.WaitAsync(cancellationToken);
        try
        {
            Interlocked.Increment(ref _reader_count);
        }
        finally
        {
            _write_lock.Release();
        }
    }

    /// <summary>
    ///     释放读锁
    /// </summary>
    public void exit_read_lock()
    {
        Interlocked.Decrement(ref _reader_count);
    }

    /// <summary>
    ///     异步获取写锁
    /// </summary>
    public async ValueTask enter_write_lock(CancellationToken cancellationToken = default)
    {
        await _write_lock.WaitAsync(cancellationToken);

        while (Volatile.Read(ref _reader_count) > 0) await Task.Yield();
    }

    /// <summary>
    ///     释放写锁
    /// </summary>
    public void exit_write_lock()
    {
        _write_lock.Release();
    }

    /// <summary>
    ///     同步获取读锁
    /// </summary>
    public void enter_read_lock()
    {
        _write_lock.Wait();
        try
        {
            Interlocked.Increment(ref _reader_count);
        }
        finally
        {
            _write_lock.Release();
        }
    }

    /// <summary>
    ///     同步获取写锁
    /// </summary>
    public void enter_write_lock()
    {
        _write_lock.Wait();

        while (Volatile.Read(ref _reader_count) > 0) Thread.Sleep(1);
    }
}