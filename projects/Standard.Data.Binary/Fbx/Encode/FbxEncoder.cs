using System.Text;
using Std.Data.Binary.Fbx.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Fbx.Encode;

/// <summary>
///     FBX 二进制文件编码器，将 C# 数据结构编码的Autodesk FBX 格式的
/// </summary>
/// <remarks>
///     FBX 的Autodesk 的3D 模型/动画交换格式，游戏行业事实标准的
///     编码器生成符的FBX 二进制规范的二进制数据的
/// </remarks>
public sealed class FbxEncoder
{
    /// <summary>
    ///     的FBX 文件数据编码的FBX 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     FBX 文件数据的/param>
    ///     <returns>FBX 二进制数据的/returns>
    public byte[] encode(FbxFileData data)
    {
        var size = estimate_size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        write_file_header(ref writer, data);

        foreach (var child in data.root.children) write_node(ref writer, child, data.version);

        write_null_record(ref writer);

        return buffer[..writer.position];
    }

    #region 私有编码方法

    private static void write_file_header(ref ByteBufferWriter writer, FbxFileData data)
    {
        writer.write(FbxConstants.binary_magic);

        var paddingNeeded = FbxConstants.magic_length - FbxConstants.binary_magic.Length;

        for (var i = 0; i < paddingNeeded; i++) writer.write_u8(0);

        writer.write_u32_le((uint)data.version);
    }

    private static void write_node(ref ByteBufferWriter writer, FbxNode node, int version)
    {
        var use64Bit = version >= FbxVersions.v75;

        var nameBytes = Encoding.ASCII.GetBytes(node.name);
        var propertyListData = build_property_list(node.properties);
        var propertyListLength = propertyListData.Length;

        var headerSize = use64Bit ? 8 + 8 + 8 + 1 : 4 + 4 + 4 + 1;
        var nodeSize = headerSize + nameBytes.Length + propertyListLength;

        foreach (var child in node.children) nodeSize += compute_node_size(child, version);

        nodeSize += 13;

        var endOffset = writer.position + nodeSize;

        if (use64Bit)
        {
            writer.write_u64_le((ulong)endOffset);
            writer.write_u64_le((ulong)node.properties.Count);
            writer.write_u64_le((ulong)propertyListLength);
        }
        else
        {
            writer.write_u32_le((uint)endOffset);
            writer.write_u32_le((uint)node.properties.Count);
            writer.write_u32_le((uint)propertyListLength);
        }

        writer.write_u8((byte)nameBytes.Length);
        writer.write(nameBytes);
        writer.write(propertyListData);

        foreach (var child in node.children) write_node(ref writer, child, version);

        write_null_record(ref writer);
    }

    private static void write_null_record(ref ByteBufferWriter writer)
    {
        writer.write(FbxConstants.null_record);
    }

    private static byte[] build_property_list(IReadOnlyList<FbxProperty> properties)
    {
        var size = 0;

        foreach (var prop in properties) size += estimate_property_size(prop);

        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        foreach (var prop in properties) write_property(ref writer, prop);

        return [.. buffer[..writer.position]];
    }

    private static void write_property(ref ByteBufferWriter writer, FbxProperty property)
    {
        writer.write_u8(property.type_code);

        switch (property.type_code)
        {
            case FbxConstants.PropertyType.boolean:
                writer.write_u8((byte)((bool)property.value! ? 1 : 0));
                break;

            case FbxConstants.PropertyType.int8:
                writer.write_u8((byte)(short)property.value!);
                break;

            case FbxConstants.PropertyType.int16:
                writer.write_i16_le((short)property.value!);
                break;

            case FbxConstants.PropertyType.int32:
                writer.write_i32_le((int)property.value!);
                break;

            case FbxConstants.PropertyType.int64:
                writer.write_i64_le((long)property.value!);
                break;

            case FbxConstants.PropertyType.float32:
                writer.write_f32_le((float)property.value!);
                break;

            case FbxConstants.PropertyType.float64:
                writer.write_f64_le((double)property.value!);
                break;

            case FbxConstants.PropertyType.@string:
                write_string_property(ref writer, (string)property.value!);
                break;

            case FbxConstants.PropertyType.raw_buffer:
                write_raw_property(ref writer, (byte[])property.value!);
                break;

            default:
                write_array_property(ref writer, property);
                break;
        }
    }

    private static void write_string_property(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        writer.write_u32_le((uint)bytes.Length);
        writer.write(bytes);
    }

    private static void write_raw_property(ref ByteBufferWriter writer, byte[] value)
    {
        writer.write_u32_le((uint)value.Length);
        writer.write(value);
    }

    private static void write_array_property(ref ByteBufferWriter writer, FbxProperty property)
    {
        var value = property.value;

        switch (value)
        {
            case int[] intArray:
                writer.write_u32_le((uint)intArray.Length);
                writer.write_u32_le(0);
                writer.write_u32_le((uint)(intArray.Length * 4));

                foreach (var v in intArray) writer.write_i32_le(v);

                break;

            case long[] longArray:
                writer.write_u32_le((uint)longArray.Length);
                writer.write_u32_le(0);
                writer.write_u32_le((uint)(longArray.Length * 8));

                foreach (var v in longArray) writer.write_i64_le(v);

                break;

            case float[] floatArray:
                writer.write_u32_le((uint)floatArray.Length);
                writer.write_u32_le(0);
                writer.write_u32_le((uint)(floatArray.Length * 4));

                foreach (var v in floatArray) writer.write_f32_le(v);

                break;

            case double[] doubleArray:
                writer.write_u32_le((uint)doubleArray.Length);
                writer.write_u32_le(0);
                writer.write_u32_le((uint)(doubleArray.Length * 8));

                foreach (var v in doubleArray) writer.write_f64_le(v);

                break;

            case bool[] boolArray:
                writer.write_u32_le((uint)boolArray.Length);
                writer.write_u32_le(0);
                writer.write_u32_le((uint)boolArray.Length);

                foreach (var v in boolArray) writer.write_u8((byte)(v ? 1 : 0));

                break;

            case byte[] rawData:
                writer.write_u32_le(0);
                writer.write_u32_le(0);
                writer.write_u32_le((uint)rawData.Length);
                writer.write(rawData);
                break;

            default:
                writer.write_u32_le(0);
                writer.write_u32_le(0);
                writer.write_u32_le(0);
                break;
        }
    }

    private static int compute_node_size(FbxNode node, int version)
    {
        var use64Bit = version >= FbxVersions.v75;
        var nameBytes = Encoding.ASCII.GetBytes(node.name);
        var propertyListLength = estimate_property_list_size(node.properties);

        var headerSize = use64Bit ? 8 + 8 + 8 + 1 : 4 + 4 + 4 + 1;
        var size = headerSize + nameBytes.Length + propertyListLength;

        foreach (var child in node.children) size += compute_node_size(child, version);

        size += 13;

        return size;
    }

    private static int estimate_property_list_size(IReadOnlyList<FbxProperty> properties)
    {
        var size = 0;

        foreach (var prop in properties) size += estimate_property_size(prop);

        return size;
    }

    private static int estimate_property_size(FbxProperty property)
    {
        return property.type_code switch
        {
            FbxConstants.PropertyType.boolean => 2,
            FbxConstants.PropertyType.int8 => 2,
            FbxConstants.PropertyType.int16 => 3,
            FbxConstants.PropertyType.int32 => 5,
            FbxConstants.PropertyType.int64 => 9,
            FbxConstants.PropertyType.float32 => 5,
            FbxConstants.PropertyType.float64 => 9,
            FbxConstants.PropertyType.@string => 5 + ((string?)property.value ?? "").Length,
            FbxConstants.PropertyType.raw_buffer => 5 + ((byte[]?)property.value ?? []).Length,
            _ => estimate_array_property_size(property)
        };
    }

    private static int estimate_array_property_size(FbxProperty property)
    {
        var baseSize = 1 + 4 + 4 + 4;

        return property.value switch
        {
            int[] arr => baseSize + arr.Length * 4,
            long[] arr => baseSize + arr.Length * 8,
            float[] arr => baseSize + arr.Length * 4,
            double[] arr => baseSize + arr.Length * 8,
            bool[] arr => baseSize + arr.Length,
            byte[] arr => baseSize + arr.Length,
            _ => baseSize
        };
    }

    private static int estimate_size(FbxFileData data)
    {
        var size = FbxConstants.header_size;

        foreach (var child in data.root.children) size += compute_node_size(child, data.version);

        size += 13;

        return size + 4096;
    }

    #endregion
}