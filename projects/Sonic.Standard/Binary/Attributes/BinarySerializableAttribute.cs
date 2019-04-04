using Std.Codec;

namespace Std.Binary.Attributes;

/// <summary>
///     标记一个结构体为二进制可序列化，源生成器将为其生成 TryRead/WriteTo 方法�?///
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BinarySerializableAttribute : Attribute
{
    /// <summary>
    ///     字节序，决定多字节字段的默认排列顺序�?    ///
    /// </summary>
    public Endianness endianness { get; set; }

    /// <summary>
    ///     显式对齐（字节）�? 表示自然对齐�?    ///
    /// </summary>
    public int explicit_alignment { get; set; }
}