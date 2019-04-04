namespace Valhalla.Client;

/// <summary>
///     包摘要信息（用于列表展示）
/// </summary>
public class PackageSummary
{
    /// <summary>规范包名</summary>
    public string name { get; set; } = string.Empty;

    /// <summary>包描述</summary>
    public string description { get; set; } = string.Empty;

    /// <summary>最新版本号</summary>
    public string latest_version { get; set; } = string.Empty;

    /// <summary>发布者公钥指纹</summary>
    public string publisher { get; set; } = string.Empty;

    /// <summary>化身编号</summary>
    public int incarnation { get; set; }

    /// <summary>包状态</summary>
    public string status { get; set; } = "active";

    /// <summary>下载计数</summary>
    public long download_count { get; set; }

    /// <summary>创建时间</summary>
    public DateTime created_at { get; set; }

    /// <summary>更新时间</summary>
    public DateTime updated_at { get; set; }
}