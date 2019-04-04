using Nyar.Binary.Frame;
using Nyar.Protocol.MySQL.Scanner;
using Nyar.Protocol.PostgreSQL.Scanner;
using Nyar.Protocol.Redis.Scanner;

namespace Nyar.Tests.Binary;

#region MySQL Scanner 测试

public sealed class MySqlScannerTests
{
    [Fact]
    public void Scan_SinglePacket_ReadsCorrectly()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var payloadLength = payload.Length;
        var packet = new byte[4 + payloadLength];
        packet[0] = (byte)(payloadLength & 0xFF);
        packet[1] = (byte)((payloadLength >> 8) & 0xFF);
        packet[2] = (byte)((payloadLength >> 16) & 0xFF);
        packet[3] = 0;
        Array.Copy(payload, 0, packet, 4, payloadLength);

        var scanner = new MySqlScanner(packet);

        var read = scanner.TryReadNext(out var frame);
        Assert.True(read);
        Assert.Equal(payloadLength, frame.Payload.Length);
        Assert.True(scanner.IsEndOfData);
    }

    [Fact]
    public void Scan_MultiplePackets_ReadsAll()
    {
        var packet1 = build_my_sql_packet([0x0A, 0x0B], 0);
        var packet2 = build_my_sql_packet([0x0C, 0x0D, 0x0E], 1);
        var packet3 = build_my_sql_packet([0x0F], 2);
        var data = packet1.Concat(packet2).Concat(packet3).ToArray();

        var scanner = new MySqlScanner(data);
        var count = 0;

        while (scanner.TryReadNext(out _))
        {
            count++;
        }

        Assert.Equal(3, count);
        Assert.True(scanner.IsEndOfData);
    }

    [Fact]
    public void Scan_Statistics_AccumulatesCorrectly()
    {
        var packet1 = build_my_sql_packet([0x01, 0x02], 0);
        var packet2 = build_my_sql_packet([0x03, 0x04, 0x05, 0x06], 1);
        var data = packet1.Concat(packet2).ToArray();

        var scanner = new MySqlScanner(data);
        var stats = scanner.ScanFrameStatistics();

        Assert.Equal(2, stats.TotalFrames);
        Assert.Equal(6, stats.TotalPayloadBytes);
        Assert.Equal(4, stats.MaxPayloadSize);
        Assert.Equal(2, stats.MinPayloadSize);
    }

    [Fact]
    public void Scan_EmptyData_ReturnsNoFrames()
    {
        var scanner = new MySqlScanner([]);

        var read = scanner.TryReadNext(out _);
        Assert.False(read);

        var stats = scanner.ScanFrameStatistics();
        Assert.Equal(0, stats.TotalFrames);
        Assert.Equal(0, stats.TotalPayloadBytes);
        Assert.Equal(0, stats.MaxPayloadSize);
        Assert.Equal(0, stats.MinPayloadSize);
    }

    [Fact]
    public void Scan_PartialHeader_ReturnsNoFrame()
    {
        var scanner = new MySqlScanner([0x01, 0x02]);

        var read = scanner.TryReadNext(out _);
        Assert.False(read);
    }

    [Fact]
    public void Scan_TruncatedPayload_ReturnsNoFrame()
    {
        var packet = new byte[6];
        packet[0] = 10;
        packet[1] = 0;
        packet[2] = 0;
        packet[3] = 0;

        var scanner = new MySqlScanner(packet);

        var read = scanner.TryReadNext(out _);
        Assert.False(read);
    }

    [Fact]
    public void Scan_LargePayload_ReadsCorrectly()
    {
        var payload = new byte[1000];
        new Random(42).NextBytes(payload);
        var packet = build_my_sql_packet(payload, 0);

        var scanner = new MySqlScanner(packet);

        var read = scanner.TryReadNext(out var frame);
        Assert.True(read);
        Assert.Equal(1000, frame.Payload.Length);
    }

    private static byte[] build_my_sql_packet(byte[] payload, byte sequenceId)
    {
        var packet = new byte[4 + payload.Length];
        packet[0] = (byte)(payload.Length & 0xFF);
        packet[1] = (byte)((payload.Length >> 8) & 0xFF);
        packet[2] = (byte)((payload.Length >> 16) & 0xFF);
        packet[3] = sequenceId;
        Array.Copy(payload, 0, packet, 4, payload.Length);
        return packet;
    }
}

#endregion

#region PostgreSQL Scanner 测试

public sealed class PostgreSqlScannerTests
{
    [Fact]
    public void Scan_SingleMessage_ReadsCorrectly()
    {
        var msgType = (byte)'R';
        var payload = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var length = 4 + payload.Length;
        var message = new byte[1 + length];
        message[0] = msgType;
        message[1] = (byte)((length >> 24) & 0xFF);
        message[2] = (byte)((length >> 16) & 0xFF);
        message[3] = (byte)((length >> 8) & 0xFF);
        message[4] = (byte)(length & 0xFF);
        Array.Copy(payload, 0, message, 5, payload.Length);

        var scanner = new PostgreSqlScanner(message);

        var read = scanner.TryReadNext(out var frame);
        Assert.True(read);
        Assert.True(scanner.IsEndOfData);
    }

    [Fact]
    public void Scan_MultipleMessages_ReadsAll()
    {
        var msg1 = build_postgre_sql_message((byte)'R', "\0\0\0\0"u8.ToArray());
        var msg2 = build_postgre_sql_message((byte)'K', []);
        var msg3 = build_postgre_sql_message((byte)'Z', [0x05]);
        var data = msg1.Concat(msg2).Concat(msg3).ToArray();

        var scanner = new PostgreSqlScanner(data);
        var count = 0;

        while (scanner.TryReadNext(out _))
        {
            count++;
        }

        Assert.Equal(3, count);
        Assert.True(scanner.IsEndOfData);
    }

    [Fact]
    public void Scan_Statistics_AccumulatesCorrectly()
    {
        var msg1 = build_postgre_sql_message((byte)'R', [0x00, 0x00, 0x00, 0x08]);
        var msg2 = build_postgre_sql_message((byte)'Z', [0x05]);
        var data = msg1.Concat(msg2).ToArray();

        var scanner = new PostgreSqlScanner(data);
        var stats = scanner.ScanFrameStatistics();

        Assert.Equal(2, stats.TotalFrames);
        Assert.True(stats.TotalPayloadBytes > 0);
    }

    [Fact]
    public void Scan_EmptyData_ReturnsNoFrames()
    {
        var scanner = new PostgreSqlScanner([]);

        var read = scanner.TryReadNext(out _);
        Assert.False(read);

        var stats = scanner.ScanFrameStatistics();
        Assert.Equal(0, stats.TotalFrames);
    }

    [Fact]
    public void Scan_PartialHeader_ReturnsNoFrame()
    {
        var scanner = new PostgreSqlScanner("R\0\0"u8);

        var read = scanner.TryReadNext(out _);
        Assert.False(read);
    }

    [Fact]
    public void Scan_TruncatedMessage_ReturnsNoFrame()
    {
        var message = new byte[6];
        message[0] = (byte)'R';
        message[1] = 0;
        message[2] = 0;
        message[3] = 0;
        message[4] = 20;

        var scanner = new PostgreSqlScanner(message);

        var read = scanner.TryReadNext(out _);
        Assert.False(read);
    }

    [Fact]
    public void Scan_AuthenticationOk_ReadsCorrectly()
    {
        var message = build_postgre_sql_message((byte)'R', "\0\0\0\0"u8.ToArray());

        var scanner = new PostgreSqlScanner(message);

        var read = scanner.TryReadNext(out var frame);
        Assert.True(read);
        Assert.True(scanner.IsEndOfData);
    }

    private static byte[] build_postgre_sql_message(byte msgType, byte[] body)
    {
        var length = 4 + body.Length;
        var message = new byte[1 + length];
        message[0] = msgType;
        message[1] = (byte)((length >> 24) & 0xFF);
        message[2] = (byte)((length >> 16) & 0xFF);
        message[3] = (byte)((length >> 8) & 0xFF);
        message[4] = (byte)(length & 0xFF);
        Array.Copy(body, 0, message, 5, body.Length);
        return message;
    }
}

#endregion

#region Redis Scanner 测试

public sealed class RedisScannerTests
{
    [Fact]
    public void Scan_SimpleString_DetectsType()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("+OK\r\n");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.SimpleStringCount);
        Assert.Equal(0, stats.ErrorCount);
        Assert.Equal(0, stats.IntegerCount);
        Assert.Equal(0, stats.BulkStringCount);
        Assert.Equal(0, stats.ArrayCount);
    }

    [Fact]
    public void Scan_Error_DetectsType()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("-ERR unknown command\r\n");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(0, stats.SimpleStringCount);
        Assert.Equal(1, stats.ErrorCount);
    }

    [Fact]
    public void Scan_Integer_DetectsType()
    {
        var data = System.Text.Encoding.UTF8.GetBytes(":42\r\n");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.IntegerCount);
    }

    [Fact]
    public void Scan_BulkString_DetectsType()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("$5\r\nhello\r\n");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.BulkStringCount);
    }

    [Fact]
    public void Scan_Array_DetectsType()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("*2\r\n+OK\r\n:1\r\n");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.ArrayCount);
        Assert.Equal(1, stats.SimpleStringCount);
        Assert.Equal(1, stats.IntegerCount);
    }

    [Fact]
    public void Scan_MixedMessages_AccumulatesAll()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("+OK\r\n:100\r\n-ERR\r\n$5\r\nhello\r\n*1\r\n+PONG\r\n");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(2, stats.SimpleStringCount);
        Assert.Equal(1, stats.ErrorCount);
        Assert.Equal(1, stats.IntegerCount);
        Assert.Equal(1, stats.BulkStringCount);
        Assert.Equal(1, stats.ArrayCount);
    }

    [Fact]
    public void Scan_Messages_ExtractsContent()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("+OK\r\n-ERR something\r\n:42\r\n");
        var scanner = new RedisScanner(data);
        var messages = scanner.ScanMessages();

        Assert.Equal(3, messages.Count);
        Assert.Equal('+', messages[0].type);
        Assert.Equal("OK", messages[0].Content);
        Assert.Equal('-', messages[1].type);
        Assert.Equal("ERR something", messages[1].Content);
        Assert.Equal(':', messages[2].type);
        Assert.Equal("42", messages[2].Content);
    }

    [Fact]
    public void Scan_EmptyData_ReturnsEmpty()
    {
        var scanner = new RedisScanner([]);
        var stats = scanner.ScanStatistics();

        Assert.Equal(0, stats.SimpleStringCount);
        Assert.Equal(0, stats.ErrorCount);
    }

    [Fact]
    public void Scan_IncompleteLine_IgnoresIt()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("+OK\r\n:42");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.SimpleStringCount);
        Assert.Equal(0, stats.IntegerCount);
    }

    [Fact]
    public void Scan_TypeCounts_TracksAllPrefixes()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("+OK\r\n+PONG\r\n-ERR\r\n:1\r\n:2\r\n:3\r\n");
        var scanner = new RedisScanner(data);
        var stats = scanner.ScanStatistics();

        Assert.Equal(2, stats.TypeCounts['+']);
        Assert.Equal(1, stats.TypeCounts['-']);
        Assert.Equal(3, stats.TypeCounts[':']);
    }
}

#endregion
