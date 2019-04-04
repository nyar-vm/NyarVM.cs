using Std.DataProcess.Enframe;
using Std.DataProcess.Write;

namespace Std.Data.Protocol.Grpc;

/// <summary>
///     <see cref="IEnframer" /> �?gRPC 帧封装实现�?/// 将载荷封装为 gRPC 标准帧格式：1 字节压缩标志 + 4 字节大端序消息长�?+ 载荷�?/// 无状态，可安全复用�?///
/// </summary>
public sealed class GrpcEnframer : IEnframer
{
    private readonly bool _compressed;

    /// <summary>
    ///     初始化一个新�?gRPC 帧封装器实例�?    ///
    /// </summary>
    /// <param name="compressed">是否启用压缩标志。实际压缩由中间件层处理，此标志仅设置帧头字节�?/param>
    public GrpcEnframer(bool compressed = false)
    {
        _compressed = compressed;
    }

    /// <inheritdoc />
    /// <exception cref="FramingException">载荷长度超过 4 字节大端序可表示的最大值时抛出�?/exception>
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var totalLength = 5 + payload.Length;
        var span = writer.get_span(totalLength);

        span[0] = _compressed ? (byte)1 : (byte)0;
        span[1] = (byte)((payload.Length >> 24) & 0xFF);
        span[2] = (byte)((payload.Length >> 16) & 0xFF);
        span[3] = (byte)((payload.Length >> 8) & 0xFF);
        span[4] = (byte)(payload.Length & 0xFF);

        payload.CopyTo(span[5..]);
        writer.advance(totalLength);
    }
}