using Std.Codec;

namespace Std.Binary.Attributes;

/// <summary>
///     标记二进制结构体中的字段，指定序列化顺序和编码方式�?///
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class FieldAttribute : Attribute
{
    /// <summary>
    ///     序列化顺序�?    ///
    /// </summary>
    public int order { get; set; }

    /// <summary>
    ///     固定长度（用于数�?字符串）�?1 表示变长�?    ///
    /// </summary>
    public int length { get; set; } = -1;

    /// <summary>
    ///     变长数组的长度来源字段名�?    ///
    /// </summary>
    public string length_field { get; set; } = "";

    /// <summary>
    ///     条件存在：指�?bool 属�?方法名�?    ///
    /// </summary>
    public string conditional_on { get; set; } = "";

    /// <summary>
    ///     局部覆盖字节序�?    ///
    /// </summary>
    public Endianness endianness { get; set; }

    /// <summary>
    ///     字符串编码名称，默认 utf-8�?    ///
    /// </summary>
    public string encoding { get; set; } = "utf-8";

    /// <summary>
    ///     标记字段为可选字段，当缓冲区数据不足时跳过该字段�?    ///
    /// </summary>
    public bool optional { get; set; }

    /// <summary>
    ///     自定义编解码器类型，实现 ICodec&lt;T&gt; 接口�?    ///
    /// </summary>
    public Type? codec { get; set; }
}