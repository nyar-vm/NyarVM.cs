using System.Text;
using Std.DataProcess.Serialize;
using Std.Text;
using Std.Text.Utf8;

namespace Std.Data.Text.Csv;

/// <summary>
///     <see cref="ISerializer" /> �?CSV 实现，内部使�?<see cref="StringBuilder" /> 构建 CSV 文本�?/// 写入完成后通过
///     <see cref="to_utf8_bytes" /> 获取 CSV 字节序列�?///
/// </summary>
public sealed class CsvSerializer : ISerializer, IDisposable
{
    private readonly StringBuilder _builder;
    private bool _is_first_value = true;

    /// <summary>
    ///     初始化一个新�?CSV 写入器实例�?    ///
    /// </summary>
    public CsvSerializer()
    {
        _builder = new StringBuilder();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void serialize_null()
    {
        write_separator();
    }

    /// <inheritdoc />
    public void serialize_bool(bool value)
    {
        write_separator();
        _builder.Append(value ? "true" : "false");
    }

    /// <inheritdoc />
    public void serialize_i8(sbyte value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i16(short value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i32(int value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i64(long value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i128(Int128 value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u8(byte value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u16(ushort value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u32(ulong value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u64(ulong value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u128(UInt128 value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_f32(float value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_f64(double value)
    {
        write_separator();
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_utf8(Utf8Text text)
    {
        write_separator();
        csv_escape_and_append(_builder, text.ToString());
    }

    /// <inheritdoc />
    public void serialize_utf16(Utf8Text text)
    {
        serialize_utf8(text);
    }

    /// <inheritdoc />
    public void serialize_bytes(ReadOnlySpan<byte> bytes)
    {
        write_separator();
        csv_escape_and_append(_builder, SonicEncoding.to_base64(bytes));
    }

    /// <inheritdoc />
    public IMapSerializer serialize_map(int? countPair = null)
    {
        return new CsvMapSerializer(this);
    }

    /// <inheritdoc />
    public ITupleSerializer serialize_tuple(int countItem)
    {
        return new CsvTupleSerializer(this);
    }

    /// <inheritdoc />
    public ISequenceSerializer serialize_sequence(int? countElement = null)
    {
        return new CsvSequenceSerializer(this);
    }

    /// <summary>
    ///     将已构建�?CSV 文本�?UTF-8 字节数组形式返回�?    ///
    /// </summary>
    /// <returns>CSV �?UTF-8 字节数组�?/returns>
    public byte[] to_utf8_bytes()
    {
        return SonicEncoding.encode_utf8(_builder.ToString());
    }

    /// <summary>
    ///     在写入新值之前添加逗号分隔符（首值除外）�?    ///
    /// </summary>
    private void write_separator()
    {
        if (!_is_first_value)
            _builder.Append(',');
        else
            _is_first_value = false;
    }

    /// <summary>
    ///     对字符串进行 CSV 转义后追加到指定 <see cref="StringBuilder" />�?    /// 如果值包含逗号、引号或换行符，则用双引号包裹并转义内部引号�?    ///
    /// </summary>
    /// <param name="builder">
    ///     目标 <see cref="StringBuilder" />�?/param>
    ///     <param name="value">要转义的原始值�?/param>
    internal static void csv_escape_and_append(StringBuilder builder, string value)
    {
        var needsQuoting = false;

        foreach (var ch in value)
            if (ch is ',' or '"' or '\n' or '\r')
            {
                needsQuoting = true;
                break;
            }

        if (!needsQuoting)
        {
            builder.Append(value);
            return;
        }

        builder.Append('"');

        foreach (var ch in value)
            if (ch == '"')
                builder.Append("\"\"");
            else
                builder.Append(ch);

        builder.Append('"');
    }

    #region 子写入器

    /// <summary>
    ///     CSV 对象序列化子写入器。第一行写入字段名作为表头�?    ///
    /// </summary>
    private sealed class CsvMapSerializer : IMapSerializer
    {
        private readonly CsvSerializer _parent;
        private bool _ended;
        private bool _is_first_field = true;

        public CsvMapSerializer(CsvSerializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public void write_field_name(string name)
        {
            if (!_is_first_field)
                _parent._builder.Append(',');
            else
                _is_first_field = false;

            csv_escape_and_append(_parent._builder, name);
        }

        /// <inheritdoc />
        public void write_value(ISerializer serializer)
        {
        }

        /// <inheritdoc />
        public void end()
        {
            if (!_ended) _ended = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended) end();
        }
    }

    /// <summary>
    ///     CSV 数组序列化子写入器。元素以逗号分隔�?    ///
    /// </summary>
    private sealed class CsvSequenceSerializer : ISequenceSerializer
    {
        private readonly CsvSerializer _parent;
        private bool _ended;

        public CsvSequenceSerializer(CsvSerializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public void write_element(ISerializer serializer)
        {
        }

        /// <inheritdoc />
        public void end()
        {
            if (!_ended) _ended = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended) end();
        }
    }

    /// <summary>
    ///     CSV 元组序列化子写入器。元素以逗号分隔�?    ///
    /// </summary>
    private sealed class CsvTupleSerializer : ITupleSerializer
    {
        private readonly CsvSerializer _parent;
        private bool _ended;

        public CsvTupleSerializer(CsvSerializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended) _ended = true;
        }
    }

    #endregion
}