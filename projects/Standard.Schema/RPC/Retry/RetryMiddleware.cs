using System.Net;

namespace Hermes.Rpc.Middleware.Retry;

/// <summary>
///     RPC 重试中间件——指数退避重试策略
/// </summary>
public sealed class RetryMiddleware : IHermesRpcMiddleware
{
    private readonly RetryOptions _options;

    public RetryMiddleware(RetryOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task HandleAsync(HttpContext context, Func<Task> next)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
            try
            {
                await next();
                return;
            }
            catch (HttpRequestException ex) when (IsRetryable(ex.StatusCode) && attempt < _options.MaxRetries)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, context.RequestAborted);
            }
            catch (TaskCanceledException) when (attempt < _options.MaxRetries)
            {
                lastException = new TimeoutException("请求超时");
                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, context.RequestAborted);
            }

        context.Response.StatusCode = 503;
        await context.Response.WriteAsync($"重试 {_options.MaxRetries} 次后仍失败: {lastException?.Message}");
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var baseDelay = _options.BaseDelayMs * Math.Pow(2, attempt);
        var jittered = baseDelay * (0.8 + Random.Shared.NextDouble() * 0.4);
        var capped = Math.Min(jittered, _options.MaxDelayMs);
        return TimeSpan.FromMilliseconds(capped);
    }

    private static bool IsRetryable(HttpStatusCode? statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }
}

/// <summary>
///     重试选项
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    ///     最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    ///     基础延迟（毫秒）
    /// </summary>
    public double BaseDelayMs { get; set; } = 200;

    /// <summary>
    ///     最大延迟（毫秒）
    /// </summary>
    public double MaxDelayMs { get; set; } = 5000;
}