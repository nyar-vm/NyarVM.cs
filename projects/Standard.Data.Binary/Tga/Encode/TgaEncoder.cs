using Std.Data.Binary.Frame;
using Std.Data.Binary.Tga.Data;

namespace Std.Data.Binary.Tga.Encode;

/// <summary>
///     TGA 文件编码器，的<see cref="TgaImageData" /> 数据结构编码的TGA 二进制格式的
/// </summary>
/// <remarks>
///     支持 24 位和 32 位未压缩 TGA 输出的
///     像素数据应为 TGA 原生 BGR/BGRA 顺序的
/// </remarks>
public sealed class TgaEncoder
{
    /// <summary>
    ///     的TGA 图像数据编码的TGA 二进制格式的
    /// </summary>
    /// <param name="image">
    ///     TGA 图像数据，像素数据应的BGR/BGRA 顺序的/param>
    ///     <returns>TGA 二进制数据的/returns>
    public byte[] encode(TgaImageData image)
    {
        var size = estimate_size(image);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        write_header(ref writer, image);

        if (image is { has_color_map: true, color_map.Length: > 0 }) write_color_map(ref writer, image);

        if (image is { id_length: > 0, image_id.Length: > 0 }) writer.write(image.image_id);

        writer.write(image.pixel_data);

        return buffer[..writer.position];
    }

    #region 私有编码方法

    private static void write_header(ref ByteBufferWriter writer, TgaImageData image)
    {
        writer.write_u8((byte)image.id_length);
        writer.write_u8(image.has_color_map ? (byte)1 : (byte)0);
        writer.write_u8((byte)image.image_type);

        writer.write_u16_le(0);
        writer.write_u16_le((ushort)(image.has_color_map ? image.color_map.Length / 4 : 0));
        writer.write_u16_le((ushort)(image.has_color_map ? 32 : 0));

        writer.write_u16_le(0);
        writer.write_u16_le(0);
        writer.write_u16_le((ushort)image.width);
        writer.write_u16_le((ushort)image.height);
        writer.write_u8((byte)image.pixel_depth);
        writer.write_u8(image.is_top_down ? (byte)0x28 : (byte)0x00);
    }

    private static void write_color_map(ref ByteBufferWriter writer, TgaImageData image)
    {
        var entryCount = image.color_map.Length / 4;

        for (var i = 0; i < entryCount; i++)
        {
            var offset = i * 4;
            writer.write_u8(image.color_map[offset + 2]);
            writer.write_u8(image.color_map[offset + 1]);
            writer.write_u8(image.color_map[offset]);
            writer.write_u8(image.color_map[offset + 3]);
        }
    }

    private static int estimate_size(TgaImageData image)
    {
        return TgaConstants.header_size + image.image_id.Length + image.color_map.Length + image.pixel_data.Length;
    }

    #endregion
}