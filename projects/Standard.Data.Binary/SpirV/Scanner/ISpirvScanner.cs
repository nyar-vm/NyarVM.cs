namespace Std.Data.Binary.SpirV.Scanner;

/// <summary>
///     SPIR-V 格式扫描器接口，提供的SPIR-V 着色器二进制数据的快速探查能力的
/// </summary>
/// <remarks>
///     SPIR-V 的Khronos 定义的着色器二进制中间语言，用的Vulkan、OpenCL 等图形和计算 API的
///     扫描器专注于快速识的SPIR-V 版本、入口点、能力声明等元信息，
///     不做完整的指令反序列化，以实现零分配高性能扫描的
/// </remarks>
public interface ISpirvScanner
{
    /// <summary>
    ///     读取 SPIR-V 小端的32 位无符号整数并前的4 字节的
    /// </summary>
    /// <returns>无符的32 位整数值的/returns>
    uint read_spirv_word();

    /// <summary>
    ///     读取 SPIR-V 指令头（操作码和字数）并前进 4 字节的
    /// </summary>
    /// <returns>包含操作码和字数的元组的/returns>
    (ushort Opcode, ushort WordCount) read_instruction_header();

    /// <summary>
    ///     读取 SPIR-V 字序列中的字符串（null 终止的 字节对齐）的
    /// </summary>
    /// <returns>解码后的字符串的/returns>
    string read_spirv_string();
}