using System.Buffers.Binary;
using Sonic.DataProcess.Encode;
using Sonic.DataProcess.Serialize;
using Sonic.DataProcess.Write;
using Sonic.Text;
using Sonic.Text.Utf8;

namespace Sonic.Data.Binary.Protobuf;

/// <summary>
/// <see cref="ISerializer"/> �?Google Protocol Buffers 线格式实现�?/// 使用 varint 编码整数、长度前缀编码字符�?嵌套消息、固�?32/64 编码浮点�?/// 写入完成后通过 <see cref="to_bytes"/> 获取 Protobuf 字节序列�?/// 字段标签（tag = field_number &lt;&lt; 3 | wire_type）需由投影层在调用标量方法前通过
/// <see cref="write_field_tag"/> 写入，或通过 <see cref="IMapSerializer.write_field_name"/> 写入
/// wire_type=2 的长度定界标签�?/// </summary>
public sealed class ProtobufSerializer : ISerializer, IDisposable
{
    private readonly ArrayBufferWriter<byte> _writer;

    /// <summary>
    /// 初始化一个新�?Protobuf 写入器实例�?    /// </summary>
    public ProtobufSerializer()
    {
        _writer = new ArrayBufferWriter<byte>();
    }

    /// <summary>
    /// 写入 Protobuf 字段标签（varint 编码�?field_number &lt;&lt; 3 | wire_type）�?    /// 投影层在写入每个字段值前调用此方法�?    /// </summary>
    /// <param name="fieldNumber">字段编号�? �?2^29-1）�?/param>
    /// <param name="wireType">线类型：0=Varint, 1=64-bit, 2=Length-delimited, 5=32-bit�?/param>
    public void write_field_tag(int fieldNumber, int wireType)
    {
        write_varint((ulong)(fieldNumber << 3) | (uint)wireType);
    }

    /// <summary>
    /// 写入 varint 编码的无符号 64 位整数，用于嵌套消息的长度前缀�?    /// </summary>
    /// <param name="value">要编码的无符号整数值�?/param>
    public void write_varint(ulong value)
    {
        while (value > 0x7F)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)((value & 0x7F) | 0x80);
            _writer.advance(1);
            value >>= 7;
        }

        {
            var span = _writer.get_span(1);
            span[0] = (byte)(value & 0x7F);
            _writer.advance(1);
        }
    }

    /// <inheritdoc />
    public void serialize_null()
    {
    }

    /// <inheritdoc />
    public void serialize_bool(bool value)
    {
        serialize_u64(value ? 1UL : 0UL);
    }

    /// <inheritdoc />
    public void serialize_i8(sbyte value)
    {
        serialize_u64((ulong)(long)value);
    }

    /// <inheritdoc />
    public void serialize_i16(short value)
    {
        serialize_u64((ulong)(long)value);
    }

    /// <inheritdoc />
    public void serialize_i32(int value)
    {
        serialize_u64((ulong)value);
    }

    /// <inheritdoc />
    public void serialize_i64(long value)
    {
        serialize_u64((ulong)value);
    }

    /// <inheritdoc />
    public void serialize_i128(Int128 value)
    {
        serialize_u64((ulong)value);
    }

    /// <inheritdoc />
    public void serialize_u8(byte value)
    {
        serialize_u64(value);
    }

    /// <inheritdoc />
    public void serialize_u16(ushort value)
    {
        serialize_u64(value);
    }

    /// <inheritdoc />
    public void serialize_u32(ulong value)
    {
        serialize_u64(value);
    }

    /// <inheritdoc />
    public void serialize_u64(ulong value)
    {
        while (value > 0x7F)
        {
            var span = _writer.get_span(1);
            span[0] = (byte)((value & 0x7F) | 0x80);
            _writer.advance(1);
            value >>= 7;
        }

        {
            var span = _writer.get_span(1);
            span[0] = (byte)(value & 0x7F);
            _writer.advance(1);
        }
    }

    /// <inheritdoc />
    public void serialize_u128(UInt128 value)
    {
        serialize_u64((ulong)value);
    }

    /// <inheritdoc />
    public void serialize_f32(float value)
    {
        var span = _writer.get_span(4);
        BinaryPrimitives.WriteSingleLittleEndian(span, value);
        _writer.advance(4);
    }

    /// <inheritdoc />
    public void serialize_f64(double value)
    {
        var span = _writer.get_span(8);
        BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        _writer.advance(8);
    }

    /// <inheritdoc />
    public void serialize_utf8(Utf8Text text)
    {
        var utf8Span = text.as_span();
        write_varint((ulong)utf8Span.Length);

        var span = _writer.get_span(utf8Span.Length);
        utf8Span.CopyTo(span);
        _writer.advance(utf8Span.Length);
    }

    /// <inheritdoc />
    public void serialize_utf16(Utf8Text text)
    {
        serialize_utf8(text);
    }

    /// <inheritdoc />
    public void serialize_bytes(ReadOnlySpan<byte> bytes)
    {
        write_varint((ulong)bytes.Length);

        var span = _writer.get_span(bytes.Length);
        bytes.CopyTo(span);
        _writer.advance(bytes.Length);
    }

    /// <inheritdoc />
    public IMapSerializer serialize_map(int? countPair = null)
    {
        return new ProtobufMapSerializer(this);
    }

    /// <inheritdoc />
    public ITupleSerializer serialize_tuple(int countItem)
    {
        return new ProtobufTupleSerializer(this);
    }

    /// <inheritdoc />
    public ISequenceSerializer serialize_sequence(int? countElement = null)
    {
        return new ProtobufSequenceSerializer(this);
    }

    /// <summary>
    /// 以字节数组形式返回已写入�?Protobuf 数据�?    /// </summary>
    /// <returns>Protobuf 字节数组�?/returns>
    public byte[] to_bytes()
    {
        return _writer.written_span.ToArray();
    }

    /// <summary>
    /// 将已写入数据复制到目标跨度�?    /// </summary>
    /// <param name="destination">目标跨度�?/param>
    public void copy_to(Span<byte> destination)
    {
        _writer.written_span.CopyTo(destination);
    }

    /// <summary>
    /// 已写入数据的字节长度�?    /// </summary>
    public int written_length => _writer.written_span.Length;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    #region 子写入器

    /// <summary>
    /// Protobuf 对象序列化子写入器。Protobuf 消息无容器开�?结束标记�?    /// <c>write_field_name</c> 写入 wire_type=2 �?varint 标签（用于嵌套消息和长度定界字段）�?    /// </summary>
    private sealed class ProtobufMapSerializer : IMapSerializer
    {
        private readonly ProtobufSerializer _parent;
        private bool _ended;

        public ProtobufMapSerializer(ProtobufSerializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public void write_field_name(string name)
        {
            if (int.TryParse(name, out var fieldNumber))
            {
                _parent.write_varint((ulong)(fieldNumber << 3) | 2);
            }
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
    /// Protobuf 数组序列化子写入器。Protobuf 无容器开�?结束标记�?    /// </summary>
    private sealed class ProtobufSequenceSerializer : ISequenceSerializer
    {
        private readonly ProtobufSerializer _parent;
        private bool _ended;

        public ProtobufSequenceSerializer(ProtobufSerializer parent)
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
    /// Protobuf 元组序列化子写入器。Protobuf 无容器开�?结束标记�?    /// </summary>
    private sealed class ProtobufTupleSerializer : ITupleSerializer
    {
        private readonly ProtobufSerializer _parent;
        private bool _ended;

        public ProtobufTupleSerializer(ProtobufSerializer parent)
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