using Std.Data.Binary.Dds.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dds.Scanner;

/// <summary>
///     DDS 文件扫描器，基于 <see cref="SpanScanner" /> 提供的DirectDraw Surface 纹理文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     DDS 文件格式由魔数、文件头、可的DX10 扩展头和纹理数据组成的
///     扫描器只读取文件头信息，不做完整的像素数据解码，以实现快速探查的
/// </remarks>
public ref struct DdsScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="DdsScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 DDS 字节数据的/param>
    public DdsScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 DDS 文件头，提取基本纹理信息的
    /// </summary>
    /// <returns>DDS 文件头信息的/returns>
    public DdsScanHeader scan_header()
    {
        if (_scanner.length < DdsConstants.full_header_size) throw new InvalidDataException("DDS 文件数据过短，无法读取文件头");

        if (!_scanner.match_magic(DdsConstants.magic_number)) throw new InvalidDataException("DDS 文件魔数不匹配。");

        _scanner.consume_magic(DdsConstants.magic_number);

        var headerSize = _scanner.buffer.read_u32_le();

        if (headerSize != DdsConstants.header_size)
            throw new InvalidDataException($"DDS 头部大小无效，期的{DdsConstants.header_size}，实的{headerSize}");

        var flags = _scanner.buffer.read_u32_le();
        var height = _scanner.buffer.read_u32_le();
        var width = _scanner.buffer.read_u32_le();
        var pitchOrLinearSize = _scanner.buffer.read_u32_le();
        var depth = _scanner.buffer.read_u32_le();
        var mipMapCount = _scanner.buffer.read_u32_le();

        _scanner.advance(44);

        var pfSize = _scanner.buffer.read_u32_le();
        var pfFlags = _scanner.buffer.read_u32_le();
        var fourCc = _scanner.buffer.read_u32_le();
        var rgbBitCount = _scanner.buffer.read_u32_le();

        return new DdsScanHeader
        {
            width = (int)width,
            height = (int)height,
            depth = (int)depth,
            mip_map_count = (int)mipMapCount,
            four_cc = fourCc,
            rgb_bit_count = rgbBitCount,
            is_compressed = (pfFlags & (uint)DdsPixelFormatFlags.four_cc) != 0
        };
    }

    /// <summary>
    ///     快速判断数据是否为 DDS 格式的
    /// </summary>
    /// <returns>是否的DDS 格式的/returns>
    public bool is_dds()
    {
        if (_scanner.length < 4) return false;

        return _scanner.match_magic(DdsConstants.magic_number);
    }
}

/// <summary>
///     DDS 扫描头部信息的
/// </summary>
public sealed class DdsScanHeader
{
    /// <summary>
    ///     纹理宽度的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     纹理高度的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     纹理深度的
    /// </summary>
    public int depth { get; init; }

    /// <summary>
    ///     Mipmap 级别数量的
    /// </summary>
    public int mip_map_count { get; init; }

    /// <summary>
    ///     FourCC 压缩格式代码的
    /// </summary>
    public uint four_cc { get; init; }

    /// <summary>
    ///     每像素位数的
    /// </summary>
    public uint rgb_bit_count { get; init; }

    /// <summary>
    ///     是否为压缩格式的
    /// </summary>
    public bool is_compressed { get; init; }

    /// <summary>
    ///     压缩格式名称的
    /// </summary>
    public string compression_format => four_cc switch
    {
        DdsFourCc.dxt1 => "BC1/DXT1",
        DdsFourCc.dxt3 => "BC2/DXT3",
        DdsFourCc.dxt5 => "BC3/DXT5",
        DdsFourCc.ati1 => "BC4/ATI1",
        DdsFourCc.ati2 => "BC5/ATI2",
        DdsFourCc.bc6_h => "BC6H",
        DdsFourCc.bc7 => "BC7",
        0 => is_compressed ? "未知压缩" : "未压缩",
        _ => $"0x{four_cc:X8}"
    };
}