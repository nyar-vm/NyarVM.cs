using System.Text;
using Std.Data.Binary.Coff.Decode;

namespace Std.Data.Binary.Coff.Scanner;

/// <summary>
///     COFF 文件扫描器，提供的Windows 目标文件的快速结构扫描的
/// </summary>
public class CoffScanner
{
    /// <summary>
    ///     扫描 COFF 文件，提取结构信息的
    /// </summary>
    /// <param name="data">
    ///     COFF 二进制数据的/param>
    ///     <returns>扫描结果的/returns>
    public static string scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("COFF File Scan Result:");
        result.AppendLine("=====================");

        if (data.Length < 20)
        {
            result.AppendLine("的文件数据过短");
            return result.ToString();
        }

        result.AppendLine("的有效的COFF 文件");
        result.AppendLine();

        try
        {
            var decoder = new CoffDecoder();
            var coffFile = decoder.decode(data);

            var machineType = get_machine_type_name(coffFile.header.machine);
            result.AppendLine($"机器类型: {machineType}");

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(coffFile.header.time_date_stamp);
            result.AppendLine($"编译时间: {timestamp:yyyy-MM-dd HH:mm:ss}");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {coffFile.header.number_of_sections}");
            if (coffFile.sections.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in coffFile.sections)
                {
                    result.AppendLine($"  📄 {section.name}");
                    result.AppendLine(
                        $"     虚拟地址: 0x{section.virtual_address:X8}, 原始大小: {section.size_of_raw_data} bytes");
                    result.AppendLine($"     重定的 {section.number_of_relocations}, 行号: {section.number_of_linenumbers}");
                }
            }

            result.AppendLine();

            result.AppendLine($"🔣 符号数量: {coffFile.symbols.Count}");
            if (coffFile.symbols.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("符号列表（前 10 个）:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var symbol in coffFile.symbols.Take(10))
                {
                    var section = symbol.section_number switch
                    {
                        0 => "UNDEF",
                        -1 => "ABS",
                        -2 => "DEBUG",
                        _ => $"SEC{symbol.section_number}"
                    };

                    result.AppendLine($"  {symbol.name} ({section}) = 0x{symbol.value:X8}");
                }

                if (coffFile.symbols.Count > 10) result.AppendLine($"  ... 还有 {coffFile.symbols.Count - 10} 个符号");
            }

            result.AppendLine();

            result.AppendLine($"🔄 重定位数的 {coffFile.relocations.Count}");
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错的 {ex.Message}");
        }

        return result.ToString();
    }

    /// <summary>
    ///     获取机器类型名称的
    /// </summary>
    private static string get_machine_type_name(ushort machine)
    {
        return machine switch
        {
            0x0000 => "未知",
            0x014C => "x86 (Intel 386)",
            0x0200 => "IA64 (Intel Itanium)",
            0x8664 => "x64 (AMD64)",
            0x01C0 => "ARM",
            0xAA64 => "ARM64",
            0xEBC => "EFI Byte Code",
            _ => $"未知 (0x{machine:X4})"
        };
    }
}