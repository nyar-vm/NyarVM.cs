using Sonic.Category;
using Sonic.DataProcess.Deserialize;
using Sonic.DataProcess.Encode;
using Sonic.DataProcess.Serialize;
using Sonic.Text;
using Sonic.Text.Utf8;

namespace Sonic.Data.Binary.MessagePack;

/// <summary>
/// <see cref="IDeserializer"/> �?MessagePack 实现，从字节数组读取并解�?MessagePack 格式数据�?/// 内部维护读取偏移量，通过 <see cref="deserialize_map"/> / <see cref="deserialize_sequence"/> 获取子读取器�?/// </summary>
public sealed class MessagePackDeserializer : IDeserializer
{
    private readonly byte[] _data;
    private int _offset;

    /// <summary>
    /// 从字节数组创�?MessagePack 读取器�?    /// </summary>
    /// <param name="data">MessagePack 格式的字节数据�?/param>
    public MessagePackDeserializer(byte[] data)
    {
        _data = data;
    }

    /// <inheritdoc />
    public bool try_read_null()
    {
        if (_offset >= _data.Length)
        {
            return false;
        }

        if (_data[_offset] == 0xC0)
        {
            _offset++;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool deserialize_bool()
    {
        var b = _data[_offset];

        if (b == 0xC3)
        {
            _offset++;
            return true;
        }

        if (b == 0xC2)
        {
            _offset++;
            return false;
        }

        throw new DeserializeException();
    }

    /// <inheritdoc />
    public int deserialize_i32()
    {
        var b = _data[_offset];

        if (b <= 0x7F)
        {
            _offset++;
            return b;
        }

        if (b >= 0xE0)
        {
            _offset++;
            return b - 0xE0 - 32;
        }

        switch (b)
        {
            case 0xD0:
                _offset += 2;
                return (sbyte)_data[_offset - 1];
            case 0xD1:
                _offset += 3;
                return BigEndianEncoder.decode_i16(_data.AsSpan(_offset - 2, 2));
            case 0xD2:
                _offset += 5;
                return BigEndianEncoder.decode_i32(_data.AsSpan(_offset - 4, 4));
            case 0xCC:
                _offset += 2;
                return _data[_offset - 1];
            case 0xCD:
                _offset += 3;
                return BigEndianEncoder.decode_u16(_data.AsSpan(_offset - 2, 2));
            default:
                throw new DeserializeException();
        }
    }

    /// <inheritdoc />
    public long deserialize_i64()
    {
        var b = _data[_offset];

        if (b <= 0x7F)
        {
            _offset++;
            return b;
        }

        if (b >= 0xE0)
        {
            _offset++;
            return (long)(b - 0xE0 - 32);
        }

        switch (b)
        {
            case 0xD0:
                _offset += 2;
                return (sbyte)_data[_offset - 1];
            case 0xD1:
                _offset += 3;
                return BigEndianEncoder.decode_i16(_data.AsSpan(_offset - 2, 2));
            case 0xD2:
                _offset += 5;
                return BigEndianEncoder.decode_i32(_data.AsSpan(_offset - 4, 4));
            case 0xD3:
                _offset += 9;
                return BigEndianEncoder.decode_i64(_data.AsSpan(_offset - 8, 8));
            case 0xCC:
                _offset += 2;
                return _data[_offset - 1];
            case 0xCD:
                _offset += 3;
                return BigEndianEncoder.decode_u16(_data.AsSpan(_offset - 2, 2));
            case 0xCE:
                _offset += 5;
                return BigEndianEncoder.decode_u32(_data.AsSpan(_offset - 4, 4));
            default:
                throw new DeserializeException();
        }
    }

    /// <inheritdoc />
    public ulong deserialize_u64()
    {
        var b = _data[_offset];

        if (b != 0xCF)
        {
            throw new DeserializeException();
        }

        _offset += 9;
        return BigEndianEncoder.decode_u64(_data.AsSpan(_offset - 8, 8));
    }

    /// <inheritdoc />
    public float deserialize_f32()
    {
        var b = _data[_offset];

        if (b != 0xCA)
        {
            throw new DeserializeException();
        }

        _offset += 5;
        return BigEndianEncoder.decode_f32(_data.AsSpan(_offset - 4, 4));
    }

    /// <inheritdoc />
    public double deserialize_f64()
    {
        var b = _data[_offset];

        if (b != 0xCB)
        {
            throw new DeserializeException();
        }

        _offset += 9;
        return BigEndianEncoder.decode_f64(_data.AsSpan(_offset - 8, 8));
    }

    /// <inheritdoc />
    public Utf8Text deserialize_utf8()
    {
        var b = _data[_offset];
        int length;

        if (b is >= 0xA0 and <= 0xBF)
        {
            length = b & 0x1F;
            _offset += 1;
        }
        else if (b == 0xD9)
        {
            _offset++;
            length = _data[_offset];
            _offset++;
        }
        else if (b == 0xDA)
        {
            _offset++;
            length = BigEndianEncoder.decode_u16(_data.AsSpan(_offset, 2));
            _offset += 2;
        }
        else
        {
            throw new DeserializeException();
        }

        var bytes = _data.AsSpan(_offset, length).ToArray();
        _offset += length;
        return Utf8Text.from_bytes_unchecked(bytes);
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> deserialize_bytes(int length)
    {
        var slice = _data.AsSpan(_offset, length).ToArray();
        _offset += length;
        return slice;
    }

    /// <inheritdoc />
    public IMapDeserializer deserialize_map(int? expectedFieldCount = null)
    {
        var b = _data[_offset];

        if (b is >= 0x80 and <= 0x8F)
        {
            _offset++;
        }
        else if (b == 0xDE)
        {
            _offset += 3;
        }
        else
        {
            throw new DeserializeException();
        }

        return new MessagePackMapDeserializer(this);
    }

    /// <inheritdoc />
    public IArrayDeserializer deserialize_sequence(int? expectedElementCount = null)
    {
        var b = _data[_offset];

        if (b is >= 0x90 and <= 0x9F)
        {
            _offset++;
        }
        else if (b == 0xDC)
        {
            _offset += 3;
        }
        else
        {
            throw new DeserializeException();
        }

        return new MessagePackArrayDeserializer(this);
    }

    #region 子读取器

    /// <summary>
    /// MessagePack 对象反序列化子读取器。对象中字段名后跟随值，字段值由调用者通过父读取器直接读取�?    /// </summary>
    private sealed class MessagePackMapDeserializer : IMapDeserializer
    {
        private readonly MessagePackDeserializer _parent;
        private bool _disposed;

        public MessagePackMapDeserializer(MessagePackDeserializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public Result<string, DeserializeException> read_field_name()
        {
            if (_parent._offset >= _parent._data.Length)
            {
                return Result<string, DeserializeException>.error(new DeserializeException());
            }

            try
            {
                var name = _parent.deserialize_utf8();
                return Result<string, DeserializeException>.ok(name.ToString());
            }
            catch (DeserializeException)
            {
                return Result<string, DeserializeException>.error(new DeserializeException());
            }
        }

        /// <inheritdoc />
        public void deserialize_value(IDeserializer deserializer)
        {
        }

        /// <inheritdoc />
        public T deserialize_value<T>(IDeserialize<T> deserialize)
        {
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
    /// MessagePack 数组反序列化子读取器�?    /// </summary>
    private sealed class MessagePackArrayDeserializer : IArrayDeserializer
    {
        private readonly MessagePackDeserializer _parent;
        private bool _disposed;

        public MessagePackArrayDeserializer(MessagePackDeserializer parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        public bool try_read_element(IDeserializer deserializer)
        {
            return _parent._offset < _parent._data.Length;
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

