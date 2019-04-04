using System.Net.Sockets;
using System.Text;

namespace Hermes.Stream.Kafka;

/// <summary>
///     Kafka 协议 API Key 常量
/// </summary>
internal static class KafkaApiKeys
{
    public const short Produce = 0;
    public const short Fetch = 1;
    public const short ListOffsets = 2;
    public const short Metadata = 3;
    public const short FindCoordinator = 10;
    public const short JoinGroup = 11;
    public const short SyncGroup = 14;
    public const short Heartbeat = 12;
    public const short LeaveGroup = 13;
    public const short ApiVersions = 18;
    public const short SaslHandshake = 17;
    public const short SaslAuthenticate = 36;
}

/// <summary>
///     Kafka 连接选项
/// </summary>
public sealed class KafkaOptions
{
    public string BootstrapServers { get; init; } = "localhost:9092";
    public string? ConsumerGroupId { get; init; }
    public string? SaslUsername { get; init; }
    public string? SaslPassword { get; init; }
    public string? SaslMechanism { get; init; }
    public int ConnectionTimeoutSeconds { get; init; } = 30;
    public string TopicPrefix { get; init; } = "hermes:";
}

/// <summary>
///     自研 Kafka 连接 — 基于原生 Kafka Binary Protocol，零外部依赖
/// </summary>
internal sealed class KafkaBrokerConnection : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private int _correlationId;
    private bool _disposed;
    private Socket? _socket;
    private NetworkStream? _stream;

    public KafkaBrokerConnection(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public bool IsConnected => _socket?.Connected == true;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _stream?.Dispose();
        _socket?.Dispose();
    }

    #region 连接管理

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            SendTimeout = 30000,
            ReceiveTimeout = 30000,
            NoDelay = true
        };

        await _socket.ConnectAsync(_host, _port, ct);
        _stream = new NetworkStream(_socket, false);

        await SendApiVersionsRequestAsync(ct);
    }

    #endregion

    #region Metadata

    public async Task<KafkaMetadataResponse> GetMetadataAsync(string[] topics, CancellationToken ct = default)
    {
        var writer = new ByteBufferWriter(1024);
        WriteRequestHeader(writer, KafkaApiKeys.Metadata, 5);
        writer.WriteU8(0);

        writer.WriteI32BE(topics.Length);
        foreach (var topic in topics) WriteCompactString(writer, topic);

        var response = await SendAndReceiveAsync(writer, ct);
        return ParseMetadataResponse(response);
    }

    #endregion

    #region Fetch

    public async Task<KafkaFetchResponse> FetchAsync(string topic, int partition, long offset, int maxBytes = 65536,
        CancellationToken ct = default)
    {
        var writer = new ByteBufferWriter(256);
        WriteRequestHeader(writer, KafkaApiKeys.Fetch, 15);

        writer.WriteI32BE(-1);
        writer.WriteI32BE(0);
        writer.WriteI32BE(1);
        writer.WriteI32BE(maxBytes);

        writer.WriteI32BE(1);
        WriteCompactString(writer, topic);
        writer.WriteI32BE(1);

        writer.WriteI32BE(partition);
        writer.WriteI64BE(offset);
        writer.WriteI32BE(1048576);
        writer.WriteI32BE(maxBytes);
        writer.WriteU8(0);

        var response = await SendAndReceiveAsync(writer, ct);
        return ParseFetchResponse(response);
    }

    #endregion

    #region Produce

    public async Task<KafkaProduceResponse> ProduceAsync(string topic, int partition, byte[] key, byte[] value,
        CancellationToken ct = default)
    {
        var writer = new ByteBufferWriter(4096 + value.Length);
        WriteRequestHeader(writer, KafkaApiKeys.Produce, 9);

        WriteCompactString(writer, "lz4");
        writer.WriteI32BE(-1);
        writer.WriteI32BE(1);

        WriteCompactString(writer, topic);
        writer.WriteI32BE(1);

        writer.WriteI32BE(partition);

        var recordBatch = BuildRecordBatch(key, value);
        writer.WriteI32BE(recordBatch.Length);
        writer.Write(recordBatch);

        var response = await SendAndReceiveAsync(writer, ct);
        return ParseProduceResponse(response);
    }

    private byte[] BuildRecordBatch(byte[] key, byte[] value)
    {
        var writer = new ByteBufferWriter(256 + key.Length + value.Length);

        var baseOffset = 0L;
        writer.WriteI64BE(baseOffset);
        var lengthPos = writer.Position;
        writer.WriteI32BE(0);
        writer.WriteI32BE(0);
        writer.WriteU8(0);
        writer.WriteU8(2);
        writer.WriteI32BE(0);
        writer.WriteI64BE(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        writer.WriteI64BE(0);
        writer.WriteI64BE(-1);
        writer.WriteI32BE(1);
        writer.WriteI32BE(1);

        var recordsStart = writer.Position;
        writer.WriteU8(0);
        writer.WriteI64BE(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        writer.WriteI32BE(0);

        writer.WriteU8(key.Length > 0 ? (byte)1 : (byte)0);
        if (key.Length > 0)
        {
            WriteVarInt(writer, key.Length);
            writer.Write(key);
        }

        WriteVarInt(writer, value.Length);
        writer.Write(value);

        writer.WriteU8(0);

        var batchLength = writer.Position - lengthPos - 4;
        var savedPos = writer.Position;
        writer.Position = lengthPos;
        writer.WriteI32BE(batchLength);
        writer.Position = savedPos;

        return writer.ToArray();
    }

    #endregion

    #region 请求/响应

    private void WriteRequestHeader(ByteBufferWriter writer, short apiKey, short apiVersion)
    {
        writer.WriteI16BE(apiKey);
        writer.WriteI16BE(apiVersion);
        writer.WriteI32BE(++_correlationId);
        WriteCompactString(writer, "hermes-kafka-client");
        writer.WriteU8(0);
    }

    private async Task<byte[]> SendAndReceiveAsync(ByteBufferWriter requestWriter, CancellationToken ct)
    {
        var payload = requestWriter.ToArray();

        var frameWriter = new ByteBufferWriter(4 + payload.Length);
        frameWriter.WriteI32BE(payload.Length);
        frameWriter.Write(payload);

        await _stream!.WriteAsync(frameWriter.ToArray(), ct);
        await _stream.FlushAsync(ct);

        var sizeBuffer = new byte[4];
        await ReadExactAsync(sizeBuffer, ct);
        var responseSize = (sizeBuffer[0] << 24) | (sizeBuffer[1] << 16) | (sizeBuffer[2] << 8) | sizeBuffer[3];

        var responseBuffer = new byte[responseSize];
        if (responseSize > 0) await ReadExactAsync(responseBuffer, ct);

        return responseBuffer;
    }

    private async Task SendApiVersionsRequestAsync(CancellationToken ct)
    {
        var writer = new ByteBufferWriter(64);
        WriteRequestHeader(writer, KafkaApiKeys.ApiVersions, 4);
        WriteCompactString(writer, "hermes-kafka-client");

        try
        {
            await SendAndReceiveAsync(writer, ct);
        }
        catch
        {
        }
    }

    private async Task ReadExactAsync(byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var bytesRead = await _stream!.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
            if (bytesRead == 0) throw new IOException("Kafka 连接已关闭");

            offset += bytesRead;
        }
    }

    #endregion

    #region 响应解析

    private static KafkaMetadataResponse ParseMetadataResponse(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        buffer.ReadI32BE();
        buffer.ReadI32BE();
        buffer.ReadU8();

        var brokers = new List<KafkaBrokerInfo>();
        var brokerCount = buffer.ReadI32BE();
        for (var i = 0; i < brokerCount; i++)
        {
            buffer.ReadI32BE();
            var host = ReadCompactString(buffer);
            var port = buffer.ReadI32BE();
            buffer.ReadU8();
            brokers.Add(new KafkaBrokerInfo(host, port));
        }

        var topics = new List<KafkaTopicInfo>();
        var topicCount = buffer.ReadI32BE();
        for (var i = 0; i < topicCount; i++)
        {
            buffer.ReadI16BE();
            var name = ReadCompactString(buffer);
            buffer.ReadU8();

            var partitions = new List<KafkaPartitionInfo>();
            var partitionCount = buffer.ReadI32BE();
            for (var j = 0; j < partitionCount; j++)
            {
                buffer.ReadI16BE();
                var partitionId = buffer.ReadI32BE();
                var leader = buffer.ReadI32BE();
                buffer.ReadI32BE();
                buffer.ReadU8();
                partitions.Add(new KafkaPartitionInfo(partitionId, leader));
            }

            topics.Add(new KafkaTopicInfo(name, partitions));
        }

        return new KafkaMetadataResponse(brokers, topics);
    }

    private static KafkaProduceResponse ParseProduceResponse(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        buffer.ReadI32BE();
        buffer.ReadI32BE();
        buffer.ReadU8();

        var topicCount = buffer.ReadI32BE();
        short errorCode = 0;
        long offset = -1;

        for (var i = 0; i < topicCount; i++)
        {
            ReadCompactString(buffer);
            var partitionCount = buffer.ReadI32BE();

            for (var j = 0; j < partitionCount; j++)
            {
                buffer.ReadI32BE();
                errorCode = buffer.ReadI16BE();
                offset = buffer.ReadI64BE();
                buffer.ReadI32BE();
                buffer.ReadU8();
            }

            buffer.ReadU8();
        }

        return new KafkaProduceResponse(errorCode, offset);
    }

    private static KafkaFetchResponse ParseFetchResponse(byte[] data)
    {
        var records = new List<KafkaRecord>();

        try
        {
            var buffer = new ByteBuffer(data);
            buffer.ReadI32BE();
            buffer.ReadI32BE();
            buffer.ReadU8();

            var topicCount = buffer.ReadI32BE();
            for (var i = 0; i < topicCount; i++)
            {
                ReadCompactString(buffer);
                var partitionCount = buffer.ReadI32BE();

                for (var j = 0; j < partitionCount; j++)
                {
                    buffer.ReadI32BE();
                    buffer.ReadI16BE();
                    var highWatermark = buffer.ReadI64BE();
                    buffer.ReadI64BE();
                    buffer.ReadU8();

                    var abortedCount = buffer.ReadI32BE();
                    for (var k = 0; k < abortedCount; k++)
                    {
                        buffer.ReadI64BE();
                        buffer.ReadI64BE();
                    }

                    buffer.ReadU8();

                    var recordBytes = buffer.ReadI32BE();
                    if (recordBytes <= 0) continue;

                    var baseOffset = buffer.ReadI64BE();
                    buffer.ReadI32BE();
                    buffer.ReadI32BE();
                    buffer.ReadU8();
                    buffer.ReadU8();
                    buffer.ReadI32BE();
                    buffer.ReadI64BE();
                    buffer.ReadI64BE();
                    buffer.ReadI64BE();
                    buffer.ReadI32BE();
                    buffer.ReadI32BE();

                    var recordCount = buffer.ReadI32BE();
                    for (var r = 0; r < recordCount; r++)
                    {
                        buffer.ReadU8();
                        buffer.ReadI64BE();
                        buffer.ReadI32BE();

                        var keyLen = buffer.ReadU8();
                        byte[]? key = null;
                        if (keyLen > 0)
                        {
                            key = new byte[keyLen];
                            buffer.Read(key);
                        }

                        var valueLen = buffer.ReadI32BE();
                        var value = new byte[valueLen];
                        buffer.Read(value);

                        buffer.ReadU8();

                        records.Add(new KafkaRecord(key, value, baseOffset + r));
                    }
                }
            }
        }
        catch
        {
        }

        return new KafkaFetchResponse(records);
    }

    #endregion

    #region 编码辅助

    private static void WriteCompactString(ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarInt(writer, bytes.Length + 1);
        writer.Write(bytes);
    }

    private static string ReadCompactString(ByteBuffer buffer)
    {
        var len = ReadVarInt(buffer) - 1;
        if (len <= 0) return "";

        var bytes = new byte[len];
        buffer.Read(bytes);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteVarInt(ByteBufferWriter writer, int value)
    {
        var uvalue = (uint)((value << 1) ^ (value >> 31));
        while (uvalue > 0x7F)
        {
            writer.WriteU8((byte)((uvalue & 0x7F) | 0x80));
            uvalue >>= 7;
        }

        writer.WriteU8((byte)uvalue);
    }

    private static int ReadVarInt(ByteBuffer buffer)
    {
        var value = 0;
        var shift = 0;
        byte b;
        do
        {
            b = buffer.ReadU8();
            value |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return (value >>> 1) ^ -(value & 1);
    }

    #endregion
}

internal sealed class ByteBuffer
{
    private readonly byte[] _data;
    private int _offset;

    public ByteBuffer(byte[] data)
    {
        _data = data;
        _offset = 0;
    }

    public byte ReadU8()
    {
        return _data[_offset++];
    }

    public short ReadI16BE()
    {
        var b1 = _data[_offset++];
        var b2 = _data[_offset++];
        return (short)((b1 << 8) | b2);
    }

    public int ReadI32BE()
    {
        var b1 = _data[_offset++];
        var b2 = _data[_offset++];
        var b3 = _data[_offset++];
        var b4 = _data[_offset++];
        return (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
    }

    public long ReadI64BE()
    {
        var b1 = (long)_data[_offset++];
        var b2 = (long)_data[_offset++];
        var b3 = (long)_data[_offset++];
        var b4 = (long)_data[_offset++];
        var b5 = (long)_data[_offset++];
        var b6 = (long)_data[_offset++];
        var b7 = (long)_data[_offset++];
        var b8 = (long)_data[_offset++];
        return (b1 << 56) | (b2 << 48) | (b3 << 40) | (b4 << 32) | (b5 << 24) | (b6 << 16) | (b7 << 8) | b8;
    }

    public void Read(byte[] destination)
    {
        Array.Copy(_data, _offset, destination, 0, destination.Length);
        _offset += destination.Length;
    }

    public byte[] ReadBytes(int count)
    {
        var result = new byte[count];
        Array.Copy(_data, _offset, result, 0, count);
        _offset += count;
        return result;
    }
}

internal sealed class ByteBufferWriter
{
    private byte[] _buffer;

    public ByteBufferWriter(int capacity)
    {
        _buffer = new byte[capacity];
    }

    public int Position { get; set; }

    public void WriteU8(byte value)
    {
        EnsureCapacity(1);
        _buffer[Position++] = value;
    }

    public void WriteI16BE(short value)
    {
        EnsureCapacity(2);
        _buffer[Position++] = (byte)((value >> 8) & 0xFF);
        _buffer[Position++] = (byte)(value & 0xFF);
    }

    public void WriteI32BE(int value)
    {
        EnsureCapacity(4);
        _buffer[Position++] = (byte)((value >> 24) & 0xFF);
        _buffer[Position++] = (byte)((value >> 16) & 0xFF);
        _buffer[Position++] = (byte)((value >> 8) & 0xFF);
        _buffer[Position++] = (byte)(value & 0xFF);
    }

    public void WriteI64BE(long value)
    {
        EnsureCapacity(8);
        _buffer[Position++] = (byte)((value >> 56) & 0xFF);
        _buffer[Position++] = (byte)((value >> 48) & 0xFF);
        _buffer[Position++] = (byte)((value >> 40) & 0xFF);
        _buffer[Position++] = (byte)((value >> 32) & 0xFF);
        _buffer[Position++] = (byte)((value >> 24) & 0xFF);
        _buffer[Position++] = (byte)((value >> 16) & 0xFF);
        _buffer[Position++] = (byte)((value >> 8) & 0xFF);
        _buffer[Position++] = (byte)(value & 0xFF);
    }

    public void Write(byte[] data)
    {
        EnsureCapacity(data.Length);
        Array.Copy(data, 0, _buffer, Position, data.Length);
        Position += data.Length;
    }

    private void EnsureCapacity(int additional)
    {
        if (Position + additional <= _buffer.Length) return;

        var newBuffer = new byte[Math.Max(_buffer.Length * 2, Position + additional)];
        Array.Copy(_buffer, newBuffer, Position);
        _buffer = newBuffer;
    }

    public byte[] ToArray()
    {
        var result = new byte[Position];
        Array.Copy(_buffer, result, Position);
        return result;
    }
}

internal sealed record KafkaBrokerInfo(string Host, int Port);

internal sealed record KafkaTopicInfo(string Name, List<KafkaPartitionInfo> Partitions);

internal sealed record KafkaPartitionInfo(int PartitionId, int Leader);

internal sealed record KafkaMetadataResponse(List<KafkaBrokerInfo> Brokers, List<KafkaTopicInfo> Topics);

internal sealed record KafkaProduceResponse(short ErrorCode, long Offset);

internal sealed record KafkaFetchResponse(List<KafkaRecord> Records);

internal sealed record KafkaRecord(byte[]? Key, byte[] Value, long Offset);