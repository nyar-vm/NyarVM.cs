using System.Security.Claims;

namespace Hermes.Rpc.Middleware.Authentication;

/// <summary>
///     RPC 认证中间件——验证请求中的身份令牌
/// </summary>
public sealed class AuthenticationMiddleware : IHermesRpcMiddleware
{
    private readonly IAuthenticationHandler _handler;

    public AuthenticationMiddleware(IAuthenticationHandler handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task HandleAsync(HttpContext context, Func<Task> next)
    {
        var token = ExtractToken(context);
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("缺少认证令牌");
            return;
        }

        var principal = await _handler.ValidateTokenAsync(token, context.RequestAborted);
        if (principal == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("认证令牌无效");
            return;
        }

        context.User = principal;
        await next();
    }

    private static string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return authHeader[7..].Trim();

        if (context.Request.Headers.TryGetValue("X-Auth-Token", out var tokenHeader)) return tokenHeader.ToString();

        if (context.Request.Query.TryGetValue("token", out var queryToken)) return queryToken.ToString();

        return null;
    }
}

/// <summary>
///     认证处理器接口
/// </summary>
public interface IAuthenticationHandler
{
    /// <summary>
    ///     验证令牌并返回用户主体
    /// </summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}