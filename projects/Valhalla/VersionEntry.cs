namespace Valhalla;

/// <summary>
///     单个版本条目
/// </summary>
public class VersionEntry
{
    /// <summary>版本号</summary>
    public string version { get; set; } = string.Empty;

    /// <summary>版本状态</summary>
    public VersionStatus status { get; set; } = VersionStatus.active;

    /// <summary>屏蔽原因（如果状态为 Shielded）</summary>
    public string? shield_reason { get; set; }

    /// <summary>屏蔽时间</summary>
    public DateTime? shielded_at { get; set; }

    /// <summary>.nyar 的 SHA-256</summary>
    public string package_digest { get; set; } = string.Empty;

    /// <summary>源码包的 SHA-256</summary>
    public string? source_digest { get; set; }

    /// <summary>.nyar 文件大小</summary>
    public long package_size { get; set; }

    /// <summary>源码包文件大小</summary>
    public long? source_size { get; set; }

    /// <summary>发布时间</summary>
    public DateTime published_at { get; set; } = DateTime.UtcNow;
}