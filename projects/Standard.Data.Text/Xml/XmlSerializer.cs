using System.Text;
using Std.DataProcess.Serialize;
using Std.Text;
using Std.Text.Utf8;

namespace Std.Data.Text.Xml;

/// <summary>
///     <see cref="ISerializer" /> �?XML 实现，内部使�?<see cref="StringBuilder" /> 构建 XML 文本�?/// 写入完成后通过
///     <see cref="to_utf8_bytes" /> 获取 XML 字节序列�?///
/// </summary>
public sealed class XmlSerializer : ISerializer, IDisposable
{
    private readonly StringBuilder _builder;

    /// <summary>
    ///     初始化一个新�?XML 写入器实例�?    ///
    /// </summary>
    public XmlSerializer()
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
        _builder.Append("<null />");
    }

    /// <inheritdoc />
    public void serialize_bool(bool value)
    {
        _builder.Append(value ? "true" : "false");
    }

    /// <inheritdoc />
    public void serialize_i8(sbyte value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i16(short value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i32(int value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i64(long value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_i128(Int128 value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u8(byte value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u16(ushort value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u32(ulong value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u64(ulong value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_u128(UInt128 value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_f32(float value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_f64(double value)
    {
        _builder.Append(value);
    }

    /// <inheritdoc />
    public void serialize_utf8(Utf8Text text)
    {
        var str = text.ToString();
        xml_escape_and_append(str);
    }

    /// <inheritdoc />
    public void serialize_utf16(Utf8Text text)
    {
        serialize_utf8(text);
    }

    /// <inheritdoc />
    public void serialize_bytes(ReadOnlySpan<byte> bytes)
    {
        _builder.Append(SonicEncoding.to_base64(bytes));
    }

    /// <inheritdoc />
    public IMapSerializer serialize_map(int? countPair = null)
    {
        return new XmlMapSerializer(_builder);
    }

    /// <inheritdoc />
    public ITupleSerializer serialize_tuple(int countItem)
    {
        return new XmlTupleSerializer(_builder);
    }

    /// <inheritdoc />
    public ISequenceSerializer serialize_sequence(int? countElement = null)
    {
        return new XmlSequenceSerializer(_builder);
    }

    /// <summary>
    ///     将已构建�?XML 文本�?UTF-8 字节数组形式返回�?    ///
    /// </summary>
    /// <returns>XML �?UTF-8 字节数组�?/returns>
    public byte[] to_utf8_bytes()
    {
        return SonicEncoding.encode_utf8(_builder.ToString());
    }

    /// <summary>
    ///     对字符串进行 XML 特殊字符转义后追加到 <see cref="StringBuilder" />�?    ///
    /// </summary>
    private void xml_escape_and_append(string value)
    {
        foreach (var ch in value)
            switch (ch)
            {
                case '<':
                    _builder.Append("&lt;");
                    break;
                case '>':
                    _builder.Append("&gt;");
                    break;
                case '&':
                    _builder.Append("&amp;");
                    break;
                case '"':
                    _builder.Append("&quot;");
                    break;
                case '\'':
                    _builder.Append("&apos;");
                    break;
                default:
                    _builder.Append(ch);
                    break;
            }
    }

    #region 子写入器

    /// <summary>
    ///     XML 对象序列化子写入器。write_field_name 写入开标签，end 写入闭标签�?    ///
    /// </summary>
    private sealed class XmlMapSerializer : IMapSerializer
    {
        private readonly StringBuilder _builder;
        private bool _ended;
        private string? _last_field_name;

        public XmlMapSerializer(StringBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void write_field_name(string name)
        {
            if (_last_field_name != null)
            {
                _builder.Append("</");
                _builder.Append(_last_field_name);
                _builder.Append('>');
            }

            _builder.Append('<');
            _builder.Append(name);
            _builder.Append('>');
            _last_field_name = name;
        }

        /// <inheritdoc />
        public void write_value(ISerializer serializer)
        {
        }

        /// <inheritdoc />
        public void end()
        {
            if (!_ended)
            {
                if (_last_field_name != null)
                {
                    _builder.Append("</");
                    _builder.Append(_last_field_name);
                    _builder.Append('>');
                }

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
    ///     XML 数组序列化子写入器。每个元素包裹在 &lt;item&gt; 标签中�?    ///
    /// </summary>
    private sealed class XmlSequenceSerializer : ISequenceSerializer
    {
        private readonly StringBuilder _builder;
        private bool _ended;

        public XmlSequenceSerializer(StringBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void write_element(ISerializer serializer)
        {
            _builder.Append("<item>");
        }

        /// <inheritdoc />
        public void end()
        {
            if (!_ended)
            {
                _builder.Append("</item>");
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
    ///     XML 元组序列化子写入器。每个元素包裹在 &lt;item&gt; 标签中�?    ///
    /// </summary>
    private sealed class XmlTupleSerializer : ITupleSerializer
    {
        private readonly StringBuilder _builder;
        private bool _ended;

        public XmlTupleSerializer(StringBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended) _ended = true;
        }
    }

    #endregion
}