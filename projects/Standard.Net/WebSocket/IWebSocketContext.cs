namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 连接上下文，提供单个连接的生命周期和消息收发能力。
/// </summary>
public interface IWebSocketContext
{
    /// <summary>
    ///     连接的唯一标识。
    /// </summary>
    string connection_id { get; }

    /// <summary>
    ///     连接建立时匹配的路径。
    /// </summary>
    string path { get; }

    /// <summary>
    ///     连接是否仍然活跃。
    /// </summary>
    bool is_open { get; }

    /// <summary>
    ///     关闭原因（仅在 IsOpen 为 false 时有效）。
    /// </summary>
    WebSocketCloseStatus? close_status { get; }

    /// <summary>
    ///     关闭描述信息。
    /// </summary>
    string? close_status_description { get; }

    /// <summary>
    ///     发送文本消息到客户端。
    /// </summary>
    Task send_text(string message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送二进制消息到客户端。
    /// </summary>
    Task send_binary(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    ///     关闭连接。
    /// </summary>
    Task close(WebSocketCloseStatus status = WebSocketCloseStatus.normal_closure, string? description = null,
        CancellationToken cancellationToken = default);
}