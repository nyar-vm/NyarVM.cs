using System.Text;
using Nyar.Types.Targets;
using Std.Data.Binary.Elf.Data;
using Std.Data.Binary.Elf.Encode;

namespace Nyar.Assembler.Format;

/// <summary>
///     ELF 可执行格式构建器，使的Nyar.Binary.Elf 数据模型和编码器的
///     支持 Linux 平台的ELF 可执行文件输出的
/// </summary>
public sealed class ElfFormat : IExecutableFormat
{
    /// <inheritdoc />
    public string name => "ELF";

    /// <inheritdoc />
    public byte[] build_and_encode(NativeBuildContext context)
    {
        var is64 = context.target.is64_bit;
        var machineType = map_elf_machine_type(context.arch);

        var headerSize = is64 ? (ushort)64 : (ushort)52;
        var phSize = is64 ? 56 : 32;
        var shSize = is64 ? 64 : 40;

        var programHeaderCount = 2;
        var textOffset = headerSize + phSize * programHeaderCount;
        var dataOffset = textOffset + context.text_bytes.Length;
        var shstrtabOffset = dataOffset + context.data_bytes.Length;
        var shOffset = (shstrtabOffset + 64 + 7) & ~7;

        var shstrtab = build_elf_string_table();

        var textVa = 0x400000ul;
        var dataVa = 0x600000ul;

        var elfData = new ElfFileData
        {
            header = new ElfHeaderData
            {
                magic = [ElfConstants.magic[0], ElfConstants.magic[1], ElfConstants.magic[2], ElfConstants.magic[3]],
                @class = is64 ? ElfConstants.class64 : ElfConstants.class32,
                data_encoding = ElfConstants.data_encoding_little_endian,
                version = 1,
                osabi = 0,
                abi_version = 0,
                type = ElfConstants.type_executable,
                machine = machineType,
                object_version = 1,
                entry_point = textVa,
                program_header_offset = headerSize,
                section_header_offset = (ulong)shOffset,
                flags = 0,
                elf_header_size = headerSize,
                program_header_size = (ushort)phSize,
                program_header_count = (ushort)programHeaderCount,
                section_header_size = (ushort)shSize,
                section_header_count = 4,
                string_table_index = 3
            },
            program_headers =
            [
                new ElfProgramHeaderData
                {
                    type = 1,
                    flags = 5,
                    offset = (ulong)textOffset,
                    virtual_address = textVa,
                    physical_address = textVa,
                    file_size = (ulong)context.text_bytes.Length,
                    memory_size = (ulong)context.text_bytes.Length,
                    alignment = 0x1000
                },
                new ElfProgramHeaderData
                {
                    type = 1,
                    flags = 6,
                    offset = (ulong)dataOffset,
                    virtual_address = dataVa,
                    physical_address = dataVa,
                    file_size = (ulong)context.data_bytes.Length,
                    memory_size = (ulong)context.data_bytes.Length,
                    alignment = 0x1000
                }
            ],
            section_headers =
            [
                new ElfSectionHeaderData
                {
                    name_index = 0, name = "", type = 0, flags = 0,
                    address = 0, offset = 0, size = 0, link = 0,
                    info = 0, alignment = 0, entry_size = 0, content = []
                },
                new ElfSectionHeaderData
                {
                    name_index = 1, name = ".text", type = 1, flags = 6,
                    address = textVa, offset = (ulong)textOffset,
                    size = (ulong)context.text_bytes.Length, link = 0,
                    info = 0, alignment = 16, entry_size = 0, content = context.text_bytes
                },
                new ElfSectionHeaderData
                {
                    name_index = 7, name = ".data", type = 1, flags = 3,
                    address = dataVa, offset = (ulong)dataOffset,
                    size = (ulong)context.data_bytes.Length, link = 0,
                    info = 0, alignment = 8, entry_size = 0, content = context.data_bytes
                },
                new ElfSectionHeaderData
                {
                    name_index = 13, name = ".shstrtab", type = 3, flags = 0,
                    address = 0, offset = (ulong)shstrtabOffset,
                    size = (ulong)shstrtab.Length, link = 0,
                    info = 0, alignment = 1, entry_size = 0, content = shstrtab
                }
            ]
        };

        var encoder = new ElfEncoder();
        return encoder.encode(elfData);
    }

    /// <summary>
    ///     构建 ELF 字符串表
    /// </summary>
    private static byte[] build_elf_string_table()
    {
        var sb = new List<byte> { 0 };
        sb.AddRange(Encoding.UTF8.GetBytes(".text\0"));
        sb.AddRange(Encoding.UTF8.GetBytes(".data\0"));
        sb.AddRange(Encoding.UTF8.GetBytes(".shstrtab\0"));
        return [.. sb];
    }

    /// <summary>
    ///     将架构映射为 ELF 机器类型
    /// </summary>
    private static ushort map_elf_machine_type(TargetArch arch)
    {
        return arch switch
        {
            TargetArch.x86 => 3,
            TargetArch.x86_64 => 62,
            TargetArch.arm => 40,
            TargetArch.a_arch64 => 183,
            TargetArch.risc_v32 => 243,
            TargetArch.risc_v64 => 243,
            _ => 62
        };
    }
}