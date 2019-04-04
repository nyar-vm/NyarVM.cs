using System.Net.Sockets;
using System.Text;

namespace Hermes.Database.MySql;

public sealed class MySqlConnection : IDisposable
{
    private readonly MySqlConnectOptions _options;
    private bool _authenticated;
    private bool _disposed;
    private byte _sequenceId;
    private Socket? _socket;
    private NetworkStream? _stream;

    public MySqlConnection(MySqlConnectOptions options)
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

    #region 辅助方法

    private static int GetCharsetNumber(string charsetName)
    {
        return charsetName.ToLowerInvariant() switch
        {
            "utf8mb4" => 45,
            "utf8" => 33,
            "latin1" => 8,
            "ascii" => 11,
            "binary" => 63,
            _ => 45
        };
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
        _sequenceId = 0;

        var handshakePacket = await ReadPacketAsync(ct);

        if (handshakePacket.type != MySqlPacketType.Handshake)
            throw new InvalidOperationException($"MySQL 服务器返回了意外的包类型：{handshakePacket.type}");

        await SendHandshakeResponseAsync(ct);
        _authenticated = true;
    }

    public async Task CloseAsync()
    {
        if (_socket?.Connected == true)
            try
            {
                var writer = new ByteBufferWriter(8);
                writer.WriteU8((byte)MySQLConstants.CommandType.Quit);
                await SendPacketAsync(writer.ToArray(), default);
            }
            catch
            {
            }

        _authenticated = false;
    }

    #endregion

    #region 查询执行

    public async Task<MySqlPacketData> ExecuteQueryAsync(string sql, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) throw new InvalidOperationException("MySQL 连接未建立或已断开");

        await SendQueryCommandAsync(sql, ct);

        var response = await ReadPacketAsync(ct);

        if (response.type == MySqlPacketType.Error)
            throw new InvalidOperationException($"MySQL 查询错误：{response.ErrorMessage}（错误码：{response.ErrorCode}）");

        return response;
    }

    public async Task<MySqlQueryResult> ExecuteQueryWithRowsAsync(string sql, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await ExecuteQueryAsync(sql, ct);

        if (response.type == MySqlPacketType.Error)
            return new MySqlQueryResult
            {
                Success = false,
                ErrorMessage = response.ErrorMessage,
                ErrorCode = response.ErrorCode
            };

        var result = new MySqlQueryResult
        {
            Success = true,
            AffectedRows = (int)ExtractAffectedRows(response),
            ServerStatus = response.ServerStatus
        };

        var columnCount = ExtractColumnCount(response);
        if (columnCount > 0)
        {
            var columns = new List<string>();
            for (var i = 0; i < columnCount; i++)
            {
                var columnPacket = await ReadPacketAsync(ct);
                if (columnPacket.type == MySqlPacketType.Eof) break;

                var columnName = ExtractColumnName(columnPacket);
                columns.Add(columnName);
            }

            if (columns.Count > 0)
            {
                var eofPacket = await ReadPacketAsync(ct);
            }

            result.Columns = columns;

            var rows = new List<IReadOnlyDictionary<string, object?>>();
            while (true)
            {
                var rowPacket = await ReadPacketAsync(ct);
                if (rowPacket.type == MySqlPacketType.Eof || rowPacket.type == MySqlPacketType.Error) break;

                var row = ParseRowData(rowPacket, columns);
                rows.Add(row);
            }

            result.Rows = rows;
        }

        return result;
    }

    #endregion

    #region 协议编码

    private async Task SendHandshakeResponseAsync(CancellationToken ct)
    {
        var clientFlags = 0x00000001UL
                          | 0x00000002UL
                          | 0x00002000UL
                          | 0x00080000UL;

        var charset = (byte)GetCharsetNumber(_options.Charset);

        var contentWriter = new ByteBufferWriter(4096);
        contentWriter.WriteU64LE(clientFlags);
        contentWriter.WriteI32LE(_options.MaxPacketSize);
        contentWriter.WriteU8(charset);
        contentWriter.Advance(23);

        var usernameBytes = Encoding.UTF8.GetBytes(_options.Username);
        contentWriter.Write(usernameBytes);
        contentWriter.WriteU8(0);

        if (!string.IsNullOrEmpty(_options.Password))
        {
            var passwordBytes = Encoding.UTF8.GetBytes(_options.Password);
            WriteLengthEncodedInteger(contentWriter, (ulong)passwordBytes.Length);
            contentWriter.Write(passwordBytes);
        }
        else
        {
            contentWriter.WriteU8(0);
        }

        if (!string.IsNullOrEmpty(_options.Database))
        {
            var databaseBytes = Encoding.UTF8.GetBytes(_options.Database);
            contentWriter.Write(databaseBytes);
            contentWriter.WriteU8(0);
        }

        await SendPacketAsync(contentWriter.ToArray(), ct);
        _sequenceId++;

        var authResponse = await ReadPacketAsync(ct);
        if (authResponse.type == MySqlPacketType.Error)
            throw new InvalidOperationException(
                $"MySQL 认证失败：{authResponse.ErrorMessage}（错误码：{authResponse.ErrorCode}）");
    }

    private async Task SendQueryCommandAsync(string sql, CancellationToken ct)
    {
        var writer = new ByteBufferWriter(Encoding.UTF8.GetMaxByteCount(sql.Length) + 1);
        writer.WriteU8((byte)MySQLConstants.CommandType.Query);
        writer.WriteString(sql);

        await SendPacketAsync(writer.ToArray(), ct);
        _sequenceId++;
    }

    private async Task SendPacketAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        var headerWriter = new ByteBufferWriter(4);
        var length = payload.Length;
        headerWriter.WriteU8((byte)(length & 0xFF));
        headerWriter.WriteU8((byte)((length >> 8) & 0xFF));
        headerWriter.WriteU8((byte)((length >> 16) & 0xFF));
        headerWriter.WriteU8(_sequenceId);

        await _stream!.WriteAsync(headerWriter.ToArray(), ct);

        if (payload.Length > 0) await _stream!.WriteAsync(payload, ct);

        await _stream.FlushAsync(ct);
    }

    #endregion

    #region 协议解码

    private async Task<MySqlPacketData> ReadPacketAsync(CancellationToken ct)
    {
        var headerBuffer = new byte[4];
        await ReadExactAsync(headerBuffer, ct);

        var payloadLength = headerBuffer[0] | (headerBuffer[1] << 8) | (headerBuffer[2] << 16);
        _sequenceId = headerBuffer[3];

        var payloadBuffer = new byte[payloadLength];
        if (payloadLength > 0) await ReadExactAsync(payloadBuffer, ct);

        var fullPacket = new byte[4 + payloadLength];
        Buffer.BlockCopy(headerBuffer, 0, fullPacket, 0, 4);
        if (payloadLength > 0) Buffer.BlockCopy(payloadBuffer, 0, fullPacket, 4, payloadLength);

        var decoder = new MySqlDecoder(fullPacket);
        return decoder.DecodePacket();
    }

    private async Task ReadExactAsync(byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var bytesRead = await _stream!.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
            if (bytesRead == 0) throw new IOException("MySQL 连接已关闭");

            offset += bytesRead;
        }
    }

    #endregion

    #region 结果解析

    private static ulong ExtractAffectedRows(MySqlPacketData packet)
    {
        if (packet.Data == null || packet.Data.Length == 0) return 0;

        if (packet.Data[0] != 0x00) return 0;

        var buffer = new ByteBuffer(packet.Data);
        buffer.ReadU8();
        return ReadLengthEncodedInteger(ref buffer);
    }

    private static int ExtractColumnCount(MySqlPacketData packet)
    {
        if (packet.Data == null || packet.Data.Length == 0) return 0;

        if (packet.Data[0] == 0x00) return 0;

        var buffer = new ByteBuffer(packet.Data);
        return (int)ReadLengthEncodedInteger(ref buffer);
    }

    private static string ExtractColumnName(MySqlPacketData packet)
    {
        if (packet.Data == null || packet.Data.Length == 0) return "";

        try
        {
            var buffer = new ByteBuffer(packet.Data);
            var catalog = ReadLengthEncodedString(ref buffer);
            var schema = ReadLengthEncodedString(ref buffer);
            var table = ReadLengthEncodedString(ref buffer);
            var orgTable = ReadLengthEncodedString(ref buffer);
            var name = ReadLengthEncodedString(ref buffer);
            return name;
        }
        catch
        {
            return "";
        }
    }

    private static IReadOnlyDictionary<string, object?> ParseRowData(MySqlPacketData packet,
        IReadOnlyList<string> columns)
    {
        var row = new Dictionary<string, object?>();

        if (packet.Data == null || packet.Data.Length == 0) return row;

        try
        {
            var buffer = new ByteBuffer(packet.Data);

            for (var i = 0; i < columns.Count && !buffer.IsEnd; i++)
            {
                var firstByte = buffer.Peek(1)[0];

                if (firstByte == 0xFB)
                {
                    buffer.ReadU8();
                    row[columns[i]] = null;
                }
                else
                {
                    var value = ReadLengthEncodedString(ref buffer);
                    row[columns[i]] = value;
                }
            }
        }
        catch
        {
        }

        return row;
    }

    private static ulong ReadLengthEncodedInteger(ref ByteBuffer buffer)
    {
        var firstByte = buffer.ReadU8();

        if (firstByte < 0xFB) return firstByte;

        if (firstByte == 0xFB) return 0;

        if (firstByte == 0xFC) return buffer.ReadU16LE();

        if (firstByte == 0xFD) return buffer.ReadU32LE();

        return buffer.ReadU64LE();
    }

    private static string ReadLengthEncodedString(ref ByteBuffer buffer)
    {
        var length = (int)ReadLengthEncodedInteger(ref buffer);
        if (length == 0) return "";

        var bytes = buffer.ReadBytes(length).ToArray();
        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteLengthEncodedInteger(ByteBufferWriter writer, ulong value)
    {
        if (value < 0xFB)
        {
            writer.WriteU8((byte)value);
        }
        else if (value <= 0xFFFF)
        {
            writer.WriteU8(0xFC);
            writer.WriteU16LE((ushort)value);
        }
        else if (value <= 0xFFFFFF)
        {
            writer.WriteU8(0xFD);
            writer.WriteU32LE((uint)value);
        }
        else
        {
            writer.WriteU8(0xFE);
            writer.WriteU64LE(value);
        }
    }

    #endregion
}

public sealed class MySqlQueryResult
{
    public bool Success { get; set; }
    public int AffectedRows { get; set; }
    public MySQLConstants.ServerStatus? ServerStatus { get; set; }
    public IReadOnlyList<string> Columns { get; set; } = [];
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public MySQLConstants.ErrorCode? ErrorCode { get; set; }
}