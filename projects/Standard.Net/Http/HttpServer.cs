using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Std.Data.Protocol.Http;
using Std.DataProcess.Write;
using Std.Text;

namespace Std.Net.Http;

/// <summary>
///     HTTP/1.1 服务器，监听 TCP 连接并使用 <see cref="HttpFrameDeframer" /> 解析请求。
///     通过 <see cref="NetApp" /> 处理请求后使用 <see cref="HttpResponse.write_to" /> 发送响应。
///     支持优雅关闭、连接跟踪和 WebSocket 连接劫持。
/// </summary>
public sealed class HttpServer : IDisposable
{
    private readonly ConcurrentDictionary<string, Socket> _active_connections = new();
    private readonly NetApp _app;
    private readonly string _host;
    private readonly int _port;
    private readonly CancellationTokenSource _shutdown_cts = new();
    private int _connection_counter;
    private CancellationTokenSource? _cts;
    private Socket? _listener;

    /// <summary>
    ///     初始化一个新的 HTTP 服务器实例。
    /// </summary>
    /// <param name="app">Net 应用实例。</param>
    /// <param name="port">监听端口。</param>
    /// <param name="host">监听地址，默认 <c>127.0.0.1</c>。</param>
    public HttpServer(NetApp app, int port, string host = "127.0.0.1")
    {
        _app = app;
        _port = port;
        _host = host;
    }

    /// <summary>
    ///     当前活跃连接数。
    /// </summary>
    public int active_connection_count => _active_connections.Count;

    /// <inheritdoc />
    public void Dispose()
    {
        stop();
        _cts?.Dispose();
        _listener?.Dispose();
        _shutdown_cts.Dispose();
    }

    /// <summary>
    ///     启动服务器，开始监听连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async ValueTask start(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
        _listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Bind(endpoint);
        _listener.Listen(128);

        while (!_cts.Token.IsCancellationRequested)
            try
            {
                var client = await _listener.AcceptAsync(_cts.Token);
                _ = handle_connection(client, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
    }

    /// <summary>
    ///     停止服务器。
    /// </summary>
    public void stop()
    {
        _cts?.Cancel();
        _listener?.Close();
    }

    /// <summary>
    ///     优雅关闭服务器，等待所有活跃连接完成或超时。
    /// </summary>
    /// <param name="timeout">最大等待时间。</param>
    public async Task stop(TimeSpan timeout)
    {
        await _shutdown_cts.CancelAsync();
        stop();

        if (_active_connections.IsEmpty) return;

        using var cts = new CancellationTokenSource(timeout);

        try
        {
            while (!_active_connections.IsEmpty && !cts.Token.IsCancellationRequested) await Task.Delay(100, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        foreach (var (_, socket) in _active_connections)
            try
            {
                socket.Close();
            }
            catch
            {
            }

        _active_connections.Clear();
    }

    private async ValueTask handle_connection(Socket client, CancellationToken ct)
    {
        var connectionId = Interlocked.Increment(ref _connection_counter).ToString();
        _active_connections.TryAdd(connectionId, client);

        try
        {
            using var unframer = new HttpFrameDeframer();
            var buffer = new byte[4096];

            while (!ct.IsCancellationRequested && !_shutdown_cts.IsCancellationRequested && client.Connected)
            {
                var received = await client.ReceiveAsync(buffer, SocketFlags.None, ct);

                if (received == 0) break;

                unframer.feed(buffer.AsSpan(0, received));

                while (unframer.try_get_next_frame())
                {
                    var frameData = unframer.current_frame.ToArray();
                    var request = parse_request(frameData);

                    if (request == null) break;

                    request.connection = client;

                    await _app.handle(request);

                    if (request.is_hijacked) return;

                    var writer = new ArrayBufferWriter<byte>();
                    request.response.write_to(writer);
                    await client.SendAsync(writer.written_memory, SocketFlags.None, ct);
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
            _active_connections.TryRemove(connectionId, out _);

            try
            {
                client.Close();
            }
            catch
            {
            }
        }
    }

    private static HttpRequest? parse_request(byte[] frameData)
    {
        var span = frameData.AsSpan();
        var headerEnd = span.IndexOf(new byte[] { 0x0D, 0x0A, 0x0D, 0x0A });

        if (headerEnd < 0) return null;

        var headerText = SonicEncoding.decode_ascii(span[..headerEnd]);
        var lines = headerText.Split("\r\n");

        if (lines.Length == 0) return null;

        var requestLine = lines[0].Split(' ');

        if (requestLine.Length < 2) return null;

        var method = parse_method(requestLine[0]);
        var rawPath = requestLine[1];
        var queryIndex = rawPath.IndexOf('?');
        var path = queryIndex >= 0 ? rawPath[..queryIndex] : rawPath;
        var queryString = queryIndex >= 0 ? rawPath[queryIndex..] : "";

        var request = new HttpRequest
        {
            method = method,
            path = path,
            query_string = queryString
        };

        for (var i = 1; i < lines.Length; i++)
        {
            var colonIndex = lines[i].IndexOf(':');

            if (colonIndex > 0)
            {
                var name = lines[i][..colonIndex].Trim();
                var value = lines[i][(colonIndex + 1)..].Trim();
                request.headers[name] = value;
            }
        }

        var bodyStart = headerEnd + 4;

        if (bodyStart < frameData.Length) request.body = frameData[bodyStart..];

        return request;
    }

    private static HttpMethod parse_method(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.get,
            "POST" => HttpMethod.post,
            "PUT" => HttpMethod.put,
            "DELETE" => HttpMethod.delete,
            "PATCH" => HttpMethod.patch,
            "HEAD" => HttpMethod.head,
            "OPTIONS" => HttpMethod.options,
            "TRACE" => HttpMethod.trace,
            _ => HttpMethod.get
        };
    }
}