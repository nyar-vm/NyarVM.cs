using Std.DataProcess.Enframe;
using Std.DataProcess.Write;

namespace Std.Data.Protocol.MySql;

/// <summary>
///     <see cref="IEnframer" /> �?MySQL 客户�?服务器协议数据包帧封装实现�?/// 将载荷封装为标准 MySQL 数据包格式：3 字节小端序长�?+ 1 字节序列号（自动递增）�?/// 当载荷超�?
///     <see cref="max_payload_size" />�?6MB - 1）时抛出 <see cref="FramingException" />�?/// 调用方需自行分包�?///
/// </summary>
public sealed class MySqlPacketEnframer : IEnframer
{
    /// <summary>
    ///     MySQL 数据包最大载荷大小：2^24 - 1 = 16,777,215 字节�?    ///
    /// </summary>
    public const int max_payload_size = (1 << 24) - 1;

    private byte _sequence_number;

    /// <summary>
    ///     初始化一个新�?MySQL 数据包封帧器实例，序列号�?0 开始�?    ///
    /// </summary>
    public MySqlPacketEnframer()
    {
        _sequence_number = 0;
    }

    /// <inheritdoc />
    /// <exception cref="FramingException">载荷超过 <see cref="max_payload_size" /> 时抛出�?/exception>
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        if (payload.Length > max_payload_size)
            throw new FramingException(
                $"载荷长度 {payload.Length} 超过 MySQL 数据包最大载荷 {max_payload_size} 字节，请分包发送。"
            );

        var totalLength = 4 + payload.Length;
        var span = writer.get_span(totalLength);

        span[0] = (byte)(payload.Length & 0xFF);
        span[1] = (byte)((payload.Length >> 8) & 0xFF);
        span[2] = (byte)((payload.Length >> 16) & 0xFF);
        span[3] = _sequence_number;

        payload.CopyTo(span[4..]);
        writer.advance(totalLength);

        _sequence_number = (byte)((_sequence_number + 1) % 256);
    }

    /// <summary>
    ///     重置序列号到指定值。通常在收到服务端 EOF �?OK 包后调用�?    ///
    /// </summary>
    /// <param name="number">新的序列号�?/param>
    public void reset_sequence(byte number = 0)
    {
        _sequence_number = number;
    }
}