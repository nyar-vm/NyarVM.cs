using System.Collections.Concurrent;

namespace Hermes.Database;

/// <summary>
///     连接池接口 — 管理数据库连接的租用与归还
/// </summary>
/// <typeparam name="TConnection">连接类型</typeparam>
public interface IConnectionPool<TConnection> : IDisposable
    where TConnection : class, IDisposable
{
    /// <summary>
    ///     池中最大连接数
    /// </summary>
    int MaxPoolSize { get; }

    /// <summary>
    ///     当前活跃连接数（已租出）
    /// </summary>
    int ActiveConnections { get; }

    /// <summary>
    ///     当前空闲连接数（可立即租用）
    /// </summary>
    int IdleConnections { get; }

    /// <summary>
    ///     从池中租用一个连接，若无可用连接则等待
    /// </summary>
    Task<PooledConnection<TConnection>> RentAsync(CancellationToken ct = default);

    /// <summary>
    ///     归还连接到池中
    /// </summary>
    void Return(TConnection connection);
}

/// <summary>
///     从连接池租用的连接包装，归还时自动调用 <see cref="DisposeAsync" />
/// </summary>
public sealed class PooledConnection<TConnection> : IAsyncDisposable
    where TConnection : class, IDisposable
{
    private readonly IConnectionPool<TConnection> _pool;
    private bool _disposed;

    internal PooledConnection(IConnectionPool<TConnection> pool, TConnection connection)
    {
        _pool = pool;
        Connection = connection;
    }

    /// <summary>
    ///     底层数据库连接
    /// </summary>
    public TConnection Connection { get; }

    /// <summary>
    ///     归还连接到池中
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _disposed = true;
        _pool.Return(Connection);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     连接池配置选项
/// </summary>
public sealed class ConnectionPoolOptions
{
    /// <summary>
    ///     最大连接数
    /// </summary>
    public int MaxPoolSize { get; init; } = 10;

    /// <summary>
    ///     连接空闲超时时间（秒），超时后关闭
    /// </summary>
    public int IdleTimeoutSeconds { get; init; } = 300;

    /// <summary>
    ///     租用连接的最大等待时间（秒），超时抛出异常
    /// </summary>
    public int ConnectionTimeoutSeconds { get; init; } = 30;
}

/// <summary>
///     基于 <see cref="ConcurrentBag{T}" /> 的默认连接池实现
/// </summary>
public sealed class DefaultConnectionPool<TConnection> : IConnectionPool<TConnection>
    where TConnection : class, IDisposable
{
    private readonly Func<CancellationToken, Task<TConnection>> _connectionFactory;
    private readonly ConcurrentBag<TConnection> _idleConnections;
    private readonly ConnectionPoolOptions _options;
    private readonly SemaphoreSlim _semaphore;
    private volatile int _activeCount;
    private bool _disposed;

    /// <summary>
    ///     创建连接池
    /// </summary>
    /// <param name="connectionFactory">连接工厂方法</param>
    /// <param name="options">连接池配置选项</param>
    public DefaultConnectionPool(
        Func<CancellationToken, Task<TConnection>> connectionFactory,
        ConnectionPoolOptions? options = null)
    {
        _connectionFactory = connectionFactory;
        _options = options ?? new ConnectionPoolOptions();
        _idleConnections = [];
        _semaphore = new SemaphoreSlim(_options.MaxPoolSize, _options.MaxPoolSize);
    }

    /// <summary>
    ///     池中最大连接数
    /// </summary>
    public int MaxPoolSize => _options.MaxPoolSize;

    /// <summary>
    ///     当前活跃连接数（已租出）
    /// </summary>
    public int ActiveConnections => _activeCount;

    /// <summary>
    ///     当前空闲连接数（可立即租用）
    /// </summary>
    public int IdleConnections => _idleConnections.Count;

    /// <summary>
    ///     从池中租用一个连接
    /// </summary>
    public async Task<PooledConnection<TConnection>> RentAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entered = await _semaphore.WaitAsync(TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds), ct);
        if (!entered)
            throw new TimeoutException(
                $"获取数据库连接超时（{_options.ConnectionTimeoutSeconds}秒），当前活跃连接数：{_activeCount}，最大连接数：{_options.MaxPoolSize}");

        try
        {
            TConnection connection;

            if (_idleConnections.TryTake(out var idleConn))
                connection = idleConn;
            else
                connection = await _connectionFactory(ct);

            Interlocked.Increment(ref _activeCount);
            return new PooledConnection<TConnection>(this, connection);
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    /// <summary>
    ///     归还连接到池中
    /// </summary>
    public void Return(TConnection connection)
    {
        if (_disposed)
        {
            connection.Dispose();
            return;
        }

        Interlocked.Decrement(ref _activeCount);

        if (_idleConnections.Count >= _options.MaxPoolSize)
            connection.Dispose();
        else
            _idleConnections.Add(connection);

        _semaphore.Release();
    }

    /// <summary>
    ///     释放所有连接
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        while (_idleConnections.TryTake(out var conn)) conn.Dispose();

        _semaphore.Dispose();
    }
}