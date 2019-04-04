using Nyar.Types;
using Nyar.VM.NyarVM.GC;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;

namespace Nyar.VM.NyarVM;

/// <summary>
///     内存操作
/// </summary>
public static class MemoryOps
{
    /// <summary>
    ///     执行内存操作
    /// </summary>
    /// <param name="instruction">当前已解码指令。</param>
    /// <param name="frame">当前帧。</param>
    /// <param name="stack">值栈。</param>
    /// <param name="heap">堆内存。</param>
    /// <param name="gc">GC，用于写屏障（可选，为 null 时跳过）。</param>
    /// <returns>是否成功执行。</returns>
    public static bool execute(NyarInstruction instruction, Frame frame, ValueStack stack, NyarHeap heap,
        NyarGc? gc = null)
    {
        switch (instruction.code)
        {
            case NyarHeadCode.load_local:
            {
                stack.push(frame.get_local(instruction.operand1));
                return true;
            }

            case NyarHeadCode.store_local:
            {
                var value = stack.pop();
                frame.set_local(instruction.operand1, value);
                return true;
            }

            case NyarHeadCode.load_arg:
            {
                stack.push(frame.get_local(instruction.operand1));
                return true;
            }

            case NyarHeadCode.store_arg:
            {
                var value = stack.pop();
                frame.set_local(instruction.operand1, value);
                return true;
            }

            case NyarHeadCode.load_global:
            {
                var value = heap.load_global(instruction.operand1);
                stack.push(value);
                return true;
            }

            case NyarHeadCode.store_global:
            {
                var value = stack.pop();
                heap.store_global(instruction.operand1, value);
                return true;
            }

            case NyarHeadCode.alloc:
            {
                var address = heap.alloc(instruction.operand1);
                stack.push(Value.from_int(address));
                return true;
            }

            case NyarHeadCode.free:
            {
                var address = stack.pop();
                heap.free(address.i32);
                return true;
            }

            case NyarHeadCode.i32_load:
            {
                var address = stack.pop();
                var value = heap.load_i32(address.i32 + instruction.operand1);
                stack.push(Value.from_int(value));
                return true;
            }

            case NyarHeadCode.i32_store:
            {
                var value = stack.pop();
                var address = stack.pop();
                heap.store_i32(address.i32 + instruction.operand1, value.i32);
                return true;
            }

            case NyarHeadCode.i64_load:
            {
                var address = stack.pop();
                var value = heap.load_i64(address.i32 + instruction.operand1);
                stack.push(Value.from_long(value));
                return true;
            }

            case NyarHeadCode.i64_store:
            {
                var value = stack.pop();
                var address = stack.pop();
                heap.store_i64(address.i32 + instruction.operand1, value.i64);
                return true;
            }

            default:
                return false;
        }
    }
}
