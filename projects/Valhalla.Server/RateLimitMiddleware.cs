using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Valhalla.Server;

/// <summary>
///     速率限制中间件，基于令牌桶算法限制每个 IP 的请求频率
/// </summary>
public class RateLimitMiddleware
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();
    private readonly int _max_requests;
    private readonly RequestDelegate _next;
    private readonly TimeSpan _window;

    /// <summary>
    ///     创建速率限制中间件
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="maxRequests">时间窗口内允许的最大请求数</param>
    /// <param name="windowSeconds">时间窗口秒数</param>
    public RateLimitMiddleware(RequestDelegate next, int maxRequests = 100, int windowSeconds = 60)
    {
        _next = next;
        _max_requests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task invoke(HttpContext context)
    {
        var clientIp = get_client_ip(context);
        var now = DateTime.UtcNow;

        var entry = _entries.AddOrUpdate(
            clientIp,
            _ => new RateLimitEntry { count = 1, window_start = now },
            (_, existing) =>
            {
                if (now - existing.window_start > _window) return new RateLimitEntry { count = 1, window_start = now };

                existing.count++;
                return existing;
            });

        var remaining = Math.Max(0, _max_requests - entry.count);
        var resetTime = entry.window_start + _window;

        context.Response.Headers["X-RateLimit-Limit"] = _max_requests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString();

        if (entry.count > _max_requests)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = ((int)(resetTime - now).TotalSeconds).ToString();
            context.Response.ContentType = "application/json; charset=utf-8";

            var errorResponse = new
            {
                error = "too_many_requests",
                message = $"请求频率超过限制（{_max_requests} 次/{(int)_window.TotalSeconds} 秒），请稍后重试",
                retry_after = (int)(resetTime - now).TotalSeconds
            };

            var json = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(json);
            return;
        }

        await _next(context);
    }

    /// <summary>
    ///     清理过期的速率限制条目
    /// </summary>
    public void cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _entries)
            if (now - kvp.Value.window_start > _window)
                _entries.TryRemove(kvp.Key, out _);
    }

    private static string get_client_ip(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor)) return forwardedFor.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp)) return realIp;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class RateLimitEntry
    {
        public int count { get; set; }
        public DateTime window_start { get; set; }
    }
}