namespace Atlas.Cloud.Models;

/// <summary>
/// 聊天补全响应，包含生成的消息和用量信息
/// </summary>
public sealed class ChatResponse
{
    /// <summary>
    /// 响应唯一标识
    /// </summary>
    public string id { get; init; } = string.Empty;

    /// <summary>
    /// 实际使用的模型名称
    /// </summary>
    public string model { get; init; } = string.Empty;

    /// <summary>
    /// 生成的消息
    /// </summary>
    public ChatMessage message { get; init; } = new();

    /// <summary>
    /// 完成原因，如 "stop"、"length"
    /// </summary>
    public string finish_reason { get; init; } = string.Empty;

    /// <summary>
    /// 总令牌用量
    /// </summary>
    public int usage_total_tokens { get; init; }
}