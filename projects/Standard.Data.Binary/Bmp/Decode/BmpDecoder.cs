using Std.Data.Binary.Bmp.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Bmp.Decode;

/// <summary>
///     BMP 文件解码器，的BMP 图像格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     BMP 的Microsoft 的标准位图格式，广泛用于 Windows 应用和游戏开发的
///     支持 1/4/8/16/24/32 位色深，RLE 压缩和位域掩码的
/// </remarks>
public ref struct BmpDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="BmpDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">BMP 二进制数据的/param>
    public BmpDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 BMP 文件的
    /// </summary>
    /// <returns>BMP 图像数据的/returns>
    public BmpImageData decode()
    {
        var magic = _buffer.read_string(2);

        if (magic != BmpConstants.magic_tag) throw new InvalidDataException($"BMP 文件签名无效，期的\"BM\"，实的\"{magic}\"");

        var fileSize = _buffer.read_u32_le();
        _buffer.advance(4);
        _buffer.advance(2);
        var dataOffset = _buffer.read_u32_le();

        var headerSize = _buffer.read_u32_le();

        if (headerSize < BmpConstants.info_header_size)
            throw new InvalidDataException($"BMP 信息头大小无效，期望 >= {BmpConstants.info_header_size}，实的{headerSize}");

        var width = _buffer.read_i32_le();
        var height = _buffer.read_i32_le();
        var planes = _buffer.read_u16_le();
        var bitsPerPixel = _buffer.read_u16_le();
        var compression = (BmpCompression)_buffer.read_u32_le();
        var imageSize = _buffer.read_u32_le();
        var xPelsPerMeter = _buffer.read_i32_le();
        var yPelsPerMeter = _buffer.read_i32_le();
        var colorsUsed = _buffer.read_u32_le();
        var colorsImportant = _buffer.read_u32_le();

        var palette = read_palette(bitsPerPixel, colorsUsed);

        _buffer.position = (int)dataOffset;

        var actualImageSize = imageSize > 0 ? (int)imageSize : compute_image_size(width, height, bitsPerPixel);
        var pixelData = _buffer.read_bytes(actualImageSize).ToArray();

        return new BmpImageData
        {
            width = width,
            height = height,
            bits_per_pixel = bitsPerPixel,
            compression = compression,
            image_size = imageSize,
            x_pels_per_meter = xPelsPerMeter,
            y_pels_per_meter = yPelsPerMeter,
            colors_used = colorsUsed,
            colors_important = colorsImportant,
            palette = palette,
            pixel_data = pixelData
        };
    }

    /// <summary>
    ///     仅解的BMP 文件头信息的
    /// </summary>
    public (int Width, int Height, ushort BitsPerPixel, BmpCompression Compression) decode_header()
    {
        var magic = _buffer.read_string(2);

        if (magic != BmpConstants.magic_tag) throw new InvalidDataException($"BMP 文件签名无效，期的\"BM\"，实的\"{magic}\"");

        _buffer.advance(8);
        _buffer.advance(2);
        _buffer.advance(4);

        var headerSize = _buffer.read_u32_le();

        if (headerSize < BmpConstants.info_header_size)
            throw new InvalidDataException($"BMP 信息头大小无效，期望 >= {BmpConstants.info_header_size}，实的{headerSize}");

        var width = _buffer.read_i32_le();
        var height = _buffer.read_i32_le();
        _buffer.advance(2);
        var bitsPerPixel = _buffer.read_u16_le();
        var compression = (BmpCompression)_buffer.read_u32_le();

        return (width, height, bitsPerPixel, compression);
    }

    #region 私有解析方法

    private uint[] read_palette(ushort bitsPerPixel, uint colorsUsed)
    {
        if (bitsPerPixel > 8) return [];

        var maxColors = 1 << bitsPerPixel;
        var count = colorsUsed > 0 ? (int)System.Math.Min(colorsUsed, maxColors) : maxColors;
        var palette = new uint[count];

        for (var i = 0; i < count; i++)
        {
            var b = _buffer.read_u8();
            var g = _buffer.read_u8();
            var r = _buffer.read_u8();
            var reserved = _buffer.read_u8();
            palette[i] = (uint)((reserved << 24) | (r << 16) | (g << 8) | b);
        }

        return palette;
    }

    private static int compute_image_size(int width, int height, ushort bitsPerPixel)
    {
        var absHeight = System.Math.Abs(height);
        var stride = (width * bitsPerPixel + 31) / 32 * 4;
        return stride * absHeight;
    }

    #endregion
}