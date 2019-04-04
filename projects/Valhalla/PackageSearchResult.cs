namespace Valhalla;

/// <summary>
///     包搜索结果
/// </summary>
public class PackageSearchResult
{
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string latest_version { get; set; } = string.Empty;
    public DateTime published_at { get; set; }
}