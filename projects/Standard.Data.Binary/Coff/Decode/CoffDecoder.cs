using System.Buffers.Binary;
using System.Text;
using Std.Codec;
using Std.Data.Binary.Coff.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Coff.Decode;

/// <summary>
///     COFF 文件解码器，解析 Windows 目标文件的obj）格式的
/// </summary>
public sealed class CoffDecoder
{
    /// <summary>
    ///     的COFF 二进制数据解码目标文件的
    /// </summary>
    /// <param name="data">
    ///     COFF 二进制数据的/param>
    ///     <returns>解码后的 COFF 文件数据�?returns>
    public CoffFileData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return decode_file(ref buffer);
    }

    private CoffFileData decode_file(ref ByteBuffer buffer)
    {
        var header = read_coff_header(ref buffer);
        var sections = read_section_headers(ref buffer, header);
        var symbols = read_symbol_table(ref buffer, header);
        var relocations = read_relocations(ref buffer, sections);

        return new CoffFileData
        {
            header = header,
            sections = sections,
            symbols = symbols,
            relocations = relocations
        };
    }

    /// <summary>
    ///     读取 COFF 头的
    /// </summary>
    private CoffHeaderData read_coff_header(ref ByteBuffer buffer)
    {
        var machine = buffer.read_u16_le();
        var numberOfSections = buffer.read_u16_le();
        var timeDateStamp = buffer.read_u32_le();
        var pointerToSymbolTable = buffer.read_u32_le();
        var numberOfSymbols = buffer.read_u32_le();
        var sizeOfOptionalHeader = buffer.read_u16_le();
        var characteristics = buffer.read_u16_le();

        var data = new CoffHeaderData
        {
            machine = machine,
            number_of_sections = numberOfSections,
            time_date_stamp = timeDateStamp,
            pointer_to_symbol_table = pointerToSymbolTable,
            number_of_symbols = numberOfSymbols,
            size_of_optional_header = sizeOfOptionalHeader,
            characteristics = characteristics
        };
        return data;
    }

    /// <summary>
    ///     读取节区头的
    /// </summary>
    private List<CoffSectionHeaderData> read_section_headers(ref ByteBuffer buffer, CoffHeaderData header)
    {
        var sections = new List<CoffSectionHeaderData>();

        for (var i = 0; i < header.number_of_sections; i++)
        {
            var nameBytes = FixedBytes8.from_span(buffer.read_bytes(8));

            var physicalAddress = buffer.read_u32_le();
            var virtualAddress = buffer.read_u32_le();
            var sizeOfRawData = buffer.read_u32_le();
            var pointerToRawData = buffer.read_u32_le();
            var pointerToRelocations = buffer.read_u32_le();
            var pointerToLinenumbers = buffer.read_u32_le();
            var numberOfRelocations = buffer.read_u16_le();
            var numberOfLinenumbers = buffer.read_u16_le();
            var characteristics = buffer.read_u32_le();

            var item = new CoffSectionHeaderData
            {
                name_bytes = nameBytes,
                physical_address = physicalAddress,
                virtual_address = virtualAddress,
                size_of_raw_data = sizeOfRawData,
                pointer_to_raw_data = pointerToRawData,
                pointer_to_relocations = pointerToRelocations,
                pointer_to_linenumbers = pointerToLinenumbers,
                number_of_relocations = numberOfRelocations,
                number_of_linenumbers = numberOfLinenumbers,
                characteristics = characteristics
            };
            sections.Add(item);
        }

        return sections;
    }

    /// <summary>
    ///     读取符号表的
    /// </summary>
    private List<CoffSymbolData> read_symbol_table(ref ByteBuffer buffer, CoffHeaderData header)
    {
        var symbols = new List<CoffSymbolData>();

        if (header.pointer_to_symbol_table == 0 || header.number_of_symbols == 0) return symbols;

        buffer.position = (int)header.pointer_to_symbol_table;

        for (var i = 0; i < header.number_of_symbols; i++)
        {
            var nameBytes = buffer.read_bytes(8).ToArray();
            var value = buffer.read_u32_le();
            var sectionNumber = buffer.read_i16_le();
            var type = buffer.read_u16_le();
            var storageClass = buffer.read_u8();
            var numberOfAuxSymbols = buffer.read_u8();

            var name = decode_symbol_name(nameBytes, ref buffer, header);

            symbols.Add(new CoffSymbolData
            {
                name = name,
                value = value,
                section_number = sectionNumber,
                type = type,
                storage_class = storageClass,
                number_of_aux_symbols = numberOfAuxSymbols
            });

            i += numberOfAuxSymbols;
            buffer.advance(numberOfAuxSymbols * 18);
        }

        return symbols;
    }

    /// <summary>
    ///     解码符号名称�?
    /// </summary>
    private static string decode_symbol_name(byte[] nameBytes, ref ByteBuffer buffer, CoffHeaderData header)
    {
        if (nameBytes[0] == 0 && nameBytes[1] == 0 && nameBytes[2] == 0 && nameBytes[3] == 0)
        {
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(nameBytes.AsSpan(4));

            var stringTableOffset = (int)(header.pointer_to_symbol_table + header.number_of_symbols * 18);
            var nameOffset = stringTableOffset + (int)offset;

            if (nameOffset < buffer.length) return buffer.read_string_at(nameOffset);
        }

        return Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
    }

    /// <summary>
    ///     读取重定位的
    /// </summary>
    private List<CoffRelocationData> read_relocations(ref ByteBuffer buffer, List<CoffSectionHeaderData> sections)
    {
        var relocations = new List<CoffRelocationData>();

        foreach (var section in sections)
        {
            if (section.pointer_to_relocations == 0 || section.number_of_relocations == 0) continue;

            buffer.position = (int)section.pointer_to_relocations;

            for (var i = 0; i < section.number_of_relocations; i++)
            {
                var virtualAddress = buffer.read_u32_le();
                var symbolTableIndex = buffer.read_u32_le();
                var type = buffer.read_u16_le();

                var item = new CoffRelocationData
                {
                    virtual_address = virtualAddress,
                    symbol_table_index = symbolTableIndex,
                    type = type
                };
                relocations.Add(item);
            }
        }

        return relocations;
    }
}