using Std.Data.Binary.Frame;
using Std.Data.Binary.SafeTensors.Data;

namespace Std.Data.Binary.SafeTensors.Decode;

/// <summary>
///     SafeTensors 格式解码器，的HuggingFace SafeTensors 格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     SafeTensors 的HuggingFace 定义的张量存储格式，使用小端序存储数值，
///     文件头为 JSON 格式，包含张量名称、数据类型、形状和偏移量等元信息的
///     解码器解析文件头 JSON 和张量数据，支持多种数据类型和形状的
/// </remarks>
public ref struct SafeTensorsDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="SafeTensorsDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">SafeTensors 二进制数据的/param>
    public SafeTensorsDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 SafeTensors 文件的
    /// </summary>
    /// <returns>SafeTensors 文件数据的/returns>
    public SafeTensorsFileData decode()
    {
        var headerLength = _buffer.read_u64_le();
        var headerJson = _buffer.read_string((int)headerLength);
        var header = parse_header(headerJson);

        var dataStart = (int)(8 + headerLength);
        var dataEnd = _buffer.length;
        var dataLength = dataEnd - dataStart;

        _buffer.position = dataStart;
        var rawData = dataLength > 0 ? _buffer.read_bytes(dataLength).ToArray() : [];

        return new SafeTensorsFileData
        {
            tensors = header,
            data = rawData
        };
    }

    /// <summary>
    ///     仅解的SafeTensors 文件头的
    /// </summary>
    /// <returns>张量名称到元数据的映射的/returns>
    public IReadOnlyDictionary<string, SafeTensorMeta> decode_header()
    {
        var headerLength = _buffer.read_u64_le();
        var headerJson = _buffer.read_string((int)headerLength);
        return parse_header(headerJson);
    }

    /// <summary>
    ///     解码指定名称的张量数据的
    /// </summary>
    /// <param name="name">
    ///     张量名称的/param>
    ///     <returns>张量数据，如果不存在则返的null的/returns>
    public SafeTensorData? decode_tensor(string name)
    {
        var headerLength = _buffer.read_u64_le();
        var headerJson = _buffer.read_string((int)headerLength);
        var header = parse_header(headerJson);

        if (!header.TryGetValue(name, out var meta)) return null;

        var tensorStart = (int)(8 + (long)headerLength + meta.data_offset);
        _buffer.position = tensorStart;
        var tensorData = _buffer.read_bytes((int)meta.data_length).ToArray();

        return new SafeTensorData
        {
            name = name,
            d_type = meta.d_type,
            shape = meta.shape,
            data = tensorData
        };
    }

    #region 私有解析方法

    private static Dictionary<string, SafeTensorMeta> parse_header(string headerJson)
    {
        var result = new Dictionary<string, SafeTensorMeta>();

        var json = headerJson.Trim();
        json = json.TrimStart('{').TrimEnd('}');

        if (string.IsNullOrWhiteSpace(json)) return result;

        var parts = split_json_entries(json);

        foreach (var part in parts)
        {
            var colonIndex = part.IndexOf(':');

            if (colonIndex < 0) continue;

            var keyJson = part[..colonIndex].Trim();
            var valueJson = part[(colonIndex + 1)..].Trim();

            if (keyJson.StartsWith('"') && keyJson.EndsWith('"')) keyJson = keyJson[1..^1];

            if (keyJson == "__metadata__") continue;

            var meta = parse_tensor_meta(valueJson);
            result[keyJson] = meta;
        }

        return result;
    }

    private static SafeTensorMeta parse_tensor_meta(string json)
    {
        var dtype = SafeTensorDType.float32;
        var shape = (IReadOnlyList<long>)[];
        var dataOffset = 0L;
        var dataLength = 0L;

        var dtypeIndex = json.IndexOf("\"dtype\"");

        if (dtypeIndex >= 0)
        {
            var valueStart = json.IndexOf(':', dtypeIndex) + 1;
            var valueEnd = json.IndexOfAny([',', '}'], valueStart);
            var dtypeValue = json[valueStart..valueEnd].Trim().Trim('"');
            dtype = parse_d_type(dtypeValue);
        }

        var shapeIndex = json.IndexOf("\"shape\"");

        if (shapeIndex >= 0)
        {
            var arrayStart = json.IndexOf('[', shapeIndex);
            var arrayEnd = json.IndexOf(']', arrayStart);
            var arrayContent = json[(arrayStart + 1)..arrayEnd];
            shape = parse_shape(arrayContent);
        }

        var offsetsIndex = json.IndexOf("\"data_offsets\"");

        if (offsetsIndex >= 0)
        {
            var arrayStart = json.IndexOf('[', offsetsIndex);
            var arrayEnd = json.IndexOf(']', arrayStart);
            var arrayContent = json[(arrayStart + 1)..arrayEnd];
            var offsets = parse_data_offsets(arrayContent);

            if (offsets.Length >= 2)
            {
                dataOffset = offsets[0];
                dataLength = offsets[1] - offsets[0];
            }
        }

        return new SafeTensorMeta
        {
            d_type = dtype,
            shape = shape,
            data_offset = dataOffset,
            data_length = dataLength
        };
    }

    private static SafeTensorDType parse_d_type(string dtype)
    {
        return dtype switch
        {
            "BOOL" => SafeTensorDType.@bool,
            "U8" => SafeTensorDType.u_int8,
            "I8" => SafeTensorDType.int8,
            "I16" => SafeTensorDType.int16,
            "I32" => SafeTensorDType.int32,
            "I64" => SafeTensorDType.int64,
            "F16" => SafeTensorDType.float16,
            "F32" => SafeTensorDType.float32,
            "F64" => SafeTensorDType.float64,
            "BF16" => SafeTensorDType.b_float16,
            _ => SafeTensorDType.float32
        };
    }

    private static long[] parse_shape(string arrayContent)
    {
        if (string.IsNullOrWhiteSpace(arrayContent)) return [];

        var parts = arrayContent.Split(',');
        var shape = new long[parts.Length];

        for (var i = 0; i < parts.Length; i++) shape[i] = long.Parse(parts[i].Trim());

        return shape;
    }

    private static long[] parse_data_offsets(string arrayContent)
    {
        var parts = arrayContent.Split(',');
        var offsets = new long[parts.Length];

        for (var i = 0; i < parts.Length; i++) offsets[i] = long.Parse(parts[i].Trim());

        return offsets;
    }

    private static List<string> split_json_entries(string json)
    {
        var entries = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < json.Length; i++)
            switch (json[i])
            {
                case '{' or '[':
                    depth++;
                    break;
                case '}' or ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    entries.Add(json[start..i].Trim());
                    start = i + 1;
                    break;
            }

        if (start < json.Length) entries.Add(json[start..].Trim());

        return entries;
    }

    #endregion
}