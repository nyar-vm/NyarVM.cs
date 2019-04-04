using Std.Data.Binary.Dwarf.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dwarf.Encode;

/// <summary>
///     DWARF 文件编码器，将调试信息结构体序列化为 DWARF 二进制数据的
/// </summary>
public sealed class DwarfEncoder
{
    /// <summary>
    ///     的DWARF 文件数据编码为二进制数据的
    /// </summary>
    /// <param name="fileData">
    ///     DWARF 文件数据的/param>
    ///     <returns>DWARF 二进制数据的/returns>
    public byte[] encode(DwarfFileData fileData)
    {
        var writer = new ByteBufferWriter(4096);

        foreach (var cu in fileData.compilation_units) write_compilation_unit(ref writer, cu);

        if (fileData.line_number_tables.Count > 0)
        {
            writer.write_u32_le(0);

            foreach (var lineTable in fileData.line_number_tables) write_line_number_table(ref writer, lineTable);
        }

        return writer.written_data.ToArray();
    }

    #region 编译单元

    /// <summary>
    ///     写入编译单元的
    /// </summary>
    private static void write_compilation_unit(ref ByteBufferWriter writer, DwarfCompilationUnitData cu)
    {
        var bodyWriter = new ByteBufferWriter(1024);
        write_compilation_unit_body(ref bodyWriter, cu);
        var bodySpan = bodyWriter.written_data;

        var unitLength = (uint)bodySpan.Length;

        writer.write_u32_le(unitLength);
        writer.write(bodySpan);
    }


    /// <summary>
    ///     写入编译单元主体（不的unitLength 字段）的
    /// </summary>
    private static void write_compilation_unit_body(ref ByteBufferWriter writer, DwarfCompilationUnitData cu)
    {
        writer.write_u16_le(cu.version);
        writer.write_u32_le(cu.debug_info_offset);
        writer.write_u8(cu.address_size);
        writer.write_u8(cu.segment_selector_size);

        foreach (var entry in cu.entries) write_entry(ref writer, entry);

        writer.write_leb128_u64(0);
    }

    #endregion

    #region 条目与属的

    /// <summary>
    ///     写入条目的
    /// </summary>
    private static void write_entry(ref ByteBufferWriter writer, DwarfEntryData entry)
    {
        writer.write_leb128_u64(entry.abbreviation_code);
        writer.write_leb128_u64(entry.tag);
        writer.write_u8((byte)(entry.has_children ? 1 : 0));

        foreach (var attr in entry.attributes) write_attribute(ref writer, attr);

        writer.write_leb128_u64(0);
        writer.write_leb128_u64(0);
    }


    /// <summary>
    ///     写入属性的
    /// </summary>
    private static void write_attribute(ref ByteBufferWriter writer, DwarfAttributeData attr)
    {
        writer.write_leb128_u64(attr.name);
        writer.write_leb128_u64(attr.form);
        write_attribute_value(ref writer, attr.form, attr.value);
    }


    /// <summary>
    ///     根据属性形式写入属性值的
    /// </summary>
    private static void write_attribute_value(ref ByteBufferWriter writer, uint form, object? value)
    {
        switch (form)
        {
            case 0x01:
                break;

            case 0x03:
                writer.write_u16_le(convert_to_u16(value));
                break;

            case 0x04:
                writer.write_u32_le(convert_to_u32(value));
                break;

            case 0x05:
                writer.write_u8((byte)(is_true(value) ? 1 : 0));
                break;

            case 0x06:
                writer.write_u8(convert_to_u8(value));
                break;

            case 0x07:
                writer.write_u8(convert_to_u8(value));
                break;

            case 0x08:
                writer.write_leb128_u64(convert_to_u64(value));
                break;

            case 0x09:
                writer.write_u16_le(convert_to_u16(value));
                break;

            case 0x0A:
                writer.write_u32_le(convert_to_u32(value));
                break;

            case 0x0B:
                writer.write_null_terminated_string(convert_to_string(value));
                break;

            case 0x0C:
                writer.write_u8(convert_to_u8(value));
                break;

            case 0x0D:
                writer.write_u16_le(convert_to_u16(value));
                break;

            case 0x0E:
                writer.write_u32_le(convert_to_u32(value));
                break;

            case 0x0F:
                writer.write_u64_le(convert_to_u64(value));
                break;

            case 0x10:
                writer.write_leb128_u64(convert_to_u64(value));
                break;

            case 0x11:
                writer.write_null_terminated_string(convert_to_string(value));
                break;
        }
    }

    #endregion

    #region 行号的

    /// <summary>
    ///     写入行号表的
    /// </summary>
    private static void write_line_number_table(ref ByteBufferWriter writer, DwarfLineNumberTableData table)
    {
        var bodyWriter = new ByteBufferWriter(512);
        write_line_number_table_body(ref bodyWriter, table);
        var bodySpan = bodyWriter.written_data;

        var unitLength = (uint)bodySpan.Length;

        writer.write_u32_le(unitLength);
        writer.write(bodySpan);
    }


    /// <summary>
    ///     写入行号表主体（不含 unitLength 字段）的
    /// </summary>
    private static void write_line_number_table_body(ref ByteBufferWriter writer, DwarfLineNumberTableData table)
    {
        writer.write_u16_le(table.version);
        writer.write_u8(table.address_size);
        writer.write_u8(table.segment_selector_size);
        writer.write_u32_le(table.header_length);
        writer.write_u8(table.minimum_instruction_length);
        writer.write_u8(table.maximum_operations_per_instruction);
        writer.write_u8(table.default_is_statement);
        writer.write_i8(table.line_base);
        writer.write_u8(table.line_range);
        writer.write_u8(table.opcode_base);

        foreach (var length in table.standard_opcode_lengths) writer.write_u8(length);

        foreach (var fileName in table.file_names) writer.write_null_terminated_string(fileName);
    }

    #endregion

    #region 值转换辅助方的

    /// <summary>
    ///     将对象值转换为 ushort的
    /// </summary>
    private static ushort convert_to_u16(object? value)
    {
        return value switch
        {
            null => 0,
            ushort v => v,
            short v => (ushort)v,
            int v => (ushort)v,
            uint v => (ushort)v,
            long v => (ushort)v,
            ulong v => (ushort)v,
            string s => ushort.TryParse(s, out var r) ? r : (ushort)0,
            _ => (ushort)Convert.ChangeType(value, typeof(ushort))
        };
    }


    /// <summary>
    ///     将对象值转换为 uint的
    /// </summary>
    private static uint convert_to_u32(object? value)
    {
        return value switch
        {
            null => 0,
            uint v => v,
            int v => (uint)v,
            ushort v => v,
            short v => (uint)v,
            long v => (uint)v,
            ulong v => (uint)v,
            string s => uint.TryParse(s, out var r) ? r : 0,
            _ => (uint)Convert.ChangeType(value, typeof(uint))
        };
    }


    /// <summary>
    ///     将对象值转换为 ulong的
    /// </summary>
    private static ulong convert_to_u64(object? value)
    {
        return value switch
        {
            null => 0,
            ulong v => v,
            long v => (ulong)v,
            uint v => v,
            int v => (ulong)v,
            ushort v => v,
            short v => (ulong)v,
            byte v => v,
            sbyte v => (ulong)v,
            string s => ulong.TryParse(s, out var r) ? r : 0,
            _ => (ulong)Convert.ChangeType(value, typeof(ulong))
        };
    }


    /// <summary>
    ///     将对象值转换为 byte的
    /// </summary>
    private static byte convert_to_u8(object? value)
    {
        return value switch
        {
            null => 0,
            byte v => v,
            sbyte v => (byte)v,
            ushort v => (byte)v,
            short v => (byte)v,
            int v => (byte)v,
            uint v => (byte)v,
            long v => (byte)v,
            ulong v => (byte)v,
            bool v => v ? (byte)1 : (byte)0,
            string s => byte.TryParse(s, out var r) ? r : (byte)0,
            _ => (byte)Convert.ChangeType(value, typeof(byte))
        };
    }


    /// <summary>
    ///     判断对象值是否为 true的
    /// </summary>
    private static bool is_true(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            byte b => b != 0,
            sbyte b => b != 0,
            ushort v => v != 0,
            short v => v != 0,
            int v => v != 0,
            uint v => v != 0,
            long v => v != 0,
            ulong v => v != 0,
            string s => bool.TryParse(s, out var r) && r,
            _ => false
        };
    }


    /// <summary>
    ///     将对象值转换为字符串的
    /// </summary>
    private static string convert_to_string(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            _ => value.ToString() ?? string.Empty
        };
    }

    #endregion
}