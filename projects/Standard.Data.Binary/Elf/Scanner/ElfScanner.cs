using System.Text;
using Std.Data.Binary.Elf.Data;
using Std.Data.Binary.Elf.Decode;

namespace Std.Data.Binary.Elf.Scanner;

/// <summary>
///     ELF 文件扫描器，提供对 Linux 可执行文件的快速结构扫描
/// </summary>
public class ElfScanner : IDetector
{
    /// <inheritdoc />
    public bool detect(ReadOnlySpan<byte> header)
    {
        return header.Length >= 4
               && header[0] == ElfConstants.magic[0]
               && header[1] == ElfConstants.magic[1]
               && header[2] == ElfConstants.magic[2]
               && header[3] == ElfConstants.magic[3];
    }

    /// <summary>
    ///     扫描 ELF 文件，提取结构信息
    /// </summary>
    /// <param name="data">ELF 二进制数据</param>
    /// <returns>扫描结果</returns>
    public static string scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("ELF File Scan Result:");
        result.AppendLine("===================");

        if (data.Length < 4)
        {
            result.AppendLine("ELF 文件数据过短");
            return result.ToString();
        }

        if (data[0] != ElfConstants.magic[0] || data[1] != ElfConstants.magic[1] || data[2] != ElfConstants.magic[2] ||
            data[3] != ElfConstants.magic[3])
        {
            result.AppendLine("不是有效的 ELF 文件");
            return result.ToString();
        }

        result.AppendLine("有效的 ELF 文件");
        result.AppendLine();

        try
        {
            var decoder = new ElfDecoder();
            var elfFile = decoder.decode(data);

            var fileType = elfFile.is_executable ? "Executable" :
                elfFile.is_shared_library ? "Shared Library (SO)" : "Other";
            result.AppendLine($"文件类型: {fileType}");

            var architecture = elfFile.header.is64_bit ? "x64 (64-bit)" : "x86 (32-bit)";
            result.AppendLine($"架构: {architecture}");

            var endianness = elfFile.header.is_little_endian ? "小端 (Little Endian)" : "大端 (Big Endian)";
            result.AppendLine($"字节序: {endianness}");

            var machineType = get_machine_type_name(elfFile.header.machine);
            result.AppendLine($"机器类型: {machineType}");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {elfFile.header.section_header_count}");
            if (elfFile.section_headers.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in elfFile.section_headers)
                {
                    result.AppendLine($"  📄 {section.name}");
                    result.AppendLine($"     类型: {get_section_type_name(section.type)}, 大小: {section.size} bytes");
                    result.AppendLine($"     虚拟地址: 0x{section.address:X16}, 文件偏移: 0x{section.offset:X16}");
                }
            }

            result.AppendLine();

            result.AppendLine($"📦 段数量: {elfFile.header.program_header_count}");
            if (elfFile.program_headers.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("段列表");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var program in elfFile.program_headers)
                {
                    result.AppendLine($"  📄 {get_segment_type_name(program.type)}");
                    result.AppendLine($"     虚拟地址: 0x{program.virtual_address:X16}, 文件大小: {program.file_size} bytes");
                    result.AppendLine($"     内存大小: {program.memory_size} bytes, 对齐: {program.alignment}");
                }
            }

            result.AppendLine();
            result.AppendLine($"入口点: 0x{elfFile.header.entry_point:X16}");
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错误: {ex.Message}");
        }

        return result.ToString();
    }

    /// <summary>
    ///     获取机器类型名称
    /// </summary>
    private static string get_machine_type_name(ushort machine)
    {
        return machine switch
        {
            0x00 => "未知",
            0x02 => "SPARC",
            0x03 => "x86",
            0x08 => "MIPS",
            0x14 => "PowerPC",
            0x28 => "ARM",
            0x2A => "SuperH",
            0x32 => "IA-64",
            0x3E => "x86-64",
            0xB7 => "AArch64",
            0xF3 => "RISC-V",
            _ => $"未知 (0x{machine:X4})"
        };
    }

    /// <summary>
    ///     获取节区类型名称
    /// </summary>
    private static string get_section_type_name(uint type)
    {
        return type switch
        {
            0x00 => "NULL",
            0x01 => "PROGBITS",
            0x02 => "SYMTAB",
            0x03 => "STRTAB",
            0x04 => "RELA",
            0x05 => "HASH",
            0x06 => "DYNAMIC",
            0x07 => "NOTE",
            0x08 => "NOBITS",
            0x09 => "REL",
            0x0A => "SHLIB",
            0x0B => "DYNSYM",
            0x0E => "INIT_ARRAY",
            0x0F => "FINI_ARRAY",
            0x10 => "PREINIT_ARRAY",
            0x11 => "GROUP",
            0x12 => "SYMTAB_SHNDX",
            0x13 => "NUM",
            _ => $"未知 (0x{type:X8})"
        };
    }

    /// <summary>
    ///     获取段类型名称
    /// </summary>
    private static string get_segment_type_name(uint type)
    {
        return type switch
        {
            0x00 => "NULL",
            0x01 => "LOAD",
            0x02 => "DYNAMIC",
            0x03 => "INTERP",
            0x04 => "NOTE",
            0x05 => "SHLIB",
            0x06 => "PHDR",
            0x07 => "TLS",
            _ => $"未知 (0x{type:X8})"
        };
    }
}