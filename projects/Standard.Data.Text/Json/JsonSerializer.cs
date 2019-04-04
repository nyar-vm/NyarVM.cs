using System.Text.Json;
using Std.DataProcess.Serialize;
using Std.Text;
using Std.Text.Utf8;

namespace Std.Data.Text.Json;

/// <summary>
///     <see cref="ISerializer" /> �?JSON 实现，内部使�?<see cref="Utf8JsonWriter" /> 进行写入�?/// 写入完成后通过
///     <see cref="to_utf8_bytes" /> 获取 JSON 字节序列�?///
/// </summary>
public sealed class JsonSerializer : ISerializer, IDisposable
{
    private readonly MemoryStream _stream;
    private readonly Utf8JsonWriter _writer;

    /// <summary>
    ///     初始化一个新�?JSON 数据写入器实例（紧凑模式）�?    ///
    /// </summary>
    public JsonSerializer()
    {
        _stream = new MemoryStream();
        _writer = new Utf8JsonWriter(_stream, new JsonWriterOptions { Indented = false });
    }

    /// <summary>
    ///     初始化一个新�?JSON 数据写入器实例，允许指定缩进格式化�?    ///
    /// </summary>
    /// <param name="indented">是否生成带缩进的人类可读 JSON�?/param>
    public JsonSerializer(bool indented)
    {
        _stream = new MemoryStream();
        _writer = new Utf8JsonWriter(_stream, new JsonWriterOptions { Indented = indented });
    }

    /// <summary>
    ///     释放写入器使用的资源�?    ///
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }

    /// <inheritdoc />
    public void serialize_null()
    {
        _writer.WriteNullValue();
    }

    /// <inheritdoc />
    public void serialize_bool(bool value)
    {
        _writer.WriteBooleanValue(value);
    }

    /// <inheritdoc />
    public void serialize_i8(sbyte value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_i16(short value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_i32(int value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_i64(long value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_i128(Int128 value)
    {
        _writer.WriteRawValue(value.ToString(), true);
    }

    /// <inheritdoc />
    public void serialize_u8(byte value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_u16(ushort value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_u32(ulong value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_u64(ulong value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_u128(UInt128 value)
    {
        _writer.WriteRawValue(value.ToString(), true);
    }

    /// <inheritdoc />
    public void serialize_f32(float value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_f64(double value)
    {
        _writer.WriteNumberValue(value);
    }

    /// <inheritdoc />
    public void serialize_utf8(Utf8Text text)
    {
        _writer.WriteStringValue(text.as_span());
    }

    /// <inheritdoc />
    public void serialize_utf16(Utf8Text text)
    {
        serialize_utf8(text);
    }

    /// <inheritdoc />
    public void serialize_bytes(ReadOnlySpan<byte> bytes)
    {
        _writer.WriteRawValue(bytes, true);
    }

    /// <inheritdoc />
    public IMapSerializer serialize_map(int? countPair = null)
    {
        _writer.WriteStartObject();
        return new JsonMapSerializer(_writer, this);
    }

    /// <inheritdoc />
    public ITupleSerializer serialize_tuple(int countItem)
    {
        _writer.WriteStartArray();
        return new JsonTupleSerializer(_writer, this);
    }

    /// <inheritdoc />
    public ISequenceSerializer serialize_sequence(int? countElement = null)
    {
        _writer.WriteStartArray();
        return new JsonSequenceSerializer(_writer, this);
    }

    /// <summary>
    ///     将已写入内容刷新到底层缓冲区并以 UTF-8 字节数组形式返回�?    ///
    /// </summary>
    /// <returns>JSON �?UTF-8 字节数组�?/returns>
    public byte[] to_utf8_bytes()
    {
        _writer.Flush();
        return _stream.ToArray();
    }

    /// <summary>
    ///     将已写入内容刷新并以 UTF-8 字节数组形式返回，然后重置写入器以便复用�?    ///
    /// </summary>
    /// <returns>JSON �?UTF-8 字节数组�?/returns>
    public byte[] to_utf8_bytes_and_reset()
    {
        _writer.Flush();
        var result = _stream.ToArray();
        _stream.SetLength(0);
        _writer.Reset();
        return result;
    }

    /// <summary>
    ///     获取 JSON 字符串表示�?    ///
    /// </summary>
    /// <returns>JSON 字符串�?/returns>
    public override string ToString()
    {
        _writer.Flush();
        return SonicEncoding.decode_utf8(_stream.ToArray());
    }

    #region 子写入器

    /// <summary>
    ///     JSON 对象序列化子写入�?    ///
    /// </summary>
    private sealed class JsonMapSerializer : IMapSerializer
    {
        private readonly JsonSerializer _parent;
        private readonly Utf8JsonWriter _writer;
        private bool _ended;

        public JsonMapSerializer(Utf8JsonWriter writer, JsonSerializer parent)
        {
            _writer = writer;
            _parent = parent;
        }

        /// <inheritdoc />
        public void write_field_name(string name)
        {
            _writer.WritePropertyName(name);
        }

        /// <inheritdoc />
        public void write_value(ISerializer serializer)
        {
            // 标量值已由外�?serializer 写入 _writer，此处为占位
        }

        /// <inheritdoc />
        public void end()
        {
            if (!_ended)
            {
                _writer.WriteEndObject();
                _ended = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended) end();
        }
    }

    /// <summary>
    ///     JSON 数组序列化子写入�?    ///
    /// </summary>
    private sealed class JsonSequenceSerializer : ISequenceSerializer
    {
        private readonly JsonSerializer _parent;
        private readonly Utf8JsonWriter _writer;
        private bool _ended;

        public JsonSequenceSerializer(Utf8JsonWriter writer, JsonSerializer parent)
        {
            _writer = writer;
            _parent = parent;
        }

        /// <inheritdoc />
        public void write_element(ISerializer serializer)
        {
            // 标量值已由外�?serializer 写入 _writer，此处为占位
        }

        /// <inheritdoc />
        public void end()
        {
            if (!_ended)
            {
                _writer.WriteEndArray();
                _ended = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended) end();
        }
    }

    /// <summary>
    ///     JSON 元组序列化子写入器。元组在 JSON 中表示为数组�?    ///
    /// </summary>
    private sealed class JsonTupleSerializer : ITupleSerializer
    {
        private readonly JsonSerializer _parent;
        private readonly Utf8JsonWriter _writer;
        private bool _ended;

        public JsonTupleSerializer(Utf8JsonWriter writer, JsonSerializer parent)
        {
            _writer = writer;
            _parent = parent;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended)
            {
                _writer.WriteEndArray();
                _ended = true;
            }
        }
    }

    #endregion
}