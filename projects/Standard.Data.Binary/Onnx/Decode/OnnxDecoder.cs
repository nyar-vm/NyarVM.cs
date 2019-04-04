using Std.Data.Binary.Frame;
using Std.Data.Binary.Onnx.Data;

namespace Std.Data.Binary.Onnx.Decode;

/// <summary>
///     ONNX 模型解码器，的ONNX Protobuf 二进制格式解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     ONNX 使用 Protobuf 序列化，文件头可能包含魔数标记的
///     本解码器解析完整的ONNX ModelProto 结构的
/// </remarks>
public sealed class OnnxDecoder
{
    /// <summary>
    ///     的ONNX 二进制数据解码模型的
    /// </summary>
    /// <param name="data">
    ///     ONNX 二进制数据的/param>
    ///     <returns>解码后的模型数据的/returns>
    public OnnxModelData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        return decode_model(ref buffer);
    }

    private OnnxModelData decode_model(ref ByteBuffer buffer)
    {
        long irVersion = 0;
        var producerName = string.Empty;
        var producerVersion = string.Empty;
        var domain = string.Empty;
        long modelVersion = 0;
        var docString = string.Empty;
        OnnxGraph? graph = null;
        var opsetImports = new List<OnnxOperatorSetId>();

        while (!buffer.is_end)
        {
            var (fieldNumber, wireType) = read_tag(ref buffer);

            switch (fieldNumber)
            {
                case 1: irVersion = (long)buffer.read_leb128_u64(); break;
                case 2: producerName = read_string(ref buffer); break;
                case 3: producerVersion = read_string(ref buffer); break;
                case 4: domain = read_string(ref buffer); break;
                case 5: modelVersion = (long)buffer.read_leb128_u64(); break;
                case 6: docString = read_string(ref buffer); break;
                case 7: graph = decode_graph(ref buffer); break;
                case 8: opsetImports.Add(decode_opset_import(ref buffer)); break;
                default: skip_field(ref buffer, wireType); break;
            }
        }

        return new OnnxModelData
        {
            ir_version = irVersion,
            producer_name = producerName,
            producer_version = producerVersion,
            domain = domain,
            model_version = modelVersion,
            doc_string = docString,
            graph = graph,
            opset_import = opsetImports
        };
    }

    private OnnxGraph decode_graph(ref ByteBuffer buffer)
    {
        var length = (int)buffer.read_leb128_u32();
        var endPosition = buffer.position + length;

        var name = string.Empty;
        var docString = string.Empty;
        var nodes = new List<OnnxNode>();
        var inputs = new List<OnnxValueInfo>();
        var outputs = new List<OnnxValueInfo>();
        var valueInfos = new List<OnnxValueInfo>();
        var initializers = new List<OnnxTensor>();

        while (buffer.position < endPosition)
        {
            var (fieldNumber, wireType) = read_tag(ref buffer);

            switch (fieldNumber)
            {
                case 1: nodes.Add(decode_node(ref buffer)); break;
                case 2: name = read_string(ref buffer); break;
                case 3: inputs.Add(decode_value_info(ref buffer)); break;
                case 4: outputs.Add(decode_value_info(ref buffer)); break;
                case 5: valueInfos.Add(decode_value_info(ref buffer)); break;
                case 10: initializers.Add(decode_tensor(ref buffer)); break;
                case 12: docString = read_string(ref buffer); break;
                default: skip_field(ref buffer, wireType); break;
            }
        }

        return new OnnxGraph
        {
            name = name,
            doc_string = docString,
            node = nodes,
            input = inputs,
            output = outputs,
            value_info = valueInfos,
            initialization = initializers
        };
    }

    private OnnxNode decode_node(ref ByteBuffer buffer)
    {
        var length = (int)buffer.read_leb128_u32();
        var endPosition = buffer.position + length;

        var name = string.Empty;
        var opType = string.Empty;
        var domain = string.Empty;
        var docString = string.Empty;
        var input = new List<string>();
        var output = new List<string>();
        var attributes = new List<OnnxAttribute>();

        while (buffer.position < endPosition)
        {
            var (fieldNumber, wireType) = read_tag(ref buffer);

            switch (fieldNumber)
            {
                case 1: input.Add(read_string(ref buffer)); break;
                case 2: output.Add(read_string(ref buffer)); break;
                case 3: name = read_string(ref buffer); break;
                case 4: opType = read_string(ref buffer); break;
                case 7: domain = read_string(ref buffer); break;
                case 8: attributes.Add(decode_attribute(ref buffer)); break;
                case 10: docString = read_string(ref buffer); break;
                default: skip_field(ref buffer, wireType); break;
            }
        }

        return new OnnxNode
        {
            name = name,
            op_type = opType,
            domain = domain,
            doc_string = docString,
            input = input,
            output = output,
            attribute = attributes
        };
    }

    private OnnxValueInfo decode_value_info(ref ByteBuffer buffer)
    {
        var length = (int)buffer.read_leb128_u32();
        var endPosition = buffer.position + length;

        var name = string.Empty;
        var dataType = OnnxDataType.undefined;
        var shape = new List<long>();

        while (buffer.position < endPosition)
        {
            var (fieldNumber, wireType) = read_tag(ref buffer);

            switch (fieldNumber)
            {
                case 1: name = read_string(ref buffer); break;
                case 2:
                    var typeLength = (int)buffer.read_leb128_u32();
                    var typeEnd = buffer.position + typeLength;

                    while (buffer.position < typeEnd)
                    {
                        var (tf, tw) = read_tag(ref buffer);

                        switch (tf)
                        {
                            case 1:
                                var tensorTypeLen = (int)buffer.read_leb128_u32();
                                var tensorTypeEnd = buffer.position + tensorTypeLen;

                                while (buffer.position < tensorTypeEnd)
                                {
                                    var (ttf, ttw) = read_tag(ref buffer);

                                    switch (ttf)
                                    {
                                        case 1: dataType = (OnnxDataType)(long)buffer.read_leb128_u64(); break;
                                        case 2:
                                            var shapeLen = (int)buffer.read_leb128_u32();
                                            var shapeEnd = buffer.position + shapeLen;

                                            while (buffer.position < shapeEnd)
                                            {
                                                var (sf, sw) = read_tag(ref buffer);

                                                if (sf == 1)
                                                {
                                                    var dimLen = (int)buffer.read_leb128_u32();
                                                    var dimEnd = buffer.position + dimLen;

                                                    while (buffer.position < dimEnd)
                                                    {
                                                        var (df, dw) = read_tag(ref buffer);

                                                        if (df == 1)
                                                            shape.Add((long)buffer.read_leb128_u64());
                                                        else
                                                            skip_field(ref buffer, dw);
                                                    }
                                                }
                                                else
                                                {
                                                    skip_field(ref buffer, sw);
                                                }
                                            }

                                            break;
                                        default: skip_field(ref buffer, ttw); break;
                                    }
                                }

                                break;
                            default: skip_field(ref buffer, tw); break;
                        }
                    }

                    break;
                default: skip_field(ref buffer, wireType); break;
            }
        }

        return new OnnxValueInfo
        {
            name = name,
            data_type = dataType,
            shape = shape
        };
    }

    private OnnxTensor decode_tensor(ref ByteBuffer buffer)
    {
        var length = (int)buffer.read_leb128_u32();
        var endPosition = buffer.position + length;

        var name = string.Empty;
        var dataType = OnnxDataType.undefined;
        var dims = new List<long>();
        byte[]? rawData = null;
        var floatData = new List<float>();
        var int32Data = new List<int>();
        var int64Data = new List<long>();
        var doubleData = new List<double>();

        while (buffer.position < endPosition)
        {
            var (fieldNumber, wireType) = read_tag(ref buffer);

            switch (fieldNumber)
            {
                case 1: dims.Add((long)buffer.read_leb128_u64()); break;
                case 2: dataType = (OnnxDataType)(long)buffer.read_leb128_u64(); break;
                case 3:
                    if (wireType == 2)
                    {
                        var rawLen = (int)buffer.read_leb128_u32();
                        rawData = [.. buffer.read_bytes(rawLen)];
                    }

                    break;
                case 4: name = read_string(ref buffer); break;
                case 5: floatData.Add(buffer.read_f32_le()); break;
                case 7: int64Data.Add((long)buffer.read_leb128_u64()); break;
                case 9: doubleData.Add(buffer.read_f64_le()); break;
                case 6: int32Data.Add((int)buffer.read_u32_le()); break;
                default: skip_field(ref buffer, wireType); break;
            }
        }

        return new OnnxTensor
        {
            name = name,
            data_type = dataType,
            dims = dims,
            raw_data = rawData,
            float_data = floatData,
            int32_data = int32Data,
            int64_data = int64Data,
            double_data = doubleData
        };
    }

    private OnnxAttribute decode_attribute(ref ByteBuffer buffer)
    {
        var length = (int)buffer.read_leb128_u32();
        var endPosition = buffer.position + length;

        var name = string.Empty;
        var type = OnnxAttributeType.undefined;
        float floatValue = 0;
        var intValue = 0;
        var stringValue = string.Empty;
        OnnxTensor? tensorValue = null;
        var floats = new List<float>();
        var ints = new List<int>();
        var strings = new List<string>();

        while (buffer.position < endPosition)
        {
            var (fieldNumber, wireType) = read_tag(ref buffer);

            switch (fieldNumber)
            {
                case 1: name = read_string(ref buffer); break;
                case 20: type = (OnnxAttributeType)buffer.read_leb128_u64(); break;
                case 2: floatValue = buffer.read_f32_le(); break;
                case 3: intValue = (int)buffer.read_leb128_u32(); break;
                case 4: stringValue = read_string(ref buffer); break;
                case 5: tensorValue = decode_tensor(ref buffer); break;
                case 7: floats.Add(buffer.read_f32_le()); break;
                case 8: ints.Add((int)buffer.read_leb128_u32()); break;
                case 9: strings.Add(read_string(ref buffer)); break;
                default: skip_field(ref buffer, wireType); break;
            }
        }

        return new OnnxAttribute
        {
            name = name,
            type = type,
            float_value = floatValue,
            int_value = intValue,
            string_value = stringValue,
            tensor_value = tensorValue,
            floats = floats,
            ints = ints,
            strings = strings
        };
    }

    private OnnxOperatorSetId decode_opset_import(ref ByteBuffer buffer)
    {
        var length = (int)buffer.read_leb128_u32();
        var endPosition = buffer.position + length;

        var domain = string.Empty;
        long version = 0;

        while (buffer.position < endPosition)
        {
            var (fieldNumber, wireType) = read_tag(ref buffer);

            switch (fieldNumber)
            {
                case 1: domain = read_string(ref buffer); break;
                case 2: version = (long)buffer.read_leb128_u64(); break;
                default: skip_field(ref buffer, wireType); break;
            }
        }

        return new OnnxOperatorSetId
        {
            domain = domain,
            version = version
        };
    }

    #region 辅助方法

    private static (int FieldNumber, int WireType) read_tag(ref ByteBuffer buffer)
    {
        var tag = buffer.read_leb128_u64();
        return ((int)(tag >> 3), (int)(tag & 0x7));
    }

    private static string read_string(ref ByteBuffer buffer)
    {
        var length = (int)buffer.read_leb128_u32();
        return buffer.read_string(length);
    }

    private static void skip_field(ref ByteBuffer buffer, int wireType)
    {
        switch (wireType)
        {
            case 0: buffer.read_leb128_u64(); break;
            case 1: buffer.advance(8); break;
            case 2:
                var len = (int)buffer.read_leb128_u32();
                buffer.advance(len);
                break;
            case 5: buffer.advance(4); break;
        }
    }

    #endregion
}