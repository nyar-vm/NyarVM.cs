namespace Core.Security.Authorization;

/// <summary>
///     IPermissionSet 接口
/// </summary>
public interface IPermissionSet
{
    /// <summary>
    ///     判断是否拥有指定权限
    /// </summary>
    bool has_permission(string permission);
}