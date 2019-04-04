using Std.DataProcess.Encode;
using Std.Text.Utf8;

namespace Std.DataProcess.Decode;

/// <summary>
///     解码器：从字节缓冲区读取基本类型�?/// 每个解码方法返回 <see cref="Decoded{T}" />，BytesConsumed �?0 表示缓冲区不足�?/// 不抛异常来表示数据不足，保持高性能。格式非法时抛出
///     <see cref="EncodeException" />�?///
/// </summary>
public interface IDecoder
{
    /// <summary>
    ///     尝试读取 null 标记
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，Value �?true 表示当前标记�?null</returns>
    Decoded<bool> try_read_null(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取布尔�?    ///
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，包含布尔值和消耗字节数</returns>
    Decoded<bool> decode_bool(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取 32 位有符号整数
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，包含整数值和消耗字节数</returns>
    Decoded<int> decode_i32(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取 64 位有符号整数
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，包含整数值和消耗字节数</returns>
    Decoded<long> decode_i64(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取 64 位无符号整数
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，包含无符号整数值和消耗字节数</returns>
    Decoded<ulong> decode_u64(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取 32 位浮点数
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，包含浮点数值和消耗字节数</returns>
    Decoded<float> decode_f32(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取 64 位浮点数
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，包含浮点数值和消耗字节数</returns>
    Decoded<double> decode_f64(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取字符串的 UTF-8 字节表示
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <returns>解码结果，包�?Utf8Text 和消耗字节数</returns>
    Decoded<Utf8Text> decode_utf8(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     读取指定长度的原始字节块
    /// </summary>
    /// <param name="buffer">
    ///     输入字节缓冲�?/param>
    ///     <param name="length">
    ///         要读取的字节�?/param>
    ///         <returns>解码结果，包含字节切片和消耗字节数</returns>
    DecodedRawBytes decode_bytes(ReadOnlySpan<byte> buffer, int length);
}