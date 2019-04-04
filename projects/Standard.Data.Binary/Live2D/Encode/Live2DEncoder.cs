using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Live2D.Data;

namespace Std.Data.Binary.Live2D.Encode;

/// <summary>
///     Live2D moc3 文件编码器，的C# 数据结构编码的Live2D Cubism moc3 二进制格式的
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组的
///     编码器按以下顺序写入数据的
///     1. 文件头（8 字节：魔的+ 版本 + 标志 + 修订号）
///     2. 段偏移表（i32 数组，每个条目指向对应段在文件中的偏移量的
///     3. 各数据段（CanvasInfo、CountInfo、ParameterIds的..的
/// </remarks>
public sealed class Live2DEncoder
{
    /// <summary>
    ///     的moc3 模型数据编码的moc3 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     moc3 模型数据的/param>
    ///     <returns>moc3 二进制数据的/returns>
    public byte[] encode_moc3(Live2DModelData data)
    {
        var size = estimate_moc3_size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        var offsetCount = Live2DConstants.get_offset_table_count(data.version);
        var offsets = new int[offsetCount];

        write_moc3_header(ref writer, data);
        write_offset_table_placeholder(ref writer, offsetCount);
        write_data_sections(ref writer, data, offsets);
        patch_offset_table(ref writer, offsets, data.is_big_endian);

        return [.. writer.written_data];
    }

    #region 头部编码

    private static void write_moc3_header(ref ByteBufferWriter writer, Live2DModelData data)
    {
        writer.write_string("MOC3");
        writer.write_u8((byte)data.version);
        writer.write_u8((byte)(data.is_big_endian ? 1 : 0));
        writer.write_i16_le((short)data.revision);
    }

    #endregion

    #region 段偏移表

    private static void write_offset_table_placeholder(ref ByteBufferWriter writer, int offsetCount)
    {
        for (var i = 0; i < offsetCount; i++) writer.write_i32_le(0);
    }

    private static void patch_offset_table(ref ByteBufferWriter writer, int[] offsets, bool isBigEndian)
    {
        var offsetTablePosition = Live2DConstants.header_size;

        for (var i = 0; i < offsets.Length; i++)
        {
            var position = offsetTablePosition + i * Live2DConstants.offset_table_entry_size;

            if (isBigEndian)
                write_i32_at(ref writer, position, offsets[i], true);
            else
                write_i32_at(ref writer, position, offsets[i], false);
        }
    }

    private static void write_i32_at(ref ByteBufferWriter writer, int position, int value, bool bigEndian)
    {
        var span = writer.get_span();
        if (bigEndian)
        {
            span[position] = (byte)(value >> 24);
            span[position + 1] = (byte)(value >> 16);
            span[position + 2] = (byte)(value >> 8);
            span[position + 3] = (byte)value;
        }
        else
        {
            span[position] = (byte)value;
            span[position + 1] = (byte)(value >> 8);
            span[position + 2] = (byte)(value >> 16);
            span[position + 3] = (byte)(value >> 24);
        }
    }

    #endregion

    #region 数据段编的

    private static void write_data_sections(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        write_canvas_info_section(ref writer, data, offsets);
        write_count_info_section(ref writer, data, offsets);
        write_parameter_ids_section(ref writer, data, offsets);
        write_parameter_minimum_values_section(ref writer, data, offsets);
        write_parameter_maximum_values_section(ref writer, data, offsets);
        write_parameter_default_values_section(ref writer, data, offsets);
        write_part_ids_section(ref writer, data, offsets);
        write_part_parent_part_indices_section(ref writer, data, offsets);
        write_drawable_ids_section(ref writer, data, offsets);
        write_drawable_constant_flags_section(ref writer, data, offsets);
        write_drawable_texture_indices_section(ref writer, data, offsets);
        write_drawable_draw_orders_section(ref writer, data, offsets);
        write_drawable_render_orders_section(ref writer, data, offsets);
        write_drawable_mask_counts_section(ref writer, data, offsets);
        write_drawable_masks_section(ref writer, data, offsets);
        write_drawable_vertex_counts_section(ref writer, data, offsets);
        write_drawable_vertex_positions_section(ref writer, data, offsets);
        write_drawable_vertex_uvs_section(ref writer, data, offsets);
        write_drawable_indices_section(ref writer, data, offsets);

        if (data.version >= 3) write_drawable_repeat_flags_section(ref writer, data, offsets);

        if (data.version >= 4)
        {
            write_deformer_ids_section(ref writer, data, offsets);
            write_deformer_types_section(ref writer, data, offsets);
            write_deformer_parent_indices_section(ref writer, data, offsets);
            write_deformer_bounding_box_x_section(ref writer, data, offsets);
        }

        if (data.version >= 5)
        {
            write_deformer_bounding_box_y_section(ref writer, data, offsets);
            write_deformer_bounding_box_width_section(ref writer, data, offsets);
            write_deformer_bounding_box_height_section(ref writer, data, offsets);
            write_deformer_rotation_section(ref writer, data, offsets);
        }
    }

    private static void write_canvas_info_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.canvas_info, offsets);
        write_f32(ref writer, data.canvas.width, data.is_big_endian);
        write_f32(ref writer, data.canvas.height, data.is_big_endian);
        write_f32(ref writer, data.canvas.center_x, data.is_big_endian);
        write_f32(ref writer, data.canvas.center_y, data.is_big_endian);
        write_f32(ref writer, data.canvas.pixels_per_unit, data.is_big_endian);
    }

    private static void write_count_info_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.count_info, offsets);
        write_i32(ref writer, data.parameters.Count, data.is_big_endian);
        write_i32(ref writer, data.parts.Count, data.is_big_endian);
        write_i32(ref writer, data.drawables.Count, data.is_big_endian);
        write_i32(ref writer, data.deformers.Count, data.is_big_endian);
        write_i32(ref writer, data.texture_count, data.is_big_endian);
    }

    private static void write_parameter_ids_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.parameter_ids, offsets);
        foreach (var parameter in data.parameters) writer.write_null_terminated_string(parameter.id);
    }

    private static void write_parameter_minimum_values_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.parameter_minimum_values, offsets);
        foreach (var parameter in data.parameters) write_f32(ref writer, parameter.min_value, data.is_big_endian);
    }

    private static void write_parameter_maximum_values_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.parameter_maximum_values, offsets);
        foreach (var parameter in data.parameters) write_f32(ref writer, parameter.max_value, data.is_big_endian);
    }

    private static void write_parameter_default_values_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.parameter_default_values, offsets);
        foreach (var parameter in data.parameters) write_f32(ref writer, parameter.default_value, data.is_big_endian);
    }

    private static void write_part_ids_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.part_ids, offsets);
        foreach (var part in data.parts) writer.write_null_terminated_string(part.id);
    }

    private static void write_part_parent_part_indices_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.part_parent_part_indices, offsets);
        foreach (var part in data.parts) write_i32(ref writer, part.parent_index, data.is_big_endian);
    }

    private static void write_drawable_ids_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_ids, offsets);
        foreach (var drawable in data.drawables) writer.write_null_terminated_string(drawable.id);
    }

    private static void write_drawable_constant_flags_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_constant_flags, offsets);
        foreach (var drawable in data.drawables)
        {
            var flags = compute_drawable_constant_flags(drawable);
            writer.write_u8(flags);
        }
    }

    private static void write_drawable_texture_indices_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_texture_indices, offsets);
        foreach (var drawable in data.drawables) write_i32(ref writer, drawable.texture_index, data.is_big_endian);
    }

    private static void write_drawable_draw_orders_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_draw_orders, offsets);
        foreach (var drawable in data.drawables) write_i32(ref writer, drawable.draw_order, data.is_big_endian);
    }

    private static void write_drawable_render_orders_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_render_orders, offsets);
        foreach (var drawable in data.drawables) write_i32(ref writer, drawable.render_order, data.is_big_endian);
    }

    private static void write_drawable_mask_counts_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_mask_counts, offsets);
        foreach (var drawable in data.drawables)
            write_i32(ref writer, drawable.mask_drawable_indices.Count, data.is_big_endian);
    }

    private static void write_drawable_masks_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_masks, offsets);
        foreach (var drawable in data.drawables)
        foreach (var maskIndex in drawable.mask_drawable_indices)
            write_i32(ref writer, maskIndex, data.is_big_endian);
    }

    private static void write_drawable_vertex_counts_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_vertex_counts, offsets);
        foreach (var drawable in data.drawables) write_i32(ref writer, drawable.vertex_count, data.is_big_endian);
    }

    private static void write_drawable_vertex_positions_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_vertex_positions, offsets);
        foreach (var drawable in data.drawables)
            for (var i = 0; i < drawable.vertex_positions.Count; i++)
                write_f32(ref writer, drawable.vertex_positions[i], data.is_big_endian);
    }

    private static void write_drawable_vertex_uvs_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_vertex_uvs, offsets);
        foreach (var drawable in data.drawables)
            for (var i = 0; i < drawable.vertex_uvs.Count; i++)
                write_f32(ref writer, drawable.vertex_uvs[i], data.is_big_endian);
    }

    private static void write_drawable_indices_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_indices, offsets);
        foreach (var drawable in data.drawables)
        foreach (var index in drawable.indices)
            write_i32(ref writer, index, data.is_big_endian);
    }

    private static void write_drawable_repeat_flags_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.drawable_repeat_flags, offsets);
        foreach (var _ in data.drawables) writer.write_u8(0);
    }

    private static void write_deformer_ids_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_ids, offsets);
        foreach (var deformer in data.deformers) writer.write_null_terminated_string(deformer.id);
    }

    private static void write_deformer_types_section(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_types, offsets);
        foreach (var deformer in data.deformers) writer.write_u8((byte)deformer.type);
    }

    private static void write_deformer_parent_indices_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_parent_indices, offsets);
        foreach (var deformer in data.deformers) write_i32(ref writer, deformer.parent_index, data.is_big_endian);
    }

    private static void write_deformer_bounding_box_x_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_bounding_box_x, offsets);
        foreach (var deformer in data.deformers) write_f32(ref writer, deformer.x, data.is_big_endian);
    }

    private static void write_deformer_bounding_box_y_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_bounding_box_y, offsets);
        foreach (var deformer in data.deformers) write_f32(ref writer, deformer.y, data.is_big_endian);
    }

    private static void write_deformer_bounding_box_width_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_bounding_box_width, offsets);
        foreach (var deformer in data.deformers) write_f32(ref writer, deformer.width, data.is_big_endian);
    }

    private static void write_deformer_bounding_box_height_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_bounding_box_height, offsets);
        foreach (var deformer in data.deformers) write_f32(ref writer, deformer.height, data.is_big_endian);
    }

    private static void write_deformer_rotation_section(ref ByteBufferWriter writer, Live2DModelData data,
        int[] offsets)
    {
        record_offset(ref writer, (int)Moc3Section.deformer_rotation, offsets);
        foreach (var _ in data.deformers) write_f32(ref writer, 0.0f, data.is_big_endian);
    }

    #endregion

    #region 辅助方法

    private static void record_offset(ref ByteBufferWriter writer, int sectionIndex, int[] offsets)
    {
        if (sectionIndex < offsets.Length) offsets[sectionIndex] = writer.position;
    }

    private static void write_i32(ref ByteBufferWriter writer, int value, bool isBigEndian)
    {
        if (isBigEndian)
            writer.write_i32_be(value);
        else
            writer.write_i32_le(value);
    }

    private static void write_f32(ref ByteBufferWriter writer, float value, bool isBigEndian)
    {
        if (isBigEndian)
            writer.write_f32_be(value);
        else
            writer.write_f32_le(value);
    }

    private static byte compute_drawable_constant_flags(Live2DDrawable drawable)
    {
        byte flags = 0;

        if (drawable.blend_mode == 1)
            flags |= 0x01;
        else if (drawable.blend_mode == 2) flags |= 0x02;

        if (drawable.flip_uv_y) flags |= 0x04;

        return flags;
    }

    private static int estimate_moc3_size(Live2DModelData data)
    {
        var size = Live2DConstants.header_size;
        size += Live2DConstants.get_offset_table_size(data.version);

        size += 5 * 4;
        size += 5 * 4;

        size += estimate_string_array_size(data.parameters, p => p.id);
        size += data.parameters.Count * 4;
        size += data.parameters.Count * 4;
        size += data.parameters.Count * 4;

        size += estimate_string_array_size(data.parts, p => p.id);
        size += data.parts.Count * 4;

        size += estimate_string_array_size(data.drawables, d => d.id);
        size += data.drawables.Count;
        size += data.drawables.Count * 4;
        size += data.drawables.Count * 4;
        size += data.drawables.Count * 4;
        size += data.drawables.Count * 4;
        size += data.drawables.Count * 4;
        size += data.drawables.Count * 4;

        foreach (var drawable in data.drawables)
        {
            size += drawable.vertex_positions.Count * 4;
            size += drawable.vertex_uvs.Count * 4;
            size += drawable.indices.Count * 4;
        }

        if (data.version >= 3) size += data.drawables.Count;

        if (data.version >= 4)
        {
            size += estimate_string_array_size(data.deformers, d => d.id);
            size += data.deformers.Count;
            size += data.deformers.Count * 4;
            size += data.deformers.Count * 4;
        }

        if (data.version >= 5)
        {
            size += data.deformers.Count * 4;
            size += data.deformers.Count * 4;
            size += data.deformers.Count * 4;
            size += data.deformers.Count * 4;
        }

        size += 4096;

        return size;
    }

    private static int estimate_string_array_size<T>(IReadOnlyList<T> items, Func<T, string> idSelector)
    {
        var size = 0;
        foreach (var item in items) size += Encoding.UTF8.GetByteCount(idSelector(item)) + 1;

        return size;
    }

    #endregion
}