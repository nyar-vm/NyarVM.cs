namespace Valhalla;

/// <summary>
///     包版本清单，对应 manifest.json
/// </summary>
public class PackageManifest
{
    /// <summary>规范包名</summary>
    public string name { get; set; } = string.Empty;

    /// <summary>当前化身编号</summary>
    public int incarnation { get; set; } = 1;

    /// <summary>发布者公钥指纹</summary>
    public string publisher { get; set; } = string.Empty;

    /// <summary>根组织路径（如果属于某组织）</summary>
    public string? namespace_root { get; set; }

    /// <summary>首次注册时间</summary>
    public DateTime registered_at { get; set; } = DateTime.UtcNow;

    /// <summary>包状态</summary>
    public PackageStatus status { get; set; } = PackageStatus.active;

    /// <summary>PURGE 原因</summary>
    public string? purge_reason { get; set; }

    /// <summary>PURGE 时间</summary>
    public DateTime? purged_at { get; set; }

    /// <summary>所有版本（键为版本号）</summary>
    public Dictionary<string, VersionEntry> versions { get; set; } = new();
}