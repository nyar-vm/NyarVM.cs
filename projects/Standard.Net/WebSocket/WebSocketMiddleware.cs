using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Std.DataProcess.Write;
using Std.Net.Http;
using HttpMethod = Std.Net.Http.HttpMethod;

namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 中间件，拦截 WebSocket 升级请求并路由到对应的处理器。
///     实现 <see cref="IMiddleware" /> 接口，在 Sonic.Standard 中间件管线中执行。
/// </summary>
public sealed class WebSocketMiddleware : IMiddleware
{
    /// <summary>
    ///     WebSocket 协议 GUID（RFC 6455 第 4.2.2 节）。
    /// </summary>
    private const string _web_socket_guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private readonly WebSocketManager _manager;
    private readonly WebSocketRouteTable _route_table;

    /// <summary>
    ///     初始化 WebSocket 中间件。
    /// </summary>
    /// <param name="manager">WebSocket 连接管理器。</param>
    public WebSocketMiddleware(WebSocketManager manager)
    {
        _manager = manager;
        _route_table = new WebSocketRouteTable();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        if (!is_web_socket_upgrade_request(context))
        {
            await next();
            return;
        }

        var path = context.request.path;
        var handlerFactory = _route_table.match(path);

        if (handlerFactory is null)
        {
            context.response.status_code = HttpStatusCode.not_found;
            return;
        }

        if (!try_get_web_socket_key(context, out var key))
        {
            context.response.status_code = HttpStatusCode.bad_request;
            return;
        }

        var acceptKey = compute_accept_key(key);

        context.response.status_code = HttpStatusCode.switching_protocols;
        context.response.headers["Upgrade"] = "websocket";
        context.response.headers["Connection"] = "Upgrade";
        context.response.headers["Sec-WebSocket-Accept"] = acceptKey;

        var socket = context.request.connection;

        if (socket is null)
        {
            context.response.status_code = HttpStatusCode.internal_server_error;
            return;
        }

        var writer = new ArrayBufferWriter<byte>();
        context.response.write_to(writer);
        await socket.SendAsync(writer.written_memory, SocketFlags.None);

        var connectionId = Guid.NewGuid().ToString("N");
        var handler = handlerFactory();

        var connection = new WebSocketConnection(
            connectionId,
            path,
            socket,
            handler,
            _manager);

        context.request.is_hijacked = true;
        context.short_circuit();

        await connection.run_receive_loop();
    }

    /// <summary>
    ///     将指定路径映射到 WebSocket 处理器。
    /// </summary>
    /// <param name="path">端点路径（如 "/ws/chat"）。</param>
    /// <param name="handlerFactory">处理器工厂函数。</param>
    public void map(string path, Func<IWebSocketHandler> handlerFactory)
    {
        _route_table.map(path, handlerFactory);
    }

    private static bool is_web_socket_upgrade_request(RouteContext context)
    {
        return context.request.method == HttpMethod.get
               && context.request.headers.TryGetValue("Upgrade", out var upgrade)
               && upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase);
    }

    private static bool try_get_web_socket_key(RouteContext context, out string key)
    {
        key = string.Empty;

        if (!context.request.headers.TryGetValue("Sec-WebSocket-Key", out var rawKey)) return false;

        key = rawKey;
        return !string.IsNullOrWhiteSpace(key);
    }

    /// <summary>
    ///     计算 Sec-WebSocket-Accept 响应头值。
    /// </summary>
    /// <param name="secWebSocketKey">客户端发来的 Sec-WebSocket-Key。</param>
    /// <returns>Base64 编码的 SHA-1 哈希值。</returns>
    private static string compute_accept_key(string secWebSocketKey)
    {
        var combined = secWebSocketKey + _web_socket_guid;
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
///     WebSocket 路由表，管理路径到处理器工厂的映射。
/// </summary>
public sealed class WebSocketRouteTable
{
    private readonly Dictionary<string, Func<IWebSocketHandler>> _routes = new();

    /// <summary>
    ///     已注册的路由数量。
    /// </summary>
    public int count => _routes.Count;

    /// <summary>
    ///     注册一个路由。
    /// </summary>
    /// <param name="path">端点路径。</param>
    /// <param name="handlerFactory">处理器工厂函数。</param>
    public void map(string path, Func<IWebSocketHandler> handlerFactory)
    {
        _routes[path] = handlerFactory;
    }

    /// <summary>
    ///     根据路径查找匹配的处理器工厂。
    /// </summary>
    /// <param name="path">请求路径。</param>
    /// <returns>处理器工厂函数，未匹配返回 <c>null</c>。</returns>
    public Func<IWebSocketHandler>? match(string path)
    {
        return _routes.GetValueOrDefault(path);
    }
}