using System.Text.Json.Serialization;

namespace Valhalla;

/// <summary>
///     包发布响应
/// </summary>
public class PublishResponse
{
    /// <summary>
    ///     是否发布成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool success { get; set; }

    /// <summary>
    ///     响应消息
    /// </summary>
    [JsonPropertyName("message")]
    public string message { get; set; } = string.Empty;

    /// <summary>
    ///     错误详情（仅在失败时有值）
    /// </summary>
    [JsonPropertyName("error")]
    public string? error { get; set; }

    /// <summary>
    ///     发布后的包名称
    /// </summary>
    [JsonPropertyName("packageName")]
    public string? package_name { get; set; }

    /// <summary>
    ///     发布后的版本号
    /// </summary>
    [JsonPropertyName("version")]
    public string? version { get; set; }
}