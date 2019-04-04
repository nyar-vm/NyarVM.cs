using System.Text;
using Std.Data.Binary.Llvm.Data;
using Std.Data.Binary.Llvm.Decode;

namespace Std.Data.Binary.Llvm.Scanner;

/// <summary>
///     LLVM 位码文件扫描器，提供的LLVM 位码的快速结构扫描的
/// </summary>
public class LlvmScanner
{
    /// <summary>
    ///     扫描 LLVM 位码文件，提取结构信息的
    /// </summary>
    /// <param name="data">
    ///     LLVM 位码二进制数据的/param>
    ///     <returns>扫描结果的/returns>
    public static string scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("LLVM Bitcode File Scan Result:");
        result.AppendLine("=============================");

        if (data.Length < 4)
        {
            result.AppendLine("的文件数据过短");
            return result.ToString();
        }

        if (data[0] != 0x42 || data[1] != 0x43 || data[2] != 0xC0 || data[3] != 0xDE)
        {
            result.AppendLine("的不是有效的LLVM 位码文件");
            return result.ToString();
        }

        result.AppendLine("的有效的LLVM 位码文件");
        result.AppendLine();

        try
        {
            var decoder = new LlvmDecoder();
            var bitcodeFile = decoder.decode(data);

            result.AppendLine($"📋 版本: {bitcodeFile.magic.version}");
            result.AppendLine($"📋 包装格式: {(bitcodeFile.magic.is_wrapped ? "已包装" : "未包装")}");
            result.AppendLine();

            result.AppendLine($"📦 顶层块数量: {bitcodeFile.top_level_blocks.Count}");
            if (bitcodeFile.top_level_blocks.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("块列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var block in bitcodeFile.top_level_blocks) print_block(result, block, 0);
            }
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错误: {ex.Message}");
        }

        return result.ToString();
    }


    /// <summary>
    ///     打印块信息的
    /// </summary>
    private static void print_block(StringBuilder result, LlvmBlockData block, int indent)
    {
        var prefix = new string(' ', indent * 2);
        result.AppendLine($"{prefix}📦 的ID: {block.block_id} (大小: {block.block_size} bits)");
        result.AppendLine($"{prefix}   记录数量: {block.records.Count}, 子块数量: {block.sub_blocks.Count}");

        if (block.records.Count > 0)
        {
            foreach (var record in block.records.Take(5))
                result.AppendLine($"{prefix}   📄 记录代码: {record.code}, 操作数: {record.operands.Count}");

            if (block.records.Count > 5) result.AppendLine($"{prefix}   ... 还有 {block.records.Count - 5} 个记录");
        }

        foreach (var subBlock in block.sub_blocks) print_block(result, subBlock, indent + 1);
    }
}