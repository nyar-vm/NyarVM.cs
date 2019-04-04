using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.MessagePack.Data;

namespace Std.Data.Binary.MessagePack.Encode;

public sealed class MsgPackEncoder
{
    public byte[] encode(MsgPackData data)
    {
        var writer = new ByteBufferWriter(256);
        write_value(ref writer, data.root);
        return writer.to_array();
    }

    public int encode(MsgPackData data, Span<byte> buffer)
    {
        var writer = new ByteBufferWriter(buffer.Length + 256);
        write_value(ref writer, data.root);
        var encoded = writer.to_array();
        encoded.CopyTo(buffer);
        return encoded.Length;
    }

    private static void write_value(ref ByteBufferWriter writer, MsgPackValue value)
    {
        switch (value.type)
        {
            case MsgPackType.nil:
                writer.write_u8(MsgPackConstants.nil);
                break;

            case MsgPackType.boolean:
                writer.write_u8((bool)value.raw_value! ? MsgPackConstants.@true : MsgPackConstants.@false);
                break;

            case MsgPackType.integer:
                write_integer(ref writer, (long)value.raw_value!);
                break;

            case MsgPackType.unsigned_integer:
                write_unsigned_integer(ref writer, (ulong)value.raw_value!);
                break;

            case MsgPackType.@float:
                writer.write_u8(MsgPackConstants.float64);
                writer.write_f64_be((double)value.raw_value!);
                break;

            case MsgPackType.@string:
                write_string(ref writer, (string)value.raw_value!);
                break;

            case MsgPackType.binary:
                write_binary(ref writer, (byte[])value.raw_value!);
                break;

            case MsgPackType.array:
                write_array(ref writer, value.array_items);
                break;

            case MsgPackType.map:
                write_map(ref writer, value.map_entries);
                break;

            case MsgPackType.extension:
                write_extension(ref writer, value.extension_type, value.extension_data);
                break;
        }
    }

    private static void write_integer(ref ByteBufferWriter writer, long value)
    {
        if (value is >= 0 and <= MsgPackConstants.positive_fix_int_max)
        {
            writer.write_u8((byte)value);
            return;
        }

        if (value is >= -32 and < 0)
        {
            writer.write_u8((byte)(0xE0 + value + 32));
            return;
        }

        if (value is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            writer.write_u8(MsgPackConstants.int8);
            writer.write_i8((sbyte)value);
            return;
        }

        if (value is >= short.MinValue and <= short.MaxValue)
        {
            writer.write_u8(MsgPackConstants.int16);
            writer.write_i16_be((short)value);
            return;
        }

        if (value is >= int.MinValue and <= int.MaxValue)
        {
            writer.write_u8(MsgPackConstants.int32);
            writer.write_i32_be((int)value);
            return;
        }

        writer.write_u8(MsgPackConstants.int64);
        writer.write_i64_be(value);
    }

    private static void write_unsigned_integer(ref ByteBufferWriter writer, ulong value)
    {
        if (value <= MsgPackConstants.positive_fix_int_max)
        {
            writer.write_u8((byte)value);
            return;
        }

        if (value <= byte.MaxValue)
        {
            writer.write_u8(MsgPackConstants.uint8);
            writer.write_u8((byte)value);
            return;
        }

        if (value <= ushort.MaxValue)
        {
            writer.write_u8(MsgPackConstants.uint16);
            writer.write_u16_be((ushort)value);
            return;
        }

        if (value <= uint.MaxValue)
        {
            writer.write_u8(MsgPackConstants.uint32);
            writer.write_u32_be((uint)value);
            return;
        }

        writer.write_u8(MsgPackConstants.uint64);
        writer.write_u64_be(value);
    }

    private static void write_string(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var length = bytes.Length;

        if (length <= 31)
        {
            writer.write_u8((byte)(MsgPackConstants.fix_str_min | length));
            writer.write(bytes);
        }
        else if (length <= byte.MaxValue)
        {
            writer.write_u8(MsgPackConstants.str8);
            writer.write_u8((byte)length);
            writer.write(bytes);
        }
        else if (length <= ushort.MaxValue)
        {
            writer.write_u8(MsgPackConstants.str16);
            writer.write_u16_be((ushort)length);
            writer.write(bytes);
        }
        else
        {
            writer.write_u8(MsgPackConstants.str32);
            writer.write_u32_be((uint)length);
            writer.write(bytes);
        }
    }

    private static void write_binary(ref ByteBufferWriter writer, byte[] data)
    {
        var length = data.Length;

        if (length <= byte.MaxValue)
        {
            writer.write_u8(MsgPackConstants.bin8);
            writer.write_u8((byte)length);
            writer.write(data);
        }
        else if (length <= ushort.MaxValue)
        {
            writer.write_u8(MsgPackConstants.bin16);
            writer.write_u16_be((ushort)length);
            writer.write(data);
        }
        else
        {
            writer.write_u8(MsgPackConstants.bin32);
            writer.write_u32_be((uint)length);
            writer.write(data);
        }
    }

    private static void write_array(ref ByteBufferWriter writer, IReadOnlyList<MsgPackValue> items)
    {
        var count = items.Count;

        if (count <= 15)
        {
            writer.write_u8((byte)(MsgPackConstants.fix_array_min | count));
        }
        else if (count <= ushort.MaxValue)
        {
            writer.write_u8(MsgPackConstants.array16);
            writer.write_u16_be((ushort)count);
        }
        else
        {
            writer.write_u8(MsgPackConstants.array32);
            writer.write_u32_be((uint)count);
        }

        for (var i = 0; i < count; i++) write_value(ref writer, items[i]);
    }

    private static void write_map(ref ByteBufferWriter writer, IReadOnlyList<MsgPackMapEntry> entries)
    {
        var count = entries.Count;

        if (count <= 15)
        {
            writer.write_u8((byte)(MsgPackConstants.fix_map_min | count));
        }
        else if (count <= ushort.MaxValue)
        {
            writer.write_u8(MsgPackConstants.map16);
            writer.write_u16_be((ushort)count);
        }
        else
        {
            writer.write_u8(MsgPackConstants.map32);
            writer.write_u32_be((uint)count);
        }

        for (var i = 0; i < count; i++)
        {
            write_value(ref writer, entries[i].key);
            write_value(ref writer, entries[i].value);
        }
    }

    private static void write_extension(ref ByteBufferWriter writer, sbyte type, byte[] data)
    {
        var length = data.Length;

        switch (length)
        {
            case 1:
                writer.write_u8(MsgPackConstants.fix_ext1);
                break;
            case 2:
                writer.write_u8(MsgPackConstants.fix_ext2);
                break;
            case 4:
                writer.write_u8(MsgPackConstants.fix_ext4);
                break;
            case 8:
                writer.write_u8(MsgPackConstants.fix_ext8);
                break;
            case 16:
                writer.write_u8(MsgPackConstants.fix_ext16);
                break;
            case <= byte.MaxValue:
                writer.write_u8(MsgPackConstants.ext8);
                writer.write_u8((byte)length);
                break;
            case <= ushort.MaxValue:
                writer.write_u8(MsgPackConstants.ext16);
                writer.write_u16_be((ushort)length);
                break;
            default:
                writer.write_u8(MsgPackConstants.ext32);
                writer.write_u32_be((uint)length);
                break;
        }

        writer.write_u8((byte)type);
        writer.write(data);
    }
}