using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.AI;

/// <summary>
///     AI 服务接口，提供文本补全和对话能力。
/// </summary>
public interface IAiService
{
    /// <summary>
    ///     执行文本补全。
    /// </summary>
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);

    /// <summary>
    ///     执行对话补全。
    /// </summary>
    Task<string> ChatAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default);
}

/// <summary>
///     聊天消息。
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    ///     消息角色。
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    ///     消息内容。
    /// </summary>
    public required string Content { get; set; }
}