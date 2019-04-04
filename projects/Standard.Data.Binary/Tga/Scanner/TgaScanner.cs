using Std.Data.Binary.Frame;
using Std.Data.Binary.Tga.Data;

namespace Std.Data.Binary.Tga.Scanner;

/// <summary>
///     TGA 文件扫描器，提供的TGA 图像文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     TGA 是一种位图图像格式，广泛用于游戏开发中的纹理资源的
///     扫描器只读取文件头信息，不做完整的像素数据解码，以实现快速探查的
/// </remarks>
public ref struct TgaScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="TgaScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 TGA 字节数据的/param>
    public TgaScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 TGA 文件头，提取基本图像信息的
    /// </summary>
    /// <returns>TGA 文件头信息的/returns>
    public TgaScanHeader scan_header()
    {
        if (_scanner.length < TgaConstants.header_size) throw new InvalidDataException("TGA 文件数据过短，无法读取文件头");

        var idLength = _scanner.buffer.read_u8();
        var colorMapType = _scanner.buffer.read_u8();
        var imageType = _scanner.buffer.read_u8();

        _scanner.buffer.read_u16_le();
        var colorMapLength = _scanner.buffer.read_u16_le();
        var colorMapEntrySize = _scanner.buffer.read_u8();

        _scanner.buffer.read_u16_le();
        _scanner.buffer.read_u16_le();

        var width = _scanner.buffer.read_u16_le();
        var height = _scanner.buffer.read_u16_le();
        var pixelDepth = _scanner.buffer.read_u8();
        var imageDescriptor = _scanner.buffer.read_u8();

        var isTopDown = (imageDescriptor & 0x20) != 0;
        var hasColorMap = colorMapType == 1;

        return new TgaScanHeader
        {
            id_length = idLength,
            has_color_map = hasColorMap,
            image_type = (TgaImageType)imageType,
            color_map_length = colorMapLength,
            color_map_entry_size = colorMapEntrySize,
            width = width,
            height = height,
            pixel_depth = pixelDepth,
            is_top_down = isTopDown,
            has_footer = check_footer()
        };
    }

    /// <summary>
    ///     快速判断数据是否为 TGA 格式的
    /// </summary>
    public bool is_tga()
    {
        if (_scanner.length < TgaConstants.header_size) return false;

        var colorMapType = _scanner.data[1];
        var imageType = _scanner.data[2];

        if (colorMapType > 1) return false;

        if (!is_valid_image_type(imageType)) return false;

        return true;
    }

    /// <summary>
    ///     检的TGA 文件是否包含文件尾的
    /// </summary>
    public bool check_footer()
    {
        if (_scanner.length < TgaConstants.footer_signature_length) return false;

        var footerStart = _scanner.length - TgaConstants.footer_signature_length;
        var signature = TgaConstants.footer_signature;
        var data = _scanner.data;

        for (var i = 0; i < signature.Length; i++)
            if (data[footerStart + i] != signature[i])
                return false;

        return true;
    }

    #region 私有方法

    private static bool is_valid_image_type(byte imageType)
    {
        return imageType is 0 or 1 or 2 or 3 or 9 or 10 or 11;
    }

    #endregion
}

/// <summary>
///     TGA 扫描头部信息的
/// </summary>
public sealed class TgaScanHeader
{
    /// <summary>
    ///     图像 ID 长度的
    /// </summary>
    public int id_length { get; init; }

    /// <summary>
    ///     是否包含调色板的
    /// </summary>
    public bool has_color_map { get; init; }

    /// <summary>
    ///     图像类型的
    /// </summary>
    public TgaImageType image_type { get; init; }

    /// <summary>
    ///     调色板条目数的
    /// </summary>
    public ushort color_map_length { get; init; }

    /// <summary>
    ///     调色板条目位数的
    /// </summary>
    public byte color_map_entry_size { get; init; }

    /// <summary>
    ///     图像宽度（像素）的
    /// </summary>
    public ushort width { get; init; }

    /// <summary>
    ///     图像高度（像素）的
    /// </summary>
    public ushort height { get; init; }

    /// <summary>
    ///     每像素位数的
    /// </summary>
    public byte pixel_depth { get; init; }

    /// <summary>
    ///     是否为从上到下的行序的
    /// </summary>
    public bool is_top_down { get; init; }

    /// <summary>
    ///     是否包含文件尾签名的
    /// </summary>
    public bool has_footer { get; init; }

    /// <summary>
    ///     图像类型名称的
    /// </summary>
    public string image_type_name => image_type switch
    {
        TgaImageType.no_data => "无数据",
        TgaImageType.uncompressed_color_map => "未压缩调色板",
        TgaImageType.uncompressed_truecolor => "未压缩真彩色",
        TgaImageType.uncompressed_grayscale => "未压缩灰度",
        TgaImageType.rle_color_map => "RLE 调色板",
        TgaImageType.rle_truecolor => "RLE 真彩色",
        TgaImageType.rle_grayscale => "RLE 灰度",
        _ => $"未知({(byte)image_type})"
    };
}