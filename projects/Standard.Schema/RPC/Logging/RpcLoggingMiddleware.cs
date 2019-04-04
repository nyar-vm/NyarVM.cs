namespace Hermes.Rpc.Middleware.Logging;

/// <summary>
///     RPC 日志中间件——请求/响应日志记录
/// </summary>
public sealed class RpcLoggingMiddleware : IHermesRpcMiddleware
{
    private readonly ILogger<RpcLoggingMiddleware> _logger;

    public RpcLoggingMiddleware(ILogger<RpcLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(HttpContext context, Func<Task> next)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation("[{RequestId}] → {Method} {Path}", requestId, method, path);

        try
        {
            await next();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            if (statusCode >= 400)
                _logger.LogWarning("[{RequestId}] ← {StatusCode} ({Elapsed:F0}ms) {Method} {Path}", requestId,
                    statusCode, elapsed, method, path);
            else
                _logger.LogInformation("[{RequestId}] ← {StatusCode} ({Elapsed:F0}ms) {Method} {Path}", requestId,
                    statusCode, elapsed, method, path);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[{RequestId}] ✗ 500 ({Elapsed:F0}ms) {Method} {Path}", requestId, elapsed, method,
                path);
            throw;
        }
    }
}