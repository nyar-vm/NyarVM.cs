using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.SpirV.Data;

namespace Std.Data.Binary.SpirV.Encode;

/// <summary>
///     SPIR-V 模块编码器，的C# 数据结构编码的Khronos SPIR-V 二进制中间语言格式的
/// </summary>
/// <remarks>
///     SPIR-V 的Khronos 定义的着色器二进制中间语言，用的Vulkan、OpenCL 等图形和计算 API的
///     编码器生成符的Khronos SPIR-V 规范的二进制数据的
/// </remarks>
public sealed class SpirvEncoder
{
    /// <summary>
    ///     将模块数据编码为 SPIR-V 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     模块数据的/param>
    ///     <returns>SPIR-V 二进制数据的/returns>
    public byte[] encode(SpirvModuleData data)
    {
        var size = 20 + calculate_instructions_size(data.instructions);
        var writer = new ByteBufferWriter(size);

        write_header(ref writer, data);
        write_instructions(ref writer, data.instructions);

        return writer.to_array();
    }

    /// <summary>
    ///     将指令列表编码为 SPIR-V 二进制格式（不包含文件头）的
    /// </summary>
    /// <param name="instructions">
    ///     指令列表的/param>
    ///     <returns>SPIR-V 指令流二进制数据的/returns>
    public byte[] encode_instructions(IReadOnlyList<SpirvInstruction> instructions)
    {
        var size = calculate_instructions_size(instructions);
        var writer = new ByteBufferWriter(size);

        write_instructions(ref writer, instructions);

        return writer.to_array();
    }

    /// <summary>
    ///     编码一的SPIR-V 指令的
    /// </summary>
    /// <param name="opcode">
    ///     操作码的/param>
    ///     <param name="operands">
    ///         操作数字列表的/param>
    ///         <returns>编码后的指令二进制数据的/returns>
    public byte[] encode_instruction(SpirvOpCode opcode, IReadOnlyList<uint> operands)
    {
        var wordCount = (ushort)(1 + operands.Count);
        var writer = new ByteBufferWriter(wordCount * 4);

        var firstWord = (uint)(wordCount << 16) | (ushort)opcode;
        writer.write_u32_le(firstWord);

        foreach (var operand in operands) writer.write_u32_le(operand);

        return writer.to_array();
    }

    /// <summary>
    ///     编码字符串为 SPIR-V 字序列（的null 终止并填充到 4 字节对齐）的
    /// </summary>
    /// <param name="value">
    ///     字符串值的/param>
    ///     <returns>编码后的字列表的/returns>
    public IReadOnlyList<uint> encode_string(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var paddedLength = (bytes.Length + 1 + 3) & ~3;
        var paddedBytes = new byte[paddedLength];

        Array.Copy(bytes, paddedBytes, bytes.Length);

        for (var i = bytes.Length; i < paddedLength; i++) paddedBytes[i] = 0;

        var words = new uint[paddedLength / 4];

        for (var i = 0; i < words.Length; i++)
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(paddedBytes.AsSpan(i * 4, 4));

        return words;
    }

    #region 私有编码方法

    private static void write_header(ref ByteBufferWriter writer, SpirvModuleData data)
    {
        writer.write_u32_le(data.magic_number);
        writer.write_u32_le(data.version);
        writer.write_u32_le(data.generator_magic);
        writer.write_u32_le(data.bound);
        writer.write_u32_le(data.schema);
    }

    private static void write_instructions(ref ByteBufferWriter writer, IReadOnlyList<SpirvInstruction> instructions)
    {
        foreach (var instruction in instructions) write_instruction(ref writer, instruction);
    }

    private static void write_instruction(ref ByteBufferWriter writer, SpirvInstruction instruction)
    {
        var wordCount = (ushort)(1 + instruction.operands.Count);
        var firstWord = (uint)(wordCount << 16) | (ushort)instruction.opcode;

        writer.write_u32_le(firstWord);

        foreach (var operand in instruction.operands) writer.write_u32_le(operand);
    }

    private static int calculate_instructions_size(IReadOnlyList<SpirvInstruction> instructions)
    {
        var size = 0;

        foreach (var instruction in instructions) size += (1 + instruction.operands.Count) * 4;

        return size;
    }

    #endregion
}