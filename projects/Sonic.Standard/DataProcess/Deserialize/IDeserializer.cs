using Std.DataProcess.Serialize;
using Std.Text.Utf8;

namespace Std.DataProcess.Deserialize;

/// <summary>
///     反序列化器接口，提供从格式化数据读取的基本原语�?/// 对称�?<see cref="ISerializer" />�?///
/// </summary>
public interface IDeserializer
{
    /// <summary>
    ///     尝试读取 null 标记�?    ///
    /// </summary>
    /// <returns>如果当前值为 null 则返�?<c>true</c>�?/returns>
    bool try_read_null();

    /// <summary>
    ///     读取布尔值�?    ///
    /// </summary>
    /// <returns>读取到的布尔值�?/returns>
    bool deserialize_bool();

    /// <summary>
    ///     读取 32 位有符号整数�?    ///
    /// </summary>
    /// <returns>读取到的整数值�?/returns>
    int deserialize_i32();

    /// <summary>
    ///     读取 64 位有符号整数�?    ///
    /// </summary>
    /// <returns>读取到的整数值�?/returns>
    long deserialize_i64();

    /// <summary>
    ///     读取 64 位无符号整数�?    ///
    /// </summary>
    /// <returns>读取到的无符号整数值�?/returns>
    ulong deserialize_u64();

    /// <summary>
    ///     读取 32 位浮点数�?    ///
    /// </summary>
    /// <returns>读取到的浮点数值�?/returns>
    float deserialize_f32();

    /// <summary>
    ///     读取 64 位浮点数�?    ///
    /// </summary>
    /// <returns>读取到的浮点数值�?/returns>
    double deserialize_f64();

    /// <summary>
    ///     读取字符串的 UTF-8 字节表示�?    ///
    /// </summary>
    /// <returns>读取到的 UTF-8 字节切片�?/returns>
    Utf8Text deserialize_utf8();

    /// <summary>
    ///     读取指定长度的原始字节块�?    ///
    /// </summary>
    /// <param name="length">
    ///     要读取的字节数�?/param>
    ///     <returns>读取到的原始字节切片�?/returns>
    ReadOnlySpan<byte> deserialize_bytes(int length);

    /// <summary>
    ///     开始读取对象结构，返回对象反序列化子读取器�?    ///
    /// </summary>
    /// <param name="expectedFieldCount">
    ///     预期字段数量，可�?null�?/param>
    ///     <returns>对象反序列化子读取器�?/returns>
    IMapDeserializer deserialize_map(int? expectedFieldCount = null);

    /// <summary>
    ///     开始读取序列结构，返回序列反序列化子读取器�?    ///
    /// </summary>
    /// <param name="expectedElementCount">
    ///     预期元素数量，可�?null�?/param>
    ///     <returns>序列反序列化子读取器�?/returns>
    IArrayDeserializer deserialize_sequence(int? expectedElementCount = null);
}