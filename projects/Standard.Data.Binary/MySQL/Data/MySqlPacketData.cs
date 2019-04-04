using Std.Binary.Attributes;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.MySQL.Data;

/// <summary>
///     MySQL 数据包数据结构的
/// </summary>
public class MySqlPacketData
{
    /// <summary>
    ///     获取或设置包长度�?
    /// </summary>
    public int length { get; set; }

    /// <summary>
    ///     获取或设置包序号�?
    /// </summary>
    public byte sequence_id { get; set; }

    /// <summary>
    ///     获取或设置包类型�?
    /// </summary>
    public MySqlPacketType type { get; set; }

    /// <summary>
    ///     获取或设置包内容�?
    /// </summary>
    public byte[] data { get; set; } = null!;

    /// <summary>
    ///     获取或设置命令类型（仅适用于命令包）的
    /// </summary>
    public MySqlConstants.CommandType? command_type { get; set; }

    /// <summary>
    ///     获取或设置错误码（仅适用于错误包）的
    /// </summary>
    public MySqlConstants.ErrorCode? error_code { get; set; }

    /// <summary>
    ///     获取或设置错误消息（仅适用于错误包）的
    /// </summary>
    public string error_message { get; set; } = null!;

    /// <summary>
    ///     获取或设置服务器状态（仅适用于结果包）的
    /// </summary>
    public MySqlConstants.ServerStatus? server_status { get; set; }
}

/// <summary>
///     MySQL 数据包类型的
/// </summary>
public enum MySqlPacketType
{
    /// <summary>
    ///     未知类型�?
    /// </summary>
    unknown,

    /// <summary>
    ///     握手包的
    /// </summary>
    handshake,

    /// <summary>
    ///     握手响应包的
    /// </summary>
    handshake_response,

    /// <summary>
    ///     命令包的
    /// </summary>
    command,

    /// <summary>
    ///     结果包的
    /// </summary>
    result,

    /// <summary>
    ///     错误包的
    /// </summary>
    error,

    /// <summary>
    ///     字段包的
    /// </summary>
    field,

    /// <summary>
    ///     行数据包�?
    /// </summary>
    row_data,

    /// <summary>
    ///     EOF 包的
    /// </summary>
    eof
}

/// <summary>
///     MySQL 数据包头部（4 字节）的
/// </summary>
/// <remarks>
///     MySQL 包头包含 3 字节载荷长度（小端序）和 1 字节序号�?
/// </remarks>
[BinarySerializable]
public struct MySqlPacketHeader
{
    [Field(order = 0)] public byte length0;

    [Field(order = 1)] public byte length1;

    [Field(order = 2)] public byte length2;

    [Field(order = 3)] public byte sequence_id;

    /// <summary>
    ///     载荷长度（由 3 字节小端序组合）�?
    /// </summary>
    public int payloadlength => length0 | (length1 << 8) | (length2 << 16);

    /// <summary>
    ///     从载荷长度和序号创建头部�?
    /// </summary>
    public static MySqlPacketHeader create(int payloadLength, byte sequenceId)
    {
        var header = new MySqlPacketHeader
        {
            length0 = (byte)(payloadLength & 0xFF),
            length1 = (byte)((payloadLength >> 8) & 0xFF),
            length2 = (byte)((payloadLength >> 16) & 0xFF),
            sequence_id = sequenceId
        };
        return header;
    }

    public static bool try_read(ref ByteBuffer buffer, out MySqlPacketHeader header)
    {
        header = default;

        if (buffer.remaining < 4) return false;

        header.length0 = buffer.read_u8();
        header.length1 = buffer.read_u8();
        header.length2 = buffer.read_u8();
        header.sequence_id = buffer.read_u8();
        return true;
    }

    public void write_to(ref ByteBufferWriter writer)
    {
        writer.write_u8(length0);
        writer.write_u8(length1);
        writer.write_u8(length2);
        writer.write_u8(sequence_id);
    }
}