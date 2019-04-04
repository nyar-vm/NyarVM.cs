using System.Net.Sockets;
using System.Text;
using Std.Data.Protocol.WebSocket;
using Std.DataProcess.Write;

namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 连接实现，封装单个 WebSocket 连接的帧读写和生命周期。
///     使用 <see cref="WebSocketDeframer" /> 解码入站帧，<see cref="WebSocketEnframer" /> 编码出站帧。
/// </summary>
public sealed class WebSocketConnection : IWebSocketContext
{
    private readonly CancellationTokenSource _connection_cts;
    private readonly IWebSocketHandler _handler;
    private readonly WebSocketManager _manager;
    private readonly object _send_lock = new();
    private readonly Socket _socket;
    private volatile bool _is_open = true;

    /// <summary>
    ///     初始化 WebSocket 连接。
    /// </summary>
    /// <param name="connectionId">连接唯一标识。</param>
    /// <param name="path">端点路径。</param>
    /// <param name="socket">底层 Socket 连接。</param>
    /// <param name="handler">事件处理器。</param>
    /// <param name="manager">连接管理器。</param>
    public WebSocketConnection(
        string connectionId,
        string path,
        Socket socket,
        IWebSocketHandler handler,
        WebSocketManager manager)
    {
        connection_id = connectionId;
        this.path = path;
        _socket = socket;
        _handler = handler;
        _manager = manager;
        _connection_cts = new CancellationTokenSource();
    }

    /// <inheritdoc />
    public string connection_id { get; }

    /// <inheritdoc />
    public string path { get; }

    /// <inheritdoc />
    public bool is_open => _is_open;

    /// <inheritdoc />
    public WebSocketCloseStatus? close_status { get; private set; }

    /// <inheritdoc />
    public string? close_status_description { get; private set; }

    /// <inheritdoc />
    public async Task send_text(string message, CancellationToken cancellationToken = default)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        var enframer = new WebSocketEnframer(true);
        var writer = new ArrayBufferWriter<byte>();
        enframer.enframe(payload, writer);
        await send_frame([.. writer.written_span], cancellationToken);
    }

    /// <inheritdoc />
    public async Task send_binary(byte[] data, CancellationToken cancellationToken = default)
    {
        var enframer = new WebSocketEnframer(false);
        var writer = new ArrayBufferWriter<byte>();
        enframer.enframe(data, writer);
        await send_frame([.. writer.written_span], cancellationToken);
    }

    /// <inheritdoc />
    public async Task close(
        WebSocketCloseStatus status = WebSocketCloseStatus.normal_closure,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (!_is_open) return;

        var closeFrame = encode_close_frame(status, description);

        try
        {
            await send_frame(closeFrame, cancellationToken);
        }
        catch
        {
        }

        await close_connection(status, description);
    }

    /// <summary>
    ///     启动接收循环，从 Socket 读取数据并分发给处理器。
    /// </summary>
    public async Task run_receive_loop()
    {
        _manager.register_connection(this);

        try
        {
            await _handler.OnConnectedAsync(this);

            using var deframer = new WebSocketDeframer(true);
            var buffer = new byte[4096];

            while (_is_open && _socket.Connected)
            {
                var received = await _socket.ReceiveAsync(buffer, SocketFlags.None, _connection_cts.Token);

                if (received == 0) break;

                deframer.feed(buffer.AsSpan(0, received));

                while (deframer.try_get_next_frame())
                {
                    var payload = deframer.current_frame.ToArray();
                    await handle_frame(deframer.current_opcode, payload);
                }
            }
        }
        catch (SocketException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await close_connection(WebSocketCloseStatus.abnormal_closure, "连接意外中断");
        }
    }

    private async Task handle_frame(int opcode, byte[] payload)
    {
        switch (opcode)
        {
            case 0x1:
                var text = Encoding.UTF8.GetString(payload);
                await _handler.OnTextMessageAsync(this, text);
                break;
            case 0x2:
                await _handler.OnBinaryMessageAsync(this, payload);
                break;
            case 0x8:
                var closeStatus = WebSocketCloseStatus.normal_closure;

                if (payload.Length >= 2)
                {
                    var statusBytes = new byte[2];
                    Buffer.BlockCopy(payload, 0, statusBytes, 0, 2);

                    if (BitConverter.IsLittleEndian) Array.Reverse(statusBytes);

                    closeStatus = (WebSocketCloseStatus)BitConverter.ToUInt16(statusBytes, 0);
                }

                await close_connection(closeStatus,
                    payload.Length > 2 ? Encoding.UTF8.GetString(payload, 2, payload.Length - 2) : "");
                break;
            case 0x9:
                var pongFrame = encode_pong_frame(payload);
                await send_frame(pongFrame, default);
                break;
            case 0xA:
                break;
        }
    }

    private async Task send_frame(byte[] frame, CancellationToken cancellationToken)
    {
        lock (_send_lock)
        {
            _socket.Send(frame, SocketFlags.None);
        }

        await Task.CompletedTask;
    }

    private async Task close_connection(WebSocketCloseStatus status, string? description)
    {
        if (!_is_open) return;

        _is_open = false;
        close_status = status;
        close_status_description = description;

        _manager.remove_connection(connection_id);

        try
        {
            await _handler.OnDisconnectedAsync(this);
        }
        catch
        {
        }

        await _connection_cts.CancelAsync();
        _connection_cts.Dispose();

        try
        {
            _socket.Close();
        }
        catch
        {
        }
    }

    private static byte[] encode_close_frame(WebSocketCloseStatus status, string? description)
    {
        var statusBytes = BitConverter.GetBytes((ushort)status);

        if (BitConverter.IsLittleEndian) Array.Reverse(statusBytes);

        byte[] payload;

        if (description is not null)
        {
            var descBytes = Encoding.UTF8.GetBytes(description);
            payload = new byte[statusBytes.Length + descBytes.Length];
            Buffer.BlockCopy(statusBytes, 0, payload, 0, statusBytes.Length);
            Buffer.BlockCopy(descBytes, 0, payload, statusBytes.Length, descBytes.Length);
        }
        else
        {
            payload = statusBytes;
        }

        using var stream = new MemoryStream();
        stream.WriteByte(0x88);

        if (payload.Length <= 125)
        {
            stream.WriteByte((byte)payload.Length);
        }
        else if (payload.Length <= 65535)
        {
            stream.WriteByte(126);
            stream.WriteByte((byte)((payload.Length >> 8) & 0xFF));
            stream.WriteByte((byte)(payload.Length & 0xFF));
        }

        stream.Write(payload, 0, payload.Length);
        return stream.ToArray();
    }

    private static byte[] encode_pong_frame(byte[] pingPayload)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0x8A);

        if (pingPayload.Length <= 125)
        {
            stream.WriteByte((byte)pingPayload.Length);
        }
        else if (pingPayload.Length <= 65535)
        {
            stream.WriteByte(126);
            stream.WriteByte((byte)((pingPayload.Length >> 8) & 0xFF));
            stream.WriteByte((byte)(pingPayload.Length & 0xFF));
        }

        stream.Write(pingPayload, 0, pingPayload.Length);
        return stream.ToArray();
    }
}