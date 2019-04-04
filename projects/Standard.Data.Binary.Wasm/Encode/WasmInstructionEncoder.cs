using Std.Data.Binary.Frame;
using Std.Data.Binary.Wasm.Data;

namespace Std.Data.Binary.Wasm.Encode;

/// <summary>
///     WasmInstruction 编码器，将结构化指令编码的WASM 字节码的
/// </summary>
public static class WasmInstructionEncoder
{
    /// <summary>
    ///     编码单个指令，写的ByteBufferWriter的
    /// </summary>
    /// <returns>编码的字节数的/returns>
    public static int encode(ref ByteBufferWriter writer, WasmInstruction instruction)
    {
        var startPos = writer.position;
        var opcodeValue = (ushort)instruction.opcode;

        if (opcodeValue > 0xFF)
        {
            var prefix = (byte)(opcodeValue >> 8);
            var suffix = (byte)(opcodeValue & 0xFF);
            writer.write_u8(prefix);
            writer.write_u8(suffix);
        }
        else
        {
            writer.write_u8((byte)opcodeValue);
        }

        if (instruction.operands is { } operands)
            foreach (var operand in operands)
                encode_operand(ref writer, operand);

        return writer.position - startPos;
    }

    /// <summary>
    ///     编码指令列表并返的BodySize（用的WasmCode.MaxLength）的
    /// </summary>
    /// <returns>BodySize（指令列表的总字节数）的/returns>
    public static uint encode_list(ref ByteBufferWriter writer, IReadOnlyList<WasmInstruction> instructions)
    {
        var startPos = writer.position;
        foreach (var instruction in instructions) encode(ref writer, instruction);

        return (uint)(writer.position - startPos);
    }

    private static void encode_operand(ref ByteBufferWriter writer, WasmImmediate operand)
    {
        switch (operand)
        {
            case WasmI32Imm i32:
                writer.write_leb128_i32(i32.value);
                break;

            case WasmI64Imm i64:
                writer.write_leb128_i64(i64.value);
                break;

            case WasmF32Imm f32:
                writer.write_f32_le(f32.value);
                break;

            case WasmF64Imm f64:
                writer.write_f64_le(f64.value);
                break;

            case WasmTypeIndexImm typeIdx:
                writer.write_leb128_u32(typeIdx.index);
                break;

            case WasmFuncIndexImm funcIdx:
                writer.write_leb128_u32(funcIdx.index);
                break;

            case WasmFieldIndexImm fieldIdx:
                writer.write_leb128_u32(fieldIdx.index);
                break;

            case WasmHeapTypeImm heapIdx:
                writer.write_leb128_u32(heapIdx.index);
                break;

            case WasmLabelIndexImm labelIdx:
                writer.write_leb128_u32(labelIdx.index);
                break;

            case WasmLocalIndexImm localIdx:
                writer.write_leb128_u32(localIdx.index);
                break;

            case WasmGlobalIndexImm globalIdx:
                writer.write_leb128_u32(globalIdx.index);
                break;

            case WasmTagIndexImm tagIdx:
                writer.write_leb128_u32(tagIdx.index);
                break;

            case WasmV128Imm v128:
                if (v128.value.Length != 16) throw new ArgumentException("V128 立即数必须为 16 字节", nameof(operand));

                writer.write(v128.value);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operand), operand.GetType().Name, "未知的立即数类型");
        }
    }
}