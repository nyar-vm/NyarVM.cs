namespace Std.Security.Authorization.Models;

/// <summary>
///     用户-角色关联模型，表示用户与角色的绑定关系。
/// </summary>
public sealed class UserRole
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
    ///     角色标识。
    /// </summary>
    public string role_id { get; set; } = string.Empty;

    /// <summary>
    ///     创建时间。
    /// </summary>
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}