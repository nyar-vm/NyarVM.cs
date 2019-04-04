using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Psd.Data;

namespace Std.Data.Binary.Psd.Encode;

/// <summary>
///     PSD 文件编码器，的C# 数据结构编码的Adobe Photoshop 文档格式的
/// </summary>
/// <remarks>
///     PSD 的Adobe Photoshop 的原生文件格式，支持图层、通道、蒙版等高级图像编辑功能的
///     编码器生成符的Adobe PSD 规范的二进制数据的
/// </remarks>
public sealed class PsdEncoder
{
    /// <summary>
    ///     的PSD 图像数据编码的PSD 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     PSD 图像数据的/param>
    ///     <returns>PSD 二进制数据的/returns>
    public byte[] encode(PsdImageData data)
    {
        var writer = new ByteBufferWriter(256);

        write_file_header(ref writer, data);
        write_color_mode_data(ref writer);
        write_image_resources(ref writer);
        write_layer_and_mask_info(ref writer, data.layers);
        write_image_data(ref writer, data);

        return writer.to_array();
    }

    #region 私有编码方法

    private static void write_file_header(ref ByteBufferWriter writer, PsdImageData data)
    {
        writer.write_string("8BPS");
        writer.write_u16_be(1);
        writer.write(new byte[6]);
        writer.write_u16_be((ushort)data.channels);
        writer.write_u32_be((uint)data.height);
        writer.write_u32_be((uint)data.width);
        writer.write_u16_be((ushort)data.depth);
        writer.write_u16_be((ushort)data.color_mode);
    }

    private static void write_color_mode_data(ref ByteBufferWriter writer)
    {
        writer.write_u32_be(0);
    }

    private static void write_image_resources(ref ByteBufferWriter writer)
    {
        writer.write_u32_be(0);
    }

    private static void write_layer_and_mask_info(ref ByteBufferWriter writer, IReadOnlyList<PsdLayer> layers)
    {
        if (layers.Count == 0)
        {
            writer.write_u32_be(0);
            return;
        }

        var layerInfoData = encode_layer_info(layers);

        writer.write_u32_be((uint)(4 + layerInfoData.Length));
        writer.write_u32_be((uint)layerInfoData.Length);
        writer.write(layerInfoData);
    }

    private static byte[] encode_layer_info(IReadOnlyList<PsdLayer> layers)
    {
        var writer = new ByteBufferWriter(256);

        writer.write_i16_be((short)layers.Count);

        foreach (var layer in layers) write_layer(ref writer, layer);

        foreach (var layer in layers) write_channel_image_data(ref writer, layer);

        if (writer.position % 2 != 0) writer.write_u8(0);

        return writer.to_array();
    }

    private static void write_layer(ref ByteBufferWriter writer, PsdLayer layer)
    {
        writer.write_i32_be(layer.bounds.Top);
        writer.write_i32_be(layer.bounds.Left);
        writer.write_i32_be(layer.bounds.Bottom);
        writer.write_i32_be(layer.bounds.Right);
        writer.write_u16_be((ushort)layer.channel_count);

        for (var i = 0; i < layer.channel_count; i++)
        {
            writer.write_i16_be((short)i);
            writer.write_u32_be(2);
        }

        writer.write_string("8BIM");
        writer.write_string(layer.blend_mode);
        writer.write_u8(layer.opacity);
        writer.write_u8(0);
        writer.write_u8((byte)(layer.is_visible ? 0 : 2));
        writer.write_u8(0);

        var nameBytes = Encoding.ASCII.GetBytes(layer.name);
        var extraDataLength = 4 + 4 + 1 + nameBytes.Length + ((nameBytes.Length + 1) % 2 != 0 ? 1 : 0);
        writer.write_u32_be((uint)extraDataLength);

        writer.write_u32_be(0);
        writer.write_u32_be(0);
        writer.write_u8((byte)nameBytes.Length);
        writer.write(nameBytes);

        if ((nameBytes.Length + 1) % 2 != 0) writer.write_u8(0);
    }

    private static void write_channel_image_data(ref ByteBufferWriter writer, PsdLayer layer)
    {
        for (var i = 0; i < layer.channel_count; i++) writer.write_u16_be(0);
    }

    private static void write_image_data(ref ByteBufferWriter writer, PsdImageData data)
    {
        writer.write_u16_be(0);

        if (data.merged_image_data != null) writer.write(data.merged_image_data);
    }

    #endregion
}