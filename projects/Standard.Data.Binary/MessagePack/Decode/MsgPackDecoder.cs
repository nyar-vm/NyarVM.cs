using Std.Data.Binary.Frame;
using Std.Data.Binary.MessagePack.Data;

namespace Std.Data.Binary.MessagePack.Decode;

/// <summary>
///     MessagePack 二进制解码器的
/// </summary>
public ref struct MsgPackDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="MsgPackDecoder" /> 结构的新实例的
    /// </summary>
    public MsgPackDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 MessagePack 数据的
    /// </summary>
    public MsgPackData decode()
    {
        var root = read_value();
        return new MsgPackData { root = root };
    }

    /// <summary>
    ///     仅解码第一个值的类型的
    /// </summary>
    public MsgPackType peek_type()
    {
        if (_buffer.is_end) return MsgPackType.nil;

        var b = _buffer.peek(1)[0];
        return classify_type(b);
    }

    private MsgPackValue read_value()
    {
        if (_buffer.is_end) return new MsgPackValue { type = MsgPackType.nil };

        var b = _buffer.read_u8();

        if (b <= MsgPackConstants.positive_fix_int_max)
            return new MsgPackValue { type = MsgPackType.integer, raw_value = (long)b };

        if (b >= MsgPackConstants.negative_fix_int_min)
            return new MsgPackValue { type = MsgPackType.integer, raw_value = (long)(sbyte)b };

        if (b is >= MsgPackConstants.fix_map_min and <= MsgPackConstants.fix_map_max) return read_map(b & 0x0F);

        if (b is >= MsgPackConstants.fix_array_min and <= MsgPackConstants.fix_array_max) return read_array(b & 0x0F);

        if (b is >= MsgPackConstants.fix_str_min and <= MsgPackConstants.fix_str_max) return read_string(b & 0x1F);

        return b switch
        {
            MsgPackConstants.nil => new MsgPackValue { type = MsgPackType.nil },
            MsgPackConstants.@false => new MsgPackValue { type = MsgPackType.boolean, raw_value = false },
            MsgPackConstants.@true => new MsgPackValue { type = MsgPackType.boolean, raw_value = true },
            MsgPackConstants.uint8 => new MsgPackValue
                { type = MsgPackType.unsigned_integer, raw_value = (long)_buffer.read_u8() },
            MsgPackConstants.uint16 => new MsgPackValue
                { type = MsgPackType.unsigned_integer, raw_value = (long)_buffer.read_u16_be() },
            MsgPackConstants.uint32 => new MsgPackValue
                { type = MsgPackType.unsigned_integer, raw_value = (long)_buffer.read_u32_be() },
            MsgPackConstants.uint64 => new MsgPackValue
                { type = MsgPackType.unsigned_integer, raw_value = (long)_buffer.read_u64_be() },
            MsgPackConstants.int8 => new MsgPackValue
                { type = MsgPackType.integer, raw_value = (long)_buffer.read_i8() },
            MsgPackConstants.int16 => new MsgPackValue
                { type = MsgPackType.integer, raw_value = (long)_buffer.read_i16_be() },
            MsgPackConstants.int32 => new MsgPackValue
                { type = MsgPackType.integer, raw_value = (long)_buffer.read_i32_be() },
            MsgPackConstants.int64 => new MsgPackValue
                { type = MsgPackType.integer, raw_value = _buffer.read_i64_be() },
            MsgPackConstants.float32 => new MsgPackValue
                { type = MsgPackType.@float, raw_value = _buffer.read_f32_be() },
            MsgPackConstants.float64 => new MsgPackValue
                { type = MsgPackType.@float, raw_value = _buffer.read_f64_be() },
            MsgPackConstants.str8 => read_string(_buffer.read_u8()),
            MsgPackConstants.str16 => read_string(_buffer.read_u16_be()),
            MsgPackConstants.str32 => read_string((int)_buffer.read_u32_be()),
            MsgPackConstants.bin8 => read_binary(_buffer.read_u8()),
            MsgPackConstants.bin16 => read_binary(_buffer.read_u16_be()),
            MsgPackConstants.bin32 => read_binary((int)_buffer.read_u32_be()),
            MsgPackConstants.array16 => read_array(_buffer.read_u16_be()),
            MsgPackConstants.array32 => read_array((int)_buffer.read_u32_be()),
            MsgPackConstants.map16 => read_map(_buffer.read_u16_be()),
            MsgPackConstants.map32 => read_map((int)_buffer.read_u32_be()),
            MsgPackConstants.fix_ext1 => read_extension(1),
            MsgPackConstants.fix_ext2 => read_extension(2),
            MsgPackConstants.fix_ext4 => read_extension(4),
            MsgPackConstants.fix_ext8 => read_extension(8),
            MsgPackConstants.fix_ext16 => read_extension(16),
            MsgPackConstants.ext8 => read_extension(_buffer.read_u8()),
            MsgPackConstants.ext16 => read_extension(_buffer.read_u16_be()),
            MsgPackConstants.ext32 => read_extension((int)_buffer.read_u32_be()),
            _ => new MsgPackValue { type = MsgPackType.nil }
        };
    }

    private MsgPackValue read_string(int length)
    {
        var str = _buffer.read_string(length);
        return new MsgPackValue { type = MsgPackType.@string, raw_value = str };
    }

    private MsgPackValue read_binary(int length)
    {
        var data = _buffer.read_bytes(length).ToArray();
        return new MsgPackValue { type = MsgPackType.binary, raw_value = data };
    }

    private MsgPackValue read_array(int count)
    {
        var items = new MsgPackValue[count];

        for (var i = 0; i < count; i++) items[i] = read_value();

        return new MsgPackValue { type = MsgPackType.array, array_items = items };
    }

    private MsgPackValue read_map(int count)
    {
        var entries = new MsgPackMapEntry[count];

        for (var i = 0; i < count; i++)
        {
            var key = read_value();
            var value = read_value();
            entries[i] = new MsgPackMapEntry { key = key, value = value };
        }

        return new MsgPackValue { type = MsgPackType.map, map_entries = entries };
    }

    private MsgPackValue read_extension(int length)
    {
        var type = (sbyte)_buffer.read_u8();
        var data = _buffer.read_bytes(length).ToArray();
        return new MsgPackValue { type = MsgPackType.extension, extension_type = type, extension_data = data };
    }

    private static MsgPackType classify_type(byte b)
    {
        if (b <= MsgPackConstants.positive_fix_int_max) return MsgPackType.integer;

        if (b >= MsgPackConstants.negative_fix_int_min) return MsgPackType.integer;

        if (b is >= MsgPackConstants.fix_map_min and <= MsgPackConstants.fix_map_max) return MsgPackType.map;

        if (b is >= MsgPackConstants.fix_array_min and <= MsgPackConstants.fix_array_max) return MsgPackType.array;

        if (b is >= MsgPackConstants.fix_str_min and <= MsgPackConstants.fix_str_max) return MsgPackType.@string;

        if (b is >= MsgPackConstants.fix_ext1 and <= MsgPackConstants.fix_ext16) return MsgPackType.extension;

        return b switch
        {
            MsgPackConstants.nil => MsgPackType.nil,
            MsgPackConstants.@false or MsgPackConstants.@true => MsgPackType.boolean,
            MsgPackConstants.float32 or MsgPackConstants.float64 => MsgPackType.@float,
            MsgPackConstants.uint8 or MsgPackConstants.uint16 or MsgPackConstants.uint32 or MsgPackConstants.uint64 =>
                MsgPackType.unsigned_integer,
            MsgPackConstants.int8 or MsgPackConstants.int16 or MsgPackConstants.int32 or MsgPackConstants.int64 =>
                MsgPackType.integer,
            MsgPackConstants.str8 or MsgPackConstants.str16 or MsgPackConstants.str32 => MsgPackType.@string,
            MsgPackConstants.bin8 or MsgPackConstants.bin16 or MsgPackConstants.bin32 => MsgPackType.binary,
            MsgPackConstants.array16 or MsgPackConstants.array32 => MsgPackType.array,
            MsgPackConstants.map16 or MsgPackConstants.map32 => MsgPackType.map,
            MsgPackConstants.ext8 or MsgPackConstants.ext16 or MsgPackConstants.ext32 => MsgPackType.extension,
            _ => MsgPackType.extension
        };
    }
}