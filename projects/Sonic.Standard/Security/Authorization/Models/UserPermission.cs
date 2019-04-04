namespace Std.Security.Authorization.Models;

/// <summary>
///     用户-权限关联模型，表示用户与权限的直接绑定关系。
/// </summary>
public sealed class UserPermission
{
    /// <summary>
    ///     关联唯一标识。
    /// </summary>
    public string id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     用户标识。
    /// </summary>
    public string user_id { get; set; } = string.Empty;

    /// <summary>
    ///     权限标识。
    /// </summary>
    public string permission_id { get; set; } = string.Empty;

    /// <summary>
    ///     创建时间。
    /// </summary>
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}