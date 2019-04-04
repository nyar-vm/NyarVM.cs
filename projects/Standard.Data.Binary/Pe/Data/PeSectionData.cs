using System.Text;
using Std.Binary.Attributes;
using Std.Codec;

namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 节区数据�?
/// </summary>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct PeSectionData
{
    /// <summary>
    ///     节区名称（原�? 字节）的
    /// </summary>
    [Field(order = 0, length = 8)] public FixedBytes8 name_bytes;

    /// <summary>
    ///     虚拟大小�?
    /// </summary>
    [Field(order = 1)] public uint virtual_size;

    /// <summary>
    ///     虚拟地址�?
    /// </summary>
    [Field(order = 2)] public uint virtual_address;

    /// <summary>
    ///     原始数据大小�?
    /// </summary>
    [Field(order = 3)] public uint size_of_raw_data;

    /// <summary>
    ///     原始数据指针�?
    /// </summary>
    [Field(order = 4)] public uint pointer_to_raw_data;

    /// <summary>
    ///     重定位指针的
    /// </summary>
    [Field(order = 5)] public uint pointer_to_relocations;

    /// <summary>
    ///     行号指针�?
    /// </summary>
    [Field(order = 6)] public uint pointer_to_linenumbers;

    /// <summary>
    ///     重定位数量的
    /// </summary>
    [Field(order = 7)] public ushort number_of_relocations;

    /// <summary>
    ///     行号数量�?
    /// </summary>
    [Field(order = 8)] public ushort number_of_linenumbers;

    /// <summary>
    ///     特征标志�?
    /// </summary>
    [Field(order = 9)] public uint characteristics;

    /// <summary>
    ///     节区名称（解码后的字符串）的
    /// </summary>
    public readonly string name => Encoding.UTF8.GetString(name_bytes.as_span()).TrimEnd('\0');
}