using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.Coff.Data;

namespace Std.Data.Binary.Coff.Encode;

/// <summary>
///     COFF 文件编码器，的<see cref="CoffFileData" /> 编码的COFF 二进制格式的
/// </summary>
/// <remarks>
///     COFF 布局：Header(20) + SectionHeaders(40×N) + Relocations + SymbolTable + StringTable的
///     当前版本不编码原始节区数据（模型中不含此字段）的
/// </remarks>
public sealed class CoffEncoder
{
    private const int _symbol_entry_size = 18;
    private const int _section_header_size = 40;
    private const int _relocation_entry_size = 10;
    private const int _coff_header_size = 20;

    /// <summary>
    ///     的COFF 文件数据编码为二进制字节数组的
    /// </summary>
    /// <param name="data">
    ///     COFF 文件数据的/param>
    ///     <returns>COFF 二进制数据的/returns>
    public byte[] encode(CoffFileData data)
    {
        var sections = data.sections;
        var symbols = data.symbols;
        var relocations = data.relocations;

        var sectionCount = sections.Count;
        var symbolCount = symbols.Count;
        var relocCount = relocations.Count;

        var totalAuxSymbols = 0;

        foreach (var sym in symbols) totalAuxSymbols += sym.number_of_aux_symbols;

        // 计算字符串表大小
        var stringTableSize = 4; // 4 字节长度的
        foreach (var sym in symbols)
        {
            var nameLen = Encoding.UTF8.GetByteCount(sym.name);

            if (nameLen > 8) stringTableSize += nameLen + 1; // 的null 结尾
        }

        // 计算布局
        var relocOffset = _coff_header_size + sectionCount * _section_header_size;
        var symbolTableOffset = relocOffset + relocCount * _relocation_entry_size;
        var stringTableOffset = symbolTableOffset + (symbolCount + totalAuxSymbols) * _symbol_entry_size;
        var totalSize = stringTableOffset + stringTableSize;

        var buffer = new byte[totalSize];
        var pos = 0;

        // 1. 写入 Header
        write_header(ref buffer, pos, data.header, (uint)symbolCount, (uint)symbolTableOffset);
        pos += _coff_header_size;

        // 2. 写入 Section Headers
        for (var i = 0; i < sectionCount; i++)
        {
            var section = sections[i];
            var sectionRelocCount = i == 0 ? (ushort)relocCount : (ushort)0;

            write_section_header(ref buffer, pos, section, (uint)relocOffset, sectionRelocCount);
            pos += _section_header_size;
        }

        // 3. 写入 Relocations
        foreach (var reloc in relocations)
        {
            write_relocation(ref buffer, relocOffset, reloc);
            relocOffset += _relocation_entry_size;
        }

        // 4. 写入 Symbol Table
        var symPos = symbolTableOffset;
        var strTableCursor = 4; // 跳过 4 字节长度的
        foreach (var sym in symbols)
        {
            var nameBytes = Encoding.UTF8.GetBytes(sym.name);

            if (nameBytes.Length <= 8)
            {
                write_short_name_symbol(ref buffer, symPos, nameBytes, sym);
            }
            else
            {
                write_long_name_symbol(ref buffer, symPos, sym, strTableCursor);
                strTableCursor += nameBytes.Length + 1;
            }

            symPos += _symbol_entry_size;

            for (var a = 0; a < sym.number_of_aux_symbols; a++)
            {
                Array.Fill(buffer, (byte)0, symPos, _symbol_entry_size);
                symPos += _symbol_entry_size;
            }
        }

        // 5. 写入 String Table
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(stringTableOffset), stringTableSize);
        pos = stringTableOffset + 4;

        foreach (var sym in symbols)
        {
            var nameBytes = Encoding.UTF8.GetBytes(sym.name);

            if (nameBytes.Length > 8)
            {
                nameBytes.CopyTo(buffer.AsSpan(pos));
                pos += nameBytes.Length;
                buffer[pos++] = 0;
            }
        }

        return buffer;
    }

    #region 写入辅助方法

    private static void write_header(ref byte[] buffer, int offset, CoffHeaderData header, uint symbolCount,
        uint symbolTableOffset)
    {
        var span = buffer.AsSpan(offset);
        BinaryPrimitives.WriteUInt16LittleEndian(span, header.machine);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2), header.number_of_sections);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4), header.time_date_stamp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), symbolTableOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12), symbolCount);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(16), header.size_of_optional_header);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(18), header.characteristics);
    }

    private static void write_section_header(ref byte[] buffer, int offset, CoffSectionHeaderData section,
        uint relocationOffset, ushort relocationCount)
    {
        var span = buffer.AsSpan(offset);
        section.name_bytes.as_span().CopyTo(span.Slice(0, 8));
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), section.physical_address);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12), section.virtual_address);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16), section.size_of_raw_data);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(20), section.pointer_to_raw_data);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(24), relocationOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(28), section.pointer_to_linenumbers);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(32), relocationCount);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(34), section.number_of_linenumbers);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(36), section.characteristics);
    }

    private static void write_relocation(ref byte[] buffer, int offset, CoffRelocationData reloc)
    {
        var span = buffer.AsSpan(offset);
        BinaryPrimitives.WriteUInt32LittleEndian(span, reloc.virtual_address);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4), reloc.symbol_table_index);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(8), reloc.type);
    }

    private static void write_short_name_symbol(ref byte[] buffer, int offset, byte[] nameBytes, CoffSymbolData symbol)
    {
        var span = buffer.AsSpan(offset);
        nameBytes.CopyTo(span.Slice(0, System.Math.Min(nameBytes.Length, 8)));

        if (nameBytes.Length < 8) span.Slice(nameBytes.Length, 8 - nameBytes.Length).Fill(0);

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), symbol.value);
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(12), symbol.section_number);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(14), symbol.type);
        span[16] = symbol.storage_class;
        span[17] = symbol.number_of_aux_symbols;
    }

    private static void write_long_name_symbol(ref byte[] buffer, int offset, CoffSymbolData symbol, int strTableOffset)
    {
        var span = buffer.AsSpan(offset);
        span.Slice(0, 4).Fill(0);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4), strTableOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), symbol.value);
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(12), symbol.section_number);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(14), symbol.type);
        span[16] = symbol.storage_class;
        span[17] = symbol.number_of_aux_symbols;
    }

    #endregion
}