using Std.Security;

namespace Std.Net.Security;

/// <summary>
///     JWT 认证中间件。从 Authorization 头提取 Bearer 令牌并验证。
/// </summary>
public sealed class JwtAuthMiddleware : IMiddleware
{
    private readonly JwtHandler _jwt_handler;

    /// <summary>
    ///     初始化 JWT 认证中间件。
    /// </summary>
    /// <param name="options">JWT 验证选项</param>
    public JwtAuthMiddleware(JwtValidationOptions? options = null)
    {
        _jwt_handler = new JwtHandler(options);
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        context.request.headers.TryGetValue("Authorization", out var authHeader);
        var token = _jwt_handler.parse_from_header(authHeader);

        if (token != null)
            context.items["Security.User"] = new SonicPrincipal
            {
                subject = token.subject ?? string.Empty,
                name = token.name ?? string.Empty,
                roles = token.roles,
                is_authenticated = true,
                auth_type = "JWT"
            };

        await next();
    }
}