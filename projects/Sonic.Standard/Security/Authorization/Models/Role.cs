namespace Std.Security.Authorization.Models;

/// <summary>
///     角色模型，表示系统中的一个角色。
/// </summary>
public sealed class Role
{
    /// <summary>
    ///     角色唯一标识。
    /// </summary>
    public string id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     角色名称。
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     角色描述。
    /// </summary>
    public string description { get; set; } = string.Empty;

    /// <summary>
    ///     创建时间。
    /// </summary>
    public DateTime created_at { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     是否启用。
    /// </summary>
    public bool is_enabled { get; set; } = true;
}