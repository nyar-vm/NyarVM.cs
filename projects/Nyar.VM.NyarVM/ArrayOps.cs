using Nyar.Types;
using ValueType = Nyar.Types.ValueType;
using Nyar.VM.NyarVM.GC;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;

namespace Nyar.VM.NyarVM;

/// <summary>
///     数组操作
/// </summary>
public static class ArrayOps
{
    private static int resolve_array_index(int rawIndex, int count)
    {
        return rawIndex < 0 ? count + rawIndex : rawIndex;
    }

    /// <summary>
    ///     执行数组操作
    /// </summary>
    /// <param name="instruction">当前已解码指令。</param>
    /// <param name="stack">值栈。</param>
    /// <param name="gc">GC，用于写屏障（可选，为 null 时跳过）。</param>
    /// <returns>是否成功执行。</returns>
    public static bool execute(NyarInstruction instruction, ValueStack stack, NyarGc? gc = null)
    {
        switch (instruction.code)
        {
            case NyarHeadCode.array_push:
            {
                var value = stack.pop();
                var array = stack.pop();
                if (array.@object is List<Value> list)
                {
                    list.Add(value);
                    gc?.write_barrier(NyarGc.extract_object_index(array), value);
                }

                stack.push(array);
                return true;
            }

            case NyarHeadCode.array_get:
            {
                var index = stack.pop();
                var array = stack.pop();
                if (array.@object is List<Value> list && index.type == ValueType.i32)
                {
                    var resolvedIndex = resolve_array_index(index.i32, list.Count);
                    stack.push(resolvedIndex >= 0 && resolvedIndex < list.Count ? list[resolvedIndex] : Value.@null);
                }
                else
                {
                    stack.push(Value.@null);
                }

                return true;
            }

            case NyarHeadCode.array_set:
            {
                var value = stack.pop();
                var index = stack.pop();
                var array = stack.pop();
                if (array.@object is List<Value> list && index.type == ValueType.i32)
                {
                    var resolvedIndex = resolve_array_index(index.i32, list.Count);
                    if (resolvedIndex >= 0 && resolvedIndex < list.Count)
                    {
                        list[resolvedIndex] = value;
                        gc?.write_barrier(NyarGc.extract_object_index(array), value);
                    }
                }

                return true;
            }

            default:
                return false;
        }
    }
}
