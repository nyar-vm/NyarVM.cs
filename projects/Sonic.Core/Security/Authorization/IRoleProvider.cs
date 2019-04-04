namespace Core.Security.Authorization;

/// <summary>
///     IRoleProvider 接口
/// </summary>
public interface IRoleProvider
{
    /// <summary>
    ///     获取角色的权限集合
    /// </summary>
    IPermissionSet get_permissions(string role);
}