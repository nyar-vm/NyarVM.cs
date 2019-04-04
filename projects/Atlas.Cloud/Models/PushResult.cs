namespace Atlas.Cloud.Models;

/// <summary>
/// 推送通知结果，包含发送状态和消息标识
/// </summary>
public sealed class PushResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool success { get; init; }

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 消息唯一标识，成功时由推送平台返回
    /// </summary>
    public string? message_id { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="messageId">消息唯一标识</param>
    public static PushResult ok(string? messageId = null) =>
        new() { success = true, message_id = messageId };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    public static PushResult fail(string error) =>
        new() { success = false, error = error };
}
