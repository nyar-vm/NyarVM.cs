using Std.DataProcess.Enframe;
using Std.DataProcess.Write;
using Std.Text;

namespace Std.Data.Protocol.Redis;

/// <summary>
///     <see cref="IEnframer" /> �?Redis RESP 协议帧封装实现�?/// 将载荷封装为 RESP Bulk String 格式�?c>$N\r\n...payload...\r\n</c>�?///
///     无状态，可安全复用�?///
/// </summary>
public sealed class RedisEnframer : IEnframer
{
    /// <inheritdoc />
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var lengthPrefix = SonicEncoding.encode_ascii($"${payload.Length}\r\n");
        var crlf = new byte[] { 0x0D, 0x0A };
        var totalLength = lengthPrefix.Length + payload.Length + crlf.Length;

        var span = writer.get_span(totalLength);
        lengthPrefix.CopyTo(span);
        payload.CopyTo(span[lengthPrefix.Length..]);
        crlf.CopyTo(span[(lengthPrefix.Length + payload.Length)..]);
        writer.advance(totalLength);
    }
}