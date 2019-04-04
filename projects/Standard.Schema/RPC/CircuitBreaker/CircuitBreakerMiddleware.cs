namespace Hermes.Rpc.Middleware.CircuitBreaker;

/// <summary>
///     RPC 熔断中间件——基于滑动窗口的断路器模式
/// </summary>
public sealed class CircuitBreakerMiddleware : IHermesRpcMiddleware
{
    private readonly CircuitBreakerOptions _options;
    private readonly ICircuitBreakerStateStore _store;

    public CircuitBreakerMiddleware(ICircuitBreakerStateStore store, CircuitBreakerOptions options)
    {
        _store = store;
        _options = options;
    }

    /// <inheritdoc />
    public async Task HandleAsync(HttpContext context, Func<Task> next)
    {
        var circuitKey = ResolveCircuitKey(context);
        var state = await _store.GetStateAsync(circuitKey, context.RequestAborted);

        if (state.State == CircuitState.Open)
        {
            var elapsed = DateTime.UtcNow - state.OpenedAt;
            if (elapsed < _options.OpenDuration)
            {
                context.Response.StatusCode = 503;
                context.Response.Headers["X-Circuit-State"] = "open";
                await context.Response.WriteAsync("熔断器开启，服务暂时不可用");
                return;
            }

            await _store.TransitionToHalfOpenAsync(circuitKey, context.RequestAborted);
            state = state with { State = CircuitState.HalfOpen };
        }

        try
        {
            await next();

            if (state.State == CircuitState.HalfOpen)
                await _store.TransitionToClosedAsync(circuitKey, context.RequestAborted);
        }
        catch (Exception ex) when (IsCircuitBreakingException(ex))
        {
            await _store.RecordFailureAsync(circuitKey, context.RequestAborted);
            var failureCount = await _store.GetFailureCountAsync(circuitKey, context.RequestAborted);

            if (state.State == CircuitState.HalfOpen || failureCount >= _options.FailureThreshold)
            {
                await _store.TransitionToOpenAsync(circuitKey, context.RequestAborted);
                context.Response.StatusCode = 503;
                context.Response.Headers["X-Circuit-State"] = "open";
                await context.Response.WriteAsync("熔断器触发，服务暂时不可用");
                return;
            }

            throw;
        }
    }

    private static string ResolveCircuitKey(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? $"{segments[0]}/{segments[1]}" : path;
    }

    private static bool IsCircuitBreakingException(Exception ex)
    {
        return ex is HttpRequestException or TimeoutException or TaskCanceledException;
    }
}

/// <summary>
///     熔断器状态
/// </summary>
public enum CircuitState
{
    /// <summary>
    ///     关闭（正常）
    /// </summary>
    Closed,

    /// <summary>
    ///     开启（熔断）
    /// </summary>
    Open,

    /// <summary>
    ///     半开（试探）
    /// </summary>
    HalfOpen
}

/// <summary>
///     熔断器状态记录
/// </summary>
public sealed record CircuitBreakerState
{
    /// <summary>
    ///     当前状态
    /// </summary>
    public CircuitState State { get; init; }

    /// <summary>
    ///     熔断开启时间
    /// </summary>
    public DateTime OpenedAt { get; init; }

    /// <summary>
    ///     失败计数
    /// </summary>
    public int FailureCount { get; init; }
}

/// <summary>
///     熔断器状态存储接口
/// </summary>
public interface ICircuitBreakerStateStore
{
    /// <summary>
    ///     获取熔断器状态
    /// </summary>
    Task<CircuitBreakerState> GetStateAsync(string circuitKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     记录失败
    /// </summary>
    Task RecordFailureAsync(string circuitKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取失败计数
    /// </summary>
    Task<int> GetFailureCountAsync(string circuitKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     转换为开启状态
    /// </summary>
    Task TransitionToOpenAsync(string circuitKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     转换为半开状态
    /// </summary>
    Task TransitionToHalfOpenAsync(string circuitKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     转换为关闭状态
    /// </summary>
    Task TransitionToClosedAsync(string circuitKey, CancellationToken cancellationToken = default);
}

/// <summary>
///     熔断器选项
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    ///     失败阈值
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    ///     熔断持续时间
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);
}