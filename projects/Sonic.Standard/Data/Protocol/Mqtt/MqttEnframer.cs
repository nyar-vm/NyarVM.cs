using Std.DataProcess.Enframe;
using Std.DataProcess.Write;

namespace Std.Data.Protocol.Mqtt;

/// <summary>
///     <see cref="IEnframer" /> �?MQTT 3.1.1/5.0 数据包帧封装实现�?/// 将载荷封装为 MQTT 标准帧格式：1 字节固定头（类型 + 标志�?+ 变长剩余长度 + 载荷�?///
///     无状态，可安全复用�?///
/// </summary>
public sealed class MqttEnframer : IEnframer
{
    private readonly byte _fixed_header_first_byte;

    /// <summary>
    ///     初始化一个新�?MQTT 帧封装器实例�?    ///
    /// </summary>
    /// <param name="packetType">
    ///     MQTT 控制包类型（1-15），�?1=CONNECT, 3=PUBLISH, 8=SUBSCRIBE�?/param>
    ///     <param name="flags">固定头低 4 位标志。PUBLISH 包的 QoS 等信息编码在此�?/param>
    public MqttEnframer(int packetType, int flags = 0)
    {
        _fixed_header_first_byte = (byte)((packetType << 4) | (flags & 0x0F));
    }

    /// <inheritdoc />
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var remainingLength = payload.Length;
        var varIntSize = var_int_size(remainingLength);
        var totalLength = 1 + varIntSize + remainingLength;

        var span = writer.get_span(totalLength);
        span[0] = _fixed_header_first_byte;

        var offset = 1;
        var value = (ulong)remainingLength;

        while (value > 0x7F)
        {
            span[offset] = (byte)((value & 0x7F) | 0x80);
            offset++;
            value >>= 7;
        }

        span[offset] = (byte)(value & 0x7F);
        offset++;

        payload.CopyTo(span[offset..]);
        writer.advance(totalLength);
    }

    /// <summary>
    ///     计算 MQTT 变长整数编码所需的字节数�?    ///
    /// </summary>
    private static int var_int_size(int value)
    {
        if (value <= 127) return 1;

        if (value <= 16383) return 2;

        if (value <= 2097151) return 3;

        return 4;
    }
}