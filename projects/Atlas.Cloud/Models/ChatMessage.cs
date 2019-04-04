namespace Atlas.Cloud.Models;

/// <summary>
/// 聊天消息，包含角色和文本内容
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    /// 消息角色，如 "user"、"assistant"、"system"
    /// </summary>
    public string role { get; init; } = string.Empty;

    /// <summary>
    /// 消息文本内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}