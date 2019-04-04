using Std.Text.Utf8;

namespace Std.DataProcess.Serialize;

/// <summary>
///     将数据序列化为 SerdeValue 的序列化器实现
/// </summary>
public class SerdeValueSerializer : ISerializer
{
    private readonly Stack<object> _stack = new Stack<object>();
    private SerdeValue? _result;

    public SerdeValueSerializer()
    {
    }

    public SerdeValue Result => _result ?? SerdeValue.@null();

    public void serialize_null()
    {
        Push(SerdeValue.@null());
    }

    public void serialize_bool(bool value)
    {
        Push(SerdeValue.boolean(value));
    }

    public void serialize_i8(sbyte value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_i16(short value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_i32(int value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_i64(long value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_i128(Int128 value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_u8(byte value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_u16(ushort value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_u32(ulong value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_u64(ulong value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_u128(UInt128 value)
    {
        Push(SerdeValue.integer(value.ToString()));
    }

    public void serialize_f32(float value)
    {
        Push(SerdeValue.@decimal(value.ToString("G17", CultureInfo.InvariantCulture)));
    }

    public void serialize_f64(double value)
    {
        Push(SerdeValue.@decimal(value.ToString("G17", CultureInfo.InvariantCulture)));
    }

    public void serialize_utf8(Utf8Text text)
    {
        Push(SerdeValue.@string(text.ToString()));
    }

    public void serialize_utf16(Utf8Text text)
    {
        Push(SerdeValue.@string(text.ToString()));
    }

    public void serialize_bytes(ReadOnlySpan<byte> bytes)
    {
        Push(SerdeValue.@string(Convert.ToBase64String(bytes)));
    }

    public IMapSerializer serialize_map(int? countPair = null)
    {
        var map = new Dictionary<string, SerdeValue>();
        _stack.Push(map);
        return new SerdeMapSerializer(this, map);
    }

    public ITupleSerializer serialize_tuple(int countItem)
    {
        var elements = new List<SerdeValue>();
        _stack.Push(elements);
        return new SerdeTupleSerializer(this, elements);
    }

    public ISequenceSerializer serialize_sequence(int? countElement = null)
    {
        var elements = new List<SerdeValue>();
        _stack.Push(elements);
        return new SerdeSequenceSerializer(this, elements);
    }

    internal void Push(SerdeValue value)
    {
        if (_stack.Count == 0)
        {
            _result = value;
        }
        else
        {
            var top = _stack.Peek();
            if (top is List<SerdeValue> list)
            {
                list.Add(value);
            }
            else
            {
                throw new InvalidOperationException("Unexpected stack state");
            }
        }
    }

    internal void PushField(string name, SerdeValue value)
    {
        if (_stack.Count > 0 && _stack.Peek() is Dictionary<string, SerdeValue> map)
        {
            map[name] = value;
        }
        else
        {
            throw new InvalidOperationException("Unexpected stack state for field push");
        }
    }

    internal void Pop()
    {
        if (_stack.Count > 0)
        {
            var popped = _stack.Pop();
            SerdeValue value;
            if (popped is Dictionary<string, SerdeValue> map)
            {
                value = SerdeValue.@object(map);
            }
            else if (popped is List<SerdeValue> list)
            {
                value = SerdeValue.array(list);
            }
            else
            {
                throw new InvalidOperationException("Unexpected stack element type");
            }

            Push(value);
        }
    }
}
