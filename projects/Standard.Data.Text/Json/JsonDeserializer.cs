using System.Text.Json;
using Std.Category;
using Std.DataProcess.Deserialize;
using Std.Text;
using Std.Text.Utf8;

namespace Std.Data.Text.Json;

/// <summary>
///     <see cref="IDeserializer" /> �?JSON 实现，内部使�?<see cref="Utf8JsonReader" /> 进行解析�?/// 采用状态机模式：通过
///     <see cref="deserialize_map" /> / <see cref="deserialize_sequence" /> 获取子读取器在结构内迭代�?///
/// </summary>
public sealed class JsonDeserializer : IDeserializer
{
    private readonly byte[] _json_utf8_bytes;
    private long _consumed;

    /// <summary>
    ///     �?JSON UTF-8 字节序列创建读取器�?    ///
    /// </summary>
    /// <param name="jsonUtf8Bytes">JSON 文本�?UTF-8 字节序列�?/param>
    public JsonDeserializer(byte[] jsonUtf8Bytes)
    {
        _json_utf8_bytes = jsonUtf8Bytes;
    }

    /// <summary>
    ///     �?JSON UTF-8 只读跨度创建读取器�?    ///
    /// </summary>
    /// <param name="jsonUtf8Bytes">JSON 文本�?UTF-8 字节只读跨度�?/param>
    public JsonDeserializer(ReadOnlySpan<byte> jsonUtf8Bytes)
    {
        _json_utf8_bytes = [.. jsonUtf8Bytes];
    }

    /// <summary>
    ///     �?JSON 字符串创建读取器�?    ///
    /// </summary>
    /// <param name="json">JSON 文本字符串�?/param>
    public JsonDeserializer(string json)
    {
        _json_utf8_bytes = SonicEncoding.encode_utf8(json);
    }

    /// <inheritdoc />
    public bool try_read_null()
    {
        var reader = create_reader();
        return reader.TokenType == JsonTokenType.Null;
    }

    /// <inheritdoc />
    public bool deserialize_bool()
    {
        var reader = create_reader();
        var value = reader.GetBoolean();
        _consumed += reader.BytesConsumed;
        return value;
    }

    /// <inheritdoc />
    public int deserialize_i32()
    {
        var reader = create_reader();
        var value = reader.GetInt32();
        _consumed += reader.BytesConsumed;
        return value;
    }

    /// <inheritdoc />
    public long deserialize_i64()
    {
        var reader = create_reader();
        var value = reader.GetInt64();
        _consumed += reader.BytesConsumed;
        return value;
    }

    /// <inheritdoc />
    public ulong deserialize_u64()
    {
        var reader = create_reader();
        var value = reader.GetUInt64();
        _consumed += reader.BytesConsumed;
        return value;
    }

    /// <inheritdoc />
    public float deserialize_f32()
    {
        var reader = create_reader();
        var value = reader.GetSingle();
        _consumed += reader.BytesConsumed;
        return value;
    }

    /// <inheritdoc />
    public double deserialize_f64()
    {
        var reader = create_reader();

        if (reader.TokenType == JsonTokenType.String && double.TryParse(reader.GetString(), out var stringResult))
        {
            _consumed += reader.BytesConsumed;
            return stringResult;
        }

        var value = reader.GetDouble();
        _consumed += reader.BytesConsumed;
        return value;
    }

    /// <inheritdoc />
    public Utf8Text deserialize_utf8()
    {
        var reader = create_reader();
        var valueSpan = reader.ValueSpan;
        _consumed += reader.BytesConsumed;
        return Utf8Text.from_bytes_unchecked(valueSpan.ToArray());
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> deserialize_bytes(int length)
    {
        var reader = create_reader();
        var slice = _json_utf8_bytes.AsSpan((int)_consumed, length);
        _consumed += length;
        return slice;
    }

    /// <inheritdoc />
    public IMapDeserializer deserialize_map(int? expectedFieldCount = null)
    {
        return new JsonMapDeserializer(this);
    }

    /// <inheritdoc />
    public IArrayDeserializer deserialize_sequence(int? expectedElementCount = null)
    {
        return new JsonArrayDeserializer(this);
    }

    /// <summary>
    ///     基于当前已消费字节偏移量创建新的 <see cref="Utf8JsonReader" />�?    ///
    /// </summary>
    private Utf8JsonReader create_reader()
    {
        return new Utf8JsonReader(_json_utf8_bytes.AsSpan((int)_consumed));
    }

    #region 内部辅助方法

    /// <summary>
    ///     读取当前 Token 位置的属性名
    /// </summary>
    internal string read_property_name()
    {
        var reader = create_reader();
        var name = reader.GetString()!;
        reader.Read();
        _consumed += reader.BytesConsumed;
        return name;
    }

    /// <summary>
    ///     跳过当前 Token 所代表的完整值（包括嵌套对象和数组）�?    ///
    /// </summary>
    internal void skip_current_value()
    {
        var reader = create_reader();
        skip_current_value(ref reader);
        _consumed += reader.BytesConsumed;
    }

    /// <summary>
    ///     跳过当前 Token 所代表的完整值（包括嵌套对象和数组）�?    ///
    /// </summary>
    /// <param name="reader">当前�?JSON 读取器引用�?/param>
    private static void skip_current_value(ref Utf8JsonReader reader)
    {
        var depth = 0;

        while (true)
        {
            var tokenType = reader.TokenType;

            if (tokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                depth++;
            }
            else if (tokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
            {
                depth--;

                if (depth < 0) return;
            }
            else
            {
                if (depth == 0) return;
            }

            if (!reader.Read()) return;
        }
    }

    /// <summary>
    ///     推进到下一个属性名 Token（在对象内部使用）�?    ///
    /// </summary>
    /// <returns>如果还有属性返�?<c>true</c>，到达对象结尾返�?<c>false</c>�?/returns>
    internal bool advance_object_property()
    {
        skip_current_value();

        var reader = create_reader();

        if (!reader.Read())
        {
            _consumed += reader.BytesConsumed;
            return false;
        }

        if (reader.TokenType == JsonTokenType.EndObject)
        {
            _consumed += reader.BytesConsumed;
            return false;
        }

        _consumed += reader.BytesConsumed;
        return true;
    }

    /// <summary>
    ///     推进到下一个数组元素�?    ///
    /// </summary>
    /// <returns>如果还有元素返回 <c>true</c>，到达数组结尾返�?<c>false</c>�?/returns>
    internal bool advance_array_element()
    {
        skip_current_value();

        var reader = create_reader();

        if (!reader.Read())
        {
            _consumed += reader.BytesConsumed;
            return false;
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            _consumed += reader.BytesConsumed;
            return false;
        }

        _consumed += reader.BytesConsumed;
        return true;
    }

    #endregion

    #region 子读取器

    /// <summary>
    ///     JSON 对象反序列化子读取器
    /// </summary>
    private sealed class JsonMapDeserializer : IMapDeserializer
    {
        private readonly JsonDeserializer _parent;
        private bool _disposed;

        public JsonMapDeserializer(JsonDeserializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public Result<string, DeserializeException> read_field_name()
        {
            var reader = _parent.create_reader();

            if (reader.TokenType == JsonTokenType.EndObject)
                return Result<string, DeserializeException>.error(new DeserializeException());

            var name = _parent.read_property_name();
            return Result<string, DeserializeException>.ok(name);
        }

        /// <inheritdoc />
        public void deserialize_value(IDeserializer deserializer)
        {
            // 值已由外�?deserializer 读取，此处为占位
        }

        /// <inheritdoc />
        public T deserialize_value<T>(IDeserialize<T> deserialize)
        {
            return deserialize.deserialize(_parent);
        }

        /// <inheritdoc />
        public void end()
        {
            // 对象结束标记�?AdvanceObjectProperty 处理
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed) _disposed = true;
        }
    }

    /// <summary>
    ///     JSON 数组反序列化子读取器
    /// </summary>
    private sealed class JsonArrayDeserializer : IArrayDeserializer
    {
        private readonly JsonDeserializer _parent;
        private bool _disposed;

        public JsonArrayDeserializer(JsonDeserializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public bool try_read_element(IDeserializer deserializer)
        {
            var reader = _parent.create_reader();

            if (reader.TokenType == JsonTokenType.EndArray) return false;

            return true;
        }

        /// <inheritdoc />
        public void end()
        {
            // 数组结束标记�?AdvanceArrayElement 处理
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed) _disposed = true;
        }
    }

    #endregion
}