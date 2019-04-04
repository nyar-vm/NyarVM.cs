using Std.DataProcess.Enframe;
using Std.DataProcess.Write;

namespace Std.Data.Protocol.WebSocket;

/// <summary>
///     <see cref="IEnframer" /> �?WebSocket 帧封装实现（RFC 6455）�?/// 支持文本帧和二进制帧，自动计算载荷长度并写入帧头�?/// 无状态，可安全复用�?///
/// </summary>
public sealed class WebSocketEnframer : IEnframer
{
    private readonly bool _is_text;

    /// <summary>
    ///     初始化一个新�?WebSocket 帧封装器实例�?    ///
    /// </summary>
    /// <param name="isText">是否为文本帧。为 <c>true</c> �?opcode �?0x01（text），否则�?0x02（binary）�?/param>
    public WebSocketEnframer(bool isText = false)
    {
        _is_text = isText;
    }

    /// <inheritdoc />
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var opcode = _is_text ? (byte)0x81 : (byte)0x82;
        var payloadLength = payload.Length;

        if (payloadLength <= 125)
        {
            var span = writer.get_span(2 + payloadLength);
            span[0] = opcode;
            span[1] = (byte)payloadLength;
            payload.CopyTo(span[2..]);
            writer.advance(2 + payloadLength);
        }
        else if (payloadLength <= 65535)
        {
            var span = writer.get_span(4 + payloadLength);
            span[0] = opcode;
            span[1] = 126;
            span[2] = (byte)((payloadLength >> 8) & 0xFF);
            span[3] = (byte)(payloadLength & 0xFF);
            payload.CopyTo(span[4..]);
            writer.advance(4 + payloadLength);
        }
        else
        {
            var span = writer.get_span(10 + payloadLength);
            span[0] = opcode;
            span[1] = 127;
            span[2] = (byte)((payloadLength >> 56) & 0xFF);
            span[3] = (byte)((payloadLength >> 48) & 0xFF);
            span[4] = (byte)((payloadLength >> 40) & 0xFF);
            span[5] = (byte)((payloadLength >> 32) & 0xFF);
            span[6] = (byte)((payloadLength >> 24) & 0xFF);
            span[7] = (byte)((payloadLength >> 16) & 0xFF);
            span[8] = (byte)((payloadLength >> 8) & 0xFF);
            span[9] = (byte)(payloadLength & 0xFF);
            payload.CopyTo(span[10..]);
            writer.advance(10 + payloadLength);
        }
    }
}