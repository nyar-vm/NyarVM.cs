using System.Security.Claims;

namespace Hermes.Rpc.Middleware.RateLimiting;

/// <summary>
///     RPC 限流中间件——基于令牌桶算法的请求速率限制
/// </summary>
public sealed class RateLimitingMiddleware : IHermesRpcMiddleware
{
    private readonly RateLimitOptions _options;
    private readonly IRateLimitStore _store;

    public RateLimitingMiddleware(IRateLimitStore store, RateLimitOptions options)
    {
        _store = store;
        _options = options;
    }

    /// <inheritdoc />
    public async Task HandleAsync(HttpContext context, Func<Task> next)
    {
        var clientId = ResolveClientId(context);
        var permit = await _store.TryAcquireAsync(clientId, _options.MaxRequests, _options.WindowSeconds,
            context.RequestAborted);

        context.Response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = permit.Remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = permit.ResetAt.ToString();

        if (!permit.Allowed)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = permit.RetryAfterSeconds.ToString();
            await context.Response.WriteAsync("请求频率超限，请稍后重试");
            return;
        }

        await next();
    }

    private static string ResolveClientId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId)) return $"user:{userId}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }
}

/// <summary>
///     限流存储接口
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    ///     尝试获取令牌
    /// </summary>
    Task<RateLimitResult> TryAcquireAsync(string clientId, int maxRequests, int windowSeconds,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     限流结果
/// </summary>
public sealed class RateLimitResult
{
    /// <summary>
    ///     是否允许请求
    /// </summary>
    public bool Allowed { get; init; }

    /// <summary>
    ///     剩余配额
    /// </summary>
    public int Remaining { get; init; }

    /// <summary>
    ///     配额重置时间
    /// </summary>
    public long ResetAt { get; init; }

    /// <summary>
    ///     建议重试等待秒数
    /// </summary>
    public int RetryAfterSeconds { get; init; }
}

/// <summary>
///     限流选项
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>
    ///     窗口内最大请求数
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    ///     窗口时间（秒）
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}