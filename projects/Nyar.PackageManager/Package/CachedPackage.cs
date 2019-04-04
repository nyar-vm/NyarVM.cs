namespace Nyar.PackageManager.Package;

/// <summary>
///     已缓存的包条目
/// </summary>
public class CachedPackage
{
    /// <summary>
    ///     包名称
    /// </summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>
    ///     版本号
    /// </summary>
    public string version { get; set; } = string.Empty;

    /// <summary>
    ///     本地存储路径
    /// </summary>
    public string local_path { get; set; } = string.Empty;

    /// <summary>
    ///     缓存时间
    /// </summary>
    public DateTime cached_at { get; set; } = DateTime.UtcNow;
}