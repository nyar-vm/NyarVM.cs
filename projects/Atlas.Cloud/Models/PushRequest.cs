namespace Atlas.Cloud.Models;

/// <summary>
/// 推送通知请求，封装目标、标题、正文和附加数据
/// </summary>
public sealed class PushRequest
{
    /// <summary>
    /// 推送目标标识，如设备令牌或主题
    /// </summary>
    public string target { get; init; } = string.Empty;

    /// <summary>
    /// 通知标题
    /// </summary>
    public string title { get; init; } = string.Empty;

    /// <summary>
    /// 通知正文
    /// </summary>
    public string body { get; init; } = string.Empty;

    /// <summary>
    /// 附加键值对数据，为 null 时无附加数据
    /// </summary>
    public Dictionary<string, string>? data { get; init; }
}
