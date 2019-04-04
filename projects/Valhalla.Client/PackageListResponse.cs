namespace Valhalla.Client;

/// <summary>
///     包列表页响应
/// </summary>
public class PackageListResponse
{
    /// <summary>包列表</summary>
    public List<PackageSummary> packages { get; set; } = [];

    /// <summary>总数</summary>
    public int total { get; set; }

    /// <summary>当前页码</summary>
    public int page { get; set; }

    /// <summary>每页大小</summary>
    public int size { get; set; }
}