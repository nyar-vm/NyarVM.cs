using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.SafeTensors.Data;

namespace Std.Data.Binary.SafeTensors.Encode;

/// <summary>
///     SafeTensors 格式编码器，的C# 数据结构编码的HuggingFace SafeTensors 格式的
/// </summary>
/// <remarks>
///     SafeTensors 的HuggingFace 定义的张量存储格式，使用小端序存储数值，
///     文件头为 JSON 格式，包含张量名称、数据类型、形状和偏移量等元信息的
///     编码器生成符的SafeTensors 规范的二进制数据的
/// </remarks>
public sealed class SafeTensorsEncoder
{
    /// <summary>
    ///     的SafeTensors 文件数据编码为二进制格式的
    /// </summary>
    /// <param name="data">
    ///     SafeTensors 文件数据的/param>
    ///     <returns>SafeTensors 二进制数据的/returns>
    public byte[] encode(SafeTensorsFileData data)
    {
        var headerJson = build_header_json_from_meta(data.tensors);
        var headerBytes = Encoding.UTF8.GetBytes(headerJson);

        while (headerBytes.Length % 8 != 0)
        {
            headerJson += " ";
            headerBytes = Encoding.UTF8.GetBytes(headerJson);
        }

        var totalSize = 8 + headerBytes.Length + (data.data?.Length ?? 0);
        var writer = new ByteBufferWriter(totalSize);

        writer.write_u64_le((ulong)headerBytes.Length);
        writer.write(headerBytes);

        if (data.data != null) writer.write(data.data);

        return writer.to_array();
    }

    /// <summary>
    ///     将张量列表编码为 SafeTensors 二进制格式的
    /// </summary>
    /// <param name="tensors">
    ///     张量数据列表的/param>
    ///     <returns>SafeTensors 二进制数据的/returns>
    public byte[] encode_tensors(IReadOnlyList<SafeTensorData> tensors)
    {
        var headerJson = build_header_json(tensors);
        var headerBytes = Encoding.UTF8.GetBytes(headerJson);

        while (headerBytes.Length % 8 != 0)
        {
            headerJson += " ";
            headerBytes = Encoding.UTF8.GetBytes(headerJson);
        }

        var totalSize = 8 + headerBytes.Length + calculate_tensor_data_size(tensors);
        var writer = new ByteBufferWriter(totalSize);

        writer.write_u64_le((ulong)headerBytes.Length);
        writer.write(headerBytes);

        foreach (var tensor in tensors) writer.write(tensor.data);

        return writer.to_array();
    }

    #region 私有编码方法

    private static string build_header_json_from_meta(IReadOnlyDictionary<string, SafeTensorMeta> tensors)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        ulong offset = 0;
        var first = true;

        foreach (var (name, meta) in tensors)
        {
            if (!first) sb.Append(',');

            first = false;

            var dtype = format_d_type(meta.d_type);
            var shape = format_shape(meta.shape);
            var dataEnd = offset + (ulong)meta.data_length;

            sb.Append($"\"{name}\":{{\"dtype\":\"{dtype}\",\"shape\":{shape},\"data_offsets\":[{offset},{dataEnd}]}}");

            offset = dataEnd;
        }

        sb.Append(",\"__metadata__\":{}");
        sb.Append('}');

        return sb.ToString();
    }

    private static string build_header_json(IReadOnlyList<SafeTensorData> tensors)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        ulong offset = 0;

        for (var i = 0; i < tensors.Count; i++)
        {
            var tensor = tensors[i];

            if (i > 0) sb.Append(',');

            var dtype = format_d_type(tensor.d_type);
            var shape = format_shape(tensor.shape);
            var dataEnd = offset + (ulong)tensor.data.Length;

            sb.Append(
                $"\"{tensor.name}\":{{\"dtype\":\"{dtype}\",\"shape\":{shape},\"data_offsets\":[{offset},{dataEnd}]}}");

            offset = dataEnd;
        }

        sb.Append(",\"__metadata__\":{}");
        sb.Append('}');

        return sb.ToString();
    }

    private static string format_d_type(SafeTensorDType dtype)
    {
        return dtype switch
        {
            SafeTensorDType.@bool => "BOOL",
            SafeTensorDType.u_int8 => "U8",
            SafeTensorDType.int8 => "I8",
            SafeTensorDType.int16 => "I16",
            SafeTensorDType.int32 => "I32",
            SafeTensorDType.int64 => "I64",
            SafeTensorDType.float16 => "F16",
            SafeTensorDType.float32 => "F32",
            SafeTensorDType.float64 => "F64",
            SafeTensorDType.b_float16 => "BF16",
            _ => "F32"
        };
    }

    private static string format_shape(IReadOnlyList<long> shape)
    {
        if (shape.Count == 0) return "[]";

        return $"[{string.Join(",", shape)}]";
    }

    private static int calculate_tensor_data_size(IReadOnlyList<SafeTensorData> tensors)
    {
        var size = 0;

        foreach (var tensor in tensors) size += tensor.data.Length;

        return size;
    }

    #endregion
}