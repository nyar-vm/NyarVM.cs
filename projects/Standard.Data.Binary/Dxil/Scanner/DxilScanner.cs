using System.Text;
using Std.Data.Binary.Dxil.Data;
using Std.Data.Binary.Dxil.Decode;

namespace Std.Data.Binary.Dxil.Scanner;

/// <summary>
///     DXIL/DXContainer 扫描器，提供对着色器二进制数据的快速结构扫描的
/// </summary>
public class DxilScanner
{
    /// <summary>
    ///     扫描 DXContainer 二进制数据，提取结构信息的
    /// </summary>
    /// <param name="data">
    ///     DXContainer 二进制数据的/param>
    ///     <returns>人类可读的扫描报告的/returns>
    public static string scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("DXContainer 扫描结果");
        result.AppendLine("=====================");

        if (data.Length < 20)
        {
            result.AppendLine("的数据过短，不是有效的 DXContainer 文件");
            return result.ToString();
        }

        var magic = BitConverter.ToUInt32(data, 0);

        if (magic != DxilConstants.container_magic_number)
        {
            result.AppendLine($"文件魔数不匹配：期望 0x{DxilConstants.container_magic_number:X8}，实际 0x{magic:X8}");
            return result.ToString();
        }

        result.AppendLine("有效的 DXContainer 文件");
        result.AppendLine();

        try
        {
            var containerDecoder = new DxContainerDecoder();
            var container = containerDecoder.decode(data);

            result.AppendLine($"📋 容器版本：{container.header.version_major}.{container.header.version_minor}");
            result.AppendLine($"📋 文件大小：{container.header.file_size} 字节");
            result.AppendLine($"📋 Part 数量：{container.header.part_count}");
            result.AppendLine();

            if (container.parts.Count > 0)
            {
                result.AppendLine("Part 列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var part in container.parts)
                {
                    var fourCcString = part.header.four_cc_string;
                    result.AppendLine($"📦 {fourCcString}（大小：{part.header.size} 字节）");

                    if (part.header.four_cc is (uint)DxilPartFourCc.dxil or (uint)DxilPartFourCc.dxil1)
                        scan_dxil_part(result, part.data);
                }
            }
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错误：{ex.Message}");
        }

        return result.ToString();
    }

    private static void scan_dxil_part(StringBuilder result, byte[] partData)
    {
        if (partData.Length < DxilConstants.program_header_size)
        {
            result.AppendLine("   ⚠️ DXIL Part 数据过短，无法解析程序头");
            return;
        }

        try
        {
            var programDecoder = new DxilProgramDecoder();
            var program = programDecoder.decode(partData);

            result.AppendLine($"   着色器模型：{program.header.shader_model_kind}");
            result.AppendLine($"   DXIL 版本：{program.header.major_version}.{program.header.minor_version}");
            result.AppendLine($"   Bitcode 大小：{program.header.bitcode_size} 字节");
            result.AppendLine($"   Bitcode 偏移：{program.header.bitcode_offset}");
        }
        catch (Exception ex)
        {
            result.AppendLine($"   ⚠️ 解析 DXIL 程序头失败：{ex.Message}");
        }
    }
}