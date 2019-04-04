namespace Std.Net.Http;

/// <summary>
///     标记接口为 HTTP 客户端，指定基础 URL、配置节和超时时间
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class HttpClientAttribute : Attribute
{
    /// <summary>
    ///     基础 URL 地址
    /// </summary>
    public string? base_url { get; set; }

    /// <summary>
    ///     配置节名称
    /// </summary>
    public string? configuration_section { get; set; }

    /// <summary>
    ///     请求超时时间（秒），默认为 30 秒
    /// </summary>
    public int timeout { get; set; } = 30;
}