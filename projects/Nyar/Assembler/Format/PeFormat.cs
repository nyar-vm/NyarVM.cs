using System.Text;
using Nyar.Types.Targets;
using Std.Codec;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Pe.Data;
using Std.Data.Binary.Pe.Encode;

namespace Nyar.Assembler.Format;

/// <summary>
///     PE 可执行格式构建器，使的Nyar.Binary.Pe 数据模型和编码器�?/// 支持 Windows 平台的PE 可执行文件输出，包含完整的导入表和入口点存根�?///
/// </summary>
public sealed class PeFormat : IExecutableFormat
{
    private static readonly byte[] _default_dos_stub =
    [
        0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
        0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68,
        0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72,
        0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F,
        0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E,
        0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20,
        0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A,
        0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    /// <inheritdoc />
    public string name => "PE";

    /// <inheritdoc />
    public byte[] build_and_encode(NativeBuildContext context)
    {
        var is64 = context.target.is64_bit;
        var machineType = map_pe_machine_type(context.arch);
        var fileAlignment = 0x200;
        var sectionAlignment = 0x1000;
        var hasExceptionData = context.xdata_bytes.Length > 0 && context.pdata_bytes.Length > 0;

        var textVa = (uint)sectionAlignment;
        var dataVa = (uint)(sectionAlignment * 2);
        var idataVa = (uint)(sectionAlignment * 3);
        var xdataVa = hasExceptionData ? (uint)(sectionAlignment * 4) : 0u;
        var pdataVa = hasExceptionData ? (uint)(sectionAlignment * 5) : 0u;
        var imageSize = (uint)(sectionAlignment * (hasExceptionData ? 6 : 4));

        var importTableBytes = context.import_table_bytes.Length > 0
            ? context.import_table_bytes
            : build_pe_import_table_static(idataVa).Data;

        var dosHeaderSize = 0x80;
        var peSignatureSize = 4;
        var coffHeaderSize = 20;
        var optionalHeaderSize = is64 ? 240 : 224;
        var sectionHeaderSize = 40;
        var sectionCount = hasExceptionData ? 5 : 3;
        var headersSize = dosHeaderSize + peSignatureSize + coffHeaderSize + optionalHeaderSize +
                          sectionHeaderSize * sectionCount;
        headersSize = (headersSize + fileAlignment - 1) & ~(fileAlignment - 1);

        var textRawOffset = headersSize;
        var textRawSize = (context.text_bytes.Length + fileAlignment - 1) & ~(fileAlignment - 1);
        var dataRawOffset = textRawOffset + textRawSize;
        var dataRawSize = (context.data_bytes.Length + fileAlignment - 1) & ~(fileAlignment - 1);
        var idataRawOffset = dataRawOffset + dataRawSize;
        var idataRawSize = (importTableBytes.Length + fileAlignment - 1) & ~(fileAlignment - 1);
        var xdataRawOffset = idataRawOffset + idataRawSize;
        var xdataRawSize = hasExceptionData
            ? (context.xdata_bytes.Length + fileAlignment - 1) & ~(fileAlignment - 1)
            : 0;
        var pdataRawOffset = xdataRawOffset + xdataRawSize;
        var pdataRawSize = hasExceptionData
            ? (context.pdata_bytes.Length + fileAlignment - 1) & ~(fileAlignment - 1)
            : 0;

        var textName = FixedBytes8.from_span(Encoding.UTF8.GetBytes(".text\0\0\0"));
        var dataName = FixedBytes8.from_span(Encoding.UTF8.GetBytes(".data\0\0\0"));
        var idataName = FixedBytes8.from_span(Encoding.UTF8.GetBytes(".idata\0\0"));

        var dataDirectories = new List<PeDirectoryDataEntry>();
        for (var i = 0; i < 16; i++) dataDirectories.Add(new PeDirectoryDataEntry());

        dataDirectories[1] = new PeDirectoryDataEntry
        {
            rva = idataVa,
            size = 40u
        };

        dataDirectories[12] = new PeDirectoryDataEntry
        {
            rva = idataVa + 0x48u,
            size = 32u
        };

        if (hasExceptionData)
            dataDirectories[(int)PeDirectoryDataIndex.exception_table] = new PeDirectoryDataEntry
            {
                rva = pdataVa,
                size = (uint)context.pdata_bytes.Length
            };

        var sections = new List<PeSectionData>
        {
            new()
            {
                name_bytes = textName,
                virtual_size = (uint)context.text_bytes.Length,
                virtual_address = textVa,
                size_of_raw_data = (uint)textRawSize,
                pointer_to_raw_data = (uint)textRawOffset,
                pointer_to_relocations = 0u,
                pointer_to_linenumbers = 0u,
                number_of_relocations = 0,
                number_of_linenumbers = 0,
                characteristics = 0x60000020u
            },
            new()
            {
                name_bytes = dataName,
                virtual_size = (uint)context.data_bytes.Length,
                virtual_address = dataVa,
                size_of_raw_data = (uint)dataRawSize,
                pointer_to_raw_data = (uint)dataRawOffset,
                pointer_to_relocations = 0u,
                pointer_to_linenumbers = 0u,
                number_of_relocations = 0,
                number_of_linenumbers = 0,
                characteristics = 0xC0000040u
            },
            new()
            {
                name_bytes = idataName,
                virtual_size = (uint)importTableBytes.Length,
                virtual_address = idataVa,
                size_of_raw_data = (uint)idataRawSize,
                pointer_to_raw_data = (uint)idataRawOffset,
                pointer_to_relocations = 0u,
                pointer_to_linenumbers = 0u,
                number_of_relocations = 0,
                number_of_linenumbers = 0,
                characteristics = 0xC0000040u
            }
        };

        if (hasExceptionData)
        {
            var item = new PeSectionData
            {
                name_bytes = FixedBytes8.from_span(Encoding.UTF8.GetBytes(".xdata\0")),
                virtual_size = (uint)context.xdata_bytes.Length,
                virtual_address = xdataVa,
                size_of_raw_data = (uint)xdataRawSize,
                pointer_to_raw_data = (uint)xdataRawOffset,
                pointer_to_relocations = 0u,
                pointer_to_linenumbers = 0u,
                number_of_relocations = 0,
                number_of_linenumbers = 0,
                characteristics = 0x40000040u
            };
            sections.Add(item);
            var data = new PeSectionData
            {
                name_bytes = FixedBytes8.from_span(Encoding.UTF8.GetBytes(".pdata\0")),
                virtual_size = (uint)context.pdata_bytes.Length,
                virtual_address = pdataVa,
                size_of_raw_data = (uint)pdataRawSize,
                pointer_to_raw_data = (uint)pdataRawOffset,
                pointer_to_relocations = 0u,
                pointer_to_linenumbers = 0u,
                number_of_relocations = 0,
                number_of_linenumbers = 0,
                characteristics = 0x40000040u
            };
            sections.Add(data);
        }

        var peData = new PeFileData
        {
            header = new PeHeaderData
            {
                dos_magic = PeConstants.dos_magic,
                pe_header_offset = (uint)dosHeaderSize,
                pe_magic = PeConstants.pe_magic,
                machine = machineType,
                number_of_sections = (ushort)sectionCount,
                time_date_stamp = 0u,
                pointer_to_symbol_table = 0u,
                number_of_symbols = 0u,
                size_of_optional_header = (ushort)optionalHeaderSize,
                characteristics = (ushort)(
                    FileCharacteristics.executable_image |
                    FileCharacteristics.large_address_aware)
            },
            optional_header = new PeOptionalHeaderData
            {
                magic = is64 ? PeConstants.optional_magic_pe32_plus : PeConstants.optional_magic_pe32,
                major_linker_version = 0,
                minor_linker_version = 0,
                size_of_code = (uint)textRawSize,
                size_of_initialized_data = (uint)(dataRawSize + idataRawSize + xdataRawSize + pdataRawSize),
                size_of_uninitialized_data = 0u,
                address_of_entry_point = context.entry_rva,
                base_of_code = textVa,
                base_of_data = is64 ? 0u : dataVa,
                image_base = is64 ? 0x140000000ul : 0x400000u,
                section_alignment = (uint)sectionAlignment,
                file_alignment = (uint)fileAlignment,
                major_operating_system_version = 6,
                minor_operating_system_version = 0,
                major_image_version = 0,
                minor_image_version = 0,
                major_subsystem_version = 6,
                minor_subsystem_version = 0,
                win32_version_value = 0,
                size_of_image = imageSize,
                size_of_headers = (uint)headersSize,
                check_sum = 0u,
                subsystem = 3,
                dll_characteristics = 0x8160,
                size_of_stack_reserve = 0x100000ul,
                size_of_stack_commit = 0x1000ul,
                size_of_heap_reserve = 0x100000ul,
                size_of_heap_commit = 0x1000ul,
                loader_flags = 0u,
                number_of_rva_and_sizes = 16,
                data_directories = dataDirectories
            },
            sections = sections,
            section_contents = new Dictionary<int, byte[]>
            {
                [0] = context.text_bytes,
                [1] = context.data_bytes,
                [2] = importTableBytes,
                [3] = context.xdata_bytes,
                [4] = context.pdata_bytes
            }
        };

        var encoder = new PeEncoder();
        return encoder.encode(peData);
    }

    /// <summary>
    ///     构建 PE 导入表，返回导入数据字节的IAT 条目 VA 映射�?
    ///     导入 kernel32.dll 的GetStdHandle、WriteFile、ExitProcess�?
    ///     静态方法供外部调用者（的NativeBackend）使用的
    /// </summary>
    public static (byte[] Data, Dictionary<string, uint> IatVAs) build_pe_import_table_static(uint idataVa)
    {
        var writer = new ByteBufferWriter(256);
        var funcNames = new[] { "GetStdHandle", "WriteFile", "ExitProcess" };

        var iltIatCount = funcNames.Length + 1;
        var iltIatSize = (uint)(iltIatCount * 8);

        var descriptorSize = 0x28u;
        var iltOffset = descriptorSize;
        var iatOffset = iltOffset + iltIatSize;
        var hintNameBase = iatOffset + iltIatSize;

        var hintNameOffsets = new uint[funcNames.Length];
        var currentHintOffset = 0u;
        for (var i = 0; i < funcNames.Length; i++)
        {
            hintNameOffsets[i] = hintNameBase + currentHintOffset;
            var entrySize = 2u + (uint)Encoding.UTF8.GetByteCount(funcNames[i]) + 1u;
            entrySize = (entrySize + 1u) & ~1u;
            currentHintOffset += entrySize;
        }

        var dllNameOffset = hintNameBase + currentHintOffset;

        writer.write_i32_le((int)(idataVa + iltOffset));
        writer.write_i32_le(0);
        writer.write_i32_le(0);
        writer.write_i32_le((int)(idataVa + dllNameOffset));
        writer.write_i32_le((int)(idataVa + iatOffset));

        for (var i = 0; i < 5; i++) writer.write_i32_le(0);

        for (var i = 0; i < funcNames.Length; i++) writer.write_i64_le(idataVa + hintNameOffsets[i]);

        writer.write_i64_le(0);

        for (var i = 0; i < funcNames.Length; i++) writer.write_i64_le(idataVa + hintNameOffsets[i]);

        writer.write_i64_le(0);

        foreach (var name in funcNames)
        {
            writer.write_i16_le(0);
            writer.write(Encoding.UTF8.GetBytes(name));
            writer.write_u8(0);
            if ((writer.position & 1) != 0) writer.write_u8(0);
        }

        writer.write(Encoding.UTF8.GetBytes("kernel32.dll"));
        writer.write_u8(0);

        var iatVAs = new Dictionary<string, uint>();
        var iatEntryRva = iatOffset;
        for (var i = 0; i < funcNames.Length; i++)
        {
            iatVAs[funcNames[i]] = idataVa + iatEntryRva;
            iatEntryRva += 8;
        }

        return (writer.to_array(), iatVAs);
    }

    /// <summary>
    ///     将架构映射为 PE 机器类型
    /// </summary>
    private static ushort map_pe_machine_type(TargetArch arch)
    {
        return arch switch
        {
            TargetArch.x86 => 0x014C,
            TargetArch.x86_64 => 0x8664,
            TargetArch.arm => 0x01C0,
            TargetArch.a_arch64 => 0xAA64,
            _ => 0x8664
        };
    }
}