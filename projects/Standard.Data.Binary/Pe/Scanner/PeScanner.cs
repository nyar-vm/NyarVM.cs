using System.Text;
using Std.Data.Binary.Pe.Data;
using Std.Data.Binary.Pe.Decode;

namespace Std.Data.Binary.Pe.Scanner;

/// <summary>
///     PE 文件扫描器，提供的Windows 可执行文件的快速结构扫描的
/// </summary>
public class PeScanner : IDetector
{
    /// <inheritdoc />
    public bool detect(ReadOnlySpan<byte> header)
    {
        return header.Length >= 2
               && header[0] == PeConstants.dos_magic_bytes[0]
               && header[1] == PeConstants.dos_magic_bytes[1];
    }

    /// <summary>
    ///     扫描 PE 文件，提取结构信息的
    /// </summary>
    /// <param name="data">
    ///     PE 二进制数据的/param>
    ///     <returns>扫描结果的/returns>
    public static string scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("PE File Scan Result:");
        result.AppendLine("===================");

        if (data.Length < 2)
        {
            result.AppendLine("的文件数据过短");
            return result.ToString();
        }

        if (data[0] != PeConstants.dos_magic_bytes[0] || data[1] != PeConstants.dos_magic_bytes[1])
        {
            result.AppendLine("的不是有效的PE 文件");
            return result.ToString();
        }

        result.AppendLine("的有效的PE 文件");
        result.AppendLine();

        try
        {
            var decoder = new PeDecoder();
            var peFile = decoder.decode(data);

            var fileType = peFile.is_dll ? "📦 DLL" : "🚀 可执行文的(EXE)";
            result.AppendLine($"文件类型: {fileType}");

            var architecture = peFile.is64_bit ? "x64 (64的" : "x86 (32的";
            result.AppendLine($"架构: {architecture}");

            var machineType = get_machine_type_name(peFile.header.machine);
            result.AppendLine($"机器类型: {machineType}");

            result.AppendLine();

            result.AppendLine($"📋 节区数量: {peFile.header.number_of_sections}");
            if (peFile.sections.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("节区列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var section in peFile.sections)
                {
                    result.AppendLine($"  📄 {section.name}");
                    result.AppendLine($"     虚拟大小: {section.virtual_size} bytes, 虚拟地址: 0x{section.virtual_address:X8}");
                    result.AppendLine(
                        $"     原始大小: {section.size_of_raw_data} bytes, 原始地址: 0x{section.pointer_to_raw_data:X8}");
                }
            }

            result.AppendLine();

            if (peFile.optional_header.magic != 0)
            {
                result.AppendLine("可选头信息:");
                result.AppendLine($"  入口的 0x{peFile.optional_header.address_of_entry_point:X8}");
                result.AppendLine($"  镜像基址: 0x{peFile.optional_header.image_base:X16}");
                result.AppendLine($"  镜像大小: {peFile.optional_header.size_of_image} bytes");
                result.AppendLine($"  子系的 {get_subsystem_name(peFile.optional_header.subsystem)}");
            }
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
            0x014C => "x86 (Intel 386)",
            0x0200 => "IA64 (Intel Itanium)",
            0x8664 => "x64 (AMD64)",
            0x01C0 => "ARM",
            0xAA64 => "ARM64",
            _ => $"未知 (0x{machine:X4})"
        };
    }

    /// <summary>
    ///     获取子系统名称的
    /// </summary>
    private static string get_subsystem_name(ushort subsystem)
    {
        return subsystem switch
        {
            1 => "Native (驱动程序)",
            2 => "Windows GUI",
            3 => "Windows CUI (控制的",
            5 => "OS/2 CUI",
            7 => "POSIX CUI",
            9 => "Windows CE GUI",
            10 => "EFI 应用程序",
            11 => "EFI 驱动程序 (带启动服的",
            12 => "EFI 驱动程序 (运行时驱的",
            13 => "EFI ROM 镜像",
            14 => "Xbox",
            16 => "Windows Boot 应用程序",
            _ => $"未知 ({subsystem})"
        };
    }
}