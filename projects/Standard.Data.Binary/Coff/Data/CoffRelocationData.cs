using Std.Binary.Attributes;
using Std.Codec;

namespace Std.Data.Binary.Coff.Data;

/// <summary>
///     COFF 重定位数据的
/// </summary>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct CoffRelocationData
{
    /// <summary>
    ///     虚拟地址�?
    /// </summary>
    [Field(order = 0)] public uint virtual_address;

    /// <summary>
    ///     符号表索引的
    /// </summary>
    [Field(order = 1)] public uint symbol_table_index;

    /// <summary>
    ///     类型�?
    /// </summary>
    [Field(order = 2)] public ushort type;
}