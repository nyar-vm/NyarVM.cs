namespace Nyar.PackageRegistry;

/// <summary>
///     发布结果
/// </summary>
public class PublishResult
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     包名称
    /// </summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>
    ///     版本号
    /// </summary>
    public string version { get; set; } = string.Empty;

    /// <summary>
    ///     结果消息
    /// </summary>
    public string message { get; set; } = string.Empty;

    /// <summary>
    ///     发布后的 URL
    /// </summary>
    public string? published_url { get; set; }
}