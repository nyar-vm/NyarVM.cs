using Std.Data.Binary.Dwarf.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dwarf.Decode;

/// <summary>
///     DWARF 文件解码器，解析调试信息格式的
/// </summary>
public sealed class DwarfDecoder
{
    /// <summary>
    ///     的DWARF 二进制数据解码调试信息的
    /// </summary>
    /// <param name="data">
    ///     DWARF 二进制数据的/param>
    ///     <returns>解码后的 DWARF 文件数据的/returns>
    public DwarfFileData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return decode_file(ref buffer);
    }

    private DwarfFileData decode_file(ref ByteBuffer buffer)
    {
        var compilationUnits = new List<DwarfCompilationUnitData>();
        var lineNumberTables = new List<DwarfLineNumberTableData>();

        while (!buffer.is_end)
        {
            var unit = read_compilation_unit(ref buffer);
            if (unit != null)
                compilationUnits.Add(unit);
            else
                break;
        }

        while (!buffer.is_end)
        {
            var table = read_line_number_table(ref buffer);
            if (table != null)
                lineNumberTables.Add(table);
            else
                break;
        }

        return new DwarfFileData
        {
            compilation_units = compilationUnits,
            line_number_tables = lineNumberTables
        };
    }


    /// <summary>
    ///     读取编译单元的
    /// </summary>
    private DwarfCompilationUnitData? read_compilation_unit(ref ByteBuffer buffer)
    {
        if (buffer.remaining < 4) return null;

        var unitLength = buffer.read_u32_le();
        if (unitLength == 0xFFFFFFFF) unitLength = (uint)buffer.read_u64_le();

        if (unitLength == 0 || buffer.position + unitLength > buffer.length) return null;

        var version = buffer.read_u16_le();
        var debugInfoOffset = buffer.read_u32_le();
        var addressSize = buffer.read_u8();
        var segmentSelectorSize = buffer.read_u8();

        var entries = new List<DwarfEntryData>(32);
        var endPosition = buffer.position + (int)unitLength - 8;

        while (buffer.position < endPosition)
        {
            var abbrevCode = buffer.read_leb128_u64();
            if (abbrevCode == 0) break;

            var tag = (uint)buffer.read_leb128_u64();
            var hasChildren = buffer.read_u8() != 0;

            var attributes = new List<DwarfAttributeData>();

            while (buffer.position < endPosition)
            {
                var attrName = buffer.read_leb128_u64();
                var attrForm = buffer.read_leb128_u64();

                if (attrName == 0 && attrForm == 0) break;

                var value = read_attribute_value(ref buffer, attrForm);
                attributes.Add(new DwarfAttributeData
                {
                    name = (uint)attrName,
                    form = (uint)attrForm,
                    value = value
                });
            }

            entries.Add(new DwarfEntryData
            {
                abbreviation_code = abbrevCode,
                tag = tag,
                has_children = hasChildren,
                attributes = attributes
            });
        }

        return new DwarfCompilationUnitData
        {
            unit_length = unitLength,
            version = version,
            debug_info_offset = debugInfoOffset,
            address_size = addressSize,
            segment_selector_size = segmentSelectorSize,
            entries = entries
        };
    }


    /// <summary>
    ///     读取行号表的
    /// </summary>
    private DwarfLineNumberTableData? read_line_number_table(ref ByteBuffer buffer)
    {
        if (buffer.remaining < 4) return null;

        var unitLength = buffer.read_u32_le();

        if (unitLength == 0 || buffer.position + unitLength > buffer.length) return null;

        var endPosition = buffer.position + (int)unitLength;

        var version = buffer.read_u16_le();
        var addressSize = buffer.read_u8();
        var segmentSelectorSize = buffer.read_u8();
        var headerLength = buffer.read_u32_le();
        var minimumInstructionLength = buffer.read_u8();
        var maximumOperationsPerInstruction = buffer.read_u8();
        var defaultIsStatement = buffer.read_u8();
        var lineBase = buffer.read_i8();
        var lineRange = buffer.read_u8();
        var opcodeBase = buffer.read_u8();

        var standardOpcodeLengths = new List<byte>(opcodeBase);
        var opcodeLengthCount = opcodeBase - 1;

        if (opcodeLengthCount < 0) return null;

        for (var i = 0; i < opcodeLengthCount; i++)
        {
            if (buffer.position >= endPosition) return null;

            standardOpcodeLengths.Add(buffer.read_u8());
        }

        var fileNames = new List<string>(4);

        while (buffer.position < endPosition)
        {
            var fileName = buffer.read_null_terminated_string();
            fileNames.Add(fileName);
        }

        return new DwarfLineNumberTableData
        {
            unit_length = unitLength,
            version = version,
            address_size = addressSize,
            segment_selector_size = segmentSelectorSize,
            header_length = headerLength,
            minimum_instruction_length = minimumInstructionLength,
            maximum_operations_per_instruction = maximumOperationsPerInstruction,
            default_is_statement = defaultIsStatement,
            line_base = lineBase,
            line_range = lineRange,
            opcode_base = opcodeBase,
            standard_opcode_lengths = standardOpcodeLengths,
            file_names = fileNames
        };
    }


    /// <summary>
    ///     读取属性值的
    /// </summary>
    private object? read_attribute_value(ref ByteBuffer buffer, ulong form)
    {
        return form switch
        {
            0x01 => null,
            0x03 => buffer.read_u16_le(),
            0x04 => buffer.read_u32_le(),
            0x05 => buffer.read_u8() == 1,
            0x06 => buffer.read_u8(),
            0x07 => buffer.read_u8(),
            0x08 => buffer.read_leb128_u64(),
            0x09 => buffer.read_u16_le(),
            0x0A => buffer.read_u32_le(),
            0x0B => buffer.read_null_terminated_string(),
            0x0C => buffer.read_u8(),
            0x0D => buffer.read_u16_le(),
            0x0E => buffer.read_u32_le(),
            0x0F => buffer.read_u64_le(),
            0x10 => buffer.read_leb128_u64(),
            0x11 => buffer.read_null_terminated_string(),
            _ => null
        };
    }
}