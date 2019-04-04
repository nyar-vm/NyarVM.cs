using Std.Data.Binary.Frame;
using Std.Data.Binary.Live2D.Data;

namespace Std.Data.Binary.Live2D.Decode;

/// <summary>
///     Live2D moc3 文件解码器，的moc3 二进制格式解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组的
///     解码器通过段偏移表定位各段数据，然后按 SoA 方式读取各字段的
/// </remarks>
public ref struct Live2DDecoder
{
    private ByteBuffer _buffer;

    public Live2DDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     的moc3 二进制数据解码完整的模型数据的
    /// </summary>
    /// <returns>解码后的 moc3 模型数据的/returns>
    public Live2DModelData decode_moc3()
    {
        var (version, isBigEndian, revision) = decode_header();

        var offsetCount = Live2DConstants.get_offset_table_count(version);
        var offsets = read_offset_table(offsetCount, isBigEndian);

        var parameterCount = read_count_info(offsets, (int)Moc3Section.count_info,
            Live2DConstants.count_info_parameter_count_offset, isBigEndian);
        var partCount = read_count_info(offsets, (int)Moc3Section.count_info,
            Live2DConstants.count_info_part_count_offset,
            isBigEndian);
        var drawableCount = read_count_info(offsets, (int)Moc3Section.count_info,
            Live2DConstants.count_info_drawable_count_offset, isBigEndian);
        var deformerCount = version >= 4
            ? read_count_info(offsets, (int)Moc3Section.count_info, Live2DConstants.count_info_deformer_count_offset,
                isBigEndian)
            : 0;
        var textureCount = read_count_info(offsets, (int)Moc3Section.count_info,
            Live2DConstants.count_info_texture_count_offset, isBigEndian);

        var canvas = decode_canvas_info(offsets, isBigEndian);
        var parameters = decode_parameters(offsets, parameterCount, isBigEndian);
        var parts = decode_parts(offsets, partCount, isBigEndian);
        var drawables = decode_drawables(offsets, drawableCount, isBigEndian);
        var deformers = version >= 4
            ? decode_deformers(offsets, deformerCount, isBigEndian)
            : [];

        return new Live2DModelData
        {
            version = version,
            is_big_endian = isBigEndian,
            revision = revision,
            canvas = canvas,
            parameters = parameters,
            parts = parts,
            drawables = drawables,
            deformers = deformers,
            texture_count = textureCount
        };
    }

    #region 头部解码

    /// <summary>
    ///     解码 moc3 文件头，返回版本、字节序和修订号的
    /// </summary>
    public (int Version, bool IsBigEndian, int Revision) decode_header()
    {
        var signature = _buffer.read_string(4);
        if (signature != "MOC3") throw new InvalidDataException($"moc3 文件签名无效，期的\"MOC3\"，实的\"{signature}\"");

        var version = _buffer.read_u8();
        var isBigEndian = _buffer.read_u8() != 0;
        var revision = _buffer.read_i16_le();

        return (version, isBigEndian, revision);
    }

    #endregion

    #region 段偏移表

    private int[] read_offset_table(int offsetCount, bool isBigEndian)
    {
        _buffer.position = Live2DConstants.header_size;

        var offsets = new int[offsetCount];
        for (var i = 0; i < offsetCount; i++) offsets[i] = read_i32(isBigEndian);

        return offsets;
    }

    private int read_count_info(int[] offsets, int countInfoSectionIndex, int fieldOffset, bool isBigEndian)
    {
        var sectionOffset = offsets[countInfoSectionIndex];
        if (sectionOffset == 0) return 0;

        _buffer.position = sectionOffset + fieldOffset;
        return read_i32(isBigEndian);
    }

    #endregion

    #region CanvasInfo 解码

    private Live2DCanvasInfo decode_canvas_info(int[] offsets, bool isBigEndian)
    {
        var offset = offsets[(int)Moc3Section.canvas_info];
        if (offset == 0) return new Live2DCanvasInfo();

        _buffer.position = offset;
        return new Live2DCanvasInfo
        {
            width = read_f32(isBigEndian),
            height = read_f32(isBigEndian),
            center_x = read_f32(isBigEndian),
            center_y = read_f32(isBigEndian),
            pixels_per_unit = read_f32(isBigEndian)
        };
    }

    #endregion

    #region Parameter 解码

    private List<Live2DParameter> decode_parameters(int[] offsets, int count, bool isBigEndian)
    {
        var ids = read_string_array(offsets, (int)Moc3Section.parameter_ids, count);
        var minValues = read_f32_array(offsets, (int)Moc3Section.parameter_minimum_values, count, isBigEndian);
        var maxValues = read_f32_array(offsets, (int)Moc3Section.parameter_maximum_values, count, isBigEndian);
        var defaultValues = read_f32_array(offsets, (int)Moc3Section.parameter_default_values, count, isBigEndian);

        var parameters = new List<Live2DParameter>(count);
        for (var i = 0; i < count; i++)
            parameters.Add(new Live2DParameter
            {
                id = ids[i],
                min_value = minValues[i],
                max_value = maxValues[i],
                default_value = defaultValues[i]
            });

        return parameters;
    }

    #endregion

    #region Part 解码

    private List<Live2DPart> decode_parts(int[] offsets, int count, bool isBigEndian)
    {
        var ids = read_string_array(offsets, (int)Moc3Section.part_ids, count);
        var parentIndices = read_i32_array(offsets, (int)Moc3Section.part_parent_part_indices, count, isBigEndian);

        var parts = new List<Live2DPart>(count);
        for (var i = 0; i < count; i++)
            parts.Add(new Live2DPart
            {
                id = ids[i],
                parent_index = parentIndices[i]
            });

        return parts;
    }

    #endregion

    #region Drawable 解码

    private List<Live2DDrawable> decode_drawables(int[] offsets, int count, bool isBigEndian)
    {
        var ids = read_string_array(offsets, (int)Moc3Section.drawable_ids, count);
        var textureIndices = read_i32_array(offsets, (int)Moc3Section.drawable_texture_indices, count, isBigEndian);
        var drawOrders = read_i32_array(offsets, (int)Moc3Section.drawable_draw_orders, count, isBigEndian);
        var renderOrders = read_i32_array(offsets, (int)Moc3Section.drawable_render_orders, count, isBigEndian);
        var maskCounts = read_i32_array(offsets, (int)Moc3Section.drawable_mask_counts, count, isBigEndian);
        var vertexCounts = read_i32_array(offsets, (int)Moc3Section.drawable_vertex_counts, count, isBigEndian);

        var maskOffset = offsets[(int)Moc3Section.drawable_masks];
        var vertexPositionsOffset = offsets[(int)Moc3Section.drawable_vertex_positions];
        var vertexUvsOffset = offsets[(int)Moc3Section.drawable_vertex_uvs];
        var indicesOffset = offsets[(int)Moc3Section.drawable_indices];

        var drawables = new List<Live2DDrawable>(count);
        var maskPos = maskOffset;
        var vertexPosPos = vertexPositionsOffset;
        var vertexUvPos = vertexUvsOffset;
        var indicesPos = indicesOffset;

        for (var i = 0; i < count; i++)
        {
            var maskDrawableIndices = read_mask_indices(ref maskPos, maskCounts[i], isBigEndian);
            var vertexPositions = read_f32_array_at(ref vertexPosPos, vertexCounts[i] * 2, isBigEndian);
            var vertexUvs = read_f32_array_at(ref vertexUvPos, vertexCounts[i] * 2, isBigEndian);

            var indexCount = estimate_index_count(vertexCounts[i]);
            var indices = read_i32_array_at(ref indicesPos, indexCount, isBigEndian);

            drawables.Add(new Live2DDrawable
            {
                id = ids[i],
                texture_index = textureIndices[i],
                draw_order = drawOrders[i],
                render_order = renderOrders[i],
                vertex_positions = [.. vertexPositions],
                vertex_uvs = [.. vertexUvs],
                indices = [.. indices],
                vertex_count = vertexCounts[i],
                mask_drawable_indices = maskDrawableIndices
            });
        }

        return drawables;
    }

    private List<int> read_mask_indices(ref int position, int maskCount, bool isBigEndian)
    {
        if (position == 0 || maskCount == 0) return [];

        _buffer.position = position;
        var result = new List<int>(maskCount);
        for (var i = 0; i < maskCount; i++) result.Add(read_i32(isBigEndian));

        position = _buffer.position;
        return result;
    }

    private static int estimate_index_count(int vertexCount)
    {
        return vertexCount > 2 ? (vertexCount - 2) * 3 : 0;
    }

    #endregion

    #region Deformer 解码

    private List<Live2DDeformer> decode_deformers(int[] offsets, int count, bool isBigEndian)
    {
        var ids = read_string_array(offsets, (int)Moc3Section.deformer_ids, count);
        var types = read_u8_array(offsets, (int)Moc3Section.deformer_types, count);
        var parentIndices = read_i32_array(offsets, (int)Moc3Section.deformer_parent_indices, count, isBigEndian);
        var boundingBoxX = read_f32_array(offsets, (int)Moc3Section.deformer_bounding_box_x, count, isBigEndian);

        var boundingBoxY = offsets.Length > (int)Moc3Section.deformer_bounding_box_y &&
                           offsets[(int)Moc3Section.deformer_bounding_box_y] != 0
            ? read_f32_array(offsets, (int)Moc3Section.deformer_bounding_box_y, count, isBigEndian)
            : new float[count];
        var boundingBoxWidth = offsets.Length > (int)Moc3Section.deformer_bounding_box_width &&
                               offsets[(int)Moc3Section.deformer_bounding_box_width] != 0
            ? read_f32_array(offsets, (int)Moc3Section.deformer_bounding_box_width, count, isBigEndian)
            : new float[count];
        var boundingBoxHeight = offsets.Length > (int)Moc3Section.deformer_bounding_box_height &&
                                offsets[(int)Moc3Section.deformer_bounding_box_height] != 0
            ? read_f32_array(offsets, (int)Moc3Section.deformer_bounding_box_height, count, isBigEndian)
            : new float[count];

        var deformers = new List<Live2DDeformer>(count);
        for (var i = 0; i < count; i++)
            deformers.Add(new Live2DDeformer
            {
                id = ids[i],
                type = types[i],
                parent_index = parentIndices[i],
                x = boundingBoxX[i],
                y = boundingBoxY[i],
                width = boundingBoxWidth[i],
                height = boundingBoxHeight[i]
            });

        return deformers;
    }

    #endregion

    #region 通用数组读取

    private string[] read_string_array(int[] offsets, int sectionIndex, int count)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0) return new string[count];

        _buffer.position = offset;
        var result = new string[count];
        for (var i = 0; i < count; i++) result[i] = _buffer.read_null_terminated_string();

        return result;
    }

    private float[] read_f32_array(int[] offsets, int sectionIndex, int count, bool isBigEndian)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0) return new float[count];

        _buffer.position = offset;
        return read_f32_array_at(offset, count, isBigEndian);
    }

    private float[] read_f32_array_at(int offset, int count, bool isBigEndian)
    {
        _buffer.position = offset;
        var result = new float[count];
        for (var i = 0; i < count; i++) result[i] = read_f32(isBigEndian);

        return result;
    }

    private float[] read_f32_array_at(ref int position, int count, bool isBigEndian)
    {
        if (position == 0 || count == 0) return new float[count];

        _buffer.position = position;
        var result = new float[count];
        for (var i = 0; i < count; i++) result[i] = read_f32(isBigEndian);

        position = _buffer.position;
        return result;
    }

    private int[] read_i32_array(int[] offsets, int sectionIndex, int count, bool isBigEndian)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0) return new int[count];

        _buffer.position = offset;
        var result = new int[count];
        for (var i = 0; i < count; i++) result[i] = read_i32(isBigEndian);

        return result;
    }

    private int[] read_i32_array_at(ref int position, int count, bool isBigEndian)
    {
        if (position == 0 || count == 0) return new int[count];

        _buffer.position = position;
        var result = new int[count];
        for (var i = 0; i < count; i++) result[i] = read_i32(isBigEndian);

        position = _buffer.position;
        return result;
    }

    private byte[] read_u8_array(int[] offsets, int sectionIndex, int count)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0) return new byte[count];

        _buffer.position = offset;
        var result = new byte[count];
        for (var i = 0; i < count; i++) result[i] = _buffer.read_u8();

        return result;
    }

    #endregion

    #region 字节序感知读的

    private int read_i32(bool isBigEndian)
    {
        return isBigEndian ? _buffer.read_i32_be() : _buffer.read_i32_le();
    }

    private float read_f32(bool isBigEndian)
    {
        return isBigEndian ? _buffer.read_f32_be() : _buffer.read_f32_le();
    }

    #endregion
}