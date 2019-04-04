using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Hermes.Database.PostgreSql;

public sealed class PostgreSqlConnection : IDisposable
{
    private readonly PostgreSqlConnectOptions _options;
    private bool _authenticated;
    private bool _disposed;
    private int _processId;
    private int _secretKey;
    private Socket? _socket;
    private NetworkStream? _stream;

    public PostgreSqlConnection(PostgreSqlConnectOptions options)
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

    #region 查询执行

    public async Task<PostgreSqlQueryResult> ExecuteQueryAsync(string sql, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) throw new InvalidOperationException("PostgreSQL 连接未建立或已断开");

        await SendQueryMessageAsync(sql, ct);

        var result = new PostgreSqlQueryResult { Success = true };
        var columns = new List<string>();
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        while (true)
        {
            var message = await ReadMessageAsync(ct);

            switch (message.type)
            {
                case PostgreSQLConstants.MessageType.RowDescription:
                    columns = ExtractColumnNames(message);
                    break;

                case PostgreSQLConstants.MessageType.DataRow:
                    var row = ParseDataRow(message, columns);
                    rows.Add(row);
                    break;

                case PostgreSQLConstants.MessageType.CommandComplete:
                    result.CommandTag = message.CommandTag;
                    result.AffectedRows = ExtractAffectedRows(message.CommandTag);
                    break;

                case PostgreSQLConstants.MessageType.EmptyQueryResponse:
                    result.Success = true;
                    break;

                case PostgreSQLConstants.MessageType.ErrorResponse:
                    result.Success = false;
                    result.ErrorMessage = FormatErrorMessage(message);
                    break;

                case PostgreSQLConstants.MessageType.ReadyForQuery:
                    result.TransactionStatus = message.TransactionStatus;
                    result.Columns = columns;
                    result.Rows = rows;
                    return result;
            }
        }
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

        await SendStartupMessageAsync(ct);

        await AuthenticateAsync(ct);
        _authenticated = true;
    }

    public async Task CloseAsync()
    {
        if (_socket?.Connected == true)
            try
            {
                var writer = new ByteBufferWriter(8);
                var encoder = new PostgreSqlEncoder(writer.GetSpan(8));
                encoder.EncodeTerminateMessage();

                var written = writer.WrittenData;
                await _stream!.WriteAsync(written.ToArray());
                await _stream.FlushAsync(default);
            }
            catch
            {
            }

        _authenticated = false;
    }

    #endregion

    #region 协议编码

    private async Task SendStartupMessageAsync(CancellationToken ct)
    {
        var contentWriter = new ByteBufferWriter(4096);
        contentWriter.WriteI32BE(PostgreSQLConstants.ProtocolVersion);
        contentWriter.WriteNullTerminatedString("user");
        contentWriter.WriteNullTerminatedString(_options.Username);

        if (!string.IsNullOrEmpty(_options.Database))
        {
            contentWriter.WriteNullTerminatedString("database");
            contentWriter.WriteNullTerminatedString(_options.Database);
        }

        contentWriter.WriteNullTerminatedString("application_name");
        contentWriter.WriteNullTerminatedString(_options.ApplicationName);
        contentWriter.WriteU8(0);

        var totalLength = contentWriter.Position + 4;
        var packetWriter = new ByteBufferWriter(totalLength);
        packetWriter.WriteI32BE(totalLength);
        packetWriter.Write(contentWriter.WrittenData);

        await _stream!.WriteAsync(packetWriter.ToArray(), ct);
        await _stream.FlushAsync(ct);
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        while (true)
        {
            var message = await ReadMessageAsync(ct);

            if (message.type == PostgreSQLConstants.MessageType.AuthenticationRequest)
                switch (message.AuthenticationType)
                {
                    case PostgreSQLConstants.AuthenticationType.AuthenticationOk:
                        break;

                    case PostgreSQLConstants.AuthenticationType.AuthenticationCleartextPassword:
                        await SendPasswordMessageAsync(_options.Password, ct);
                        continue;

                    case PostgreSQLConstants.AuthenticationType.AuthenticationMD5Password:
                        var md5Hash = ComputeMd5Password(_options.Username, _options.Password,
                            message.AuthenticationData);
                        await SendPasswordMessageAsync(md5Hash, ct);
                        continue;

                    default:
                        throw new NotSupportedException($"不支持的认证类型：{message.AuthenticationType}");
                }
            else if (message.type == PostgreSQLConstants.MessageType.ErrorResponse)
                throw new InvalidOperationException($"PostgreSQL 认证失败：{FormatErrorMessage(message)}");
            else if (message.type == PostgreSQLConstants.MessageType.BackendKeyData)
                ParseBackendKeyData(message);
            else if (message.type == PostgreSQLConstants.MessageType.ReadyForQuery) break;
        }
    }

    private async Task SendPasswordMessageAsync(string password, CancellationToken ct)
    {
        var contentWriter = new ByteBufferWriter(256);
        contentWriter.WriteNullTerminatedString(password);

        var totalLength = contentWriter.Position + 5;
        var packetWriter = new ByteBufferWriter(totalLength);
        packetWriter.WriteU8((byte)'p');
        packetWriter.WriteI32BE(totalLength - 1);
        packetWriter.Write(contentWriter.WrittenData);

        await _stream!.WriteAsync(packetWriter.ToArray(), ct);
        await _stream.FlushAsync(ct);
    }

    private async Task SendQueryMessageAsync(string sql, CancellationToken ct)
    {
        var contentWriter = new ByteBufferWriter(Encoding.UTF8.GetMaxByteCount(sql.Length) + 1);
        contentWriter.WriteNullTerminatedString(sql);

        var totalLength = contentWriter.Position + 5;
        var packetWriter = new ByteBufferWriter(totalLength);
        packetWriter.WriteU8((byte)PostgreSQLConstants.MessageType.Query);
        packetWriter.WriteI32BE(totalLength - 1);
        packetWriter.Write(contentWriter.WrittenData);

        await _stream!.WriteAsync(packetWriter.ToArray(), ct);
        await _stream.FlushAsync(ct);
    }

    #endregion

    #region 协议解码

    private async Task<PostgreSqlMessageData> ReadMessageAsync(CancellationToken ct)
    {
        var typeBuffer = new byte[1];
        await ReadExactAsync(typeBuffer, ct);
        var messageType = (PostgreSQLConstants.MessageType)typeBuffer[0];

        var lengthBuffer = new byte[4];
        await ReadExactAsync(lengthBuffer, ct);
        var length = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

        var contentLength = length - 4;
        var contentBuffer = new byte[Math.Max(contentLength, 0)];
        if (contentLength > 0) await ReadExactAsync(contentBuffer, ct);

        var fullMessage = new byte[1 + length];
        fullMessage[0] = typeBuffer[0];
        Buffer.BlockCopy(lengthBuffer, 0, fullMessage, 1, 4);
        if (contentLength > 0) Buffer.BlockCopy(contentBuffer, 0, fullMessage, 5, contentLength);

        var decoder = new PostgreSqlDecoder(fullMessage);
        return decoder.DecodeMessage();
    }

    private async Task ReadExactAsync(byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var bytesRead = await _stream!.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
            if (bytesRead == 0) throw new IOException("PostgreSQL 连接已关闭");

            offset += bytesRead;
        }
    }

    #endregion

    #region 结果解析

    private static List<string> ExtractColumnNames(PostgreSqlMessageData message)
    {
        return message.FieldDescriptions.Select(f => f.Name).ToList();
    }

    private static IReadOnlyDictionary<string, object?> ParseDataRow(PostgreSqlMessageData message,
        IReadOnlyList<string> columns)
    {
        var row = new Dictionary<string, object?>();

        if (message.Data == null || message.Data.Length == 0) return row;

        try
        {
            var buffer = new ByteBuffer(message.Data);
            var columnCount = buffer.ReadI16BE();

            for (var i = 0; i < columnCount && i < columns.Count; i++)
            {
                var columnLength = buffer.ReadI32BE();

                if (columnLength == -1)
                {
                    row[columns[i]] = null;
                }
                else
                {
                    var bytes = buffer.ReadBytes(columnLength).ToArray();
                    row[columns[i]] = Encoding.UTF8.GetString(bytes);
                }
            }
        }
        catch
        {
        }

        return row;
    }

    private static int ExtractAffectedRows(string? commandTag)
    {
        if (string.IsNullOrEmpty(commandTag)) return 0;

        var spaceIndex = commandTag.LastIndexOf(' ');
        if (spaceIndex < 0 || spaceIndex >= commandTag.Length - 1) return 0;

        var countStr = commandTag.Substring(spaceIndex + 1);
        return int.TryParse(countStr, out var count) ? count : 0;
    }

    private static string FormatErrorMessage(PostgreSqlMessageData message)
    {
        var sb = new StringBuilder();

        if (message.ErrorFields.TryGetValue("S", out var severity)) sb.Append($"[{severity}] ");

        if (message.ErrorFields.TryGetValue("M", out var msg)) sb.Append(msg);

        if (message.ErrorFields.TryGetValue("D", out var detail)) sb.Append($" — {detail}");

        if (message.ErrorFields.TryGetValue("C", out var code)) sb.Append($"（错误码：{code}）");

        return sb.ToString();
    }

    private void ParseBackendKeyData(PostgreSqlMessageData message)
    {
        if (message.Data == null || message.Data.Length < 8) return;

        var buffer = new ByteBuffer(message.Data);
        _processId = buffer.ReadI32BE();
        _secretKey = buffer.ReadI32BE();
    }

    private static string ComputeMd5Password(string username, string password, byte[]? salt)
    {
        if (salt == null || salt.Length != 4) return password;

        using var md5 = MD5.Create();

        var passwordUser = Encoding.UTF8.GetBytes(password + username);
        var hash1 = md5.ComputeHash(passwordUser);
        var hash1Hex = Convert.ToHexString(hash1).ToLowerInvariant();

        var hash1Bytes = Encoding.UTF8.GetBytes(hash1Hex);
        var combined = new byte[hash1Bytes.Length + 4];
        Buffer.BlockCopy(hash1Bytes, 0, combined, 0, hash1Bytes.Length);
        Buffer.BlockCopy(salt, 0, combined, hash1Bytes.Length, 4);

        var hash2 = md5.ComputeHash(combined);
        var hash2Hex = Convert.ToHexString(hash2).ToLowerInvariant();

        return $"md5{hash2Hex}";
    }

    #endregion
}

public sealed class PostgreSqlQueryResult
{
    public bool Success { get; set; }
    public int AffectedRows { get; set; }
    public string? CommandTag { get; set; }
    public PostgreSQLConstants.TransactionStatus? TransactionStatus { get; set; }
    public IReadOnlyList<string> Columns { get; set; } = [];
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];
    public string? ErrorMessage { get; set; }
}