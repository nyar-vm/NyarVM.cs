using System.Net.Sockets;
using System.Text;

namespace Hermes.Stream.RabbitMQ;

/// <summary>
///     AMQP 0-9-1 协议常量
/// </summary>
internal static class AmqpConstants
{
    public const ushort Port = 5672;
    public const byte FrameMethod = 1;
    public const byte FrameHeader = 2;
    public const byte FrameBody = 3;
    public const byte FrameHeartbeat = 8;
    public const ushort FrameEnd = 0xCE;

    public const ushort ConnectionClass = 10;
    public const ushort ChannelClass = 20;
    public const ushort ExchangeClass = 40;
    public const ushort QueueClass = 50;
    public const ushort BasicClass = 60;

    public const ushort ConnectionStartMethod = 10;
    public const ushort ConnectionStartOkMethod = 11;
    public const ushort ConnectionTuneMethod = 30;
    public const ushort ConnectionTuneOkMethod = 31;
    public const ushort ConnectionOpenMethod = 40;
    public const ushort ConnectionOpenOkMethod = 41;
    public const ushort ConnectionCloseMethod = 50;
    public const ushort ConnectionCloseOkMethod = 51;

    public const ushort ChannelOpenMethod = 10;
    public const ushort ChannelOpenOkMethod = 11;

    public const ushort ExchangeDeclareMethod = 10;
    public const ushort ExchangeDeclareOkMethod = 11;

    public const ushort QueueDeclareMethod = 10;
    public const ushort QueueDeclareOkMethod = 11;
    public const ushort QueueBindMethod = 20;
    public const ushort QueueBindOkMethod = 21;

    public const ushort BasicPublishMethod = 40;
    public const ushort BasicConsumeMethod = 20;
    public const ushort BasicConsumeOkMethod = 21;
    public const ushort BasicDeliverMethod = 60;
    public const ushort BasicAckMethod = 80;
}

/// <summary>
///     AMQP 帧
/// </summary>
internal sealed class AmqpFrame
{
    public byte Type { get; init; }
    public ushort Channel { get; init; }
    public byte[] Payload { get; init; } = [];

    public ushort ReadClassId()
    {
        return Payload.Length >= 2 ? (ushort)((Payload[0] << 8) | Payload[1]) : (ushort)0;
    }

    public ushort ReadMethodId()
    {
        return Payload.Length >= 4 ? (ushort)((Payload[2] << 8) | Payload[3]) : (ushort)0;
    }
}

/// <summary>
///     RabbitMQ 连接选项
/// </summary>
public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public int ConnectionTimeoutSeconds { get; init; } = 30;
    public ushort MaxChannels { get; init; } = 2047;
    public uint MaxFrameSize { get; init; } = 131072;
    public ushort HeartbeatSeconds { get; init; } = 60;
}

/// <summary>
///     自研 RabbitMQ 连接 — 基于 AMQP 0-9-1 协议，零外部依赖
/// </summary>
internal sealed class RabbitMqConnection : IDisposable
{
    private readonly RabbitMqOptions _options;
    private bool _authenticated;
    private ushort _channelNumber;
    private bool _disposed;
    private Socket? _socket;
    private NetworkStream? _stream;

    public RabbitMqConnection(RabbitMqOptions options)
    {
        _options = options;
    }

    public bool IsConnected => _socket?.Connected == true && _authenticated;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _authenticated = false;
        _stream?.Dispose();
        _socket?.Dispose();
    }

    #region Channel 管理

    public async Task OpenChannelAsync(ushort channel, CancellationToken ct = default)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.ChannelClass);
        WriteU16BE(payload, AmqpConstants.ChannelOpenMethod);
        WriteShortString(payload, "");

        await SendFrameAsync(AmqpConstants.FrameMethod, channel, [.. payload], ct);
        await ReadFrameAsync(ct);
        _channelNumber = channel;
    }

    #endregion

    #region 发布消息

    public async Task PublishAsync(ushort channel, string exchange, string routingKey, byte[] body,
        CancellationToken ct = default)
    {
        var methodPayload = new List<byte>();
        WriteU16BE(methodPayload, AmqpConstants.BasicClass);
        WriteU16BE(methodPayload, AmqpConstants.BasicPublishMethod);
        WriteU16BE(methodPayload, 0);
        WriteShortString(methodPayload, exchange);
        WriteShortString(methodPayload, routingKey);
        methodPayload.Add(0);

        await SendFrameAsync(AmqpConstants.FrameMethod, channel, [.. methodPayload], ct);

        var headerPayload = new List<byte>();
        WriteU16BE(headerPayload, AmqpConstants.BasicClass);
        WriteU16BE(headerPayload, 0);
        WriteU64BE(headerPayload, (ulong)body.Length);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);
        WriteU32BE(headerPayload, 0);

        await SendFrameAsync(AmqpConstants.FrameHeader, channel, [.. headerPayload], ct);

        await SendFrameAsync(AmqpConstants.FrameBody, channel, body, ct);
    }

    #endregion

    #region 连接管理

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            SendTimeout = _options.ConnectionTimeoutSeconds * 1000,
            ReceiveTimeout = _options.ConnectionTimeoutSeconds * 1000,
            NoDelay = true
        };

        await _socket.ConnectAsync(_options.Host, _options.Port, ct);
        _stream = new NetworkStream(_socket, false);

        await SendProtocolHeaderAsync(ct);
        await NegotiateConnectionAsync(ct);
        _authenticated = true;
    }

    public async Task CloseAsync(CancellationToken ct = default)
    {
        if (_socket?.Connected == true && _authenticated)
            try
            {
                var payload = new List<byte>();
                WriteU16BE(payload, AmqpConstants.ConnectionClass);
                WriteU16BE(payload, AmqpConstants.ConnectionCloseMethod);
                WriteU16BE(payload, 200);
                WriteShortString(payload, "OK");
                WriteU16BE(payload, 0);
                WriteU16BE(payload, 0);

                await SendFrameAsync(AmqpConstants.FrameMethod, 0, [.. payload], ct);
                await ReadFrameAsync(ct);
            }
            catch
            {
            }

        _authenticated = false;
    }

    #endregion

    #region Exchange / Queue

    public async Task DeclareExchangeAsync(ushort channel, string exchange, string exchangeType = "topic",
        bool durable = true, CancellationToken ct = default)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.ExchangeClass);
        WriteU16BE(payload, AmqpConstants.ExchangeDeclareMethod);
        WriteU16BE(payload, 0);
        WriteShortString(payload, exchange);
        WriteShortString(payload, exchangeType);
        payload.Add((byte)((durable ? 1 : 0) << 1));
        WriteU32BE(payload, 0);

        await SendFrameAsync(AmqpConstants.FrameMethod, channel, [.. payload], ct);
        await ReadFrameAsync(ct);
    }

    public async Task DeclareQueueAsync(ushort channel, string queue, bool durable = true,
        CancellationToken ct = default)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.QueueClass);
        WriteU16BE(payload, AmqpConstants.QueueDeclareMethod);
        WriteU16BE(payload, 0);
        WriteShortString(payload, queue);
        payload.Add((byte)((durable ? 1 : 0) << 1));
        WriteU32BE(payload, 0);

        await SendFrameAsync(AmqpConstants.FrameMethod, channel, [.. payload], ct);
        await ReadFrameAsync(ct);
    }

    public async Task BindQueueAsync(ushort channel, string queue, string exchange, string routingKey,
        CancellationToken ct = default)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.QueueClass);
        WriteU16BE(payload, AmqpConstants.QueueBindMethod);
        WriteU16BE(payload, 0);
        WriteShortString(payload, queue);
        WriteShortString(payload, exchange);
        WriteShortString(payload, routingKey);
        payload.Add(0);
        WriteU32BE(payload, 0);

        await SendFrameAsync(AmqpConstants.FrameMethod, channel, [.. payload], ct);
        await ReadFrameAsync(ct);
    }

    #endregion

    #region 消费消息

    public async Task<string> ConsumeAsync(ushort channel, string queue, CancellationToken ct = default)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.BasicClass);
        WriteU16BE(payload, AmqpConstants.BasicConsumeMethod);
        WriteU16BE(payload, 0);
        WriteShortString(payload, queue);
        WriteShortString(payload, "");
        payload.Add(0);
        payload.Add(0);

        await SendFrameAsync(AmqpConstants.FrameMethod, channel, [.. payload], ct);

        var response = await ReadFrameAsync(ct);
        return ExtractConsumerTag(response);
    }

    public async Task AckAsync(ushort channel, ulong deliveryTag, CancellationToken ct = default)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.BasicClass);
        WriteU16BE(payload, AmqpConstants.BasicAckMethod);
        WriteU64BE(payload, deliveryTag);
        payload.Add(0);

        await SendFrameAsync(AmqpConstants.FrameMethod, channel, [.. payload], ct);
    }

    public async Task<AmqpFrame> ReadFrameAsync(CancellationToken ct = default)
    {
        var headerBuffer = new byte[7];
        await ReadExactAsync(headerBuffer, ct);

        var frameType = headerBuffer[0];
        var channel = (ushort)((headerBuffer[1] << 8) | headerBuffer[2]);
        var payloadSize = (headerBuffer[3] << 24) | (headerBuffer[4] << 16) | (headerBuffer[5] << 8) | headerBuffer[6];

        var payloadBuffer = new byte[payloadSize];
        if (payloadSize > 0) await ReadExactAsync(payloadBuffer, ct);

        var endMarker = new byte[1];
        await ReadExactAsync(endMarker, ct);

        return new AmqpFrame
        {
            Type = frameType,
            Channel = channel,
            Payload = payloadBuffer
        };
    }

    #endregion

    #region 协议握手

    private async Task SendProtocolHeaderAsync(CancellationToken ct)
    {
        var header = new byte[8]
        {
            (byte)'A', (byte)'M', (byte)'Q', (byte)'P',
            0, 9, 1, 0
        };

        await _stream!.WriteAsync(header, ct);
        await _stream.FlushAsync(ct);
    }

    private async Task NegotiateConnectionAsync(CancellationToken ct)
    {
        await ReadFrameAsync(ct);

        var startOkPayload = BuildStartOkPayload();
        await SendFrameAsync(AmqpConstants.FrameMethod, 0, startOkPayload, ct);

        var tuneFrame = await ReadFrameAsync(ct);
        var (maxChannels, maxFrameSize, heartbeat) = ParseTuneFrame(tuneFrame);

        maxChannels = Math.Min(maxChannels, _options.MaxChannels);
        maxFrameSize = Math.Min(maxFrameSize, _options.MaxFrameSize);
        heartbeat = Math.Min(heartbeat, _options.HeartbeatSeconds);

        var tuneOkPayload = BuildTuneOkPayload(maxChannels, maxFrameSize, heartbeat);
        await SendFrameAsync(AmqpConstants.FrameMethod, 0, tuneOkPayload, ct);

        var openPayload = BuildConnectionOpenPayload(_options.VirtualHost);
        await SendFrameAsync(AmqpConstants.FrameMethod, 0, openPayload, ct);

        await ReadFrameAsync(ct);
    }

    private byte[] BuildStartOkPayload()
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.ConnectionClass);
        WriteU16BE(payload, AmqpConstants.ConnectionStartOkMethod);

        WriteShortString(payload, "PLAIN");
        WriteLongString(payload, $"\0{_options.Username}\0{_options.Password}");
        WriteShortString(payload, "en_US");
        WriteShortString(payload, "Hermes.Stream.RabbitMQ");

        return [.. payload];
    }

    private byte[] BuildTuneOkPayload(ushort maxChannels, uint maxFrameSize, ushort heartbeat)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.ConnectionClass);
        WriteU16BE(payload, AmqpConstants.ConnectionTuneOkMethod);
        WriteU16BE(payload, maxChannels);
        WriteU32BE(payload, maxFrameSize);
        WriteU16BE(payload, heartbeat);

        return [.. payload];
    }

    private byte[] BuildConnectionOpenPayload(string virtualHost)
    {
        var payload = new List<byte>();
        WriteU16BE(payload, AmqpConstants.ConnectionClass);
        WriteU16BE(payload, AmqpConstants.ConnectionOpenMethod);
        WriteShortString(payload, virtualHost);
        WriteShortString(payload, "");
        payload.Add(0);

        return [.. payload];
    }

    private static (ushort maxChannels, uint maxFrameSize, ushort heartbeat) ParseTuneFrame(AmqpFrame frame)
    {
        var p = frame.Payload;
        var offset = 4;
        var maxChannels = (ushort)((p[offset] << 8) | p[offset + 1]);
        offset += 2;
        var maxFrameSize = (uint)((p[offset] << 24) | (p[offset + 1] << 16) | (p[offset + 2] << 8) | p[offset + 3]);
        offset += 4;
        var heartbeat = (ushort)((p[offset] << 8) | p[offset + 1]);

        return (maxChannels, maxFrameSize, heartbeat);
    }

    #endregion

    #region 帧编解码

    private async Task SendFrameAsync(byte type, ushort channel, byte[] payload, CancellationToken ct)
    {
        var frame = new List<byte>();
        frame.Add(type);
        frame.Add((byte)(channel >> 8));
        frame.Add((byte)(channel & 0xFF));
        frame.Add((byte)((payload.Length >> 24) & 0xFF));
        frame.Add((byte)((payload.Length >> 16) & 0xFF));
        frame.Add((byte)((payload.Length >> 8) & 0xFF));
        frame.Add((byte)(payload.Length & 0xFF));
        frame.AddRange(payload);
        frame.Add((byte)AmqpConstants.FrameEnd);

        await _stream!.WriteAsync(frame.ToArray(), ct);
        await _stream.FlushAsync(ct);
    }

    private async Task ReadExactAsync(byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var bytesRead = await _stream!.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
            if (bytesRead == 0) throw new IOException("RabbitMQ 连接已关闭");

            offset += bytesRead;
        }
    }

    #endregion

    #region AMQP 类型编码

    private static void WriteShortString(List<byte> buffer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        buffer.Add((byte)bytes.Length);
        buffer.AddRange(bytes);
    }

    private static void WriteLongString(List<byte> buffer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteI32BE(buffer, bytes.Length);
        buffer.AddRange(bytes);
    }

    private static void WriteU16BE(List<byte> buffer, ushort value)
    {
        buffer.Add((byte)(value >> 8));
        buffer.Add((byte)(value & 0xFF));
    }

    private static void WriteI32BE(List<byte> buffer, int value)
    {
        buffer.Add((byte)((value >> 24) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)(value & 0xFF));
    }

    private static void WriteU32BE(List<byte> buffer, uint value)
    {
        buffer.Add((byte)((value >> 24) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)(value & 0xFF));
    }

    private static void WriteU64BE(List<byte> buffer, ulong value)
    {
        buffer.Add((byte)((value >> 56) & 0xFF));
        buffer.Add((byte)((value >> 48) & 0xFF));
        buffer.Add((byte)((value >> 40) & 0xFF));
        buffer.Add((byte)((value >> 32) & 0xFF));
        buffer.Add((byte)((value >> 24) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)(value & 0xFF));
    }

    private static string ExtractConsumerTag(AmqpFrame frame)
    {
        try
        {
            var p = frame.Payload;
            var offset = 4;
            var len = p[offset];
            offset++;
            return Encoding.UTF8.GetString(p, offset, len);
        }
        catch
        {
            return "";
        }
    }

    #endregion
}