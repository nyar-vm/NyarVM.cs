using Std.Binary.Attributes;
using Std.Codec;

namespace Std.Data.Binary.Coff.Data;

/// <summary>
///     COFF 文件头数据的
/// </summary>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct CoffHeaderData
{
    /// <summary>
    ///     机器类型�?
    /// </summary>
    [Field(order = 0)] public ushort machine;

    /// <summary>
    ///     节区数量�?
    /// </summary>
    [Field(order = 1)] public ushort number_of_sections;

    /// <summary>
    ///     时间戳的
    /// </summary>
    [Field(order = 2)] public uint time_date_stamp;

    /// <summary>
    ///     符号表指针的
    /// </summary>
    [Field(order = 3)] public uint pointer_to_symbol_table;

    /// <summary>
    ///     符号数量�?
    /// </summary>
    [Field(order = 4)] public uint number_of_symbols;

    /// <summary>
    ///     可选头大小�?
    /// </summary>
    [Field(order = 5)] public ushort size_of_optional_header;

    /// <summary>
    ///     特征标志�?
    /// </summary>
    [Field(order = 6)] public ushort characteristics;
}