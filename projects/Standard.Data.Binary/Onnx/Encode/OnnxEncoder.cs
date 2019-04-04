using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Onnx.Data;

namespace Std.Data.Binary.Onnx.Encode;

/// <summary>
///     ONNX 模型编码器，的C# 数据结构编码的ONNX Protobuf 二进制格式的
/// </summary>
/// <remarks>
///     ONNX 使用 Protobuf 序列化，编码器按的ONNX 规范将模型数据写入二进制缓冲区的
/// </remarks>
public static class OnnxEncoder
{
    /// <summary>
    ///     的ONNX 模型数据编码写入缓冲区的
    /// </summary>
    /// <param name="buffer">
    ///     要写入的目标字节缓冲区的/param>
    ///     <param name="model">
    ///         要编码的 ONNX 模型数据的/param>
    ///         <returns>已写入的字节数的/returns>
    public static int encode(Span<byte> buffer, OnnxModelData model)
    {
        var writer = new ByteBufferWriter(buffer);
        encode_core(ref writer, model);
        writer.written_data.CopyTo(buffer);
        return writer.position;
    }

    /// <summary>
    ///     的ONNX 模型数据编码为字节数组的
    /// </summary>
    /// <param name="model">
    ///     要编码的 ONNX 模型数据的/param>
    ///     <returns>编码后的字节数组的/returns>
    public static byte[] encode(OnnxModelData model)
    {
        var writer = new ByteBufferWriter(1024 * 1024);
        encode_core(ref writer, model);
        return writer.to_array();
    }

    private static void encode_core(ref ByteBufferWriter writer, OnnxModelData model)
    {
        if (model.ir_version != 0)
        {
            write_tag(ref writer, 1, 0);
            writer.write_leb128_u64((ulong)model.ir_version);
        }

        if (!string.IsNullOrEmpty(model.producer_name))
        {
            write_tag(ref writer, 2, 2);
            write_string(ref writer, model.producer_name);
        }

        if (!string.IsNullOrEmpty(model.producer_version))
        {
            write_tag(ref writer, 3, 2);
            write_string(ref writer, model.producer_version);
        }

        if (!string.IsNullOrEmpty(model.domain))
        {
            write_tag(ref writer, 4, 2);
            write_string(ref writer, model.domain);
        }

        if (model.model_version != 0)
        {
            write_tag(ref writer, 5, 0);
            writer.write_leb128_u64((ulong)model.model_version);
        }

        if (!string.IsNullOrEmpty(model.doc_string))
        {
            write_tag(ref writer, 6, 2);
            write_string(ref writer, model.doc_string);
        }

        if (model.graph != null)
        {
            write_tag(ref writer, 7, 2);
            write_length_delimited(ref writer, encode_graph(model.graph));
        }

        foreach (var opset in model.opset_import)
        {
            write_tag(ref writer, 8, 2);
            write_length_delimited(ref writer, encode_opset_import(opset));
        }
    }

    private static byte[] encode_graph(OnnxGraph graph)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var node in graph.node)
        {
            w.write_u8((1 << 3) | 2);
            write_length_delimited(ref w, encode_node(node));
        }

        if (!string.IsNullOrEmpty(graph.name))
        {
            w.write_u8((2 << 3) | 2);
            write_string(ref w, graph.name);
        }

        foreach (var input in graph.input)
        {
            w.write_u8((3 << 3) | 2);
            write_length_delimited(ref w, encode_value_info(input));
        }

        foreach (var output in graph.output)
        {
            w.write_u8((4 << 3) | 2);
            write_length_delimited(ref w, encode_value_info(output));
        }

        foreach (var valueInfo in graph.value_info)
        {
            w.write_u8((5 << 3) | 2);
            write_length_delimited(ref w, encode_value_info(valueInfo));
        }

        foreach (var initializer in graph.initialization)
        {
            w.write_u8((10 << 3) | 2);
            write_length_delimited(ref w, encode_tensor(initializer));
        }

        if (!string.IsNullOrEmpty(graph.doc_string))
        {
            w.write_u8((12 << 3) | 2);
            write_string(ref w, graph.doc_string);
        }

        return w.to_array();
    }

    private static byte[] encode_node(OnnxNode node)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var input in node.input)
        {
            w.write_u8((1 << 3) | 2);
            write_string(ref w, input);
        }

        foreach (var output in node.output)
        {
            w.write_u8((2 << 3) | 2);
            write_string(ref w, output);
        }

        if (!string.IsNullOrEmpty(node.name))
        {
            w.write_u8((3 << 3) | 2);
            write_string(ref w, node.name);
        }

        if (!string.IsNullOrEmpty(node.op_type))
        {
            w.write_u8((4 << 3) | 2);
            write_string(ref w, node.op_type);
        }

        if (!string.IsNullOrEmpty(node.domain))
        {
            w.write_u8((7 << 3) | 2);
            write_string(ref w, node.domain);
        }

        foreach (var attribute in node.attribute)
        {
            w.write_u8((8 << 3) | 2);
            write_length_delimited(ref w, encode_attribute(attribute));
        }

        if (!string.IsNullOrEmpty(node.doc_string))
        {
            w.write_u8((10 << 3) | 2);
            write_string(ref w, node.doc_string);
        }

        return w.to_array();
    }

    private static byte[] encode_value_info(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        if (!string.IsNullOrEmpty(valueInfo.name))
        {
            w.write_u8((1 << 3) | 2);
            write_string(ref w, valueInfo.name);
        }

        if (valueInfo.data_type != OnnxDataType.undefined)
        {
            w.write_u8((2 << 3) | 2);
            var typeBytes = encode_type_with_shape(valueInfo);
            write_length_delimited(ref w, typeBytes);
        }

        return w.to_array();
    }

    private static byte[] encode_type_with_shape(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        w.write_u8((1 << 3) | 2);
        var tensorTypeBytes = encode_tensor_type(valueInfo);
        write_length_delimited(ref w, tensorTypeBytes);

        return w.to_array();
    }

    private static byte[] encode_tensor_type(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        w.write_u8((1 << 3) | 0);
        w.write_leb128_u64((ulong)valueInfo.data_type);

        if (valueInfo.shape.Count > 0)
        {
            w.write_u8((2 << 3) | 2);
            var shapeBytes = encode_shape(valueInfo);
            write_length_delimited(ref w, shapeBytes);
        }

        return w.to_array();
    }

    private static byte[] encode_shape(OnnxValueInfo valueInfo)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var dim in valueInfo.shape)
        {
            w.write_u8((1 << 3) | 2);
            var dimBytes = encode_dim(dim);
            write_length_delimited(ref w, dimBytes);
        }

        return w.to_array();
    }

    private static byte[] encode_dim(long dim)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        w.write_u8((1 << 3) | 0);
        w.write_leb128_u64((ulong)dim);

        return w.to_array();
    }

    private static byte[] encode_tensor(OnnxTensor tensor)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        foreach (var dim in tensor.dims)
        {
            w.write_u8((1 << 3) | 0);
            w.write_leb128_u64((ulong)dim);
        }

        if (tensor.data_type != OnnxDataType.undefined)
        {
            w.write_u8((2 << 3) | 0);
            w.write_leb128_u64((ulong)tensor.data_type);
        }

        if (tensor.raw_data != null)
        {
            w.write_u8((3 << 3) | 2);
            w.write_leb128_u32((uint)tensor.raw_data.Length);
            w.write(tensor.raw_data);
        }

        if (!string.IsNullOrEmpty(tensor.name))
        {
            w.write_u8((4 << 3) | 2);
            write_string(ref w, tensor.name);
        }

        foreach (var f in tensor.float_data)
        {
            w.write_u8((5 << 3) | 5);
            w.write_f32_le(f);
        }

        foreach (var i in tensor.int32_data)
        {
            w.write_u8((6 << 3) | 5);
            w.write_i32_le(i);
        }

        foreach (var i in tensor.int64_data)
        {
            w.write_u8((7 << 3) | 0);
            w.write_leb128_u64((ulong)i);
        }

        foreach (var d in tensor.double_data)
        {
            w.write_u8((9 << 3) | 1);
            w.write_f64_le(d);
        }

        return w.to_array();
    }

    private static byte[] encode_attribute(OnnxAttribute attribute)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        if (!string.IsNullOrEmpty(attribute.name))
        {
            w.write_u8((1 << 3) | 2);
            write_string(ref w, attribute.name);
        }

        if (attribute.type != OnnxAttributeType.undefined)
        {
            w.write_leb128_u64((20 << 3) | 0);
            w.write_leb128_u64((ulong)attribute.type);
        }

        if (attribute.float_value != 0)
        {
            w.write_u8((2 << 3) | 5);
            w.write_f32_le(attribute.float_value);
        }

        if (attribute.int_value != 0)
        {
            w.write_u8((3 << 3) | 0);
            w.write_leb128_u32((uint)attribute.int_value);
        }

        if (!string.IsNullOrEmpty(attribute.string_value))
        {
            w.write_u8((4 << 3) | 2);
            write_string(ref w, attribute.string_value);
        }

        if (attribute.tensor_value != null)
        {
            w.write_u8((5 << 3) | 2);
            write_length_delimited(ref w, encode_tensor(attribute.tensor_value));
        }

        foreach (var f in attribute.floats)
        {
            w.write_u8((7 << 3) | 5);
            w.write_f32_le(f);
        }

        foreach (var i in attribute.ints)
        {
            w.write_u8((8 << 3) | 0);
            w.write_leb128_u32((uint)i);
        }

        foreach (var s in attribute.strings)
        {
            w.write_u8((9 << 3) | 2);
            write_string(ref w, s);
        }

        return w.to_array();
    }

    private static byte[] encode_opset_import(OnnxOperatorSetId opset)
    {
        var w = new ByteBufferWriter(1024 * 1024);

        if (!string.IsNullOrEmpty(opset.domain))
        {
            w.write_u8((1 << 3) | 2);
            write_string(ref w, opset.domain);
        }

        if (opset.version != 0)
        {
            w.write_u8((2 << 3) | 0);
            w.write_leb128_u64((ulong)opset.version);
        }

        return w.to_array();
    }

    #region 辅助方法

    private static void write_tag(ref ByteBufferWriter writer, int fieldNumber, int wireType)
    {
        writer.write_leb128_u64((ulong)((fieldNumber << 3) | wireType));
    }

    private static void write_string(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.write_leb128_u32((uint)bytes.Length);
        writer.write(bytes);
    }

    private static void write_length_delimited(ref ByteBufferWriter writer, byte[] data)
    {
        writer.write_leb128_u32((uint)data.Length);
        writer.write(data);
    }

    #endregion
}