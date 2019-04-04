using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Ttf.Data;

namespace Std.Data.Binary.Ttf.Encode;

/// <summary>
///     TrueType/OpenType 字体文件编码器，的TtfFontData 编码的TTF 二进制格式的
///     TTF 格式始终使用大端序的
/// </summary>
public sealed class TtfEncoder
{
    /// <summary>
    ///     编码 TTF 字体数据为字节数组的
    /// </summary>
    /// <param name="data">
    ///     TTF 字体数据的/param>
    ///     <returns>编码后的字节数组的/returns>
    public byte[] encode(TtfFontData data)
    {
        if (data.font_type == TtfFontType.collection) throw new NotSupportedException("暂不支持 TrueType 集合字体编码");

        var tables = data.tables;
        var sortedTables = tables.OrderBy(t => t.tag).ToList();
        var tableCount = (ushort)sortedTables.Count;

        var headerSize = TtfConstants.offset_table_size + tableCount * TtfConstants.table_record_size;

        var dataOffset = (uint)headerSize;
        var tableOffsets = new uint[tableCount];

        for (var i = 0; i < sortedTables.Count; i++)
        {
            tableOffsets[i] = dataOffset;
            var paddedLength = pad_to4(sortedTables[i].data.Length);
            dataOffset += (uint)paddedLength;
        }

        var totalSize = (int)dataOffset;
        var writer = new ByteBufferWriter(totalSize);

        write_offset_table(ref writer, data.font_type, tableCount);
        write_table_records(ref writer, sortedTables, tableOffsets);
        write_table_data(ref writer, sortedTables);

        return writer.to_array();
    }

    #region 偏移的

    private static void write_offset_table(ref ByteBufferWriter writer, TtfFontType fontType, ushort tableCount)
    {
        switch (fontType)
        {
            case TtfFontType.true_type:
                writer.write_u32_be(TtfConstants.true_type_magic);
                break;
            case TtfFontType.cff:
                writer.write("OTTO"u8);
                break;
            default:
                writer.write_u32_be(TtfConstants.true_type_magic);
                break;
        }

        writer.write_u16_be(tableCount);

        var (searchRange, entrySelector, rangeShift) = calculate_search_fields(tableCount);
        writer.write_u16_be(searchRange);
        writer.write_u16_be(entrySelector);
        writer.write_u16_be(rangeShift);
    }

    #endregion

    #region 表记的

    private static void write_table_records(ref ByteBufferWriter writer, List<TtfTableRecord> tables, uint[] offsets)
    {
        for (var i = 0; i < tables.Count; i++)
        {
            var table = tables[i];
            var tagBytes = Encoding.ASCII.GetBytes(table.tag);

            if (tagBytes.Length < 4)
            {
                var padded = new byte[4];
                Array.Copy(tagBytes, padded, tagBytes.Length);
                writer.write(padded);
            }
            else
            {
                writer.write(tagBytes.AsSpan(0, 4));
            }

            var checksum = table.data.Length > 0 ? calculate_checksum(table.data) : table.checksum;
            writer.write_u32_be(checksum);
            writer.write_u32_be(offsets[i]);
            writer.write_u32_be((uint)table.data.Length);
        }
    }

    #endregion

    #region 表数的

    private static void write_table_data(ref ByteBufferWriter writer, List<TtfTableRecord> tables)
    {
        foreach (var table in tables)
        {
            if (table.data.Length == 0) continue;

            writer.write(table.data);

            var padding = pad_to4(table.data.Length) - table.data.Length;
            for (var i = 0; i < padding; i++) writer.write_u8(0);
        }
    }

    #endregion

    #region 校验和计的

    private static uint calculate_checksum(byte[] data)
    {
        uint sum = 0;
        var paddedLength = pad_to4(data.Length);

        for (var i = 0; i < paddedLength; i += 4)
        {
            uint word = 0;
            word |= (uint)(i < data.Length ? data[i] : 0) << 24;
            word |= (uint)(i + 1 < data.Length ? data[i + 1] : 0) << 16;
            word |= (uint)(i + 2 < data.Length ? data[i + 2] : 0) << 8;
            word |= (uint)(i + 3 < data.Length ? data[i + 3] : 0);
            sum += word;
        }

        return sum;
    }

    #endregion

    #region 辅助方法

    private static int pad_to4(int length)
    {
        return (length + 3) & ~3;
    }

    private static (ushort SearchRange, ushort EntrySelector, ushort RangeShift) calculate_search_fields(
        ushort tableCount)
    {
        if (tableCount == 0) return (0, 0, 0);

        var log2 = 0;
        var power = 1;

        while (power * 2 <= tableCount)
        {
            power *= 2;
            log2++;
        }

        var searchRange = (ushort)(power * 16);
        var entrySelector = (ushort)log2;
        var rangeShift = (ushort)(tableCount * 16 - searchRange);

        return (searchRange, entrySelector, rangeShift);
    }

    #endregion
}