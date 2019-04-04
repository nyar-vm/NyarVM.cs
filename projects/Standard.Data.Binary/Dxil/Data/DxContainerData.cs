using System.Text;
using Std.Binary.Attributes;
using Std.Codec;

namespace Std.Data.Binary.Dxil.Data;

/// <summary>
///     DXContainer 文件头数据的
/// </summary>
/// <remarks>
///     DXContainer 文件头由魔数、版本号、文件大小和 Part 数量组成�?///     魔数�?DXBC"（小端序读取后为 0x43425844）的
/// </remarks>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct DxContainerHeader
{
    /// <summary>
    ///     魔数的DXBC" 小端�? 0x43425844）的
    /// </summary>
    [Field(order = 0)] public uint magic_number;

    /// <summary>
    ///     容器格式主版本号�?
    /// </summary>
    [Field(order = 1)] public ushort version_major;

    /// <summary>
    ///     容器格式次版本号�?
    /// </summary>
    [Field(order = 2)] public ushort version_minor;

    /// <summary>
    ///     文件总大小（字节）的
    /// </summary>
    [Field(order = 3)] public uint file_size;

    /// <summary>
    ///     Part 数量�?
    /// </summary>
    [Field(order = 4)] public uint part_count;
}

/// <summary>
///     DXContainer Part 头数据的
/// </summary>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct DxContainerPartHeader
{
    /// <summary>
    ///     Part 类型标识�?字符 ASCII FourCC）的
    /// </summary>
    [Field(order = 0)] public uint four_cc;

    /// <summary>
    ///     Part 数据大小（字节，不含 Part 头）�?
    /// </summary>
    [Field(order = 1)] public uint size;

    /// <summary>
    ///     获取 FourCC 的ASCII 字符串表示的
    /// </summary>
    public readonly string four_cc_string
    {
        get
        {
            var bytes = new byte[4];
            bytes[0] = (byte)(four_cc & 0xFF);
            bytes[1] = (byte)((four_cc >> 8) & 0xFF);
            bytes[2] = (byte)((four_cc >> 16) & 0xFF);
            bytes[3] = (byte)((four_cc >> 24) & 0xFF);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}

/// <summary>
///     DXContainer Part 数据，包的Part 头和原始数据�?///
/// </summary>
public sealed class DxContainerPart
{
    /// <summary>
    ///     Part 头的
    /// </summary>
    public DxContainerPartHeader header { get; init; }

    /// <summary>
    ///     Part 原始数据�?
    /// </summary>
    public byte[] data { get; init; } = [];
}

/// <summary>
///     DXContainer 完整数据�?///
/// </summary>
public sealed class DxContainerData
{
    /// <summary>
    ///     容器文件头的
    /// </summary>
    public DxContainerHeader header { get; init; }

    /// <summary>
    ///     Part 列表�?
    /// </summary>
    public IReadOnlyList<DxContainerPart> parts { get; init; } = [];
}