using Std.Data.Binary.Frame;
using Std.Data.Binary.Png.Data;

namespace Std.Data.Binary.Png.Scanner;

/// <summary>
///     PNG 文件扫描器，基于 <see cref="SpanScanner" /> 提供的PNG 图像文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     PNG 是无损压缩的位图格式，使的zlib/deflate 压缩，支持多种色彩类型和 Alpha 通道的
///     扫描器只读取 IHDR 块信息，不做完整的像素数据解码，以实现快速探查的
/// </remarks>
public ref struct PngScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="PngScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 PNG 字节数据的/param>
    public PngScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 PNG 文件头，提取基本图像信息的
    /// </summary>
    /// <returns>PNG 文件头信息的/returns>
    public PngScanHeader scan_header()
    {
        if (_scanner.length < PngConstants.signature_length + PngConstants.chunk_header_size +
            PngConstants.ihdr_data_length + PngConstants.chunk_crc_size)
            throw new InvalidDataException("PNG 文件数据过短，无法读取签名和 IHDR 块。");

        if (!_scanner.match_magic(PngConstants.signature)) throw new InvalidDataException("PNG 文件签名不匹配。");

        _scanner.consume_magic(PngConstants.signature);

        var length = _scanner.buffer.read_u32_be();
        var type = _scanner.buffer.read_string(4);

        if (type != PngConstants.ihdr_tag) throw new InvalidDataException($"PNG 第一个块不是 IHDR，实际为 \"{type}\"");

        var width = _scanner.buffer.read_u32_be();
        var height = _scanner.buffer.read_u32_be();
        var bitDepth = _scanner.buffer.read_u8();
        var colorType = (PngColorType)_scanner.buffer.read_u8();
        var compressionMethod = (PngCompressionMethod)_scanner.buffer.read_u8();
        var filterMethod = (PngFilterMethod)_scanner.buffer.read_u8();
        var interlaceMethod = (PngInterlaceMethod)_scanner.buffer.read_u8();

        return new PngScanHeader
        {
            width = (int)width,
            height = (int)height,
            bit_depth = bitDepth,
            color_type = colorType,
            compression_method = compressionMethod,
            filter_method = filterMethod,
            interlace_method = interlaceMethod
        };
    }

    /// <summary>
    ///     快速判断数据是否为 PNG 格式的
    /// </summary>
    public bool is_png()
    {
        if (_scanner.length < PngConstants.signature_length) return false;

        return _scanner.match_magic(PngConstants.signature);
    }
}

/// <summary>
///     PNG 扫描头部信息的
/// </summary>
public sealed class PngScanHeader
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
    ///     位深度的
    /// </summary>
    public byte bit_depth { get; init; }

    /// <summary>
    ///     色彩类型的
    /// </summary>
    public PngColorType color_type { get; init; }

    /// <summary>
    ///     压缩方法的
    /// </summary>
    public PngCompressionMethod compression_method { get; init; }

    /// <summary>
    ///     滤波方法的
    /// </summary>
    public PngFilterMethod filter_method { get; init; }

    /// <summary>
    ///     隔行扫描方法的
    /// </summary>
    public PngInterlaceMethod interlace_method { get; init; }

    /// <summary>
    ///     色彩类型名称的
    /// </summary>
    public string color_type_name => color_type switch
    {
        PngColorType.grayscale => "灰度",
        PngColorType.indexed => "索引色",
        PngColorType.truecolor => "真彩色",
        PngColorType.grayscale_alpha => "灰度+Alpha",
        PngColorType.truecolor_alpha => "真彩色+Alpha",
        _ => $"未知({(byte)color_type})"
    };
}