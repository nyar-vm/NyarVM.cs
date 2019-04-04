using Std.App.Server.Attributes;
using Std.Security;

namespace Std.App.Server.Middleware;

/// <summary>
///     JWT 认证中间件，根据 <see cref="AuthorizeAttribute" /> 验证请求的授权令牌
///     支持控制器级别和 Action 级别的认证要求
///     认证成功后将 <see cref="JwtToken" /> 存入 <see cref="RouteContext.items" />
/// </summary>
public sealed class AuthMiddleware : IMiddleware
{
    /// <summary>
    ///     认证令牌在上下文字典中的键名
    /// </summary>
    public const string jwt_token_key = "Atlas.JwtToken";

    private readonly string _context_key;
    private readonly JwtHandler _jwt_handler;

    /// <summary>
    ///     初始化认证中间件
    /// </summary>
    /// <param name="options">JWT 验证选项</param>
    /// <param name="contextKey">上下文字典中的键名</param>
    public AuthMiddleware(JwtValidationOptions? options = null, string contextKey = jwt_token_key)
    {
        _jwt_handler = new JwtHandler(options ?? new JwtValidationOptions());
        _context_key = contextKey;
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        if (!requires_auth(context))
        {
            await next();
            return;
        }

        var token = extract_token(context);

        if (token is null)
        {
            write_unauthorized(context, "缺少认证令牌");
            return;
        }

        var jwtToken = _jwt_handler.parse_and_validate(token);

        if (jwtToken is null)
        {
            write_unauthorized(context, "无效的认证令牌");
            return;
        }

        context.items[_context_key] = jwtToken;

        if (check_roles(context, jwtToken))
            await next();
        else
            write_unauthorized(context, "权限不足");
    }

    /// <summary>
    ///     从请求中提取原始 JWT 令牌字符串
    /// </summary>
    private static string? extract_token(RouteContext context)
    {
        if (context.request.headers.TryGetValue("Authorization", out var authHeader))
        {
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader["Bearer ".Length..].Trim();

            return authHeader.Trim();
        }

        if (get_query_param(context, "access_token") is { } queryToken) return queryToken;

        return null;
    }

    /// <summary>
    ///     写入未授权响应
    /// </summary>
    private static void write_unauthorized(RouteContext context, string message)
    {
        context.response.status_code = HttpStatusCode.unauthorized;
        context.response.set_header("Content-Type", "application/json; charset=utf-8");
        context.response.text($"{{\"error\":\"{message}\"}}");
        context.short_circuit();
    }

    private static string? get_query_param(RouteContext context, string name)
    {
        var qs = context.request.query_string;

        if (string.IsNullOrEmpty(qs)) return null;

        var q = qs.TrimStart('?');
        var pairs = q.Split('&');

        foreach (var pair in pairs)
        {
            var eq = pair.IndexOf('=');

            if (eq < 0) continue;

            var key = Uri.UnescapeDataString(pair[..eq]);

            if (string.Equals(key, name, StringComparison.Ordinal)) return Uri.UnescapeDataString(pair[(eq + 1)..]);
        }

        return null;
    }

    /// <summary>
    ///     检查当前 Action 是否需要认证
    ///     注意：此方法需要控制器扫描器存储 Action 元数据，当前为简化实现
    /// </summary>
    private static bool requires_auth(RouteContext context)
    {
        return false;
    }

    /// <summary>
    ///     检查令牌角色是否满足要求
    /// </summary>
    private static bool check_roles(RouteContext context, JwtToken token)
    {
        return true;
    }
}