using Std.DataProcess.Write;
using Std.Text;

namespace Std.DataProcess.Enframe;

/// <summary>
///     分隔符封帧器，将载荷加上分隔符后写入输出器�?///
/// </summary>
public sealed class DelimiterEnframer : IEnframer
{
    private readonly byte[] _delimiter;

    /// <summary>
    ///     使用指定分隔符初始化封帧器�?    ///
    /// </summary>
    /// <param name="delimiter">帧分隔符的字节序列�?/param>
    public DelimiterEnframer(ReadOnlySpan<byte> delimiter)
    {
        _delimiter = [.. delimiter];
    }

    /// <summary>
    ///     使用指定字符串作为分隔符初始化封帧器（UTF-8 编码）�?    ///
    /// </summary>
    /// <param name="delimiter">帧分隔符字符串�?/param>
    public DelimiterEnframer(string delimiter)
    {
        _delimiter = SonicEncoding.encode_utf8(delimiter);
    }

    /// <inheritdoc />
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var totalLength = payload.Length + _delimiter.Length;
        var span = writer.get_span(totalLength);
        payload.CopyTo(span);
        _delimiter.AsSpan().CopyTo(span[payload.Length..]);
        writer.advance(totalLength);
    }
}