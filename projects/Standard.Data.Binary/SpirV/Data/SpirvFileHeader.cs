using Std.Binary.Attributes;
using Std.Codec;

namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 文件头部�? 字节）的
/// </summary>
/// <remarks>
///     SPIR-V 文件头由 5 �?2 位字组成：魔数、版本号、生成器魔数、ID 绑定值和保留字的
///     字节序为小端序，魔数 0x07230203 同时用于检测字节序是否正确�?
/// </remarks>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct SpirvFileHeader
{
    /// <summary>
    ///     SPIR-V 魔数的x07230203）的
    /// </summary>
    [Field(order = 0)] public uint magic_number;

    /// <summary>
    ///     SPIR-V 版本号（�?x00010000 表示 1.0的x00010300 表示 1.3）的
    /// </summary>
    [Field(order = 1)] public uint version;

    /// <summary>
    ///     生成器魔数，标识生成的SPIR-V 模块的工具的
    /// </summary>
    [Field(order = 2)] public uint generator_magic;

    /// <summary>
    ///     ID 绑定值，所的ID 必须小于此值的
    /// </summary>
    [Field(order = 3)] public uint bound;

    /// <summary>
    ///     保留字（通常�?）的
    /// </summary>
    [Field(order = 4)] public uint schema;
}