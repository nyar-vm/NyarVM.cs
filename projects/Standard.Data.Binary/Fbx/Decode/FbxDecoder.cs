using Std.Data.Binary.Fbx.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Fbx.Decode;

/// <summary>
///     FBX 二进制文件解码器，将 Autodesk FBX 格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     FBX 的Autodesk 的3D 模型/动画交换格式，游戏行业事实标准的
///     解码器解的FBX 二进制格式的节点层级结构的
/// </remarks>
public ref struct FbxDecoder
{
    private ByteBuffer _buffer;
    private int _version;

    /// <summary>
    ///     初始的<see cref="FbxDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">FBX 二进制数据的/param>
    public FbxDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
        _version = 0;
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 FBX 二进制文件的
    /// </summary>
    /// <returns>FBX 文件数据的/returns>
    public FbxFileData decode()
    {
        read_file_header();

        var rootChildren = new List<FbxNode>();

        while (!_buffer.is_end)
        {
            var node = read_node();

            if (node == null) break;

            rootChildren.Add(node);
        }

        return new FbxFileData
        {
            version = _version,
            root = new FbxNode { name = "Root", children = rootChildren }
        };
    }

    /// <summary>
    ///     仅解的FBX 文件头信息的
    /// </summary>
    public int decode_header()
    {
        read_file_header();
        return _version;
    }

    #region 私有解析方法

    private void read_file_header()
    {
        if (_buffer.remaining < FbxConstants.header_size) throw new InvalidDataException("FBX 文件数据过短，无法读取文件头");

        var magic = _buffer.read_string(FbxConstants.magic_length);

        if (!magic.StartsWith("Kaydara FBX Binary"))
            throw new InvalidDataException(
                $"FBX 文件签名无效，期的\"Kaydara FBX Binary\"，实的\"{magic[..System.Math.Min(19, magic.Length)]}\"");

        _version = (int)_buffer.read_u32_le();
    }

    private FbxNode? read_node()
    {
        if (_buffer.remaining < 4) return null;

        var endOffset = (int)_buffer.read_u32_le();

        if (endOffset == 0) return null;

        if (endOffset > _buffer.length) return null;

        var propertyCount = (int)_buffer.read_u32_le();
        var propertyListLength = (int)_buffer.read_u32_le();
        var nameLength = _buffer.read_u8();

        if (nameLength == 0)
        {
            _buffer.position = endOffset;
            return null;
        }

        var name = _buffer.read_string(nameLength);
        var properties = read_properties(propertyCount, propertyListLength);

        var children = new List<FbxNode>();
        var childStart = _buffer.position;

        while (_buffer.position < endOffset - 13)
        {
            var child = read_node();

            if (child != null)
                children.Add(child);
            else
                break;
        }

        _buffer.position = endOffset;

        return new FbxNode
        {
            name = name,
            properties = properties,
            children = children
        };
    }

    private List<FbxProperty> read_properties(int count, int totalLength)
    {
        var properties = new List<FbxProperty>(count);
        var endPos = _buffer.position + totalLength;

        for (var i = 0; i < count && _buffer.position < endPos; i++) properties.Add(read_property());

        return properties;
    }

    private FbxProperty read_property()
    {
        var typeCode = _buffer.read_u8();

        var value = typeCode switch
        {
            FbxConstants.PropertyType.boolean => _buffer.read_u8() != 0,
            FbxConstants.PropertyType.int8 => (short)_buffer.read_u8(),
            FbxConstants.PropertyType.int16 => _buffer.read_i16_le(),
            FbxConstants.PropertyType.int32 => _buffer.read_i32_le(),
            FbxConstants.PropertyType.int64 => _buffer.read_i64_le(),
            FbxConstants.PropertyType.float32 => _buffer.read_f32_le(),
            FbxConstants.PropertyType.float64 => _buffer.read_f64_le(),
            FbxConstants.PropertyType.@string => read_string_property(),
            FbxConstants.PropertyType.raw_buffer => read_raw_property(),
            _ => read_array_property(typeCode)
        };

        return new FbxProperty { type_code = typeCode, value = value };
    }

    private string read_string_property()
    {
        var length = (int)_buffer.read_u32_le();
        return _buffer.read_string(length);
    }

    private byte[] read_raw_property()
    {
        var length = (int)_buffer.read_u32_le();
        return [.. _buffer.read_bytes(length)];
    }

    private object read_array_property(byte typeCode)
    {
        var arrayType = (char)typeCode;

        if (arrayType != 'i' && arrayType != 'l' && arrayType != 'f' && arrayType != 'd' && arrayType != 'b')
            return Array.Empty<byte>();

        var count = (int)_buffer.read_u32_le();
        var encoding = _buffer.read_u32_le();
        var compressedLength = (int)_buffer.read_u32_le();

        if (encoding == 0)
            return arrayType switch
            {
                'i' => read_int32_array(count),
                'l' => read_int64_array(count),
                'f' => read_float32_array(count),
                'd' => read_float64_array(count),
                'b' => read_bool_array(count),
                _ => Array.Empty<byte>()
            };

        var data = _buffer.read_bytes(compressedLength).ToArray();

        return arrayType switch
        {
            'i' => data,
            'l' => data,
            'f' => data,
            'd' => data,
            _ => data
        };
    }

    private int[] read_int32_array(int count)
    {
        var result = new int[count];

        for (var i = 0; i < count; i++) result[i] = _buffer.read_i32_le();

        return result;
    }

    private long[] read_int64_array(int count)
    {
        var result = new long[count];

        for (var i = 0; i < count; i++) result[i] = _buffer.read_i64_le();

        return result;
    }

    private float[] read_float32_array(int count)
    {
        var result = new float[count];

        for (var i = 0; i < count; i++) result[i] = _buffer.read_f32_le();

        return result;
    }

    private double[] read_float64_array(int count)
    {
        var result = new double[count];

        for (var i = 0; i < count; i++) result[i] = _buffer.read_f64_le();

        return result;
    }

    private bool[] read_bool_array(int count)
    {
        var result = new bool[count];

        for (var i = 0; i < count; i++) result[i] = _buffer.read_u8() != 0;

        return result;
    }

    #endregion
}