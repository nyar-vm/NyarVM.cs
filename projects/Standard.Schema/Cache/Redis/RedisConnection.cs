using System.Net.Sockets;
using System.Text;

namespace Hermes.Cache.Redis;

public sealed class RedisConnection : IDisposable
{
    private readonly List<byte> _buffer;
    private readonly string _host;
    private readonly int _port;
    private bool _disposed;
    private NetworkStream? _stream;
    private TcpClient? _tcpClient;

    public RedisConnection(string host, int port)
    {
        _host = host;
        _port = port;
        _buffer = [];
    }

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _stream?.Dispose();
        _tcpClient?.Dispose();
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(_host, _port, ct);
        _stream = _tcpClient.GetStream();
    }

    public async Task SendAsync(byte[] data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream == null) throw new InvalidOperationException("Redis 连接未建立");

        await _stream.WriteAsync(data, ct);
    }

    public async Task<RedisMessageData> ReceiveResponseAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream == null) throw new InvalidOperationException("Redis 连接未建立");

        while (true)
        {
            if (_buffer.Count > 0)
            {
                var data = _buffer.ToArray();
                if (TryGetMessageLength(data, out var messageLength) && data.Length >= messageLength)
                {
                    var messageData = new byte[messageLength];
                    Array.Copy(data, messageData, messageLength);
                    _buffer.RemoveRange(0, messageLength);

                    var decoder = new RedisDecoder(new ReadOnlySpan<byte>(messageData));
                    return decoder.DecodeMessage();
                }
            }

            var recvBuffer = new byte[4096];
            var received = await _stream.ReadAsync(recvBuffer, ct);

            if (received == 0) throw new InvalidOperationException("Redis 连接已关闭");

            _buffer.AddRange([.. recvBuffer.AsSpan(0, received)]);
        }
    }

    #region RESP 消息长度检测

    private static bool TryGetMessageLength(byte[] data, out int length)
    {
        var span = new ReadOnlySpan<byte>(data);
        return TryGetMessageLength(span, out length);
    }

    private static bool TryGetMessageLength(ReadOnlySpan<byte> data, out int length)
    {
        length = 0;
        if (data.IsEmpty) return false;

        var prefix = (char)data[0];
        return prefix switch
        {
            '+' or '-' or ':' => TryGetLineEnd(data, out length),
            '$' => TryGetBulkStringLength(data, out length),
            '*' => TryGetArrayLength(data, out length),
            _ => false
        };
    }

    private static bool TryGetLineEnd(ReadOnlySpan<byte> data, out int length)
    {
        var pos = FindCRLF(data);
        if (pos < 0)
        {
            length = 0;
            return false;
        }

        length = pos + 2;
        return true;
    }

    private static bool TryGetBulkStringLength(ReadOnlySpan<byte> data, out int length)
    {
        length = 0;
        if (data.Length < 2) return false;

        var crlfPos = FindCRLF(data.Slice(1));
        if (crlfPos < 0) return false;

        var lengthSpan = data.Slice(1, crlfPos);
        var lengthStr = Encoding.UTF8.GetString(lengthSpan);
        if (!int.TryParse(lengthStr, out var bulkLength)) return false;

        if (bulkLength == RedisConstants.NullBulkString)
        {
            length = 1 + crlfPos + 2;
            return true;
        }

        var totalLength = 1 + crlfPos + 2 + bulkLength + 2;
        if (data.Length < totalLength) return false;

        length = totalLength;
        return true;
    }

    private static bool TryGetArrayLength(ReadOnlySpan<byte> data, out int length)
    {
        length = 0;
        if (data.Length < 2) return false;

        var crlfPos = FindCRLF(data.Slice(1));
        if (crlfPos < 0) return false;

        var countSpan = data.Slice(1, crlfPos);
        var countStr = Encoding.UTF8.GetString(countSpan);
        if (!int.TryParse(countStr, out var count)) return false;

        if (count == RedisConstants.NullArray)
        {
            length = 1 + crlfPos + 2;
            return true;
        }

        var offset = 1 + crlfPos + 2;
        for (var i = 0; i < count; i++)
        {
            if (offset >= data.Length) return false;

            if (!TryGetMessageLength(data.Slice(offset), out var elementLength)) return false;

            offset += elementLength;
        }

        length = offset;
        return true;
    }

    private static int FindCRLF(ReadOnlySpan<byte> data)
    {
        for (var i = 0; i < data.Length - 1; i++)
            if (data[i] == '\r' && data[i + 1] == '\n')
                return i;

        return -1;
    }

    #endregion
}