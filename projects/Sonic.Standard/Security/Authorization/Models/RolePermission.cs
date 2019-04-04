namespace Std.Security.Authorization.Models;

/// <summary>
///     角色-权限关联模型，表示角色与权限的绑定关系。
/// </summary>
public sealed class RolePermission
{
    /// <summary>
    ///     关联唯一标识。
    /// </summary>
    public string id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     角色标识。
    /// </summary>
    public string role_id { get; set; } = string.Empty;

    /// <summary>
    ///     权限标识。
    /// </summary>
    public string permission_id { get; set; } = string.Empty;

    /// <summary>
    ///     创建时间。
    /// </summary>
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}