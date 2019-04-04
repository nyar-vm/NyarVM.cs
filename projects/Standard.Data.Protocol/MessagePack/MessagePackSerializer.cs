using Sonic.DataProcess.Encode;
using Sonic.DataProcess.Serialize;
using Sonic.DataProcess.Write;
using Sonic.Text;
using Sonic.Text.Utf8;

namespace Sonic.Data.Binary.MessagePack;

/// <summary>
/// <see cref="ISerializer"/> �?MessagePack 实现，内部使�?<see cref="ArrayBufferWriter{Byte}"/> 进行写入�?/// 写入完成后通过 <see cref="to_bytes"/> 获取 MessagePack 字节序列�?/// </summary>
public sealed class MessagePackSerializer : ISerializer, IDisposable
{
    private readonly ArrayBufferWriter<byte> _writer;

    /// <summary>
    /// 初始化一个新�?MessagePack 写入器实例�?    /// </summary>
    public MessagePackSerializer()
    {
        _writer = new ArrayBufferWriter<byte>();
    }

    /// <inheritdoc />
    public void serialize_null()
    {
        var span = _writer.get_span(1);
        span[0] = 0xC0;
        _writer.advance(1);
    }

    /// <inheritdoc />
    public void serialize_bool(bool value)
    {
        var span = _writer.get_span(1);
        span[0] = value ? (byte)0xC3 : (byte)0xC2;
        _writer.advance(1);
    }

    /// <inheritdoc />
    public void serialize_i8(sbyte value)
    {
        var span = _writer.get_span(2);
        span[0] = 0xD0;
        span[1] = (byte)value;
        _writer.advance(2);
    }

    /// <inheritdoc />
    public void serialize_i16(short value)
    {
        var span = _writer.get_span(3);
        span[0] = 0xD1;
        BigEndianEncoder.encode_i16(span.Slice(1), value);
        _writer.advance(3);
    }

    /// <inheritdoc />
    public void serialize_i32(int value)
    {
        if (value is >= 0 and <= 127)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)value;
            _writer.advance(1);
            return;
        }

        if (value is >= -32 and <= -1)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)(0xE0 + (value + 32));
            _writer.advance(1);
            return;
        }

        if (value is >= -128 and <= 127)
        {
            var span = _writer.get_span(2);
            span[0] = 0xD0;
            span[1] = (byte)value;
            _writer.advance(2);
            return;
        }

        if (value is >= -32768 and <= 32767)
        {
            var span = _writer.get_span(3);
            span[0] = 0xD1;
            BigEndianEncoder.encode_i16(span.Slice(1), (short)value);
            _writer.advance(3);
            return;
        }

        {
            var span = _writer.get_span(5);
            span[0] = 0xD2;
            BigEndianEncoder.encode_i32(span.Slice(1), value);
            _writer.advance(5);
        }
    }

    /// <inheritdoc />
    public void serialize_i64(long value)
    {
        if (value is >= 0 and <= 127)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)value;
            _writer.advance(1);
            return;
        }

        if (value is >= -32 and <= -1)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)(0xE0 + (value + 32));
            _writer.advance(1);
            return;
        }

        if (value is >= -128 and <= 127)
        {
            var span = _writer.get_span(2);
            span[0] = 0xD0;
            span[1] = (byte)value;
            _writer.advance(2);
            return;
        }

        if (value is >= -32768 and <= 32767)
        {
            var span = _writer.get_span(3);
            span[0] = 0xD1;
            BigEndianEncoder.encode_i16(span.Slice(1), (short)value);
            _writer.advance(3);
            return;
        }

        if (value is >= int.MinValue and <= int.MaxValue)
        {
            var span = _writer.get_span(5);
            span[0] = 0xD2;
            BigEndianEncoder.encode_i32(span.Slice(1), (int)value);
            _writer.advance(5);
            return;
        }

        {
            var span = _writer.get_span(9);
            span[0] = 0xD3;
            BigEndianEncoder.encode_i64(span.Slice(1), value);
            _writer.advance(9);
        }
    }

    /// <inheritdoc />
    public void serialize_i128(Int128 value)
    {
        var span = _writer.get_span(17);
        span[0] = 0xD3;

        for (var i = 0; i < 16; i++)
        {
            span[1 + i] = (byte)(value >> (120 - i * 8));
        }

        _writer.advance(17);
    }

    /// <inheritdoc />
    public void serialize_u8(byte value)
    {
        var span = _writer.get_span(2);
        span[0] = 0xCC;
        span[1] = value;
        _writer.advance(2);
    }

    /// <inheritdoc />
    public void serialize_u16(ushort value)
    {
        var span = _writer.get_span(3);
        span[0] = 0xCD;
        BigEndianEncoder.encode_u16(span.Slice(1), value);
        _writer.advance(3);
    }

    /// <inheritdoc />
    public void serialize_u32(ulong value)
    {
        serialize_u64(value);
    }

    /// <inheritdoc />
    public void serialize_u64(ulong value)
    {
        var span = _writer.get_span(9);
        span[0] = 0xCF;
        BigEndianEncoder.encode_u64(span.Slice(1), value);
        _writer.advance(9);
    }

    /// <inheritdoc />
    public void serialize_u128(UInt128 value)
    {
        var span = _writer.get_span(17);
        span[0] = 0xCF;

        for (var i = 0; i < 16; i++)
        {
            span[1 + i] = (byte)(value >> (120 - i * 8));
        }

        _writer.advance(17);
    }

    /// <inheritdoc />
    public void serialize_f32(float value)
    {
        var span = _writer.get_span(5);
        span[0] = 0xCA;
        BigEndianEncoder.encode_f32(span.Slice(1), value);
        _writer.advance(5);
    }

    /// <inheritdoc />
    public void serialize_f64(double value)
    {
        var span = _writer.get_span(9);
        span[0] = 0xCB;
        BigEndianEncoder.encode_f64(span.Slice(1), value);
        _writer.advance(9);
    }

    /// <inheritdoc />
    public void serialize_utf8(Utf8Text text)
    {
        var utf8Span = text.as_span();
        var byteLength = utf8Span.Length;

        if (byteLength <= 31)
        {
            var span = _writer.get_span(1 + byteLength);
            span[0] = (byte)(0xA0 | byteLength);
            utf8Span.CopyTo(span.Slice(1));
            _writer.advance(1 + byteLength);
            return;
        }

        if (byteLength <= 255)
        {
            var span = _writer.get_span(2 + byteLength);
            span[0] = 0xD9;
            span[1] = (byte)byteLength;
            utf8Span.CopyTo(span.Slice(2));
            _writer.advance(2 + byteLength);
            return;
        }

        {
            var span = _writer.get_span(3 + byteLength);
            span[0] = 0xDA;
            BigEndianEncoder.encode_u16(span.Slice(1), (ushort)byteLength);
            utf8Span.CopyTo(span.Slice(3));
            _writer.advance(3 + byteLength);
        }
    }

    /// <inheritdoc />
    public void serialize_utf16(Utf8Text text)
    {
        serialize_utf8(text);
    }

    /// <inheritdoc />
    public void serialize_bytes(ReadOnlySpan<byte> bytes)
    {
        var len = bytes.Length;

        if (len <= 255)
        {
            var span = _writer.get_span(2 + len);
            span[0] = 0xC4;
            span[1] = (byte)len;
            bytes.CopyTo(span.Slice(2));
            _writer.advance(2 + len);
            return;
        }

        if (len <= 65535)
        {
            var span = _writer.get_span(3 + len);
            span[0] = 0xC5;
            BigEndianEncoder.encode_u16(span.Slice(1), (ushort)len);
            bytes.CopyTo(span.Slice(3));
            _writer.advance(3 + len);
            return;
        }

        {
            var span = _writer.get_span(5 + len);
            span[0] = 0xC6;
            BigEndianEncoder.encode_u32(span.Slice(1), (uint)len);
            bytes.CopyTo(span.Slice(5));
            _writer.advance(5 + len);
        }
    }

    /// <inheritdoc />
    public IMapSerializer serialize_map(int? countPair = null)
    {
        var count = countPair ?? 0;

        if (count <= 15)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)(0x80 | count);
            _writer.advance(1);
        }
        else
        {
            var span = _writer.get_span(3);
            span[0] = 0xDE;
            BigEndianEncoder.encode_u16(span.Slice(1), (ushort)count);
            _writer.advance(3);
        }

        return new MessagePackMapSerializer(this);
    }

    /// <inheritdoc />
    public ITupleSerializer serialize_tuple(int countItem)
    {
        if (countItem <= 15)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)(0x90 | countItem);
            _writer.advance(1);
        }
        else
        {
            var span = _writer.get_span(3);
            span[0] = 0xDC;
            BigEndianEncoder.encode_u16(span.Slice(1), (ushort)countItem);
            _writer.advance(3);
        }

        return new MessagePackTupleSerializer(this);
    }

    /// <inheritdoc />
    public ISequenceSerializer serialize_sequence(int? countElement = null)
    {
        var count = countElement ?? 0;

        if (count <= 15)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)(0x90 | count);
            _writer.advance(1);
        }
        else
        {
            var span = _writer.get_span(3);
            span[0] = 0xDC;
            BigEndianEncoder.encode_u16(span.Slice(1), (ushort)count);
            _writer.advance(3);
        }

        return new MessagePackSequenceSerializer(this);
    }

    /// <summary>
    /// 以字节数组形式返回已写入�?MessagePack 数据�?    /// </summary>
    /// <returns>MessagePack 字节数组�?/returns>
    public byte[] to_bytes()
    {
        return _writer.written_span.ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    #region 子写入器

    /// <summary>
    /// MessagePack 对象序列化子写入器。MessagePack 中对象无需闭合标记，记录完成即可�?    /// </summary>
    private sealed class MessagePackMapSerializer : IMapSerializer
    {
        private readonly MessagePackSerializer _parent;
        private bool _ended;

        public MessagePackMapSerializer(MessagePackSerializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public void write_field_name(string name)
        {
            _parent.serialize_utf8(Utf8Text.from_string(name));
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
                _ended = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended)
            {
                end();
            }
        }
    }

    /// <summary>
    /// MessagePack 数组序列化子写入器。MessagePack 中数组无需闭合标记�?    /// </summary>
    private sealed class MessagePackSequenceSerializer : ISequenceSerializer
    {
        private readonly MessagePackSerializer _parent;
        private bool _ended;

        public MessagePackSequenceSerializer(MessagePackSerializer parent)
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
            if (!_ended)
            {
                _ended = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended)
            {
                end();
            }
        }
    }

    /// <summary>
    /// MessagePack 元组序列化子写入器。元组在 MessagePack 中表示为数组，无需闭合标记�?    /// </summary>
    private sealed class MessagePackTupleSerializer : ITupleSerializer
    {
        private readonly MessagePackSerializer _parent;
        private bool _ended;

        public MessagePackTupleSerializer(MessagePackSerializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_ended)
            {
                _ended = true;
            }
        }
    }

    #endregion
}
