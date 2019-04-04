using Core.Security.Authentication;

namespace Std.Security.Authentication;

/// <summary>
///     <see cref="IClaimsPrincipal" /> 的扩展方法，提供角色检查等便捷操作。
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    ///     检查声明主体是否属于指定角色。
    /// </summary>
    /// <param name="principal">声明主体。</param>
    /// <param name="role">角色名称。</param>
    /// <returns>如果主体属于该角色则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool is_in_role(this IClaimsPrincipal principal, string role)
    {
        return principal.claims.Any(c => c.type == "role" && c.value == role);
    }
}