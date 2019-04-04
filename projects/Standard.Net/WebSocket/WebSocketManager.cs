using System.Collections.Concurrent;

namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 连接管理器，负责连接的注册、查找和生命周期管理。
/// </summary>
public sealed class WebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();

    /// <summary>
    ///     当前活跃连接数。
    /// </summary>
    public int connection_count => _connections.Count;

    /// <summary>
    ///     注册一个新连接。
    /// </summary>
    /// <param name="connection">WebSocket 连接实例。</param>
    public void register_connection(WebSocketConnection connection)
    {
        _connections.TryAdd(connection.connection_id, connection);
    }

    /// <summary>
    ///     移除一个连接（当连接关闭时）。
    /// </summary>
    /// <param name="connectionId">连接 ID。</param>
    public void remove_connection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    /// <summary>
    ///     根据连接 ID 查找连接。
    /// </summary>
    /// <param name="connectionId">连接 ID。</param>
    /// <returns>连接实例，未找到返回 <c>null</c>。</returns>
    public WebSocketConnection? get_connection(string connectionId)
    {
        return _connections.GetValueOrDefault(connectionId);
    }

    /// <summary>
    ///     获取指定路径上的所有连接。
    /// </summary>
    /// <param name="path">WebSocket 端点路径。</param>
    /// <returns>连接列表。</returns>
    public IReadOnlyList<IWebSocketContext> get_connections_by_path(string path)
    {
        return
        [
            .. _connections.Values
                .Where(c => c.path == path && c.is_open)
                .Cast<IWebSocketContext>()
        ];
    }

    /// <summary>
    ///     向指定路径上的所有连接广播文本消息。
    /// </summary>
    /// <param name="path">目标路径。</param>
    /// <param name="message">文本消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task broadcast_text(string path, string message, CancellationToken cancellationToken = default)
    {
        var connections = get_connections_by_path(path);
        var tasks = connections.Select(c => c.send_text(message, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    ///     向所有活跃连接广播文本消息。
    /// </summary>
    /// <param name="message">文本消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task broadcast_all(string message, CancellationToken cancellationToken = default)
    {
        var tasks = _connections.Values
            .Where(c => c.is_open)
            .Select(c => c.send_text(message, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    ///     优雅关闭所有连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task shutdown(CancellationToken cancellationToken = default)
    {
        var tasks = _connections.Values
            .Select(c => c.close(WebSocketCloseStatus.going_away, "服务器正在关闭", cancellationToken));
        await Task.WhenAll(tasks);
        _connections.Clear();
    }
}