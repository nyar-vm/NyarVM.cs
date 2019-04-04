using System.Text;
using Std.Data.Binary.Elf.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Elf.Decode;

/// <summary>
///     ELF 文件解码器，解析 Linux 可执行文件（.elf, .so）格式的
/// </summary>
public sealed class ElfDecoder
{
    /// <summary>
    ///     的ELF 二进制数据解码文件的
    /// </summary>
    /// <param name="data">
    ///     ELF 二进制数据的/param>
    ///     <returns>解码后的 ELF 文件数据的/returns>
    public ElfFileData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return decode_file(ref buffer);
    }

    private ElfFileData decode_file(ref ByteBuffer buffer)
    {
        var header = read_elf_header(ref buffer);
        var sectionHeaders = read_section_headers(ref buffer, header);
        var programHeaders = read_program_headers(ref buffer, header);
        var symbolTable = read_symbol_table(ref buffer, sectionHeaders, header);
        read_section_contents(ref buffer, sectionHeaders);

        return new ElfFileData
        {
            header = header,
            section_headers = sectionHeaders,
            program_headers = programHeaders,
            symbol_table = symbolTable
        };
    }

    /// <summary>
    ///     读取 ELF 头的
    /// </summary>
    private ElfHeaderData read_elf_header(ref ByteBuffer buffer)
    {
        var magic = buffer.read_bytes(4).ToArray();

        if (magic[0] != ElfConstants.magic[0] || magic[1] != ElfConstants.magic[1] ||
            magic[2] != ElfConstants.magic[2] || magic[3] != ElfConstants.magic[3])
            throw new InvalidDataException("Invalid ELF file: magic mismatch.");

        var elfClass = buffer.read_u8();
        var dataEncoding = buffer.read_u8();
        var version = buffer.read_u8();
        var osAbi = buffer.read_u8();
        var abiVersion = buffer.read_u8();

        buffer.advance(7);

        var type = read_u_int16(ref buffer, dataEncoding);
        var machine = read_u_int16(ref buffer, dataEncoding);
        var objectVersion = read_u_int32(ref buffer, dataEncoding);

        ulong entryPoint;
        ulong programHeaderOffset;
        ulong sectionHeaderOffset;

        if (elfClass == 1)
        {
            entryPoint = read_u_int32(ref buffer, dataEncoding);
            programHeaderOffset = read_u_int32(ref buffer, dataEncoding);
            sectionHeaderOffset = read_u_int32(ref buffer, dataEncoding);
        }
        else
        {
            entryPoint = read_u_int64(ref buffer, dataEncoding);
            programHeaderOffset = read_u_int64(ref buffer, dataEncoding);
            sectionHeaderOffset = read_u_int64(ref buffer, dataEncoding);
        }

        var flags = read_u_int32(ref buffer, dataEncoding);
        var elfHeaderSize = read_u_int16(ref buffer, dataEncoding);
        var programHeaderSize = read_u_int16(ref buffer, dataEncoding);
        var programHeaderCount = read_u_int16(ref buffer, dataEncoding);
        var sectionHeaderSize = read_u_int16(ref buffer, dataEncoding);
        var sectionHeaderCount = read_u_int16(ref buffer, dataEncoding);
        var stringTableIndex = read_u_int16(ref buffer, dataEncoding);

        return new ElfHeaderData
        {
            magic = magic,
            @class = elfClass,
            data_encoding = dataEncoding,
            version = version,
            osabi = osAbi,
            abi_version = abiVersion,
            type = type,
            machine = machine,
            object_version = objectVersion,
            entry_point = entryPoint,
            program_header_offset = programHeaderOffset,
            section_header_offset = sectionHeaderOffset,
            flags = flags,
            elf_header_size = elfHeaderSize,
            program_header_size = programHeaderSize,
            program_header_count = programHeaderCount,
            section_header_size = sectionHeaderSize,
            section_header_count = sectionHeaderCount,
            string_table_index = stringTableIndex
        };
    }

    /// <summary>
    ///     读取节区头的
    /// </summary>
    private List<ElfSectionHeaderData> read_section_headers(ref ByteBuffer buffer, ElfHeaderData header)
    {
        var sections = new List<ElfSectionHeaderData>();

        buffer.position = (int)header.section_header_offset;

        for (var i = 0; i < header.section_header_count; i++)
        {
            var nameIndex = read_u_int32(ref buffer, header.data_encoding);
            var type = read_u_int32(ref buffer, header.data_encoding);

            ulong flags;
            ulong address;
            ulong offset;
            ulong size;

            if (header.is64_bit)
            {
                flags = read_u_int64(ref buffer, header.data_encoding);
                address = read_u_int64(ref buffer, header.data_encoding);
                offset = read_u_int64(ref buffer, header.data_encoding);
                size = read_u_int64(ref buffer, header.data_encoding);
            }
            else
            {
                flags = read_u_int32(ref buffer, header.data_encoding);
                address = read_u_int32(ref buffer, header.data_encoding);
                offset = read_u_int32(ref buffer, header.data_encoding);
                size = read_u_int32(ref buffer, header.data_encoding);
            }

            var link = read_u_int32(ref buffer, header.data_encoding);
            var info = read_u_int32(ref buffer, header.data_encoding);

            ulong alignment;
            ulong entrySize;

            if (header.is64_bit)
            {
                alignment = read_u_int64(ref buffer, header.data_encoding);
                entrySize = read_u_int64(ref buffer, header.data_encoding);
            }
            else
            {
                alignment = read_u_int32(ref buffer, header.data_encoding);
                entrySize = read_u_int32(ref buffer, header.data_encoding);
            }

            sections.Add(new ElfSectionHeaderData
            {
                name_index = nameIndex,
                type = type,
                flags = flags,
                address = address,
                offset = offset,
                size = size,
                link = link,
                info = info,
                alignment = alignment,
                entry_size = entrySize
            });
        }

        return sections;
    }

    /// <summary>
    ///     读取程序头的
    /// </summary>
    private List<ElfProgramHeaderData> read_program_headers(ref ByteBuffer buffer, ElfHeaderData header)
    {
        var programs = new List<ElfProgramHeaderData>();

        buffer.position = (int)header.program_header_offset;

        for (var i = 0; i < header.program_header_count; i++)
        {
            var type = read_u_int32(ref buffer, header.data_encoding);

            uint flags;
            ulong offset;
            ulong virtualAddress;
            ulong physicalAddress;
            ulong fileSize;
            ulong memorySize;
            ulong alignment;

            if (header.is64_bit)
            {
                flags = read_u_int32(ref buffer, header.data_encoding);
                offset = read_u_int64(ref buffer, header.data_encoding);
                virtualAddress = read_u_int64(ref buffer, header.data_encoding);
                physicalAddress = read_u_int64(ref buffer, header.data_encoding);
                fileSize = read_u_int64(ref buffer, header.data_encoding);
                memorySize = read_u_int64(ref buffer, header.data_encoding);
                alignment = read_u_int64(ref buffer, header.data_encoding);
            }
            else
            {
                offset = read_u_int32(ref buffer, header.data_encoding);
                virtualAddress = read_u_int32(ref buffer, header.data_encoding);
                physicalAddress = read_u_int32(ref buffer, header.data_encoding);
                fileSize = read_u_int32(ref buffer, header.data_encoding);
                memorySize = read_u_int32(ref buffer, header.data_encoding);
                flags = read_u_int32(ref buffer, header.data_encoding);
                alignment = read_u_int32(ref buffer, header.data_encoding);
            }

            programs.Add(new ElfProgramHeaderData
            {
                type = type,
                flags = flags,
                offset = offset,
                virtual_address = virtualAddress,
                physical_address = physicalAddress,
                file_size = fileSize,
                memory_size = memorySize,
                alignment = alignment
            });
        }

        return programs;
    }

    /// <summary>
    ///     读取 UInt16，根据编码选择字节序的
    /// </summary>
    private static ushort read_u_int16(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == ElfConstants.data_encoding_little_endian ? buffer.read_u16_le() : buffer.read_u16_be();
    }

    /// <summary>
    ///     读取 UInt32，根据编码选择字节序的
    /// </summary>
    private static uint read_u_int32(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == ElfConstants.data_encoding_little_endian ? buffer.read_u32_le() : buffer.read_u32_be();
    }

    /// <summary>
    ///     读取 UInt64，根据编码选择字节序的
    /// </summary>
    private static ulong read_u_int64(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == ElfConstants.data_encoding_little_endian ? buffer.read_u64_le() : buffer.read_u64_be();
    }

    /// <summary>
    ///     读取符号表的
    /// </summary>
    /// <param name="buffer">
    ///     数据缓冲区的/param>
    ///     <param name="sectionHeaders">
    ///         节区头列表的/param>
    ///         <param name="header">
    ///             ELF 头数据的/param>
    ///             <returns>符号表数据，若无符号表则返回 null的/returns>
    private ElfSymbolTableData? read_symbol_table(ref ByteBuffer buffer,
        IReadOnlyList<ElfSectionHeaderData> sectionHeaders, ElfHeaderData header)
    {
        ElfSectionHeaderData? symtabSection = null;

        for (var i = 0; i < sectionHeaders.Count; i++)
            if (sectionHeaders[i].type == 2)
            {
                symtabSection = sectionHeaders[i];
                break;
            }

        if (symtabSection == null) return null;

        var strtabIndex = (int)symtabSection.link;

        if (strtabIndex < 0 || strtabIndex >= sectionHeaders.Count) return null;

        var strtabSection = sectionHeaders[strtabIndex];

        buffer.position = (int)strtabSection.offset;
        var strTabBytes = buffer.read_bytes((int)strtabSection.size).ToArray();

        buffer.position = (int)symtabSection.offset;
        var entrySize = header.is64_bit ? ElfConstants.symbol_entry_size64 : ElfConstants.symbol_entry_size32;
        var symbolCount = (int)(symtabSection.size / (ulong)entrySize);
        var symbols = new List<ElfSymbolData>(symbolCount);

        for (var i = 0; i < symbolCount; i++)
        {
            uint nameIndex;
            byte info;
            byte other;
            ushort sectionIndex;
            ulong value;
            ulong size;

            if (header.is64_bit)
            {
                nameIndex = read_u_int32(ref buffer, header.data_encoding);
                info = buffer.read_u8();
                other = buffer.read_u8();
                sectionIndex = read_u_int16(ref buffer, header.data_encoding);
                value = read_u_int64(ref buffer, header.data_encoding);
                size = read_u_int64(ref buffer, header.data_encoding);
            }
            else
            {
                nameIndex = read_u_int32(ref buffer, header.data_encoding);
                value = read_u_int32(ref buffer, header.data_encoding);
                size = read_u_int32(ref buffer, header.data_encoding);
                info = buffer.read_u8();
                other = buffer.read_u8();
                sectionIndex = read_u_int16(ref buffer, header.data_encoding);
            }

            var name = read_string_from_table(strTabBytes, nameIndex);

            var item = new ElfSymbolData
            {
                name_index = nameIndex,
                info = info,
                other = other,
                section_index = sectionIndex,
                value = value,
                size = size,
                name = name
            };
            symbols.Add(item);
        }

        return new ElfSymbolTableData
        {
            symbols = symbols
        };
    }

    /// <summary>
    ///     从字符串表数据中读取的null 结尾的字符串的
    /// </summary>
    /// <param name="strTabData">
    ///     字符串表原始数据的/param>
    ///     <param name="nameIndex">
    ///         字符串起始索引的/param>
    ///         <returns>解码后的字符串的/returns>
    private static string read_string_from_table(ReadOnlySpan<byte> strTabData, uint nameIndex)
    {
        var index = (int)nameIndex;

        if (index < 0 || index >= strTabData.Length) return string.Empty;

        var end = index;

        while (end < strTabData.Length && strTabData[end] != 0) end++;

        if (end == index) return string.Empty;

        return Encoding.UTF8.GetString(strTabData.Slice(index, end - index));
    }

    /// <summary>
    ///     读取所有节区的原始内容的
    /// </summary>
    /// <param name="buffer">
    ///     数据缓冲区的/param>
    ///     <param name="sectionHeaders">节区头列表的/param>
    private void read_section_contents(ref ByteBuffer buffer, List<ElfSectionHeaderData> sectionHeaders)
    {
        for (var i = 0; i < sectionHeaders.Count; i++)
        {
            var section = sectionHeaders[i];

            if (section.offset == 0 || section.size == 0) continue;

            buffer.position = (int)section.offset;
            section.content = [.. buffer.read_bytes((int)section.size)];
        }
    }
}