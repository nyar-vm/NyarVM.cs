using System.Collections.Concurrent;
using System.Text.Json;
using Std.Net.Http;

namespace Std.Net.Middleware;

/// <summary>
///     速率限制策略。
/// </summary>
public enum RateLimitStrategy
{
    /// <summary>
    ///     固定窗口计数
    /// </summary>
    fixed_window,

    /// <summary>
    ///     令牌桶算法
    /// </summary>
    token_bucket
}

/// <summary>
///     速率限制选项。
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>
    ///     限制策略（默认令牌桶）。
    /// </summary>
    public RateLimitStrategy strategy { get; set; } = RateLimitStrategy.token_bucket;

    /// <summary>
    ///     时间窗口内允许的最大请求数。
    /// </summary>
    public int max_requests { get; set; } = 100;

    /// <summary>
    ///     时间窗口间隔（默认 1 分钟）。
    /// </summary>
    public TimeSpan window_size { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     令牌桶容量。
    /// </summary>
    public int bucket_capacity { get; set; } = 100;

    /// <summary>
    ///     令牌补充速率（每秒补充的令牌数）。
    /// </summary>
    public double refill_rate_per_second { get; set; } = 10;

    /// <summary>
    ///     超出限制时的 HTTP 状态码（默认 429）。
    /// </summary>
    public HttpStatusCode quota_exceeded_status_code { get; set; } = HttpStatusCode.too_many_requests;

    /// <summary>
    ///     超出限制时的提示消息。
    /// </summary>
    public string quota_exceeded_message { get; set; } = "请求过于频繁，请稍后再试";
}

/// <summary>
///     速率限制中间件，基于 IP 或自定义 Key 限制请求频率。
/// </summary>
public sealed class RateLimitingMiddleware : IMiddleware
{
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitState> _states = new();

    /// <summary>
    ///     初始化速率限制中间件。
    /// </summary>
    /// <param name="options">速率限制选项</param>
    public RateLimitingMiddleware(RateLimitOptions? options = null)
    {
        _options = options ?? new RateLimitOptions();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        var key = get_client_key(context);
        var allowed = check_limit(key);

        if (!allowed)
        {
            context.response.status_code = _options.quota_exceeded_status_code;
            var message = JsonSerializer.Serialize(new { error = _options.quota_exceeded_message });
            context.response.json(message);
            context.response.headers["Retry-After"] = _options.window_size.TotalSeconds.ToString("F0");
            return;
        }

        context.response.headers["X-RateLimit-Remaining"] = get_remaining_tokens(key).ToString();

        await next();
    }

    /// <summary>
    ///     获取指定 Key 的剩余可用请求数。
    /// </summary>
    /// <param name="key">客户端标识</param>
    /// <returns>剩余请求数</returns>
    public int get_remaining(string key)
    {
        return get_remaining_tokens(key);
    }

    private bool check_limit(string key)
    {
        var state = _states.GetOrAdd(key, _ => create_state());

        lock (state)
        {
            if (_options.strategy == RateLimitStrategy.fixed_window) return check_fixed_window(state);

            return check_token_bucket(state);
        }
    }

    private bool check_fixed_window(RateLimitState state)
    {
        var now = DateTimeOffset.UtcNow;

        if (now - state.window_start >= _options.window_size)
        {
            state.window_start = now;
            state.count = 0;
        }

        if (state.count >= _options.max_requests) return false;

        state.count++;
        return true;
    }

    private bool check_token_bucket(RateLimitState state)
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = (now - state.last_refill_time).TotalSeconds;
        var tokensToAdd = elapsed * _options.refill_rate_per_second;

        if (tokensToAdd > 0)
        {
            state.tokens = System.Math.Min(_options.bucket_capacity, state.tokens + tokensToAdd);
            state.last_refill_time = now;
        }

        if (state.tokens < 1) return false;

        state.tokens--;
        return true;
    }

    private int get_remaining_tokens(string key)
    {
        if (!_states.TryGetValue(key, out var state)) return _options.max_requests;

        lock (state)
        {
            if (_options.strategy == RateLimitStrategy.fixed_window)
                return System.Math.Max(0, _options.max_requests - state.count);

            return System.Math.Max(0, (int)state.tokens);
        }
    }

    private RateLimitState create_state()
    {
        var now = DateTimeOffset.UtcNow;

        return new RateLimitState
        {
            window_start = now,
            last_refill_time = now,
            tokens = _options.bucket_capacity,
            count = 0
        };
    }

    private static string get_client_key(RouteContext context)
    {
        if (context.request.headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return context.request.headers.GetValueOrDefault("X-Real-IP", "127.0.0.1");
    }

    private sealed class RateLimitState
    {
        public DateTimeOffset window_start { get; set; }
        public DateTimeOffset last_refill_time { get; set; }
        public double tokens { get; set; }
        public int count { get; set; }
    }
}