using Std.DataProcess.Enframe;
using Std.DataProcess.Write;
using Std.Text;

namespace Std.Data.Protocol.Http;

/// <summary>
///     <see cref="IEnframer" /> �?HTTP/1.1 响应帧封装实现�?/// 将载荷封装为标准�?HTTP/1.1 200 OK 响应帧，�?Content-Length 头�?///
/// </summary>
public sealed class HttpFrameEnframer : IEnframer
{
    /// <inheritdoc />
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var header = SonicEncoding.encode_ascii(
            $"HTTP/1.1 200 OK\r\nContent-Length: {payload.Length}\r\n\r\n"
        );

        var span = writer.get_span(header.Length + payload.Length);
        header.CopyTo(span);
        payload.CopyTo(span[header.Length..]);
        writer.advance(header.Length + payload.Length);
    }
}