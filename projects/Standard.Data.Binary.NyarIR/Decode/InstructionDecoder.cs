using System.Runtime.CompilerServices;
using Std.Data.Binary.NyarIR.Data;

namespace Std.Data.Binary.NyarIR.Decode;

/// <summary>
///     指令预解码器。
///     它负责把代码字节流转换为 <see cref="NyarInstruction" /> 数组；代码是编码结果，指令是解码结果。
/// </summary>
public static class InstructionDecoder
{
    /// <summary>
    ///     将代码字节流预解码为 <see cref="NyarInstruction" /> 数组。
    ///     返回数组长度与原始字节流相同，并按字节偏移建立索引；仅在指令起始位置存储有效指令。
    /// </summary>
    /// <param name="bytecode">原始代码字节流。</param>
    /// <returns>按字节偏移索引的预解码指令数组。</returns>
    public static NyarInstruction[] decode(byte[] bytecode)
    {
        var result = new NyarInstruction[bytecode.Length];
        Array.Fill(result, default);

        var pc = 0;
        while (pc < bytecode.Length)
        {
            var instruction = decode_at(bytecode, pc);
            if (!instruction.is_valid) break;

            result[pc] = instruction;
            pc += instruction.size;
        }

        return result;
    }

    /// <summary>
    ///     解码指定字节偏移处的一条指令。
    /// </summary>
    /// <param name="bytecode">原始代码字节流。</param>
    /// <param name="pc">指令起始字节偏移。</param>
    /// <returns>解码后的指令；越界时返回无效指令。</returns>
    public static NyarInstruction decode_at(byte[] bytecode, int pc)
    {
        if (pc < 0 || pc >= bytecode.Length) return default;

        var opcode = (NyarHeadCode)bytecode[pc];
        var size = try_get_instruction_size(bytecode, pc);
        if (size == 0) return default;

        var operand1 = 0;
        var operand2 = 0;
        var operand3 = 0;

        if (size >= 5 && pc + 4 < bytecode.Length) operand1 = Unsafe.ReadUnaligned<int>(ref bytecode[pc + 1]);

        if (size >= 9 && pc + 8 < bytecode.Length) operand2 = Unsafe.ReadUnaligned<int>(ref bytecode[pc + 5]);

        if (size >= 13 && pc + 12 < bytecode.Length) operand3 = Unsafe.ReadUnaligned<int>(ref bytecode[pc + 9]);

        return new NyarInstruction(opcode, operand1, operand2, operand3, size);
    }

    /// <summary>
    ///     获取指令大小。
    ///     `0` 表示头码无效或当前解码器不识别该编码。
    /// </summary>
    public static int get_instruction_size(NyarHeadCode headCode)
    {
        return NyarInstruction.code_size(headCode);
    }

    /// <summary>
    ///     从代码字节流中尝试获取指定偏移处指令的真实编码长度。
    ///     对固定形态指令，长度只依赖头码；对前缀形态指令，长度可继续依赖子码与扩展负载。
    /// </summary>
    /// <param name="bytecode">原始代码字节流。</param>
    /// <param name="pc">指令起始字节偏移。</param>
    /// <returns>真实编码长度；无法完整解码时返回 <c>0</c>。</returns>
    public static byte try_get_instruction_size(byte[] bytecode, int pc)
    {
        if (pc < 0 || pc >= bytecode.Length) return 0;

        var opcode = (NyarHeadCode)bytecode[pc];
        var form = NyarInstruction.get_form(opcode);

        return form switch
        {
            NyarInstructionForm.plain => 1,
            NyarInstructionForm.imm1 => pc + 4 < bytecode.Length ? (byte)5 : (byte)0,
            NyarInstructionForm.imm2 => pc + 8 < bytecode.Length ? (byte)9 : (byte)0,
            NyarInstructionForm.imm3 => pc + 12 < bytecode.Length ? (byte)13 : (byte)0,
            NyarInstructionForm.prefixed => try_get_prefixed_instruction_size(bytecode, pc, opcode),
            _ => 0
        };
    }

    /// <summary>
    ///     获取前缀形态指令的编码长度。
    ///     当前先收敛到统一入口，后续可按不同前缀族继续扩展子码与负载长度规则。
    /// </summary>
    /// <param name="bytecode">原始代码字节流。</param>
    /// <param name="pc">指令起始字节偏移。</param>
    /// <param name="headCode">一级头码。</param>
    /// <returns>真实编码长度；无法完整解码时返回 <c>0</c>。</returns>
    private static byte try_get_prefixed_instruction_size(byte[] bytecode, int pc, NyarHeadCode headCode)
    {
        return headCode switch
        {
            NyarHeadCode.simd => pc + 4 < bytecode.Length ? (byte)5 : (byte)0,
            _ => 0
        };
    }

    /// <summary>
    ///     重新解码指定偏移量处的指令。
    ///     当代码字节流被修补后，从指定偏移重新解码一条指令，并清除旧范围内残留的无效占位标记。
    /// </summary>
    /// <param name="instructions">按字节偏移索引的预解码指令数组。</param>
    /// <param name="bytecode">原始代码字节流。</param>
    /// <param name="pc">指令起始字节偏移。</param>
    /// <param name="oldSize">修补前的指令字节大小，用于清除残留标记。</param>
    public static void re_decode_at(NyarInstruction[] instructions, byte[] bytecode, int pc, int oldSize)
    {
        if (pc < 0 || pc >= bytecode.Length) return;

        instructions[pc] = decode_at(bytecode, pc);

        for (var i = 1; i < oldSize; i++)
            if (pc + i < instructions.Length)
                instructions[pc + i] = default;
    }
}