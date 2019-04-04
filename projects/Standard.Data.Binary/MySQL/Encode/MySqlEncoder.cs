using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.MySQL.Data;

namespace Std.Data.Binary.MySQL.Encode;

/// <summary>
///     MySQL 编码器，用于编码 MySQL 协议数据包的
/// </summary>
public ref struct MySqlEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始的<see cref="MySqlEncoder" /> 结构的新实例的
    /// </summary>
    /// <param name="buffer">目标字节缓冲区的/param>
    public MySqlEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 MySQL 数据包的
    /// </summary>
    /// <param name="packet">要编码的数据包的/param>
    public void encode_packet(MySqlPacketData packet)
    {
        var contentLength = packet.data?.Length ?? 0;
        var header = MySqlPacketHeader.create(contentLength, packet.sequence_id);
        header.write_to(ref _writer);

        if (packet.data is { Length: > 0 }) _writer.write(packet.data);
    }

    /// <summary>
    ///     编码握手响应包的
    /// </summary>
    public void encode_handshake_response(byte sequenceId, ulong clientFlags, int maxPacketSize, byte charset,
        string username, string password, string database)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.write_u64_le(clientFlags);
        writer.write_i32_le(maxPacketSize);
        writer.write_u8(charset);
        writer.advance(23);
        writer.write_string(username);
        writer.write_u8(0);

        if (!string.IsNullOrEmpty(password))
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var pwLength = passwordBytes.Length;

            if (pwLength <= MySqlConstants.length_encoded_max_single)
            {
                writer.write_u8((byte)pwLength);
            }
            else if (pwLength < MySqlConstants.length_encoded_int16_threshold)
            {
                writer.write_u8(MySqlConstants.length_encoded_int16);
                writer.write_u16_le((ushort)pwLength);
            }
            else
            {
                writer.write_u8(MySqlConstants.length_encoded_int24);
                writer.write_u32_le((uint)pwLength);
            }

            writer.write(passwordBytes);
        }

        if (!string.IsNullOrEmpty(database))
        {
            writer.write_string(database);
            writer.write_u8(0);
        }

        var written = temp.Slice(0, writer.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.handshake_response,
            data = written
        };

        encode_packet(packet);
    }

    /// <summary>
    ///     编码查询命令包的
    /// </summary>
    public void encode_query_command(byte sequenceId, string query)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.write_u8((byte)MySqlConstants.CommandType.query);
        writer.write_string(query);

        var written = temp.Slice(0, writer.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.command,
            command_type = MySqlConstants.CommandType.query,
            data = written
        };

        encode_packet(packet);
    }

    #region 服务端编码方的

    /// <summary>
    ///     编码服务端初始握手包（Protocol::HandshakeV10）的
    /// </summary>
    /// <param name="sequenceId">
    ///     包序号（通常的0）的/param>
    ///     <param name="serverVersion">
    ///         服务端版本字符串（如 "5.7.32-OlympDB"）的/param>
    ///         <param name="connectionId">
    ///             连接 ID的/param>
    ///             <param name="authPluginName">
    ///                 认证插件名（的"mysql_native_password"）的/param>
    ///                 <param name="authPluginData">
    ///                     认证插件数据的0 字节随机盐值）的/param>
    ///                     <param name="capabilityFlags">
    ///                         服务端能力标志的/param>
    ///                         <param name="charset">
    ///                             默认字符集编号的/param>
    ///                             <param name="serverStatus">服务端状态标志的/param>
    public void encode_handshake_init(byte sequenceId, string serverVersion, int connectionId,
        string authPluginName, byte[] authPluginData, uint capabilityFlags, byte charset,
        MySqlConstants.ServerStatus serverStatus)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.write_u8(MySqlConstants.protocol_version);
        w.write_null_terminated_string(serverVersion);
        w.write_u32_le((uint)connectionId);

        var authDataLen = System.Math.Min(authPluginData.Length, 20);
        w.write(authPluginData.AsSpan(0, authDataLen));

        if (authDataLen < 20) w.advance(20 - authDataLen);

        w.advance(1);

        w.write_u16_le((ushort)(capabilityFlags & 0xFFFF));
        w.write_u8(charset);
        w.write_u16_le((ushort)(int)serverStatus);
        w.write_u16_le((ushort)((capabilityFlags >> 16) & 0xFFFF));

        var authDataTotalLen = (byte)(authPluginData.Length + 1);
        w.write_u8(authDataTotalLen);

        w.advance(10);

        if (authPluginData.Length > 20)
        {
            var remaining = System.Math.Min(authPluginData.Length - 20, 12);
            w.write(authPluginData.AsSpan(20, remaining));
            w.advance(12 - remaining);
        }
        else
        {
            w.advance(12);
        }

        w.write_null_terminated_string(authPluginName);

        var written = temp.Slice(0, w.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.handshake,
            data = written
        };

        encode_packet(packet);
    }

    /// <summary>
    ///     编码 OK 响应包的
    /// </summary>
    /// <param name="sequenceId">
    ///     包序号的/param>
    ///     <param name="affectedRows">
    ///         影响行数的/param>
    ///         <param name="lastInsertId">
    ///             最后插入的 ID的/param>
    ///             <param name="statusFlags">
    ///                 服务端状态标志的/param>
    ///                 <param name="warnings">
    ///                     警告数的/param>
    ///                     <param name="info">可选的附加信息字符串的/param>
    public void encode_ok_response(byte sequenceId, ulong affectedRows, ulong lastInsertId,
        MySqlConstants.ServerStatus statusFlags, ushort warnings, string? info = null)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.write_u8(MySqlConstants.packet_marker_ok);
        write_length_encoded_integer(ref w, affectedRows);
        write_length_encoded_integer(ref w, lastInsertId);
        w.write_u16_le((ushort)statusFlags);
        w.write_u16_le(warnings);

        if (info is not null) write_length_encoded_string(ref w, info);

        var written = temp.Slice(0, w.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.result,
            data = written
        };

        encode_packet(packet);
    }

    /// <summary>
    ///     编码错误响应包的
    /// </summary>
    /// <param name="sequenceId">
    ///     包序号的/param>
    ///     <param name="errorCode">
    ///         MySQL 错误码的/param>
    ///         <param name="sqlState">
    ///             5 字符 SQL 状态标识（的"42000"）的/param>
    ///             <param name="errorMessage">错误消息的/param>
    public void encode_error_response(byte sequenceId, ushort errorCode, string sqlState, string errorMessage)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.write_u8(MySqlConstants.packet_marker_error);
        w.write_u16_le(errorCode);

        w.write_u8((byte)'#');

        if (sqlState.Length >= 5)
        {
            var stateBytes = Encoding.UTF8.GetBytes(sqlState[..5]);
            w.write(stateBytes);
        }
        else
        {
            w.write_string(sqlState);
            w.advance(5 - sqlState.Length);
        }

        w.write(Encoding.UTF8.GetBytes(errorMessage));

        var written = temp.Slice(0, w.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.error,
            data = written
        };

        encode_packet(packet);
    }

    /// <summary>
    ///     编码 EOF 响应包的
    /// </summary>
    /// <param name="sequenceId">
    ///     包序号的/param>
    ///     <param name="warnings">
    ///         警告数的/param>
    ///         <param name="statusFlags">服务端状态标志的/param>
    public void encode_eof_response(byte sequenceId, ushort warnings, MySqlConstants.ServerStatus statusFlags)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.write_u8(MySqlConstants.packet_marker_eof);
        w.write_u16_le(warnings);
        w.write_u16_le((ushort)statusFlags);

        var written = temp.Slice(0, w.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.eof,
            data = written
        };

        encode_packet(packet);
    }

    /// <summary>
    ///     编码 ColumnDefinition41 包（结果集元数据中的列定义）的
    /// </summary>
    /// <param name="sequenceId">
    ///     包序号的/param>
    ///     <param name="catalog">
    ///         目录名（通常的"def"）的/param>
    ///         <param name="schema">
    ///             模式名的/param>
    ///             <param name="table">
    ///                 表名的/param>
    ///                 <param name="orgTable">
    ///                     原始表名的/param>
    ///                     <param name="name">
    ///                         列名的/param>
    ///                         <param name="orgName">
    ///                             原始列名的/param>
    ///                             <param name="charset">
    ///                                 字符集编号的/param>
    ///                                 <param name="columnLength">
    ///                                     列最大长度的/param>
    ///                                     <param name="columnType">
    ///                                         列类型（MYSQL_TYPE_*）的/param>
    ///                                         <param name="flags">
    ///                                             列标志的/param>
    ///                                             <param name="decimals">小数位数的/param>
    public void encode_column_definition41(byte sequenceId, string catalog, string schema, string table,
        string orgTable, string name, string orgName, ushort charset, uint columnLength,
        byte columnType, ushort flags, byte decimals)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        write_length_encoded_string(ref w, catalog);
        write_length_encoded_string(ref w, schema);
        write_length_encoded_string(ref w, table);
        write_length_encoded_string(ref w, orgTable);
        write_length_encoded_string(ref w, name);
        write_length_encoded_string(ref w, orgName);

        w.write_u8(0x0C);

        w.write_u16_le(charset);
        w.write_u32_le(columnLength);
        w.write_u8(columnType);
        w.write_u16_le(flags);
        w.write_u8(decimals);

        w.advance(2);

        var written = temp.Slice(0, w.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.field,
            data = written
        };

        encode_packet(packet);
    }

    /// <summary>
    ///     编码文本协议结果集行数据包的
    /// </summary>
    /// <param name="sequenceId">
    ///     包序号的/param>
    ///     <param name="values">行中各列的字符串值的/param>
    public void encode_text_result_set_row(byte sequenceId, IReadOnlyList<string?> values)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        foreach (var value in values)
            if (value is null)
            {
                w.write_u8(MySqlConstants.length_encoded_null);
            }
            else
            {
                var valueBytes = Encoding.UTF8.GetBytes(value);
                write_length_encoded_integer(ref w, (ulong)valueBytes.Length);
                w.write(valueBytes);
            }

        var written = temp.Slice(0, w.position).ToArray();
        var packet = new MySqlPacketData
        {
            length = written.Length,
            sequence_id = sequenceId,
            type = MySqlPacketType.row_data,
            data = written
        };

        encode_packet(packet);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     写入长度编码整数（Length-Encoded Integer）的
    /// </summary>
    private static void write_length_encoded_integer(ref ByteBufferWriter writer, ulong value)
    {
        if (value <= MySqlConstants.length_encoded_max_single)
        {
            writer.write_u8((byte)value);
        }
        else if (value < MySqlConstants.length_encoded_int16_threshold)
        {
            writer.write_u8(MySqlConstants.length_encoded_int16);
            writer.write_u16_le((ushort)value);
        }
        else if (value <= 0xFFFFFF)
        {
            writer.write_u8(MySqlConstants.length_encoded_int24);
            writer.write_u8((byte)(value & 0xFF));
            writer.write_u8((byte)((value >> 8) & 0xFF));
            writer.write_u8((byte)((value >> 16) & 0xFF));
        }
        else
        {
            writer.write_u8(0xFE);
            writer.write_u64_le(value);
        }
    }

    /// <summary>
    ///     写入长度编码字符串的
    /// </summary>
    private static void write_length_encoded_string(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        write_length_encoded_integer(ref writer, (ulong)bytes.Length);
        writer.write(bytes);
    }

    #endregion
}