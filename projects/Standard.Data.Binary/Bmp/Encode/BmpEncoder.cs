using Std.Data.Binary.Bmp.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Bmp.Encode;

/// <summary>
///     BMP 文件编码器，的C# 数据结构编码的BMP 图像格式的
/// </summary>
/// <remarks>
///     BMP 的Microsoft 的标准位图格式，广泛用于 Windows 应用和游戏开发的
///     编码器生成符的BMP 规范的二进制数据，支的8/24/32 位色深的
/// </remarks>
public sealed class BmpEncoder
{
    /// <summary>
    ///     的BMP 图像数据编码的BMP 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     BMP 图像数据的/param>
    ///     <returns>BMP 二进制数据的/returns>
    public byte[] encode(BmpImageData data)
    {
        var size = estimate_size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        write_file_header(ref writer, data);
        write_info_header(ref writer, data);
        write_palette(ref writer, data);
        write_pixel_data(ref writer, data);

        return buffer[..writer.position];
    }

    #region 私有编码方法

    private static void write_file_header(ref ByteBufferWriter writer, BmpImageData data)
    {
        var paletteSize = data.palette.Length * 4;
        var pixelDataSize = data.pixel_data.Length > 0 ? data.pixel_data.Length : compute_image_size(data);
        var fileSize = BmpConstants.file_header_size + BmpConstants.info_header_size + paletteSize + pixelDataSize;
        var dataOffset = BmpConstants.file_header_size + BmpConstants.info_header_size + paletteSize;

        writer.write_string(BmpConstants.magic_tag);
        writer.write_u32_le((uint)fileSize);
        writer.write_u16_le(0);
        writer.write_u16_le(0);
        writer.write_u32_le((uint)dataOffset);
    }

    private static void write_info_header(ref ByteBufferWriter writer, BmpImageData data)
    {
        writer.write_u32_le(BmpConstants.info_header_size);
        writer.write_i32_le(data.width);
        writer.write_i32_le(data.height);
        writer.write_u16_le(1);
        writer.write_u16_le(data.bits_per_pixel);
        writer.write_u32_le((uint)data.compression);
        writer.write_u32_le(data.image_size > 0 ? data.image_size : (uint)compute_image_size(data));
        writer.write_i32_le(data.x_pels_per_meter);
        writer.write_i32_le(data.y_pels_per_meter);
        writer.write_u32_le(data.colors_used);
        writer.write_u32_le(data.colors_important);
    }

    private static void write_palette(ref ByteBufferWriter writer, BmpImageData data)
    {
        foreach (var color in data.palette)
        {
            writer.write_u8((byte)(color & 0xFF));
            writer.write_u8((byte)((color >> 8) & 0xFF));
            writer.write_u8((byte)((color >> 16) & 0xFF));
            writer.write_u8((byte)((color >> 24) & 0xFF));
        }
    }

    private static void write_pixel_data(ref ByteBufferWriter writer, BmpImageData data)
    {
        if (data.pixel_data.Length > 0) writer.write(data.pixel_data);
    }

    private static int compute_image_size(BmpImageData data)
    {
        var stride = (data.width * data.bits_per_pixel + 31) / 32 * 4;
        return stride * System.Math.Abs(data.height);
    }

    private static int estimate_size(BmpImageData data)
    {
        var paletteSize = data.palette.Length * 4;
        var pixelDataSize = data.pixel_data.Length > 0 ? data.pixel_data.Length : compute_image_size(data);
        return BmpConstants.file_header_size + BmpConstants.info_header_size + paletteSize + pixelDataSize + 256;
    }

    #endregion
}