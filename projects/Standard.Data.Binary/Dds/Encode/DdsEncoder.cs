using Std.Data.Binary.Dds.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dds.Encode;

/// <summary>
///     DDS 文件编码器，的C# 数据结构编码的DirectDraw Surface 纹理格式的
/// </summary>
/// <remarks>
///     DDS 的Microsoft DirectDraw 的纹理容器格式，广泛用于 PC 游戏和图形应用的
///     编码器生成符的Microsoft DDS 规范的二进制数据的
/// </remarks>
public sealed class DdsEncoder
{
    /// <summary>
    ///     的DDS 纹理数据编码的DDS 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     DDS 纹理数据的/param>
    ///     <returns>DDS 二进制数据的/returns>
    public byte[] encode(DdsTextureData data)
    {
        var size = estimate_size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        write_magic(ref writer);
        write_header(ref writer, data);
        write_surfaces(ref writer, data);

        return buffer[..writer.position];
    }

    #region 私有编码方法

    private static void write_magic(ref ByteBufferWriter writer)
    {
        writer.write_string("DDS ");
    }

    private static void write_header(ref ByteBufferWriter writer, DdsTextureData data)
    {
        writer.write_u32_le(DdsConstants.header_size);

        var flags = DdsFlags.height | DdsFlags.width | DdsFlags.pixel_format;

        if (data.mip_map_count > 1) flags |= DdsFlags.mip_map_count | DdsFlags.linear_size;

        if (data.depth > 0) flags |= DdsFlags.depth;

        writer.write_u32_le((uint)flags);
        writer.write_u32_le((uint)data.height);
        writer.write_u32_le((uint)data.width);
        writer.write_u32_le(0);
        writer.write_u32_le((uint)data.depth);
        writer.write_u32_le((uint)data.mip_map_count);

        writer.write(new byte[44]);

        write_pixel_format(ref writer, data.pixel_format);

        var caps1 = 0x1000u;

        if (data.mip_map_count > 1) caps1 |= 0x400008;

        writer.write_u32_le(caps1);

        var caps2 = 0u;

        if (data.is_cube_map) caps2 |= 0x200 | 0x400 | 0x800 | 0x1000 | 0x2000 | 0x4000 | 0x8000;

        writer.write_u32_le(caps2);
        writer.write_u32_le(0);
        writer.write_u32_le(0);
        writer.write_u32_le(0);
    }

    private static void write_pixel_format(ref ByteBufferWriter writer, DdsPixelFormatData format)
    {
        writer.write_u32_le(DdsConstants.pixel_format_size);
        writer.write_u32_le((uint)format.flags);
        writer.write_u32_le(format.four_cc);
        writer.write_u32_le(format.rgb_bit_count);
        writer.write_u32_le(format.r_bit_mask);
        writer.write_u32_le(format.g_bit_mask);
        writer.write_u32_le(format.b_bit_mask);
        writer.write_u32_le(format.a_bit_mask);
    }

    private static void write_surfaces(ref ByteBufferWriter writer, DdsTextureData data)
    {
        foreach (var surface in data.surfaces)
        foreach (var mip in surface.mip_levels)
            writer.write(mip.data);
    }

    private static int estimate_size(DdsTextureData data)
    {
        var headerSize = DdsConstants.full_header_size;
        var dataSize = 0;

        foreach (var surface in data.surfaces)
        foreach (var mip in surface.mip_levels)
            dataSize += mip.data.Length;

        return headerSize + dataSize + 256;
    }

    #endregion
}