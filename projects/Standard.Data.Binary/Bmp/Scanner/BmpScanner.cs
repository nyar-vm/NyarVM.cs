using Std.Data.Binary.Bmp.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Bmp.Scanner;

/// <summary>
///     BMP 文件扫描器，基于 <see cref="SpanScanner" /> 提供的BMP 图像文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     BMP 的Microsoft 的标准位图格式，由文件头、信息头、可选调色板和像素数据组成的
///     扫描器只读取文件头和信息头，不做完整的像素数据解码，以实现快速探查的
/// </remarks>
public ref struct BmpScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="BmpScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 BMP 字节数据的/param>
    public BmpScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 BMP 文件头，提取基本图像信息的
    /// </summary>
    /// <returns>BMP 文件头信息的/returns>
    public BmpScanHeader scan_header()
    {
        if (_scanner.length < BmpConstants.file_header_size + BmpConstants.info_header_size)
            throw new InvalidDataException("BMP 文件数据过短，无法读取文件头");

        if (!_scanner.match_magic(BmpConstants.magic_number)) throw new InvalidDataException("BMP 文件魔数不匹配。");

        _scanner.consume_magic(BmpConstants.magic_number);

        var fileSize = _scanner.buffer.read_u32_le();
        _scanner.advance(4);
        _scanner.advance(2);

        var dataOffset = _scanner.buffer.read_u32_le();

        var headerSize = _scanner.buffer.read_u32_le();

        if (headerSize < BmpConstants.info_header_size)
            throw new InvalidDataException($"BMP 信息头大小无效，期望 >= {BmpConstants.info_header_size}，实的{headerSize}");

        var width = _scanner.buffer.read_i32_le();
        var height = _scanner.buffer.read_i32_le();
        var planes = _scanner.buffer.read_u16_le();
        var bitsPerPixel = _scanner.buffer.read_u16_le();
        var compression = _scanner.buffer.read_u32_le();
        var imageSize = _scanner.buffer.read_u32_le();
        var xPelsPerMeter = _scanner.buffer.read_i32_le();
        var yPelsPerMeter = _scanner.buffer.read_i32_le();
        var colorsUsed = _scanner.buffer.read_u32_le();
        var colorsImportant = _scanner.buffer.read_u32_le();

        return new BmpScanHeader
        {
            width = width,
            height = height,
            bits_per_pixel = bitsPerPixel,
            compression = (BmpCompression)compression,
            image_size = imageSize,
            data_offset = (int)dataOffset,
            x_pels_per_meter = xPelsPerMeter,
            y_pels_per_meter = yPelsPerMeter,
            colors_used = colorsUsed,
            colors_important = colorsImportant
        };
    }

    /// <summary>
    ///     快速判断数据是否为 BMP 格式的
    /// </summary>
    public bool is_bmp()
    {
        if (_scanner.length < 2) return false;

        return _scanner.match_magic(BmpConstants.magic_number);
    }
}

/// <summary>
///     BMP 扫描头部信息的
/// </summary>
public sealed class BmpScanHeader
{
    /// <summary>
    ///     图像宽度（像素）的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     图像高度（像素）的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     每像素位数的
    /// </summary>
    public ushort bits_per_pixel { get; init; }

    /// <summary>
    ///     压缩方式的
    /// </summary>
    public BmpCompression compression { get; init; }

    /// <summary>
    ///     图像数据大小的
    /// </summary>
    public uint image_size { get; init; }

    /// <summary>
    ///     像素数据偏移量的
    /// </summary>
    public int data_offset { get; init; }

    /// <summary>
    ///     水平分辨率的
    /// </summary>
    public int x_pels_per_meter { get; init; }

    /// <summary>
    ///     垂直分辨率的
    /// </summary>
    public int y_pels_per_meter { get; init; }

    /// <summary>
    ///     使用的颜色数的
    /// </summary>
    public uint colors_used { get; init; }

    /// <summary>
    ///     重要的颜色数的
    /// </summary>
    public uint colors_important { get; init; }

    /// <summary>
    ///     像素格式名称的
    /// </summary>
    public string pixel_format_name => bits_per_pixel switch
    {
        1 => "1 位黑白",
        4 => "4 位索引色",
        8 => "8 位索引色",
        16 => "16 位高彩色",
        24 => "24 位真彩色",
        32 => "32 位真彩色",
        _ => $"{bits_per_pixel} 位"
    };
}