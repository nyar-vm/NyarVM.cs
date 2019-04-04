using Core.Security.Authorization;

namespace Std.Security.Authorization;

/// <summary>
///     授权上下文，提供权限检查能力。
/// </summary>
public static class AuthorizationContext
{
    private static IPermissionSet? _permission_set;

    /// <summary>
    ///     检查当前主体是否拥有指定权限。
    /// </summary>
    /// <param name="permission">权限名称。</param>
    /// <returns>如果拥有该权限则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool check_permission(string permission)
    {
        return _permission_set?.has_permission(permission) ?? false;
    }

    /// <summary>
    ///     设置当前权限集合。
    /// </summary>
    /// <param name="permissionSet">权限集合。</param>
    public static void set_permission_set(IPermissionSet? permissionSet)
    {
        _permission_set = permissionSet;
    }
}