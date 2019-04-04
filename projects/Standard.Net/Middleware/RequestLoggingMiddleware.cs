namespace Std.Net.Middleware;

/// <summary>
///     日志记录器接口，用于请求日志中间件输出日志。
/// </summary>
public interface ISonicLogger
{
    /// <summary>
    ///     记录一条日志。
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="category">日志类别</param>
    /// <param name="message">日志消息</param>
    /// <param name="properties">附加属性</param>
    void log(SonicLogLevel level, string category, string message, Dictionary<string, object?>? properties = null);

    /// <summary>
    ///     判断指定日志级别是否启用。
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>是否启用</returns>
    bool is_enabled(SonicLogLevel level);
}

/// <summary>
///     日志级别。
/// </summary>
public enum SonicLogLevel
{
    /// <summary>
    ///     调试级别
    /// </summary>
    debug,

    /// <summary>
    ///     信息级别
    /// </summary>
    information,

    /// <summary>
    ///     警告级别
    /// </summary>
    warning,

    /// <summary>
    ///     错误级别
    /// </summary>
    error
}

/// <summary>
///     请求日志中间件，记录每个 HTTP 请求的基本信息。
/// </summary>
public sealed class RequestLoggingMiddleware : IMiddleware
{
    private readonly ISonicLogger _logger;

    /// <summary>
    ///     初始化请求日志中间件。
    /// </summary>
    /// <param name="logger">日志器实例</param>
    public RequestLoggingMiddleware(ISonicLogger? logger = null)
    {
        _logger = logger ?? new NoOpLogger();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        var startTime = DateTimeOffset.UtcNow;

        _logger.log(SonicLogLevel.information, "Middleware.RequestLogging",
            $"处理请求 {context.request.method} {context.request.path}",
            new Dictionary<string, object?>
            {
                ["Method"] = context.request.method.ToString(),
                ["Path"] = context.request.path,
                ["ContentType"] = context.request.headers.GetValueOrDefault("Content-Type")
            });

        try
        {
            await next();
        }
        finally
        {
            var elapsed = DateTimeOffset.UtcNow - startTime;

            _logger.log(SonicLogLevel.information, "Middleware.RequestLogging",
                $"请求完成 {context.request.method} {context.request.path} → {context.response.status_code} ({elapsed.TotalMilliseconds:F2}ms)",
                new Dictionary<string, object?>
                {
                    ["Method"] = context.request.method.ToString(),
                    ["Path"] = context.request.path,
                    ["StatusCode"] = (int)context.response.status_code,
                    ["ElapsedMs"] = elapsed.TotalMilliseconds.ToString("F2")
                });
        }
    }

    private sealed class NoOpLogger : ISonicLogger
    {
        public void log(SonicLogLevel level, string category, string message,
            Dictionary<string, object?>? properties = null)
        {
        }

        public bool is_enabled(SonicLogLevel level)
        {
            return false;
        }
    }
}