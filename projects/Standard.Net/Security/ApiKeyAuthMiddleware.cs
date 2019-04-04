using Std.Net.Http;

namespace Std.Net.Security;

/// <summary>
///     API Key 认证中间件。从 X-Api-Key 头或查询参数中提取并验证 API Key。
/// </summary>
public sealed class ApiKeyAuthMiddleware : IMiddleware
{
    private readonly ApiKeyOptions _options;

    /// <summary>
    ///     初始化 API Key 认证中间件。
    /// </summary>
    /// <param name="options">API Key 配置选项</param>
    public ApiKeyAuthMiddleware(ApiKeyOptions? options = null)
    {
        _options = options ?? new ApiKeyOptions();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        var apiKey = extract_api_key(context);

        if (apiKey != null)
        {
            if (_options.valid_keys.TryGetValue(apiKey, out var keyInfo))
            {
                context.items["Security.User"] = new SonicPrincipal
                {
                    subject = keyInfo.name,
                    name = keyInfo.name,
                    roles = keyInfo.roles,
                    is_authenticated = true,
                    auth_type = "APIKey"
                };
            }
            else if (!_options.allow_unauthenticated_passthrough)
            {
                context.response.status_code = HttpStatusCode.unauthorized;
                context.response.json("{\"error\":\"无效的 API Key\"}");
                context.short_circuit();
                return;
            }
        }

        await next();
    }

    private static string? extract_api_key(RouteContext context)
    {
        if (context.request.headers.TryGetValue("X-Api-Key", out var headerValue) && !string.IsNullOrEmpty(headerValue))
            return headerValue;

        if (!string.IsNullOrEmpty(context.request.query_string))
        {
            var query = context.request.query_string.TrimStart('?');
            foreach (var pair in query.Split('&'))
            {
                var kv = pair.Split('=', 2);
                if (kv is ["api_key", _]) return Uri.UnescapeDataString(kv[1]);
            }
        }

        return null;
    }
}

/// <summary>
///     验证通过后挂载到 <see cref="RouteContext.items" /> 的用户主体。
/// </summary>
public sealed class SonicPrincipal
{
    /// <summary>
    ///     主体标识。
    /// </summary>
    public string subject { get; init; } = string.Empty;

    /// <summary>
    ///     显示名称。
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     角色列表。
    /// </summary>
    public IReadOnlyList<string> roles { get; init; } = [];

    /// <summary>
    ///     是否已认证。
    /// </summary>
    public bool is_authenticated { get; init; }

    /// <summary>
    ///     认证类型（JWT / APIKey）。
    /// </summary>
    public string auth_type { get; init; } = string.Empty;
}

/// <summary>
///     API Key 认证配置选项。
/// </summary>
public sealed class ApiKeyOptions
{
    /// <summary>
    ///     有效的 API Key 到密钥信息的映射。
    /// </summary>
    public Dictionary<string, ApiKeyInfo> valid_keys { get; set; } = [];

    /// <summary>
    ///     当 API Key 无效时是否放行（允许后续中间件决定）。
    /// </summary>
    public bool allow_unauthenticated_passthrough { get; set; } = true;
}

/// <summary>
///     API Key 关联的信息。
/// </summary>
public sealed class ApiKeyInfo
{
    /// <summary>
    ///     Key 对应的名称/标识。
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     Key 对应的角色列表。
    /// </summary>
    public IReadOnlyList<string> roles { get; set; } = [];
}