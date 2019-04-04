using System.Buffers.Binary;
using Nyar.Assembler;
using Nyar.Types;
using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using RuntimeModuleImport = Nyar.Types.ModuleImport;
using RuntimeModuleExport = Nyar.Types.ModuleExport;
using RuntimeNyarFunction = Nyar.Types.NyarFunction;
using RuntimeNyarModule = Nyar.Types.NyarModule;
using RuntimeWitnessDispatchEntry = Nyar.Types.WitnessDispatchEntry;

namespace Nyar.VM.NyarVM.Bytecode;

/// <summary>
///     将 Assembler 层的 `GenerateModule` 序列化为运行时 `NyarModule`。
///     这里产出的是模块元数据与代码字节流，不是解码后的指令对象。
/// </summary>
public static class CompilationUnitSerializer
{
    /// <summary>
    ///     将 `GenerateModule` 序列化为运行时 `NyarModule`。
    /// </summary>
    /// <param name="unit">编译单元。</param>
    /// <returns>运行时模块。</returns>
    public static RuntimeNyarModule serialize(GenerateModule unit)
    {
        var module = new RuntimeNyarModule(unit.name);

        var constantPool = build_constant_pool(unit);

        for (var i = 0; i < constantPool.int64Set.Count; i++)
            module.constants.Add(Value.from_int((int)constantPool.int64Set[i]));

        for (var i = 0; i < constantPool.float64Set.Count; i++)
            module.constants.Add(Value.from_double(constantPool.float64Set[i]));

        for (var i = 0; i < constantPool.stringSet.Count; i++)
            module.constants.Add(Value.from_string(constantPool.stringSet[i]));

        var functionIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < unit.functions.Count; i++) functionIndexByName[unit.functions[i].name] = i;

        var writer = new ByteBufferWriter(4096);
        var functionOffsets = new List<(int Offset, int Length)>();
        var allFixups = new List<LabelFixup>();

        foreach (var asmFunc in unit.functions)
        {
            var startOffset = writer.position;
            serialize_function(asmFunc, ref writer, constantPool, functionIndexByName, allFixups);
            var length = writer.position - startOffset;
            functionOffsets.Add((startOffset, length));
        }

        var bytecode = writer.to_array();

        apply_label_fixups(bytecode, allFixups);

        for (var i = 0; i < unit.functions.Count; i++)
        {
            var asmFunc = unit.functions[i];
            var (offset, length) = functionOffsets[i];

            var runtimeFunc = new RuntimeNyarFunction(
                asmFunc.name,
                asmFunc.parameters.Count,
                asmFunc.local_variables.Count,
                offset,
                length
            )
            {
                module = module
            };
            module.functions.Add(runtimeFunc);
        }

        foreach (var import in unit.imports)
        {
            var kind = (ImportKind)import.kind;
            module.imports.Add(new RuntimeModuleImport(import.module_name, import.symbol_name, kind));
        }

        foreach (var exportItem in unit.exports)
        {
            var kind = (ExportKind)exportItem.kind;
            module.exports.Add(new RuntimeModuleExport(exportItem.name, kind, exportItem.function_index));
        }

        foreach (var witnessEntry in unit.witness_entries)
        {
            var functionIndex = unit.functions.FindIndex(function =>
                string.Equals(function.name, witnessEntry.implementation_function_name, StringComparison.Ordinal));
            if (functionIndex < 0)
            {
                // 跨模块 witness 绑定在当前模块中找不到实现函数，跳过。
                // 这些绑定需要在链接时或运行时通过其他模块解析。
                continue;
            }

            module.witness_entries.Add(new RuntimeWitnessDispatchEntry(
                witnessEntry.method_id,
                witnessEntry.type_id,
                witnessEntry.method_name,
                functionIndex,
                witnessEntry.interface_id,
                witnessEntry.interface_method_index));
        }

        module.raw_bytecode = bytecode;
        return module;
    }

    /// <summary>
    ///     从 `GenerateModule` 的内联操作数中收集常量，构建扁平常量池。
    ///     布局：`int64` 值在前，`float64` 值居中，字符串值在后。
    /// </summary>
    private static FlatConstantPool build_constant_pool(GenerateModule unit)
    {
        var int64Set = new List<long>();
        var float64Set = new List<double>();
        var stringSet = new List<string>();

        var int64Index = new Dictionary<long, int>();
        var float64Index = new Dictionary<double, int>();
        var stringIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var func in unit.functions)
        foreach (var instr in func.instructions)
        foreach (var operand in instr.operands)
            collect_inline_constants(operand, int64Set, int64Index, float64Set, float64Index, stringSet, stringIndex);

        return new FlatConstantPool(int64Set, float64Set, stringSet, int64Index, float64Index, stringIndex);
    }

    /// <summary>
    ///     递归收集单个操作数中的内联常�?    /// </summary>
    private static void collect_inline_constants(
        GenerateOperand operand,
        List<long> int64Set, Dictionary<long, int> int64Index,
        List<double> float64Set, Dictionary<double, int> float64Index,
        List<string> stringSet, Dictionary<string, int> stringIndex)
    {
        switch (operand)
        {
            case GenerateOperand.I32 i32:
                ensure_int64(i32.value, int64Set, int64Index);
                break;

            case GenerateOperand.I64 i64:
                ensure_int64(i64.value, int64Set, int64Index);
                break;

            case GenerateOperand.F32 f32:
                ensure_float64(f32.value, float64Set, float64Index);
                break;

            case GenerateOperand.F64 f64:
                ensure_float64(f64.value, float64Set, float64Index);
                break;

            case GenerateOperand.Str str:
                ensure_string(str.value, stringSet, stringIndex);
                break;
        }
    }

    private static void ensure_int64(long value, List<long> list, Dictionary<long, int> index)
    {
        if (index.TryAdd(value, list.Count)) list.Add(value);
    }

    private static void ensure_float64(double value, List<double> list, Dictionary<double, int> index)
    {
        if (index.TryAdd(value, list.Count)) list.Add(value);
    }

    private static void ensure_string(string value, List<string> list, Dictionary<string, int> index)
    {
        if (index.TryAdd(value, list.Count)) list.Add(value);
    }

    /// <summary>
    ///     序列化单个函数的指令序列
    /// </summary>
    private static void serialize_function(
        GenerateFunction function,
        ref ByteBufferWriter writer,
        FlatConstantPool pool,
        Dictionary<string, int> functionIndexByName,
        List<LabelFixup> allFixups)
    {
        var labelPositions = compute_label_positions(function);

        for (var i = 0; i < function.instructions.Count; i++)
        {
            var instruction = function.instructions[i];
            var instrOffset = writer.position;

            writer.write_u8((byte)instruction.head_code);

            switch (instruction.head_code)
            {
                case NyarHeadCode.@const:
                {
                    var operand = instruction.operands.Count > 0 ? instruction.operands[0] : new GenerateOperand.I32(0);
                    writer.write_i32_le(operand_to_const_index(operand, pool));
                    break;
                }

                case NyarHeadCode.load_local:
                case NyarHeadCode.store_local:
                case NyarHeadCode.load_arg:
                case NyarHeadCode.store_arg:
                {
                    var index = instruction.operands.Count > 0 ? extract_index(instruction.operands[0]) : 0;
                    writer.write_i32_le(index);
                    break;
                }

                case NyarHeadCode.call:
                case NyarHeadCode.call_static:
                case NyarHeadCode.tail_call:
                {
                    var funcIndex = instruction.operands.Count > 0
                        ? resolve_func_index(instruction.operands[0], functionIndexByName)
                        : 0;
                    writer.write_i32_le(funcIndex);
                    break;
                }

                case NyarHeadCode.jump:
                case NyarHeadCode.jump_if_true:
                case NyarHeadCode.jump_if_false:
                {
                    if (instruction.operands.Count > 0 && instruction.operands[0] is GenerateOperand.Label label)
                    {
                        if (labelPositions.TryGetValue(label.name, out var targetInstrIndex))
                        {
                            writer.write_i32_le(0);
                            allFixups.Add(new LabelFixup(writer.position - 4, targetInstrIndex, function));
                        }
                        else
                        {
                            writer.write_i32_le(0);
                        }
                    }
                    else
                    {
                        writer.write_i32_le(instruction.operands.Count > 0
                            ? extract_index(instruction.operands[0])
                            : 0);
                    }

                    break;
                }

                case NyarHeadCode.new_object:
                {
                    var operand = instruction.operands.Count > 0 ? instruction.operands[0] : new GenerateOperand.I32(0);
                    writer.write_i32_le(operand_to_const_index(operand, pool));
                    break;
                }

                case NyarHeadCode.field_store:
                {
                    var operand = instruction.operands.Count > 0 ? instruction.operands[0] : new GenerateOperand.I32(0);
                    writer.write_i32_le(operand_to_const_index(operand, pool));
                    break;
                }

                case NyarHeadCode.index_store:
                    break;

                case NyarHeadCode.call_dynamic:
                {
                    for (var opIdx = 0; opIdx < 3; opIdx++)
                    {
                        var val = opIdx < instruction.operands.Count ? extract_index(instruction.operands[opIdx]) : 0;
                        writer.write_i32_le(val);
                    }

                    break;
                }

                case NyarHeadCode.call_witness:
                {
                    for (var opIdx = 0; opIdx < 2; opIdx++)
                    {
                        var val = opIdx < instruction.operands.Count ? extract_index(instruction.operands[opIdx]) : 0;
                        writer.write_i32_le(val);
                    }

                    break;
                }

                case NyarHeadCode.access_static:
                case NyarHeadCode.access_witness:
                {
                    var val = instruction.operands.Count > 0 ? extract_index(instruction.operands[0]) : 0;
                    writer.write_i32_le(val);
                    break;
                }

                case NyarHeadCode.access_dynamic:
                {
                    for (var opIdx = 0; opIdx < 2; opIdx++)
                    {
                        var val = opIdx < instruction.operands.Count ? extract_index(instruction.operands[opIdx]) : 0;
                        writer.write_i32_le(val);
                    }

                    break;
                }

                case NyarHeadCode.@catch:
                case NyarHeadCode.resume:
                case NyarHeadCode.effect_handle:
                case NyarHeadCode.inline_cache_update:
                {
                    var val = instruction.operands.Count > 0 ? extract_index(instruction.operands[0]) : 0;
                    writer.write_i32_le(val);
                    break;
                }

                case NyarHeadCode.builtin_call:
                {
                    var val = instruction.operands.Count > 0 ? extract_index(instruction.operands[0]) : 0;
                    writer.write_i32_le(val);
                    break;
                }

                case NyarHeadCode.call_intrinsic:
                {
                    // call_intrinsic 是 imm2 形态：operand1 = 常量池索引（函数名），operand2 = 参数数量
                    var nameOperand = instruction.operands.Count > 0
                        ? instruction.operands[0]
                        : new GenerateOperand.Str(string.Empty);
                    writer.write_i32_le(operand_to_const_index(nameOperand, pool));
                    var argCount = instruction.operands.Count > 1
                        ? extract_index(instruction.operands[1])
                        : 0;
                    writer.write_i32_le(argCount);
                    break;
                }

                case NyarHeadCode.simd:
                {
                    var val = instruction.operands.Count > 0 ? extract_index(instruction.operands[0]) : 0;
                    writer.write_i32_le(val);
                    break;
                }
            }
        }
    }

    /// <summary>
    ///     �?GenerateOperand 转换为扁平常量池索引
    /// </summary>
    private static int operand_to_const_index(GenerateOperand operand, FlatConstantPool pool)
    {
        return operand switch
        {
            GenerateOperand.I32 i32 => pool.int64Index[i32.value],
            GenerateOperand.I64 i64 => pool.int64Index[i64.value],
            GenerateOperand.F64 f64 => pool.int64Set.Count + pool.float64Index[f64.value],
            GenerateOperand.F32 f32 => pool.int64Set.Count + pool.float64Index[f32.value],
            GenerateOperand.Str str => pool.int64Set.Count + pool.float64Set.Count + pool.stringIndex[str.value],
            GenerateOperand.Const c => map_pool_const(c, pool),
            GenerateOperand.Null => 0,
            _ => 0
        };
    }

    /// <summary>
    ///     将已有的 GenerateConstantPool 索引映射到扁平索�?    /// </summary>
    private static int map_pool_const(GenerateOperand.Const c, FlatConstantPool pool)
    {
        return c.type switch
        {
            GenerateValueType.i32 or GenerateValueType.@bool or GenerateValueType.@void or GenerateValueType.unit
                => c.pool_index < pool.int64Set.Count ? c.pool_index : 0,
            GenerateValueType.f64 or GenerateValueType.f32
                => pool.int64Set.Count + (c.pool_index < pool.float64Set.Count ? c.pool_index : 0),
            GenerateValueType.utf8 or GenerateValueType.utf16
                => pool.int64Set.Count + pool.float64Set.Count +
                   (c.pool_index < pool.stringSet.Count ? c.pool_index : 0),
            _ => c.pool_index
        };
    }

    /// <summary>
    ///     从操作数中提取整数索引�?    /// </summary>
    private static int extract_index(GenerateOperand operand)
    {
        return operand switch
        {
            GenerateOperand.Local local => local.index,
            GenerateOperand.Param param => param.index,
            GenerateOperand.I32 i32 => i32.value,
            GenerateOperand.I64 i64 => (int)i64.value,
            GenerateOperand.FuncRef funcRef => 0,
            GenerateOperand.Label => 0,
            _ => 0
        };
    }

    /// <summary>
    ///     �?FuncRef 操作数解析为函数索引
    /// </summary>
    private static int resolve_func_index(GenerateOperand operand, Dictionary<string, int> functionIndexByName)
    {
        if (operand is GenerateOperand.FuncRef funcRef) return functionIndexByName.GetValueOrDefault(funcRef.name, 0);

        return extract_index(operand);
    }

    /// <summary>
    ///     计算函数内所有标签对应的指令索引
    /// </summary>
    private static Dictionary<string, int> compute_label_positions(GenerateFunction function)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var label in function.labels) result[label.name] = label.instruction_index;

        return result;
    }

    /// <summary>
    ///     对代码字节流执行标签回填。
    ///     标签先按“第几条指令”建模，最终再映射为代码字节偏移写回。
    /// </summary>
    private static void apply_label_fixups(byte[] bytecode, List<LabelFixup> fixups)
    {
        var instrByteOffsets = new Dictionary<GenerateFunction, Dictionary<int, int>>();

        foreach (var fixup in fixups)
        {
            if (!instrByteOffsets.TryGetValue(fixup.function, out var offsetMap))
            {
                offsetMap = compute_instruction_byte_offsets(fixup.function);
                instrByteOffsets[fixup.function] = offsetMap;
            }

            if (!offsetMap.TryGetValue(fixup.target_instruction_index, out var targetByteOffset)) continue;

            var relativeOffset = targetByteOffset - fixup.fixup_byte_offset + 4;
            BinaryPrimitives.WriteInt32LittleEndian(bytecode.AsSpan(fixup.fixup_byte_offset, 4), relativeOffset);
        }
    }

    /// <summary>
    ///     计算函数内每条指令的字节偏移
    /// </summary>
    private static Dictionary<int, int> compute_instruction_byte_offsets(GenerateFunction function)
    {
        var result = new Dictionary<int, int>();
        var offset = 0;

        for (var i = 0; i < function.instructions.Count; i++)
        {
            result[i] = offset;
            offset += get_instruction_size(function.instructions[i].head_code);
        }

        return result;
    }

    /// <summary>
    ///     获取指令的字节大小。
    ///     统一复用 IR 侧的编码长度定义，避免序列化链路维护重复大小表。
    /// </summary>
    private static int get_instruction_size(NyarHeadCode headCode)
    {
        return NyarInstruction.code_size(headCode);
    }

    /// <summary>
    ///     扁平常量池：收集内联操作数中的常量，维护去重索引
    /// </summary>
    private sealed class FlatConstantPool
    {
        public readonly Dictionary<double, int> float64Index;
        public readonly List<double> float64Set;
        public readonly Dictionary<long, int> int64Index;
        public readonly List<long> int64Set;
        public readonly Dictionary<string, int> stringIndex;
        public readonly List<string> stringSet;

        public FlatConstantPool(
            List<long> int64Set,
            List<double> float64Set,
            List<string> stringSet,
            Dictionary<long, int> int64Index,
            Dictionary<double, int> float64Index,
            Dictionary<string, int> stringIndex)
        {
            this.int64Set = int64Set;
            this.float64Set = float64Set;
            this.stringSet = stringSet;
            this.int64Index = int64Index;
            this.float64Index = float64Index;
            this.stringIndex = stringIndex;
        }
    }

    /// <summary>
    ///     待回填的标签引用
    /// </summary>
    /// <param name="fixup_byte_offset">回填目标在字节码中的字节偏移�?/param>
    /// <param name="target_instruction_index">标签目标的指令索引�?/param>
    /// <param name="function">所属函数（用于计算字节偏移）�?/param>
    private readonly record struct LabelFixup(
        int fixup_byte_offset,
        int target_instruction_index,
        GenerateFunction function);
}
