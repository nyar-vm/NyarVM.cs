using Core.Security.Authentication;

namespace Std.Security.Authentication;

/// <summary>
///     身份验证上下文，提供当前线程的主体和用户标识访问。
/// </summary>
public static class AuthContext
{
    private static readonly AsyncLocal<IClaimsPrincipal?> _current = new();

    /// <summary>
    ///     获取当前线程的声明主体。
    /// </summary>
    /// <returns>当前声明主体，未认证时返回 <c>null</c>。</returns>
    public static IClaimsPrincipal? current_principal()
    {
        return _current.Value;
    }

    /// <summary>
    ///     获取当前线程的用户标识。
    /// </summary>
    /// <returns>用户标识字符串，未认证时返回 <c>null</c>。</returns>
    public static string? current_user_id()
    {
        return _current.Value?.claims?
            .FirstOrDefault(c => c.type == "sub")?.value;
    }

    /// <summary>
    ///     设置当前线程的声明主体。
    /// </summary>
    /// <param name="principal">声明主体。</param>
    public static void set_principal(IClaimsPrincipal? principal)
    {
        _current.Value = principal;
    }
}