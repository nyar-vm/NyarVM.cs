using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.ZeroMQ.Data;

namespace Std.Data.Binary.ZeroMQ.Encode;

/// <summary>
///     ZeroMQ 编码器，用于编码 ZeroMQ 协议消息的
/// </summary>
public ref struct ZeroMqEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始的<see cref="ZeroMqEncoder" /> 结构的新实例的
    /// </summary>
    /// <param name="buffer">目标字节缓冲区的/param>
    public ZeroMqEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 ZeroMQ 消息的
    /// </summary>
    /// <param name="message">要编码的消息的/param>
    public void encode_message(ZeroMqMessageData message)
    {
        if (message.parts is { Count: > 0 })
            for (var i = 0; i < message.parts.Count; i++)
            {
                var part = message.parts[i];
                encode_frame(part.data, i == message.parts.Count - 1);
            }
        else
            encode_frame(message.data, message.type == ZeroMqConstants.MessageType.message_end);
    }

    /// <summary>
    ///     编码 ZeroMQ 帧的
    /// </summary>
    /// <param name="data">
    ///     帧数据的/param>
    ///     <param name="isLast">是否是最后一帧的/param>
    private void encode_frame(byte[] data, bool isLast)
    {
        var length = data?.Length ?? 0;
        var flags = isLast ? (byte)1 : (byte)0;

        _writer.write_u8(flags);
        _writer.write_u64_be((ulong)length);

        if (length > 0 && data != null) _writer.write(data);
    }

    /// <summary>
    ///     编码命令消息的
    /// </summary>
    /// <param name="commandType">
    ///     命令类型的/param>
    ///     <param name="socketType">
    ///         套接字类型的/param>
    ///         <param name="address">地址的/param>
    public void encode_command(ZeroMqConstants.CommandType commandType, ZeroMqConstants.SocketType socketType,
        string address)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.write_u8((byte)commandType);

        writer.write_i32_le((int)socketType);

        var addressBytes = Encoding.UTF8.GetBytes(address);
        writer.write_i32_le(addressBytes.Length);

        writer.write(addressBytes);

        var message = new ZeroMqMessageData
        {
            type = ZeroMqConstants.MessageType.message_end,
            data = [.. temp.Slice(0, writer.position)],
            command_type = commandType,
            socket_type = socketType,
            address = address
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码数据消息的
    /// </summary>
    /// <param name="data">
    ///     消息数据的/param>
    ///     <param name="more">是否有更多消息部分的/param>
    public void encode_data_message(byte[] data, bool more = false)
    {
        var message = new ZeroMqMessageData
        {
            type = more ? ZeroMqConstants.MessageType.message_part : ZeroMqConstants.MessageType.message_end,
            data = data
        };

        encode_message(message);
    }

    /// <summary>
    ///     编码多部分消息的
    /// </summary>
    /// <param name="parts">消息部分的/param>
    public void encode_multipart_message(params byte[][] parts)
    {
        var message = new ZeroMqMessageData
        {
            type = ZeroMqConstants.MessageType.message_end,
            parts = []
        };

        for (var i = 0; i < parts.Length; i++)
        {
            var item = new ZeroMqMessageData
            {
                type = i == parts.Length - 1
                    ? ZeroMqConstants.MessageType.message_end
                    : ZeroMqConstants.MessageType.message_part,
                data = parts[i]
            };
            message.parts.Add(item);
        }

        encode_message(message);
    }
}