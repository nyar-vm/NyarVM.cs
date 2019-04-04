using Std.DataProcess.Write;
using Std.Text.Utf16;
using Std.Text.Utf8;

namespace Std.DataProcess.Encode;

/// <summary>
///     编码器：将基本类型写入字节输出器�?/// 编码器是无状态的，不持有缓冲区，每次调用都是独立的�?/// 所有方法直接写�?<see cref="IBufferWriter{Byte}" />，由调用者管理其获取和提交�?///
/// </summary>
public interface IEncoder
{
    /// <summary>
    ///     写入 null 标记�?    ///
    /// </summary>
    /// <param name="writer">目标字节输出器�?/param>
    void encode_null(IBufferWriter<byte> writer);

    /// <summary>
    ///     写入布尔值�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的布尔值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_bool(bool value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 32 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_i8(sbyte value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 32 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_i16(short value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 32 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_i32(int value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_i64(long value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_i128(Int128 value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的无符号整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_u8(byte value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的无符号整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_u16(ushort value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的无符号整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_u32(uint value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的无符号整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_u64(ulong value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的无符号整数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_u128(UInt128 value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 32 位浮点数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的浮点数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_f32(float value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 64 位浮点数�?    ///
    /// </summary>
    /// <param name="value">
    ///     要写入的浮点数值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_f64(double value, IBufferWriter<byte> writer);

    /// <summary>
    ///     写入 UTF-8 字符串的字节表示�?    ///
    /// </summary>
    /// <param name="utf8Bytes">
    ///     UTF-8 编码的字节内容�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_utf8(Utf8Text text, IBufferWriter<byte> writer);


    /// <summary>
    ///     写入 UTF-16 字符串的字节表示�?    ///
    /// </summary>
    /// <param name="utf8Bytes">
    ///     UTF-8 编码的字节内容�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_utf16(Utf16Text text, IBufferWriter<byte> writer);

    /// <summary>
    ///     原样写入字节块，不做任何编码转换�?    ///
    /// </summary>
    /// <param name="bytes">
    ///     要写入的原始字节�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void encode_bytes(ReadOnlySpan<byte> bytes, IBufferWriter<byte> writer);
}