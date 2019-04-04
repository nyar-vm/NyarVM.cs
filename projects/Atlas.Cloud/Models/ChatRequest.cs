namespace Atlas.Cloud.Models;

/// <summary>
/// 聊天补全请求，封装模型、消息和生成参数
/// </summary>
public sealed class ChatRequest
{
    /// <summary>
    /// 模型名称，如 "gpt-4"
    /// </summary>
    public string model { get; init; } = string.Empty;

    /// <summary>
    /// 对话消息列表
    /// </summary>
    public List<ChatMessage> messages { get; init; } = [];

    /// <summary>
    /// 采样温度，范围 0~2，默认 0.7
    /// </summary>
    public double temperature { get; init; } = 0.7;

    /// <summary>
    /// 最大生成令牌数，默认 1024
    /// </summary>
    public int max_tokens { get; init; } = 1024;
}