using Std.DataProcess.Enframe;
using Std.DataProcess.Write;

namespace Std.Data.Protocol.PostgreSql;

/// <summary>
///     <see cref="IEnframer" /> �?PostgreSQL 前端/后端协议消息帧封装实现�?/// 将载荷封装为标准 PostgreSQL 消息格式�? 字节消息类型 + 4 字节大端序长度（含自身）�?///
///     �?Startup 消息（类型为 <c>\0</c>）外，所有消息都有类型标识�?///
/// </summary>
public sealed class PostgreSqlEnframer : IEnframer
{
    private readonly byte _message_type;

    /// <summary>
    ///     初始化一个新�?PostgreSQL 消息封帧器实例�?    ///
    /// </summary>
    /// <param name="messageType">消息类型字节，如 <c>(byte)'Q'</c> 表示简单查询�?/param>
    public PostgreSqlEnframer(byte messageType)
    {
        _message_type = messageType;
    }

    /// <inheritdoc />
    /// <exception cref="FramingException">载荷长度溢出 32 位整数时抛出�?/exception>
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var length = 4 + payload.Length;

        if (length > int.MaxValue)
            throw new FramingException(
                $"PG 消息总长度 {length} 溢出 32 位整数范围。"
            );

        var totalLength = 1 + 4 + payload.Length;
        var span = writer.get_span(totalLength);

        span[0] = _message_type;
        span[1] = (byte)((length >> 24) & 0xFF);
        span[2] = (byte)((length >> 16) & 0xFF);
        span[3] = (byte)((length >> 8) & 0xFF);
        span[4] = (byte)(length & 0xFF);

        payload.CopyTo(span[5..]);
        writer.advance(totalLength);
    }
}