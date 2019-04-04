using System.Buffers.Binary;
using Sonic.Category;
using Sonic.DataProcess.Deserialize;
using Sonic.DataProcess.Serialize;
using Sonic.Text;
using Sonic.Text.Utf8;

namespace Sonic.Data.Binary.Protobuf;

/// <summary>
/// <see cref="IDeserializer"/> �?Google Protocol Buffers 线格式实现�?/// 从字节数组读取解�?Protobuf 格式数据，支�?varint、fixed32、fixed64、length-delimited 四种线类型�?/// 内部维护读取偏移量，通过 <see cref="read_field_tag"/> 获取当前字段的标签信息�?/// 未知字段自动跳过，保证向前兼容�?/// </summary>
public sealed class ProtobufDeserializer : IDeserializer
{
    private readonly byte[] _data;
    private int _offset;
    private int _current_field_number;
    private int _current_wire_type;
    private int _nested_limit;

    /// <summary>
    /// 从字节数组创�?Protobuf 读取器�?    /// </summary>
    /// <param name="data">Protobuf 格式的字节数据�?/param>
    public ProtobufDeserializer(byte[] data)
    {
        _data = data;
    }

    /// <summary>
    /// 创建嵌套消息的子读取器。子读取器共享数据数组但拥有独立的偏移量和长度限制�?    /// </summary>
    /// <param name="data">共享的字节数组�?/param>
    /// <param name="start">子读取器在数组中的起始偏移�?/param>
    /// <param name="length">子读取器的数据长度�?/param>
    private ProtobufDeserializer(byte[] data, int start, int length)
    {
        _data = data;
        _offset = start;
        _nested_limit = start + length;
    }

    /// <summary>
    /// 当前正在读取的字段编号�?    /// </summary>
    public int current_field_number => _current_field_number;

    /// <summary>
    /// 当前正在读取的线类型�?    /// </summary>
    public int current_wire_type => _current_wire_type;

    /// <summary>
    /// 读取下一个字段标签（varint 编码�?field_number &lt;&lt; 3 | wire_type）�?    /// 投影层在处理完当前字段后调用此方法前进到下一个字段�?    /// 如果没有更多字段返回 <c>false</c>�?    /// </summary>
    /// <returns>如果成功读取下一个标签返�?<c>true</c>，否则返�?<c>false</c>�?/returns>
    public bool read_field_tag()
    {
        var limit = _nested_limit > 0 ? _nested_limit : _data.Length;

        if (_offset >= limit)
        {
            return false;
        }

        if (!try_read_varint(out var tag))
        {
            return false;
        }

        _current_field_number = (int)(tag >> 3);
        _current_wire_type = (int)(tag & 0x07);

        return true;
    }

    /// <inheritdoc />
    public bool try_read_null()
    {
        return false;
    }

    /// <inheritdoc />
    public bool deserialize_bool()
    {
        return deserialize_u64() != 0;
    }

    /// <inheritdoc />
    public int deserialize_i32()
    {
        return (int)deserialize_u64();
    }

    /// <inheritdoc />
    public long deserialize_i64()
    {
        return (long)deserialize_u64();
    }

    /// <inheritdoc />
    public ulong deserialize_u64()
    {
        if (!try_read_varint(out var value))
        {
            throw new DeserializeException();
        }

        return value;
    }

    /// <inheritdoc />
    public float deserialize_f32()
    {
        if (_offset + 4 > _data.Length)
        {
            throw new DeserializeException();
        }

        var value = BinaryPrimitives.ReadSingleLittleEndian(_data.AsSpan(_offset, 4));
        _offset += 4;
        return value;
    }

    /// <inheritdoc />
    public double deserialize_f64()
    {
        if (_offset + 8 > _data.Length)
        {
            throw new DeserializeException();
        }

        var value = BinaryPrimitives.ReadDoubleLittleEndian(_data.AsSpan(_offset, 8));
        _offset += 8;
        return value;
    }

    /// <inheritdoc />
    public Utf8Text deserialize_utf8()
    {
        if (!try_read_varint(out var length))
        {
            throw new DeserializeException();
        }

        var len = (int)length;

        if (_offset + len > _data.Length)
        {
            throw new DeserializeException();
        }

        var bytes = _data.AsSpan(_offset, len).ToArray();
        _offset += len;
        return Utf8Text.from_bytes_unchecked(bytes);
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> deserialize_bytes(int length)
    {
        if (_offset + length > _data.Length)
        {
            throw new DeserializeException();
        }

        var slice = _data.AsSpan(_offset, length).ToArray();
        _offset += length;
        return slice;
    }

    /// <inheritdoc />
    public IMapDeserializer deserialize_map(int? expectedFieldCount = null)
    {
        if (!try_read_varint(out var length))
        {
            throw new DeserializeException();
        }

        var len = (int)length;

        if (_offset + len > _data.Length)
        {
            throw new DeserializeException();
        }

        var subReader = new ProtobufDeserializer(_data, _offset, len);
        _offset += len;
        return new ProtobufMapDeserializer(subReader);
    }

    /// <inheritdoc />
    public IArrayDeserializer deserialize_sequence(int? expectedElementCount = null)
    {
        return new ProtobufArrayDeserializer(this, _nested_limit > 0 ? _nested_limit : _data.Length);
    }

    /// <summary>
    /// 跳过当前字段的值（根据 <see cref="current_wire_type"/>）�?    /// 投影层在遇到未知字段编号时调用此方法以保持向前兼容�?    /// </summary>
    public void skip_field()
    {
        switch (_current_wire_type)
        {
            case 0:
            {
                try_read_varint(out _);
                break;
            }
            case 1:
            {
                _offset += 8;
                break;
            }
            case 2:
            {
                if (try_read_varint(out var length))
                {
                    _offset += (int)length;
                }

                break;
            }
            case 5:
            {
                _offset += 4;
                break;
            }
        }
    }

    /// <summary>
    /// 尝试从当前偏移读取一�?varint 编码的无符号 64 位整数�?    /// </summary>
    private bool try_read_varint(out ulong value)
    {
        value = 0;
        var shift = 0;

        while (_offset < _data.Length)
        {
            var b = _data[_offset];
            _offset++;
            value |= (ulong)(b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0)
            {
                return true;
            }

            if (shift >= 64)
            {
                throw new DeserializeException();
            }
        }

        throw new DeserializeException();
    }

    #region 子读取器

    /// <summary>
    /// Protobuf 对象子读取器。委托给内嵌�?ProtobufDeserializer 实例�?    /// <c>read_field_name</c> 读取下一个字段标签并返回字段编号�?    /// </summary>
    private sealed class ProtobufMapDeserializer : IMapDeserializer
    {
        private readonly ProtobufDeserializer _parent;
        private bool _disposed;

        public ProtobufMapDeserializer(ProtobufDeserializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public Result<string, DeserializeException> read_field_name()
        {
            if (!_parent.read_field_tag())
            {
                return Result<string, DeserializeException>.error(new DeserializeException());
            }

            while (true)
            {
                switch (_parent._current_wire_type)
                {
                    case 0:
                    case 1:
                    case 5:
                    {
                        return Result<string, DeserializeException>.ok(_parent._current_field_number.ToString());
                    }
                    case 2:
                    {
                        return Result<string, DeserializeException>.ok(_parent._current_field_number.ToString());
                    }
                    default:
                    {
                        _parent.skip_field();

                        if (!_parent.read_field_tag())
                        {
                            return Result<string, DeserializeException>.error(new DeserializeException());
                        }

                        break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void deserialize_value(IDeserializer deserializer)
        {
        }

        /// <inheritdoc />
        public T deserialize_value<T>(IDeserialize<T> deserialize)
        {
            if (_parent._current_wire_type == 2)
            {
                if (!_parent.try_read_varint(out var length))
                {
                    throw new DeserializeException();
                }

                var len = (int)length;

                if (_parent._offset + len > _parent._data.Length)
                {
                    throw new DeserializeException();
                }

                var subReader = new ProtobufDeserializer(_parent._data, _parent._offset, len);
                _parent._offset += len;
                return deserialize.deserialize(subReader);
            }

            return deserialize.deserialize(_parent);
        }

        /// <inheritdoc />
        public void end()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Protobuf 数组子读取器。重复字段通过相同 tag 重复读取�?    /// </summary>
    private sealed class ProtobufArrayDeserializer : IArrayDeserializer
    {
        private readonly ProtobufDeserializer _parent;
        private readonly int _limit;
        private bool _disposed;
        private bool _first_element;

        public ProtobufArrayDeserializer(ProtobufDeserializer parent, int limit)
        {
            _parent = parent;
            _limit = limit;
            _first_element = true;
        }

        /// <inheritdoc />
        public bool try_read_element(IDeserializer deserializer)
        {
            if (_parent._offset >= _limit)
            {
                return false;
            }

            if (!_first_element)
            {
                if (!_parent.read_field_tag())
                {
                    return false;
                }
            }

            _first_element = false;
            return true;
        }

        /// <inheritdoc />
        public void end()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    #endregion
}
