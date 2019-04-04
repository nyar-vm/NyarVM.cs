using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.GC;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM;

/// <summary>
///     对象操作
/// </summary>
public static class ObjectOps
{
    private static int resolve_offset_index(int rawIndex, int count)
    {
        return rawIndex < 0 ? count + rawIndex : rawIndex;
    }

    private static int? resolve_ordinal_index(int rawOrdinal, int count)
    {
        if (rawOrdinal == 0) return null;

        return rawOrdinal > 0 ? rawOrdinal - 1 : count + rawOrdinal;
    }

    /// <summary>
    ///     执行对象操作
    /// </summary>
    /// <param name="instruction">当前已解码指令�?/param>
    /// <param name="frame">当前帧�?/param>
    /// <param name="stack">值栈�?/param>
    /// <param name="gc">GC，用于写屏障（可选，�?null 时跳过）�?/param>
    /// <returns>是否成功执行�?/returns>
    public static bool execute(NyarInstruction instruction, Frame frame, ValueStack stack, NyarGc? gc = null)
    {
        switch (instruction.code)
        {
            case NyarHeadCode.new_object:
            {
                stack.push(Value.from_object(new Dictionary<string, Value>()));
                return true;
            }

            case NyarHeadCode.get_field:
            {
                var fieldName = stack.pop();
                var obj = stack.pop();
                if (obj.@object is Dictionary<string, Value> dict && fieldName.utf8 is string name)
                    stack.push(dict.GetValueOrDefault(name, Value.@null));
                else
                    stack.push(Value.@null);

                return true;
            }

            case NyarHeadCode.set_field:
            {
                var value = stack.pop();
                var fieldName = stack.pop();
                var obj = stack.pop();
                if (obj.@object is Dictionary<string, Value> dict && fieldName.utf8 is string name)
                {
                    dict[name] = value;
                    gc?.write_barrier(NyarGc.extract_object_index(obj), value);
                }

                return true;
            }

            case NyarHeadCode.field_store:
            {
                var value = stack.pop();
                var obj = stack.pop();
                var constIdx = instruction.operand1;
                var module = frame.function.module;
                if (module != null && constIdx >= 0 && constIdx < module.constants.Count)
                {
                    var fieldName = module.constants[constIdx].utf8 as string;
                    if (obj.@object is Dictionary<string, Value> dict && fieldName != null)
                    {
                        dict[fieldName] = value;
                        gc?.write_barrier(NyarGc.extract_object_index(obj), value);
                    }
                }

                stack.push(value);
                return true;
            }

            case NyarHeadCode.get_ordinal_index:
            {
                var ordinal = stack.pop();
                var obj = stack.pop();
                if (obj.@object is List<Value> list && ordinal.type == ValueType.i32)
                {
                    var resolvedIndex = resolve_ordinal_index(ordinal.i32, list.Count);
                    stack.push(resolvedIndex >= 0 && resolvedIndex < list.Count
                        ? list[resolvedIndex.Value]
                        : Value.@null);
                }
                else
                {
                    stack.push(Value.@null);
                }

                return true;
            }

            case NyarHeadCode.get_offset_index:
            {
                var index = stack.pop();
                var obj = stack.pop();
                if (obj.@object is List<Value> list && index.type == ValueType.i32)
                {
                    var resolvedIndex = resolve_offset_index(index.i32, list.Count);
                    stack.push(resolvedIndex >= 0 && resolvedIndex < list.Count ? list[resolvedIndex] : Value.@null);
                }
                else if (obj.@object is Dictionary<string, Value> dict && index.utf8 is string key)
                {
                    stack.push(dict.GetValueOrDefault(key, Value.@null));
                }
                else
                {
                    stack.push(Value.@null);
                }

                return true;
            }

            case NyarHeadCode.set_ordinal_index:
            {
                var value = stack.pop();
                var ordinal = stack.pop();
                var obj = stack.pop();
                if (obj.@object is List<Value> list && ordinal.type == ValueType.i32)
                {
                    var resolvedIndex = resolve_ordinal_index(ordinal.i32, list.Count);
                    if (resolvedIndex >= 0 && resolvedIndex < list.Count)
                    {
                        list[resolvedIndex.Value] = value;
                        gc?.write_barrier(NyarGc.extract_object_index(obj), value);
                    }
                }

                stack.push(value);
                return true;
            }

            case NyarHeadCode.set_offset_index:
            {
                var value = stack.pop();
                var index = stack.pop();
                var obj = stack.pop();
                if (obj.@object is List<Value> list && index.type == ValueType.i32)
                {
                    var resolvedIndex = resolve_offset_index(index.i32, list.Count);
                    if (resolvedIndex >= 0 && resolvedIndex < list.Count)
                    {
                        list[resolvedIndex] = value;
                        gc?.write_barrier(NyarGc.extract_object_index(obj), value);
                    }
                }
                else if (obj.@object is Dictionary<string, Value> dict && index.utf8 is string key)
                {
                    dict[key] = value;
                    gc?.write_barrier(NyarGc.extract_object_index(obj), value);
                }

                stack.push(value);
                return true;
            }

            case NyarHeadCode.index_store:
            {
                var value = stack.pop();
                var index = stack.pop();
                var obj = stack.pop();
                if (obj.@object is List<Value> list && index.type == ValueType.i32)
                {
                    if (index.i32 >= 0 && index.i32 < list.Count)
                    {
                        list[index.i32] = value;
                        gc?.write_barrier(NyarGc.extract_object_index(obj), value);
                    }
                }
                else if (obj.@object is Dictionary<string, Value> dict && index.utf8 is string key)
                {
                    dict[key] = value;
                    gc?.write_barrier(NyarGc.extract_object_index(obj), value);
                }

                stack.push(value);
                return true;
            }

            case NyarHeadCode.length:
            {
                var obj = stack.pop();
                if (obj.@object is List<Value> list)
                    stack.push(Value.from_int(list.Count));
                else if (obj.@object is Dictionary<string, Value> dict)
                    stack.push(Value.from_int(dict.Count));
                else if (obj.utf8 is string str)
                    stack.push(Value.from_int(str.Length));
                else
                    stack.push(Value.from_int(0));

                return true;
            }

            case NyarHeadCode.new_closure:
            {
                var funcIndex = instruction.operand1;
                var func = frame.function.module?.functions[funcIndex];
                var upvalues = new List<Value>();
                var closure = new NyarClosure(func!, upvalues);
                stack.push(Value.from_closure(closure));
                return true;
            }

            case NyarHeadCode.get_upvalue:
            {
                var upvalueIndex = instruction.operand1;
                if (frame.function.module?.functions.Count > 0)
                {
                    var closure = stack.peek();
                    if (closure.closure is NyarClosure nc && upvalueIndex < nc.upvalues.Count)
                    {
                        stack.pop();
                        stack.push(nc.upvalues[upvalueIndex]);
                    }
                    else
                    {
                        stack.push(Value.@null);
                    }
                }
                else
                {
                    stack.push(Value.@null);
                }

                return true;
            }

            case NyarHeadCode.set_upvalue:
            {
                var upvalueIndex = instruction.operand1;
                var value = stack.pop();
                var closure = stack.pop();
                if (closure.closure is NyarClosure nc)
                {
                    while (nc.upvalues.Count <= upvalueIndex) nc.upvalues.Add(Value.@null);

                    nc.upvalues[upvalueIndex] = value;
                    gc?.write_barrier(NyarGc.extract_object_index(closure), value);
                }

                stack.push(value);
                return true;
            }

            case NyarHeadCode.access_static:
            {
                var fieldOffset = instruction.operand1;
                var obj = stack.pop();
                if (obj.@object is List<Value> list && fieldOffset >= 0 && fieldOffset < list.Count)
                {
                    stack.push(list[fieldOffset]);
                }
                else if (obj.@object is Dictionary<string, Value> dict)
                {
                    var keys = dict.Keys.ToList();
                    if (fieldOffset >= 0 && fieldOffset < keys.Count)
                        stack.push(dict[keys[fieldOffset]]);
                    else
                        stack.push(Value.@null);
                }
                else
                {
                    stack.push(Value.@null);
                }

                return true;
            }

            case NyarHeadCode.access_witness:
            {
                var witnessOffset = instruction.operand1;
                var obj = stack.pop();
                if (obj.@object is Dictionary<string, Value> dict)
                {
                    var keys = dict.Keys.ToList();
                    if (witnessOffset >= 0 && witnessOffset < keys.Count)
                        stack.push(dict[keys[witnessOffset]]);
                    else
                        stack.push(Value.@null);
                }
                else if (obj.@object is List<Value> list && witnessOffset >= 0 && witnessOffset < list.Count)
                {
                    stack.push(list[witnessOffset]);
                }
                else
                {
                    stack.push(Value.@null);
                }

                return true;
            }

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

            default:
                return false;
        }
    }
}

/// <summary>
///     闭包数据结构，封装函数和上�?/// </summary>
public sealed class NyarClosure
{
    /// <summary>
    ///     初始�?NyarClosure
    /// </summary>
    /// <param name="function">闭包捕获的函数�?/param>
    /// <param name="upvalues">上值列表�?/param>
    public NyarClosure(IFunction function, List<Value> upvalues)
    {
        this.function = function;
        this.upvalues = upvalues;
    }

    /// <summary>
    ///     闭包捕获的函�?    /// </summary>
    public IFunction function { get; }

    /// <summary>
    ///     上值列�?    /// </summary>
    public List<Value> upvalues { get; }
}
