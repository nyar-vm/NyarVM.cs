namespace Std.Security.Authorization.Models;

/// <summary>
///     权限模型，表示系统中的一项权限。
/// </summary>
public sealed class Permission
{
    /// <summary>
    ///     权限唯一标识。
    /// </summary>
    public string id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     权限名称。
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     权限描述。
    /// </summary>
    public string description { get; set; } = string.Empty;

    /// <summary>
    ///     权限关联的资源。
    /// </summary>
    public string resource { get; set; } = string.Empty;

    /// <summary>
    ///     权限关联的操作。
    /// </summary>
    public string action { get; set; } = string.Empty;

    /// <summary>
    ///     创建时间。
    /// </summary>
    public DateTime created_at { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     是否启用。
    /// </summary>
    public bool is_enabled { get; set; } = true;
}