namespace Std.App.Server.WebSocket;

/// <summary>
///     WebSocket 处理器抽象基类，提供连接生命周期管理
/// </summary>
public abstract class WebSocketHandler
{
    /// <summary>
    ///     连接唯一标识，自动生成 GUID
    /// </summary>
    public string connection_id { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     连接建立时调用
    /// </summary>
    public virtual Task on_connected()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     收到消息时调用
    /// </summary>
    /// <param name="message">消息文本</param>
    public abstract Task on_message(string message);

    /// <summary>
    ///     连接断开时调用
    /// </summary>
    public virtual Task on_disconnected()
    {
        return Task.CompletedTask;
    }
}