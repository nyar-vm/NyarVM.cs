namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 事件处理器接口，每个路径对应一个处理器实例。
/// </summary>
public interface IWebSocketHandler
{
    /// <summary>
    ///     当新客户端连接建立时调用。
    /// </summary>
    Task OnConnectedAsync(IWebSocketContext context);

    /// <summary>
    ///     当收到文本消息时调用。
    /// </summary>
    Task OnTextMessageAsync(IWebSocketContext context, string message);

    /// <summary>
    ///     当收到二进制消息时调用。
    /// </summary>
    Task OnBinaryMessageAsync(IWebSocketContext context, byte[] data);

    /// <summary>
    ///     当连接关闭时调用。
    /// </summary>
    Task OnDisconnectedAsync(IWebSocketContext context);
}