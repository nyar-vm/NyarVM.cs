using System.Text;
using Std.Codec;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Pe.Data;

namespace Std.Data.Binary.Pe.Decode;

/// <summary>
///     PE 文件解码器，解析 Windows 可执行文件（.exe, .dll）格式的
///     支持 DOS 头、PE 头、可选头、节区表、节区内容、导入表和重定位表解码的///�?/summary>
public sealed class PeDecoder
{
    /// <summary>
    ///     的PE 二进制数据解码文件的    ///�?/summary>
    ///     <param name="data">
    ///         PE 二进制数据的/param>
    ///         <returns>解码后的 PE 文件数据�?returns>
    public PeFileData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return decode_file(ref buffer);
    }

    private PeFileData decode_file(ref ByteBuffer buffer)
    {
        var dosHeader = read_dos_header(ref buffer);

        buffer.position = (int)dosHeader.pe_header_offset;

        var peHeader = read_pe_header(ref buffer);
        var optionalHeader = read_optional_header(ref buffer);
        var sections = read_sections(ref buffer, peHeader.number_of_sections);

        var mergedHeader = new PeHeaderData
        {
            dos_magic = dosHeader.dos_magic,
            pe_header_offset = dosHeader.pe_header_offset,
            pe_magic = peHeader.pe_magic,
            machine = peHeader.machine,
            number_of_sections = peHeader.number_of_sections,
            time_date_stamp = peHeader.time_date_stamp,
            pointer_to_symbol_table = peHeader.pointer_to_symbol_table,
            number_of_symbols = peHeader.number_of_symbols,
            size_of_optional_header = peHeader.size_of_optional_header,
            characteristics = peHeader.characteristics
        };

        var result = new PeFileData
        {
            header = mergedHeader,
            optional_header = optionalHeader,
            sections = sections
        };

        read_section_contents(ref buffer, result);
        result.imports = read_import_table(ref buffer, result);
        result.relocations = read_relocations(ref buffer, result);

        return result;
    }

    /// <summary>
    ///     读取 DOS 头的    ///�?/summary>
    private PeHeaderData read_dos_header(ref ByteBuffer buffer)
    {
        var dosMagic = buffer.read_u16_le();

        if (dosMagic != PeConstants.dos_magic)
            throw new InvalidDataException("Invalid PE file: DOS header magic mismatch.");

        buffer.advance(PeConstants.pe_offset_position - 2);

        var peHeaderOffset = buffer.read_u32_le();

        return new PeHeaderData
        {
            dos_magic = dosMagic,
            pe_header_offset = peHeaderOffset
        };
    }

    /// <summary>
    ///     读取 PE 头的    ///�?/summary>
    private PeHeaderData read_pe_header(ref ByteBuffer buffer)
    {
        var peMagic = buffer.read_u32_le();

        if (peMagic != PeConstants.pe_magic)
            throw new InvalidDataException("Invalid PE file: PE header magic mismatch.");

        var machine = buffer.read_u16_le();
        var numberOfSections = buffer.read_u16_le();
        var timeDateStamp = buffer.read_u32_le();
        var pointerToSymbolTable = buffer.read_u32_le();
        var numberOfSymbols = buffer.read_u32_le();
        var sizeOfOptionalHeader = buffer.read_u16_le();
        var characteristics = buffer.read_u16_le();

        return new PeHeaderData
        {
            pe_magic = peMagic,
            machine = machine,
            number_of_sections = numberOfSections,
            time_date_stamp = timeDateStamp,
            pointer_to_symbol_table = pointerToSymbolTable,
            number_of_symbols = numberOfSymbols,
            size_of_optional_header = sizeOfOptionalHeader,
            characteristics = characteristics
        };
    }

    /// <summary>
    ///     读取可选头�?   ///�?/summary>
    private PeOptionalHeaderData read_optional_header(ref ByteBuffer buffer)
    {
        var magic = buffer.read_u16_le();

        var majorLinkerVersion = buffer.read_u8();
        var minorLinkerVersion = buffer.read_u8();
        var sizeOfCode = buffer.read_u32_le();
        var sizeOfInitializedData = buffer.read_u32_le();
        var sizeOfUninitializedData = buffer.read_u32_le();
        var addressOfEntryPoint = buffer.read_u32_le();
        var baseOfCode = buffer.read_u32_le();

        uint baseOfData = 0;
        ulong imageBase;

        if (magic == PeConstants.optional_magic_pe32)
        {
            baseOfData = buffer.read_u32_le();
            imageBase = buffer.read_u32_le();
        }
        else if (magic == PeConstants.optional_magic_pe32_plus)
        {
            imageBase = buffer.read_u64_le();
        }
        else
        {
            throw new InvalidDataException($"不支持的可选头魔术数字: 0x{magic:X4}");
        }

        var sectionAlignment = buffer.read_u32_le();
        var fileAlignment = buffer.read_u32_le();
        var majorOperatingSystemVersion = buffer.read_u16_le();
        var minorOperatingSystemVersion = buffer.read_u16_le();
        var majorImageVersion = buffer.read_u16_le();
        var minorImageVersion = buffer.read_u16_le();
        var majorSubsystemVersion = buffer.read_u16_le();
        var minorSubsystemVersion = buffer.read_u16_le();
        var win32VersionValue = buffer.read_u32_le();
        var sizeOfImage = buffer.read_u32_le();
        var sizeOfHeaders = buffer.read_u32_le();
        var checkSum = buffer.read_u32_le();
        var subsystem = buffer.read_u16_le();
        var dllCharacteristics = buffer.read_u16_le();

        ulong sizeOfStackReserve;
        ulong sizeOfStackCommit;
        ulong sizeOfHeapReserve;
        ulong sizeOfHeapCommit;

        if (magic == PeConstants.optional_magic_pe32)
        {
            sizeOfStackReserve = buffer.read_u32_le();
            sizeOfStackCommit = buffer.read_u32_le();
            sizeOfHeapReserve = buffer.read_u32_le();
            sizeOfHeapCommit = buffer.read_u32_le();
        }
        else
        {
            sizeOfStackReserve = buffer.read_u64_le();
            sizeOfStackCommit = buffer.read_u64_le();
            sizeOfHeapReserve = buffer.read_u64_le();
            sizeOfHeapCommit = buffer.read_u64_le();
        }

        var loaderFlags = buffer.read_u32_le();
        var numberOfRvaAndSizes = buffer.read_u32_le();

        var dataDirectories = read_data_directories(ref buffer, (int)System.Math.Min(numberOfRvaAndSizes, 16));

        return new PeOptionalHeaderData
        {
            magic = magic,
            major_linker_version = majorLinkerVersion,
            minor_linker_version = minorLinkerVersion,
            size_of_code = sizeOfCode,
            size_of_initialized_data = sizeOfInitializedData,
            size_of_uninitialized_data = sizeOfUninitializedData,
            address_of_entry_point = addressOfEntryPoint,
            base_of_code = baseOfCode,
            base_of_data = baseOfData,
            image_base = imageBase,
            section_alignment = sectionAlignment,
            file_alignment = fileAlignment,
            major_operating_system_version = majorOperatingSystemVersion,
            minor_operating_system_version = minorOperatingSystemVersion,
            major_image_version = majorImageVersion,
            minor_image_version = minorImageVersion,
            major_subsystem_version = majorSubsystemVersion,
            minor_subsystem_version = minorSubsystemVersion,
            win32_version_value = win32VersionValue,
            size_of_image = sizeOfImage,
            size_of_headers = sizeOfHeaders,
            check_sum = checkSum,
            subsystem = subsystem,
            dll_characteristics = dllCharacteristics,
            size_of_stack_reserve = sizeOfStackReserve,
            size_of_stack_commit = sizeOfStackCommit,
            size_of_heap_reserve = sizeOfHeapReserve,
            size_of_heap_commit = sizeOfHeapCommit,
            loader_flags = loaderFlags,
            number_of_rva_and_sizes = numberOfRvaAndSizes,
            data_directories = dataDirectories
        };
    }

    /// <summary>
    ///     读取数据目录�?   ///�?/summary>
    private List<PeDirectoryDataEntry> read_data_directories(ref ByteBuffer buffer, int count)
    {
        var directories = new List<PeDirectoryDataEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var rva = buffer.read_u32_le();
            var size = buffer.read_u32_le();

            directories.Add(new PeDirectoryDataEntry { rva = rva, size = size });
        }

        return directories;
    }

    /// <summary>
    ///     读取节区表的    ///�?/summary>
    private List<PeSectionData> read_sections(ref ByteBuffer buffer, int numberOfSections)
    {
        var sections = new List<PeSectionData>();

        for (var i = 0; i < numberOfSections; i++)
        {
            var nameBytes = FixedBytes8.from_span(buffer.read_bytes(8));

            var virtualSize = buffer.read_u32_le();
            var virtualAddress = buffer.read_u32_le();
            var sizeOfRawData = buffer.read_u32_le();
            var pointerToRawData = buffer.read_u32_le();
            var pointerToRelocations = buffer.read_u32_le();
            var pointerToLinenumbers = buffer.read_u32_le();
            var numberOfRelocations = buffer.read_u16_le();
            var numberOfLinenumbers = buffer.read_u16_le();
            var characteristics = buffer.read_u32_le();

            var item = new PeSectionData
            {
                name_bytes = nameBytes,
                virtual_size = virtualSize,
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
    ///     读取所有节区的原始内容�?   ///�?/summary>
    private void read_section_contents(ref ByteBuffer buffer, PeFileData data)
    {
        for (var i = 0; i < data.sections.Count; i++)
        {
            var section = data.sections[i];

            if (section.pointer_to_raw_data == 0 || section.size_of_raw_data == 0) continue;

            var offset = (int)section.pointer_to_raw_data;
            var rawSize = (int)section.size_of_raw_data;
            var contentSize = (int)System.Math.Min(section.virtual_size, section.size_of_raw_data);

            if (contentSize == 0) continue;

            if (offset + contentSize > buffer.length) continue;

            buffer.position = offset;
            var content = buffer.read_bytes(contentSize).ToArray();

            if (content.Length > 0) data.section_contents[i] = content;
        }
    }

    /// <summary>
    ///     读取导入表的    ///�?/summary>
    private List<PeImportDescriptor> read_import_table(ref ByteBuffer buffer, PeFileData data)
    {
        var imports = new List<PeImportDescriptor>();
        var importDir = data.get_data_directory(PeDirectoryDataIndex.import_table);

        if (importDir.is_empty) return imports;

        var is64 = data.is64_bit;
        var thunkSize = is64 ? PeConstants.import_thunk_size64 : PeConstants.import_thunk_size32;
        var ordinalFlag = is64 ? PeConstants.import_ordinal_flag64 : PeConstants.import_ordinal_flag32;
        var descOffset = data.rva_to_offset(importDir.rva);

        buffer.position = descOffset;

        while (true)
        {
            var originalFirstThunk = buffer.read_u32_le();
            var timeDateStamp = buffer.read_u32_le();
            var forwarderChain = buffer.read_u32_le();
            var nameRva = buffer.read_u32_le();
            var firstThunk = buffer.read_u32_le();

            if (originalFirstThunk == 0 && firstThunk == 0) break;

            var nameOffset = data.rva_to_offset(nameRva);
            var savedPos = buffer.position;

            buffer.position = nameOffset;
            var dllName = read_null_terminated_ascii(ref buffer);
            buffer.position = savedPos;

            var iltOffset = data.rva_to_offset(originalFirstThunk);
            buffer.position = iltOffset;

            var thunks = new List<PeImportThunk>();

            while (true)
            {
                ulong thunkValue;

                if (is64)
                    thunkValue = buffer.read_u64_le();
                else
                    thunkValue = buffer.read_u32_le();

                if (thunkValue == 0) break;

                var isOrdinal = (thunkValue & ordinalFlag) != 0;
                ushort ordinal = 0;
                var funcName = "";

                if (isOrdinal)
                {
                    ordinal = (ushort)(thunkValue & 0xFFFF);
                }
                else
                {
                    var hintNameOffset = data.rva_to_offset((uint)thunkValue);
                    savedPos = buffer.position;

                    buffer.position = hintNameOffset;
                    buffer.read_u16_le();
                    funcName = read_null_terminated_ascii(ref buffer);
                    buffer.position = savedPos;
                }

                var item = new PeImportThunk
                {
                    value = thunkValue,
                    is_ordinal = isOrdinal,
                    ordinal = ordinal,
                    name = funcName
                };
                thunks.Add(item);
            }

            var descriptor = new PeImportDescriptor
            {
                original_first_thunk = originalFirstThunk,
                time_date_stamp = timeDateStamp,
                forwarder_chain = forwarderChain,
                name_rva = nameRva,
                first_thunk = firstThunk,
                thunks = thunks,
                name = dllName
            };
            imports.Add(descriptor);

            buffer.position = descOffset + imports.Count * PeConstants.import_descriptor_size;
        }

        return imports;
    }

    /// <summary>
    ///     读取重定位表�?   ///�?/summary>
    private List<PeBaseRelocationBlock> read_relocations(ref ByteBuffer buffer, PeFileData data)
    {
        var relocs = new List<PeBaseRelocationBlock>();
        var relocDir = data.get_data_directory(PeDirectoryDataIndex.base_relocation_table);

        if (relocDir.is_empty) return relocs;

        var offset = data.rva_to_offset(relocDir.rva);
        var endOffset = System.Math.Min(offset + (int)relocDir.size, buffer.length);

        buffer.position = offset;

        while (buffer.position < endOffset)
        {
            var virtualAddress = buffer.read_u32_le();
            var sizeOfBlock = buffer.read_u32_le();

            if (sizeOfBlock == 0) break;

            var entryCount = ((int)sizeOfBlock - PeConstants.relocation_block_header_size) /
                             PeConstants.relocation_entry_size;
            var entries = new List<PeBaseRelocationEntry>();

            for (var i = 0; i < entryCount; i++)
            {
                var encoded = buffer.read_u16_le();
                var type = (byte)(encoded >> 12);
                var relOffset = (ushort)(encoded & 0xFFF);

                entries.Add(new PeBaseRelocationEntry { type = type, offset = relOffset });
            }

            relocs.Add(new PeBaseRelocationBlock
            {
                virtual_address = virtualAddress,
                entries = entries
            });
        }

        return relocs;
    }

    /// <summary>
    ///     从缓冲区当前位置读取 null 终止的ASCII 字符串的    ///�?/summary>
    private static string read_null_terminated_ascii(ref ByteBuffer buffer)
    {
        var bytes = new List<byte>();

        while (true)
        {
            var b = buffer.read_u8();

            if (b == 0) break;

            bytes.Add(b);
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}