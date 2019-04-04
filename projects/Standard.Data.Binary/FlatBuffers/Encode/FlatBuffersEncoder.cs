using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.FlatBuffers.Data;

namespace Std.Data.Binary.FlatBuffers.Encode;

/// <summary>
///     FlatBuffers 编码器，的<see cref="FlatBufferData" /> 编码的FlatBuffers 二进制格式的
/// </summary>
public sealed class FlatBuffersEncoder
{
    /// <summary>
    ///     的FlatBuffer 数据编码为二进制字节数组的
    /// </summary>
    /// <param name="data">
    ///     FlatBuffer 数据，包含根表和可选文件标识符的/param>
    ///     <returns>FlatBuffers 二进制数据的/returns>
    public byte[] encode(FlatBufferData data)
    {
        var fields = data.root_table.fields;

        return encode_table(fields, data.file_identifier);
    }

    /// <summary>
    ///     将单个表编码的FlatBuffers 二进制的
    /// </summary>
    /// <param name="table">
    ///     表数据的/param>
    ///     <param name="fileIdentifier">
    ///         可选文件标识符的/param>
    ///         <returns>FlatBuffers 二进制数据的/returns>
    public byte[] encode_table(FlatBufferTable table, string? fileIdentifier = null)
    {
        return encode_table(table.fields, fileIdentifier);
    }

    private static byte[] encode_table(IReadOnlyList<FlatBufferField> fields, string? fileIdentifier)
    {
        var hasFileId = !string.IsNullOrEmpty(fileIdentifier);
        var headerSize = hasFileId ? 8 : 4;

        // 计算每个字段的序列化数据和大小
        var fieldInfos = new (int size, byte[]? data)[fields.Count];
        var maxOffset = 0;

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var size = get_type_size(field.type);
            var serialized = serialize_value(field.value, field.type);

            fieldInfos[i] = (size, serialized);

            var endOffset = field.v_table_offset + size;

            if (endOffset > maxOffset) maxOffset = endOffset;
        }

        // VTable 布局：[大小:2][对象大小:2][字段偏移0:2]...[字段偏移N:2]
        var vtableSize = FlatBuffersConstants.v_table_field_start +
                         fields.Count * FlatBuffersConstants.v_table_offset_size;
        var objectSize = maxOffset;
        var tableDataSize = FlatBuffersConstants.file_identifier_size + objectSize; // soffset(4) + field data

        var totalSize = headerSize + vtableSize + tableDataSize;
        var buffer = new byte[totalSize];
        var pos = headerSize;

        // 写入 VTable
        var vtableStart = pos;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), (ushort)vtableSize);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), (ushort)objectSize);
        pos += 2;

        for (var i = 0; i < fields.Count; i++)
        {
            var voffset = fields[i].v_table_offset;
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), voffset);
            pos += 2;
        }

        // 写入 Table
        var tableStart = pos;

        // soffset 指向 VTable（相对于 table 起始的偏移）
        var soffset = vtableStart - tableStart;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), soffset);
        pos += 4;

        // 写入各字段数据
        for (var i = 0; i < fields.Count; i++)
        {
            var (size, data) = fieldInfos[i];

            if (data != null)
            {
                var fieldPos = tableStart + fields[i].v_table_offset;
                Array.Copy(data, 0, buffer, fieldPos, data.Length);
            }
        }

        // 写入根偏移（位置 0）
        var rootOffset = (uint)tableStart;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0), rootOffset);

        // 写入文件标识
        if (hasFileId)
        {
            var fileId = fileIdentifier!.PadRight(4).Substring(0, 4);
            Encoding.ASCII.GetBytes(fileId, 0, 4, buffer, 4);
        }

        return buffer;
    }

    /// <summary>
    ///     获取字段类型对应的字节大小的
    /// </summary>
    private static int get_type_size(FlatBufferFieldType type)
    {
        return type switch
        {
            FlatBufferFieldType.@bool or FlatBufferFieldType.@byte or FlatBufferFieldType.u_byte => 1,
            FlatBufferFieldType.@short or FlatBufferFieldType.u_short => 2,
            FlatBufferFieldType.@int or FlatBufferFieldType.u_int or FlatBufferFieldType.@float => 4,
            FlatBufferFieldType.@long or FlatBufferFieldType.u_long or FlatBufferFieldType.@double => 8,
            _ => 4
        };
    }

    /// <summary>
    ///     将字段值序列化为字节数组的
    /// </summary>
    private static byte[]? serialize_value(object? value, FlatBufferFieldType type)
    {
        if (value == null) return null;

        return type switch
        {
            FlatBufferFieldType.@bool => [(byte)((bool)value ? 1 : 0)],
            FlatBufferFieldType.@byte => [(byte)(sbyte)value],
            FlatBufferFieldType.u_byte => [(byte)value],
            FlatBufferFieldType.@short => BitConverter.GetBytes((short)value),
            FlatBufferFieldType.u_short => BitConverter.GetBytes((ushort)value),
            FlatBufferFieldType.@int => BitConverter.GetBytes((int)value),
            FlatBufferFieldType.u_int => BitConverter.GetBytes((uint)value),
            FlatBufferFieldType.@float => BitConverter.GetBytes((float)value),
            FlatBufferFieldType.@long => BitConverter.GetBytes((long)value),
            FlatBufferFieldType.u_long => BitConverter.GetBytes((ulong)value),
            FlatBufferFieldType.@double => BitConverter.GetBytes((double)value),
            _ => null
        };
    }
}