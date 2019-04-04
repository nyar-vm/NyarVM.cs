using Std.Text.Utf8;

namespace Std.DataProcess.Serialize;

/// <summary>
///     序列化器接口，提供将数据写入特定格式的基本原语�?/// 本身提供标量写入和复合结构入口�?///
/// </summary>
public interface ISerializer
{
    /// <summary>
    ///     写入 null 标记�?    ///
    /// </summary>
    void serialize_null();

    /// <summary>
    ///     写入布尔值�?    ///
    /// </summary>
    /// <param name="value">要写入的布尔值�?/param>
    void serialize_bool(bool value);

    /// <summary>
    ///     写入 8 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的整数值�?/param>
    void serialize_i8(sbyte value);

    /// <summary>
    ///     写入 16 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的整数值�?/param>
    void serialize_i16(short value);

    /// <summary>
    ///     写入 32 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的整数值�?/param>
    void serialize_i32(int value);

    /// <summary>
    ///     写入 64 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的整数值�?/param>
    void serialize_i64(long value);

    /// <summary>
    ///     写入 128 位有符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的整数值�?/param>
    void serialize_i128(Int128 value);

    /// <summary>
    ///     写入 8 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的无符号整数值�?/param>
    void serialize_u8(byte value);

    /// <summary>
    ///     写入 16 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的无符号整数值�?/param>
    void serialize_u16(ushort value);

    /// <summary>
    ///     写入 32 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的无符号整数值�?/param>
    void serialize_u32(ulong value);

    /// <summary>
    ///     写入 64 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的无符号整数值�?/param>
    void serialize_u64(ulong value);

    /// <summary>
    ///     写入 64 位无符号整数�?    ///
    /// </summary>
    /// <param name="value">要写入的无符号整数值�?/param>
    void serialize_u128(UInt128 value);

    /// <summary>
    ///     写入 32 位浮点数�?    ///
    /// </summary>
    /// <param name="value">要写入的浮点数值�?/param>
    void serialize_f32(float value);

    /// <summary>
    ///     写入 64 位浮点数�?    ///
    /// </summary>
    /// <param name="value">要写入的浮点数值�?/param>
    void serialize_f64(double value);

    /// <summary>
    ///     写入 UTF-8 字符串�?    ///
    /// </summary>
    /// <param name="utf8Value">UTF-8 编码的字节内容�?/param>
    void serialize_utf8(Utf8Text text);

    /// <summary>
    ///     写入 UTF-16 字符串�?    ///
    /// </summary>
    /// <param name="utf8Value">UTF-16 编码的字节内容�?/param>
    void serialize_utf16(Utf8Text text);

    /// <summary>
    ///     原样写入已格式化的字节块，允许零拷贝写入�?    ///
    /// </summary>
    /// <param name="bytes">要写入的原始字节�?/param>
    void serialize_bytes(ReadOnlySpan<byte> bytes);

    /// <summary>
    ///     开始写入映射结构，返回映射序列化子写入器�?    ///
    /// </summary>
    /// <param name="countPair">
    ///     预期字段数量，用于优化预分配，可�?null�?/param>
    ///     <returns>映射序列化子写入器，需在完成后调用 <see cref="IMapSerializer.end" />�?/returns>
    IMapSerializer serialize_map(int? countPair = null);

    /// <summary>
    ///     开始写入元组结构，返回序列化子写入器�?    ///
    /// </summary>
    /// <param name="countItem">
    ///     预期元素数量，用于优化预分配，可�?null�?/param>
    ///     <returns>序列化子写入器，需在完成后调用 <see cref="ITupleSerializer.end" />�?/returns>
    ITupleSerializer serialize_tuple(int countItem);

    /// <summary>
    ///     开始写入序列结构，返回序列化子写入器�?    ///
    /// </summary>
    /// <param name="countElement">
    ///     预期元素数量，用于优化预分配，可�?null�?/param>
    ///     <returns>序列化子写入器，需在完成后调用 <see cref="ISequenceSerializer.end" />�?/returns>
    ISequenceSerializer serialize_sequence(int? countElement = null);
}