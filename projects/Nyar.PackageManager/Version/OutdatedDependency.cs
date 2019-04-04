namespace Nyar.PackageManager.Version;

/// <summary>
///     过期依赖信息
/// </summary>
public class OutdatedDependency
{
    /// <summary>
    ///     包名
    /// </summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>
    ///     当前安装版本
    /// </summary>
    public string current_version { get; set; } = string.Empty;

    /// <summary>
    ///     最新可用版本
    /// </summary>
    public string latest_version { get; set; } = string.Empty;

    /// <summary>
    ///     legion.von 中的版本约束
    /// </summary>
    public string constraint { get; set; } = string.Empty;

    /// <summary>
    ///     是否为重大更新（主版本号不同）
    /// </summary>
    public bool is_major_update
    {
        get
        {
            var currentParts = current_version.Split('.');
            var latestParts = latest_version.Split('.');

            if (currentParts.Length > 0 && latestParts.Length > 0) return currentParts[0] != latestParts[0];

            return false;
        }
    }
}