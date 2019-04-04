using Std.Net.Http;
using HttpMethod = Std.Net.Http.HttpMethod;

namespace Std.Net.Middleware;

/// <summary>
///     CORS（跨域资源共享）中间件，根据配置添加跨域响应头。
/// </summary>
public sealed class CorsMiddleware : IMiddleware
{
    private readonly CorsOptions _options;

    /// <summary>
    ///     初始化 CORS 中间件。
    /// </summary>
    /// <param name="options">CORS 配置选项</param>
    public CorsMiddleware(CorsOptions? options = null)
    {
        _options = options ?? new CorsOptions();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        context.request.headers.TryGetValue("Origin", out var requestOrigin);

        if (string.IsNullOrEmpty(requestOrigin))
        {
            await next();
            return;
        }

        var responseHeaders = context.response.headers;

        if (_options.allow_any_origin || _options.allowed_origins.Contains(requestOrigin))
        {
            responseHeaders["Access-Control-Allow-Origin"] = requestOrigin;
        }
        else
        {
            await next();
            return;
        }

        if (_options.allow_credentials) responseHeaders["Access-Control-Allow-Credentials"] = "true";

        if (_options.allowed_methods.Count > 0)
            responseHeaders["Access-Control-Allow-Methods"] = string.Join(", ", _options.allowed_methods);

        if (_options.allowed_headers.Count > 0)
            responseHeaders["Access-Control-Allow-Headers"] = string.Join(", ", _options.allowed_headers);

        responseHeaders["Access-Control-Max-Age"] = _options.max_age_seconds.ToString();

        if (_options.allow_any_origin) responseHeaders["Vary"] = "Origin";

        if (context.request.method == HttpMethod.options)
        {
            context.response.status_code = _options.preflight_success_status_code;
            return;
        }

        await next();
    }
}

/// <summary>
///     CORS 配置选项。
/// </summary>
public sealed class CorsOptions
{
    /// <summary>
    ///     允许所有来源。
    /// </summary>
    public bool allow_any_origin { get; set; } = true;

    /// <summary>
    ///     允许的来源列表。
    /// </summary>
    public HashSet<string> allowed_origins { get; set; } = [];

    /// <summary>
    ///     允许的 HTTP 方法列表。
    /// </summary>
    public HashSet<string> allowed_methods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];

    /// <summary>
    ///     允许的请求头列表。
    /// </summary>
    public HashSet<string> allowed_headers { get; set; } = ["Content-Type", "Authorization"];

    /// <summary>
    ///     是否允许携带凭据。
    /// </summary>
    public bool allow_credentials { get; set; }

    /// <summary>
    ///     预检请求缓存时间（秒）。
    /// </summary>
    public int max_age_seconds { get; set; } = 86400;

    /// <summary>
    ///     预检请求成功时的状态码。
    /// </summary>
    public HttpStatusCode preflight_success_status_code { get; set; } = HttpStatusCode.no_content;
}