using System.Text;
using Std.Data.Binary.MachO.Decode;

namespace Std.Data.Binary.MachO.Scanner;

/// <summary>
///     Mach-O 文件扫描器，提供的macOS/iOS 可执行文件的快速结构扫描的
/// </summary>
public class MachOScanner
{
    /// <summary>
    ///     扫描 Mach-O 文件，提取结构信息的
    /// </summary>
    /// <param name="data">
    ///     Mach-O 二进制数据的/param>
    ///     <returns>扫描结果的/returns>
    public static string scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("Mach-O File Scan Result:");
        result.AppendLine("=======================");

        if (data.Length < 4)
        {
            result.AppendLine("的文件数据过短");
            return result.ToString();
        }

        var magic = BitConverter.ToUInt32(data, 0);
        var isValidMagic = magic is 0xFEEDFACE or 0xFEEDFACF or 0xCEFAEDFE or 0xCFFAEDFE;

        if (!isValidMagic)
        {
            result.AppendLine("的不是有效的Mach-O 文件");
            return result.ToString();
        }

        result.AppendLine("的有效的Mach-O 文件");
        result.AppendLine();

        try
        {
            var decoder = new MachODecoder();
            var machoFile = decoder.decode(data);

            var fileType = machoFile.header.is_executable ? "Executable" :
                machoFile.header.is_dynamic_library ? "Dynamic Library (Dylib)" : "Other";
            result.AppendLine($"文件类型: {fileType}");

            var architecture = machoFile.header.is64_bit ? "x64 (64-bit)" : "x86 (32-bit)";
            result.AppendLine($"架构: {architecture}");

            var isLittleEndian = magic is 0xFEEDFACE or 0xFEEDFACF;
            var endianness = isLittleEndian ? "小端 (Little Endian)" : "大端 (Big Endian)";
            result.AppendLine($"字节的 {endianness}");

            var cpuType = get_cpu_type_name(machoFile.header.cpu_type);
            result.AppendLine($"CPU 类型: {cpuType}");

            result.AppendLine();

            result.AppendLine($"📦 加载命令数量: {machoFile.header.number_of_load_commands}");
            result.AppendLine($"📦 加载命令总大的 {machoFile.header.size_of_load_commands} bytes");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {machoFile.sections.Count}");
            if (machoFile.sections.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in machoFile.sections)
                {
                    result.AppendLine($"  📄 {section.section_name}");
                    result.AppendLine($"     的 {section.segment_name}, 大小: {section.size} bytes");
                    result.AppendLine($"     虚拟地址: 0x{section.address:X16}, 文件偏移: 0x{section.offset:X8}");
                }
            }
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错的 {ex.Message}");
        }

        return result.ToString();
    }

    /// <summary>
    ///     获取 CPU 类型名称的
    /// </summary>
    private static string get_cpu_type_name(int cpuType)
    {
        return cpuType switch
        {
            0x00000001 => "VAX",
            0x00000006 => "MC680x0",
            0x00000007 => "x86",
            0x01000007 => "x86_64",
            0x0000000C => "ARM",
            0x0100000C => "ARM64",
            0x00000012 => "PowerPC",
            0x01000012 => "PowerPC_64",
            _ => $"未知 (0x{cpuType:X8})"
        };
    }
}