using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.MySQL.Data;

namespace Std.Data.Binary.MySQL.Decode;

/// <summary>
///     MySQL 解码器，用于解析 MySQL 协议数据包的
/// </summary>
public ref struct MySqlDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="MySqlDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要解码的字节数据的/param>
    public MySqlDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 MySQL 数据包的
    /// </summary>
    /// <returns>解码后的 MySQL 数据包数据的/returns>
    public MySqlPacketData decode_packet()
    {
        if (!MySqlPacketHeader.try_read(ref _buffer, out var header))
            throw new InvalidDataException("MySQL 数据包头部读取失败。");

        var length = header.payloadlength;
        var sequenceId = header.sequence_id;

        var data = _buffer.read_bytes(length).ToArray();

        var packet = new MySqlPacketData
        {
            length = length,
            sequence_id = sequenceId,
            data = data
        };

        if (data.Length > 0)
        {
            var firstByte = data[0];

            if (firstByte == MySqlConstants.packet_marker_ok)
            {
                packet.type = MySqlPacketType.result;
                decode_result_packet(packet);
            }
            else if (firstByte == MySqlConstants.packet_marker_error)
            {
                packet.type = MySqlPacketType.error;
                decode_error_packet(packet);
            }
            else if (firstByte == MySqlConstants.packet_marker_eof)
            {
                packet.type = MySqlPacketType.eof;
            }
            else if (firstByte == MySqlConstants.protocol_version)
            {
                packet.type = MySqlPacketType.handshake;
            }
            else if (firstByte is >= MySqlConstants.command_type_min and <= MySqlConstants.command_type_max)
            {
                packet.type = MySqlPacketType.command;
                packet.command_type = (MySqlConstants.CommandType)firstByte;
            }
            else
            {
                packet.type = MySqlPacketType.unknown;
            }
        }

        return packet;
    }

    /// <summary>
    ///     解码结果包的
    /// </summary>
    /// <param name="packet">数据包的/param>
    private void decode_result_packet(MySqlPacketData packet)
    {
        var buffer = new ByteBuffer(packet.data);

        buffer.read_u8();

        var affectedRows = read_length_encoded_integer(ref buffer);

        var lastInsertId = read_length_encoded_integer(ref buffer);

        var serverStatus = (MySqlConstants.ServerStatus)buffer.read_u16_le();
        packet.server_status = serverStatus;

        var warningCount = buffer.read_u16_le();
    }

    /// <summary>
    ///     解码错误包的
    /// </summary>
    /// <param name="packet">数据包的/param>
    private void decode_error_packet(MySqlPacketData packet)
    {
        var buffer = new ByteBuffer(packet.data);

        buffer.read_u8();

        var errorCode = (MySqlConstants.ErrorCode)buffer.read_u16_le();
        packet.error_code = errorCode;

        if (!buffer.is_end && buffer.peek(1)[0] == (byte)'#') buffer.advance(6);

        var errorMessageBytes = buffer.read_bytes(buffer.remaining).ToArray();
        packet.error_message = Encoding.UTF8.GetString(errorMessageBytes);
    }

    /// <summary>
    ///     读取长度编码的整数的
    /// </summary>
    /// <param name="buffer">
    ///     字节缓冲区的/param>
    ///     <returns>读取的整数的/returns>
    private ulong read_length_encoded_integer(ref ByteBuffer buffer)
    {
        var firstByte = buffer.read_u8();

        if (firstByte <= MySqlConstants.length_encoded_max_single) return firstByte;

        if (firstByte == MySqlConstants.length_encoded_null) return 0;

        if (firstByte == MySqlConstants.length_encoded_int16) return buffer.read_u16_le();

        if (firstByte == MySqlConstants.length_encoded_int24) return buffer.read_u32_le();

        return buffer.read_u64_le();
    }
}