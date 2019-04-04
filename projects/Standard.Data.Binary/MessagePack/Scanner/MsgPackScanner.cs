using System.Buffers.Binary;
using Std.Data.Binary.MessagePack.Data;

namespace Std.Data.Binary.MessagePack.Scanner;

public ref struct MsgPackScanner
{
    private readonly ReadOnlySpan<byte> _data;

    public MsgPackScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
        position = 0;
    }

    public int position { get; private set; }

    public bool is_end => position >= _data.Length;

    public int length => _data.Length;

    public int remaining_bytes => _data.Length - position;

    public MsgPackScanHeader scan_header()
    {
        if (is_end) return new MsgPackScanHeader { root_type = MsgPackType.nil };

        var b = _data[position];
        var type = classify_type(b);
        return new MsgPackScanHeader { root_type = type, first_byte = b };
    }

    public bool is_possible_msg_pack()
    {
        if (is_end) return false;

        var b = _data[position];
        return b is <= MsgPackConstants.positive_fix_int_max or >= MsgPackConstants.negative_fix_int_min
            or MsgPackConstants.nil or MsgPackConstants.@false or MsgPackConstants.@true
            or >= MsgPackConstants.fix_map_min;
    }

    public MsgPackScanStatistics scan_statistics()
    {
        var stats = new MsgPackScanStatistics();

        while (position < _data.Length)
        {
            if (_data[position] == 0) break;

            var b = _data[position];
            var type = classify_type(b);
            count_type(ref stats, type);

            var skip = get_value_byte_count(b);
            if (skip <= 0) break;

            position += skip;
        }

        return stats;
    }

    private int get_value_byte_count(byte b)
    {
        if (b <= MsgPackConstants.positive_fix_int_max) return 1;

        if (b >= MsgPackConstants.negative_fix_int_min) return 1;

        if (b is >= MsgPackConstants.fix_map_min and <= MsgPackConstants.fix_map_max) return 1 + 2 * (b & 0x0F);

        if (b is >= MsgPackConstants.fix_array_min and <= MsgPackConstants.fix_array_max) return 1 + (b & 0x0F);

        if (b is >= MsgPackConstants.fix_str_min and <= MsgPackConstants.fix_str_max) return 1 + (b & 0x1F);

        return b switch
        {
            MsgPackConstants.nil or MsgPackConstants.@false or MsgPackConstants.@true => 1,
            MsgPackConstants.float32 => 5,
            MsgPackConstants.float64 => 9,
            MsgPackConstants.uint8 => 2,
            MsgPackConstants.uint16 => 3,
            MsgPackConstants.uint32 => 5,
            MsgPackConstants.uint64 => 9,
            MsgPackConstants.int8 => 2,
            MsgPackConstants.int16 => 3,
            MsgPackConstants.int32 => 5,
            MsgPackConstants.int64 => 9,
            MsgPackConstants.str8 => 2 + _data[position + 1],
            MsgPackConstants.str16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(position + 1, 2)),
            MsgPackConstants.str32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(position + 1, 4)),
            MsgPackConstants.bin8 => 2 + _data[position + 1],
            MsgPackConstants.bin16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(position + 1, 2)),
            MsgPackConstants.bin32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(position + 1, 4)),
            MsgPackConstants.array16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(position + 1, 2)),
            MsgPackConstants.array32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(position + 1, 4)),
            MsgPackConstants.map16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(position + 1, 2)),
            MsgPackConstants.map32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(position + 1, 4)),
            MsgPackConstants.fix_ext1 => 3,
            MsgPackConstants.fix_ext2 => 4,
            MsgPackConstants.fix_ext4 => 6,
            MsgPackConstants.fix_ext8 => 10,
            MsgPackConstants.fix_ext16 => 18,
            MsgPackConstants.ext8 => 3 + _data[position + 1],
            MsgPackConstants.ext16 => 4 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(position + 1, 2)),
            MsgPackConstants.ext32 => 6 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(position + 1, 4)),
            _ => 1
        };
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

    private static void count_type(ref MsgPackScanStatistics stats, MsgPackType type)
    {
        switch (type)
        {
            case MsgPackType.nil:
                stats.nil_count++;
                break;
            case MsgPackType.boolean:
                stats.bool_count++;
                break;
            case MsgPackType.integer:
            case MsgPackType.unsigned_integer:
                stats.integer_count++;
                break;
            case MsgPackType.@float:
                stats.float_count++;
                break;
            case MsgPackType.@string:
                stats.string_count++;
                break;
            case MsgPackType.binary:
                stats.binary_count++;
                break;
            case MsgPackType.array:
                stats.array_count++;
                break;
            case MsgPackType.map:
                stats.map_count++;
                break;
            case MsgPackType.extension:
                stats.extension_count++;
                break;
        }
    }
}

public sealed class MsgPackScanHeader
{
    public MsgPackType root_type { get; init; }
    public byte first_byte { get; init; }
}

public sealed class MsgPackScanStatistics
{
    public int nil_count { get; set; }
    public int bool_count { get; set; }
    public int integer_count { get; set; }
    public int float_count { get; set; }
    public int string_count { get; set; }
    public int binary_count { get; set; }
    public int array_count { get; set; }
    public int map_count { get; set; }
    public int extension_count { get; set; }
}