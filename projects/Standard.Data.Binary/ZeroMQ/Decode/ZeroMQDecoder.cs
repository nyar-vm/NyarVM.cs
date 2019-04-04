using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.ZeroMQ.Data;

namespace Std.Data.Binary.ZeroMQ.Decode;

/// <summary>
///     ZeroMQ 解码器，用于解析 ZeroMQ 协议消息的
/// </summary>
public ref struct ZeroMqDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="ZeroMqDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要解码的字节数据的/param>
    public ZeroMqDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 ZeroMQ 消息的
    /// </summary>
    /// <returns>解码后的 ZeroMQ 消息数据的/returns>
    public ZeroMqMessageData decode_message()
    {
        var message = new ZeroMqMessageData();
        var parts = new List<ZeroMqMessageData>();

        while (!_buffer.is_end)
        {
            var frame = decode_frame();
            if (frame == null) break;

            var part = new ZeroMqMessageData
            {
                type = (frame.flags & 1) == 0
                    ? ZeroMqConstants.MessageType.message_part
                    : ZeroMqConstants.MessageType.message_end,
                length = frame.length,
                data = frame.data,
                flags = (ZeroMqConstants.Flags)frame.flags
            };

            parts.Add(part);

            if (part.type == ZeroMqConstants.MessageType.message_end) break;
        }

        if (parts.Count == 1) return parts[0];

        message.type = ZeroMqConstants.MessageType.message_end;
        message.parts = parts;
        return message;
    }

    /// <summary>
    ///     解码 ZeroMQ 帧的
    /// </summary>
    /// <returns>解码后的 ZeroMQ 帧数据的/returns>
    private ZeroMqFrameData? decode_frame()
    {
        if (_buffer.remaining < ZeroMqConstants.frame_size) return null;

        var header = _buffer.read_bytes(ZeroMqConstants.frame_size);

        var flags = header[0];
        var length = 0;

        for (var i = 1; i < ZeroMqConstants.frame_size; i++) length = length * 256 + header[i];

        if (_buffer.remaining < length) return null;

        var data = _buffer.read_bytes(length).ToArray();

        var frame = new ZeroMqFrameData
        {
            flags = flags,
            length = length,
            data = data
        };
        return frame;
    }

    /// <summary>
    ///     解码命令消息的
    /// </summary>
    /// <param name="message">消息数据的/param>
    public void decode_command(ZeroMqMessageData message)
    {
        if (message.data == null || message.data.Length == 0) return;

        var buffer = new ByteBuffer(message.data);

        var commandType = (ZeroMqConstants.CommandType)buffer.read_u8();
        message.command_type = commandType;

        switch (commandType)
        {
            case ZeroMqConstants.CommandType.connect:
            case ZeroMqConstants.CommandType.bind:
                decode_connect_bind_command(message, ref buffer);
                break;
        }
    }

    /// <summary>
    ///     解码连接/绑定命令的
    /// </summary>
    /// <param name="message">
    ///     消息数据的/param>
    ///     <param name="buffer">字节缓冲区的/param>
    private void decode_connect_bind_command(ZeroMqMessageData message, ref ByteBuffer buffer)
    {
        var socketType = (ZeroMqConstants.SocketType)buffer.read_i32_le();
        message.socket_type = socketType;

        var addressLength = buffer.read_i32_le();

        var addressBytes = buffer.read_bytes(addressLength).ToArray();
        message.address = Encoding.UTF8.GetString(addressBytes);
    }
}