namespace Valhalla;

/// <summary>
///     包级元信息（不参与校验，可随时修改）
/// </summary>
public class ValhallaPackageMeta
{
    /// <summary>包描述</summary>
    public string description { get; set; } = string.Empty;

    /// <summary>标签列表</summary>
    public List<string> tags { get; set; } = [];

    /// <summary>分类列表</summary>
    public List<string> categories { get; set; } = [];

    /// <summary>项目主页</summary>
    public string? homepage { get; set; }

    /// <summary>代码仓库 URL</summary>
    public string? repository { get; set; }

    /// <summary>许可证</summary>
    public string? license { get; set; }

    /// <summary>图标 URL</summary>
    public string? icon { get; set; }

    /// <summary>自定义链接</summary>
    public Dictionary<string, string> links { get; set; } = new();

    /// <summary>创建时间</summary>
    public DateTime created_at { get; set; } = DateTime.UtcNow;

    /// <summary>最后修改时间</summary>
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
}