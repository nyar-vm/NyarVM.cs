using System.Diagnostics;
using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Assembler.Backends.Clr;

/// <summary>
///     CLR 后��?partial：指令发射相关方法�。
/// </summary>
public partial class ClrBackend
{
    private const string _clr_string_adaptor_namespace = "std.adaptor.clr.string";
    private const string _clr_string_equals_binding = _clr_string_adaptor_namespace + ".utf8_equals";
    private const string _clr_string_concat_binding = _clr_string_adaptor_namespace + ".utf8_concat";
    private const string _clr_string_substring_binding = _clr_string_adaptor_namespace + ".utf8_substring";
    private const string _clr_string_length_binding = _clr_string_adaptor_namespace + ".utf8_length";

    /// <summary>
    ///     从 `set/get_offset_index` 的附加操作数解析数组元素类型。
    /// </summary>
    private static GenerateTypeReference get_array_index_element_type(GenerateInstruction instruction)
    {
        if (instruction.operands.Count > 0 &&
            instruction.operands[^1] is GenerateOperand.Str elementTypeName &&
            !string.IsNullOrWhiteSpace(elementTypeName.value))
            return GenerateTypeReference.parse(elementTypeName.value);

        return GenerateTypeReference.@object;
    }

    /// <summary>
    ///     根据数组元素类型选择 `ldelem.*` 指令。
    /// </summary>
    private static ClrOpcode get_array_load_opcode(GenerateTypeReference elementType)
    {
        return elementType.kind switch
        {
            GenerateTypeKind.i8 or GenerateTypeKind.u8 => ClrOpcode.ldelem_i1,
            GenerateTypeKind.@bool => ClrOpcode.ldelem_u1,
            GenerateTypeKind.i16 or GenerateTypeKind.u16 => ClrOpcode.ldelem_i2,
            GenerateTypeKind.@char => ClrOpcode.ldelem_u2,
            GenerateTypeKind.i32 or GenerateTypeKind.u32 => ClrOpcode.ldelem_i4,
            GenerateTypeKind.i64 or GenerateTypeKind.u64 => ClrOpcode.ldelem_i8,
            GenerateTypeKind.f32 => ClrOpcode.ldelem_r4,
            GenerateTypeKind.f64 => ClrOpcode.ldelem_r8,
            _ => ClrOpcode.ldelem_ref
        };
    }

    /// <summary>
    ///     根据数组元素类型选择 `stelem.*` 指令。
    /// </summary>
    private static ClrOpcode get_array_store_opcode(GenerateTypeReference elementType)
    {
        return elementType.kind switch
        {
            GenerateTypeKind.i8 or GenerateTypeKind.u8 or GenerateTypeKind.@bool => ClrOpcode.stelem_i1,
            GenerateTypeKind.i16 or GenerateTypeKind.u16 or GenerateTypeKind.@char => ClrOpcode.stelem_i2,
            GenerateTypeKind.i32 or GenerateTypeKind.u32 => ClrOpcode.stelem_i4,
            GenerateTypeKind.i64 or GenerateTypeKind.u64 => ClrOpcode.stelem_i8,
            GenerateTypeKind.f32 => ClrOpcode.stelem_r4,
            GenerateTypeKind.f64 => ClrOpcode.stelem_r8,
            _ => ClrOpcode.stelem_ref
        };
    }

    private static ClrMethodDef build_method(
        GenerateFunction function,
        GenerateModule module,
        IReadOnlyDictionary<string, uint> methodTokenMap,
        IReadOnlyDictionary<string, uint> externalRefTokenMap,
        IReadOnlySet<string> externalCtorFunctionNames,
        IReadOnlyDictionary<string, uint> typeRefTokenMap,
        IReadOnlyDictionary<string, uint> newObjectTokenMap,
        IDictionary<string, uint> fieldTokenMap,
        bool isEntryPoint,
        ICollection<string> userStrings,
        IDictionary<string, uint> userStringTokens,
        IReadOnlySet<string> arrayFieldKeys)
    {
        var instructions = new List<ClrInstruction>();
        // 诊断：dump LIR（与 WasmBackend/JvmBackend 一致的环境变量协议）
        // 环境变量 LEGION_SPY_LIR_DUMP=1 强制开启 dump
        // 环境变量 LEGION_SPY_LIR_FUNC=<func> 指定只 dump 该函数（函数名包含匹配）
        var spyDumpEnabled = Environment.GetEnvironmentVariable("LEGION_SPY_LIR_DUMP") == "1";
        var spyFuncFilter = Environment.GetEnvironmentVariable("LEGION_SPY_LIR_FUNC");
        var shouldDiag = spyDumpEnabled
            ? string.IsNullOrEmpty(spyFuncFilter) || function.name.Contains(spyFuncFilter)
            : false;
        if (shouldDiag)
        {
            Console.Error.WriteLine(
                $"\n=== LIR dump: {function.name} ({function.instructions.Count} instrs, return={function.return_type_ref}) ===");
            for (var di = 0; di < function.instructions.Count; di++)
            {
                var instr = function.instructions[di];
                var operands = string.Join(", ", instr.operands);
                Console.Error.WriteLine($"  [{di:D3}] {instr.head_code} {operands}");
            }

            Console.Error.WriteLine("=== labels ===");
            if (function.labels is not null)
                foreach (var label in function.labels)
                    Console.Error.WriteLine($"  label {label.name} -> instr {label.instruction_index}");
        }

        var traceThisMethod =
            string.Equals(function.name, "legion.emit_single_project_build", StringComparison.Ordinal);
        var traceExecutePack = string.Equals(function.name, "legion.execute_pack", StringComparison.Ordinal);
        var traceStopwatch = traceExecutePack || traceThisMethod ? Stopwatch.StartNew() : null;

        void TraceStage(string stageName)
        {
            if (traceStopwatch is null) return;
            Console.WriteLine(
                $"[ClrBackend] {function.name}::{stageName} at {traceStopwatch.ElapsedMilliseconds} ms, emitted={instructions.Count}");
        }

        if (function.name == "legion.legion_canonical_target")
        {
        }

        var sourceInstructionToEmittedIndex = new int[function.instructions.Count + 1];
        var pendingBranches = new List<PendingClrBranch>();
        var syntheticLabelOffsets = new Dictionary<string, int>();
        var usesBackwardField = false;
        var usesLengthTemp = false;
        // NyarVM 栈式调用约��?�?CLR 参数槽约定的桥接
        // 函数体前 N 条指令�果是从栈顶连�?store_local 0, 1, 2, ...
        // 则先生成对应�?ldarg 将参数从 CLR 参数槽加载到求�€�栈
        var prologueStoreCount = 0;
        for (var i = 0; i < Math.Min(function.parameters.Count, function.instructions.Count); i++)
        {
            var inst = function.instructions[i];
            if (inst is { opcode: NyarHeadCode.store_local, operands.Count: > 0 } &&
                inst.operands[0] is GenerateOperand.Local { index: var localIdx } &&
                localIdx == prologueStoreCount)
                prologueStoreCount++;
            else
                break;
        }

        for (var i = 0; i < prologueStoreCount; i++)
            instructions.Add(i switch
            {
                0 => new ClrInstruction { opcode = ClrOpcode.ldarg_0 },
                1 => new ClrInstruction { opcode = ClrOpcode.ldarg_1 },
                2 => new ClrInstruction { opcode = ClrOpcode.ldarg_2 },
                3 => new ClrInstruction { opcode = ClrOpcode.ldarg_3 },
                <= byte.MaxValue => new ClrInstruction
                    { opcode = ClrOpcode.ldarg_s, operand = new ClrArgumentIndexOperand { index = (uint)i } },
                _ => new ClrInstruction
                    { opcode = ClrOpcode.ldarg, operand = new ClrArgumentIndexOperand { index = (uint)i } }
            });
        string? pendingFieldName = null;
        var pendingFieldNameSourceIndex = -1;
        var typeContextStack = new Stack<(string typeName, int remainingFields)>();
        var valueTypeStack = new Stack<GenerateTypeReference>();
        // 追踪���过的 const Str 指令索引（作为字段名被 forward scan 跳过，未发射 ldstr。
        // 由 backward scan 判��?const Str ���已�跳过，避免错���入修复代。
        var skippedConstStrIndices = new HashSet<int>();
        // 追踪 void call 指令的发射索引，供 normalize_non_void_returns 使用
        var voidCallIndices = new HashSet<int>();
        // 追踪返回 object 元素类型的 call/callvirt 指令发射索引。
        // 供 fix_comparison_type_mismatch 识别 `call(object) + ldc_i4 + ceq/clt/cgt` 模式。
        var objectCallIndices = new HashSet<int>();
        // 追踪 call/callvirt 指令的参数弹出数量，供 normalize_void_returns_stack_depth 使用。
        var callPopCounts = new Dictionary<int, int>();
        // 预�测是否使用了 swap 指令，需要追加临时变量槽。
        // 必须在指令循���声明，因为 backward scan 分支�€要使用�标志
        var usesSwap = function.instructions.Any(inst => inst.opcode == NyarHeadCode.swap);
        // 预�测是否使用了 length 指令，需要追加临时变量槽。
        // 必须在指令循���声明，因为 backward scan 分支�€要使用�标志来�算����临时变量索引
        var usesLengthTempPreScan = function.instructions.Any(inst => inst.opcode == NyarHeadCode.length);
        for (var instructionIndex = 0; instructionIndex < function.instructions.Count; instructionIndex++)
        {
            var inst = function.instructions[instructionIndex];
            if (traceThisMethod && instructionIndex % 50 == 0)
                Console.WriteLine(
                    $"[ClrBackend] {function.name}::emit_instruction_loop idx={instructionIndex}/{function.instructions.Count} opcode={inst.opcode}");
            if (function.name == "legion.legion_canonical_target")
            {
            }

            // 追踪 new_object 类型名，使用栈式作用域支持嵌�?struct 构�。
            // 注意：当 pendingFieldName != null 时，�?new_object ���字�赋�€�创建的值，
            // 不是正在构�€�的外层对象，不应推�?typeContextStack。
            if (inst is { opcode: NyarHeadCode.new_object, operands.Count: > 1 } &&
                inst.operands[0] is GenerateOperand.Str newObjTypeOp &&
                inst.operands[1] is GenerateOperand.I32 newObjFieldCount)
            {
                if (pendingFieldName == null) typeContextStack.Push((newObjTypeOp.value, newObjFieldCount.value));
                valueTypeStack.Push(GenerateTypeReference.parse(newObjTypeOp.value));
            }

            // 追踪 load_local 的类型，以便后续 get_field/set_field 确定对象类型
            if (inst is { opcode: NyarHeadCode.load_local, operands.Count: > 0 } &&
                inst.operands[0] is GenerateOperand.Local localOp)
            {
                var localVar = function.local_variables.FirstOrDefault(l => l.index == localOp.index);
                if (localVar is { type_ref.is_empty: false })
                    valueTypeStack.Push(localVar.type_ref);
            }

            // 追踪 load_arg 的类型，以便后续 get_field/set_field ���对象类�。
            // 优先使用函数签名���参数类型（保留结构体类型名）。
            // 仅在签名类型不可用时回�€€�?LIR 的 GenerateValueType（可能�擦除�?external_ref）�。
            if (inst is { opcode: NyarHeadCode.load_arg, operands.Count: > 0 } &&
                inst.operands[0] is GenerateOperand.Param paramOp)
            {
                var paramIndex = paramOp.index;
                var signatureType = paramIndex < function.parameters.Count
                    ? function.parameters[paramIndex].type_ref
                    : null;
                if (function.name.Contains("legion_project_name"))
                {
                }

                if (signatureType != null && !signatureType.is_empty &&
                    !signatureType.is_erased_object_type)
                    valueTypeStack.Push(signatureType);
                else
                    valueTypeStack.Push(GenerateTypeReference.from_value_type(paramOp.type));
            }

            // 追踪 const Str 字�名：向后����它最终� set_field/get_field 消�。
            // 跳��?const Str 的发射，使其不推入求值栈，直接由 set_field/get_field 使�。
            // 注意：��?pendingFieldName 已����说明前一个 const Str 已�识别为字段名。
            // 当��?const Str ���段�€��€�非字�名，应�常发射��?ldstr
            if (inst is { opcode: NyarHeadCode.@const, operands.Count: > 0 } &&
                inst.operands[0] is GenerateOperand.Str strOp)
            {
                var isFieldName = pendingFieldName == null &&
                                  is_field_name_for_set_or_get_field(function, instructionIndex);
                if (function.name.Contains("legion"))
                {
                }

                if (isFieldName)
                {
                    pendingFieldName = strOp.value;
                    pendingFieldNameSourceIndex = instructionIndex;
                    skippedConstStrIndices.Add(instructionIndex);
                    continue;
                }
            }

            if (inst.opcode == NyarHeadCode.set_field)
            {
                if (function.name.Contains("legion"))
                {
                }

                // 优先使用 forward-scan 识别�?pendingFieldName（pattern A。
                // 如��?pendingFieldName 为 null，尝�?backward scan 回�€€（pattern B。
                var fieldName = pendingFieldName;
                var isForwardScan = true;
                if (fieldName == null)
                {
                    fieldName = find_field_name_backward(function, instructionIndex);
                    isForwardScan = false;
                }

                if (fieldName != null)
                {
                    if (function.name.Contains("legion"))
                    {
                    }

                    sourceInstructionToEmittedIndex[instructionIndex] = instructions.Count;
                    var currentType = typeContextStack.Count > 0
                        ? typeContextStack.Peek().typeName
                        : valueTypeStack.Count > 1
                            ? valueTypeStack.ElementAt(valueTypeStack.Count - 2).display_name
                            : null;
                    var qualifiedKey = build_field_qualified_key(currentType, fieldName, fieldTokenMap, function.name);
                    emit_set_field_stfld(instructions, qualifiedKey, fieldTokenMap, function.name);
                    // 判断����€�?backward scan �。
                    // - isForwardScan: pendingFieldName 存在，const Str 已�跳过，stfld 直接消费 (obj_ref, value)
                    // - !isForwardScan: pendingFieldName 为 null，backward scan 找到了字段�。
                    //   �€要��?const Str ������射��?ldstr。
                    //   如��?const Str 的索引与前一�?pendingFieldNameSourceIndex 相同。
                    //   说��?const Str ���过了，不�€要修改
                    if (isForwardScan)
                    {
                        // forward scan 模式：const Str 已�跳过，栈上只有 obj_ref �?value
                        // stfld 正好消费这两。
                    }
                    else
                    {
                        // backward scan 模式：需要确认 const Str ������射��?ldstr
                        // 查找字�名�应��?const Str 索�。
                        var backwardConstStrIdx = find_const_str_index_before_set_field(function, instructionIndex);
                        var constStrWasSkipped = backwardConstStrIdx >= 0 &&
                                                 (backwardConstStrIdx == pendingFieldNameSourceIndex ||
                                                  skippedConstStrIndices.Contains(backwardConstStrIdx));
                        if (constStrWasSkipped)
                        {
                            // const Str �?forward scan 跳过（未发射 ldstr）�。
                            // 如果 pendingFieldName ������令清除了。
                            // stfld 已经正确消费 (obj_ref, value)，无�€�。
                            if (function.name.Contains("legion"))
                            {
                            }
                        }
                        else
                        {
                            // const Str ���射为 ldstr，栈：(obj_ref, field_name, value)
                            // 已发射的 stfld 错�地消费了 field_name（作�?obj_ref）��?value。
                            // �€要修复：
                            //   1. stloc_s tmp     �?�?value 暂�。
                            //   2. pop             �?弹��?field_name 字�。
                            //   3. ldloc_s tmp     �?加��?value 回栈。
                            //   4. stfld           �?正确消费 (obj_ref, value)
                            // 临时变量索引计算。
                            //   - swap 占��?[n, n+1]（��?usesSwap。
                            //   - length 占��?n+2（��?usesLengthTempPreScan。
                            //   - backward field 占��?n+2+1（�果两者都����?n+1（�果只�?length）��?n+1（�果只�?swap）��?n（�果都没有。
                            var backwardTempIdx = swap_temp_index(function) +
                                                  (usesSwap ? 2 : 0) +
                                                  (usesLengthTempPreScan ? 1 : 0);
                            instructions.RemoveAt(instructions.Count - 1);
                            instructions.Add(new ClrInstruction
                            {
                                opcode = ClrOpcode.stloc_s,
                                operand = new ClrLocalIndexOperand { index = (uint)backwardTempIdx }
                            });
                            instructions.Add(new ClrInstruction { opcode = ClrOpcode.pop });
                            instructions.Add(new ClrInstruction
                            {
                                opcode = ClrOpcode.ldloc_s,
                                operand = new ClrLocalIndexOperand { index = (uint)backwardTempIdx }
                            });
                            emit_set_field_stfld(instructions, qualifiedKey, fieldTokenMap, function.name);
                            usesBackwardField = true;
                        }
                    }

                    pendingFieldName = null;
                    pendingFieldNameSourceIndex = -1;
                    // 递减当前类型的剩余字段数，归零则出栈
                    if (typeContextStack.Count > 0)
                    {
                        var (typeName, remaining) = typeContextStack.Pop();
                        remaining--;
                        if (remaining > 0) typeContextStack.Push((typeName, remaining));
                    }

                    // 更��?valueTypeStack：set_field 弹出值和对�。
                    if (valueTypeStack.Count > 0) valueTypeStack.Pop(); // value
                    if (valueTypeStack.Count > 0) valueTypeStack.Pop(); // object
                    continue;
                }

                // fallback: pendingFieldName 为 null，回�€�?emit_set_field（pop;pop;pop。
                if (function.name.Contains("legion"))
                {
                }
            }

            if (inst.opcode == NyarHeadCode.get_field && pendingFieldName != null)
            {
                sourceInstructionToEmittedIndex[instructionIndex] = instructions.Count;
                var currentType = typeContextStack.Count > 0
                    ? typeContextStack.Peek().typeName
                    : valueTypeStack.Count > 0
                        ? valueTypeStack.Peek().display_name
                        : null;
                var qualifiedKey = build_field_qualified_key(currentType, pendingFieldName, fieldTokenMap, function.name);
                emit_get_field_ldfld(instructions, qualifiedKey, fieldTokenMap, function.name);
                pendingFieldName = null;
                pendingFieldNameSourceIndex = -1;
                // 更��?valueTypeStack：get_field 弹出对�。
                if (valueTypeStack.Count > 0) valueTypeStack.Pop(); // object
                continue;
            }

            // �€�?const Null ���为空数��?literal（[]），如果 pendingFieldName 已����
            // 对应的字段是数组类型，则发��?newarr 替��?ldnull
            if (inst is { opcode: NyarHeadCode.@const, operands.Count: > 0 } &&
                inst.operands[0] is GenerateOperand.Null &&
                pendingFieldName != null)
            {
                var currentType = typeContextStack.Count > 0
                    ? typeContextStack.Peek().typeName
                    : valueTypeStack.Count > 1
                        ? valueTypeStack.ElementAt(valueTypeStack.Count - 2).display_name
                        : null;
                var qualifiedKey = build_field_qualified_key(currentType, pendingFieldName, fieldTokenMap, function.name);
                if (arrayFieldKeys.Contains(qualifiedKey))
                    // 空数�?literal：发�?newarr System.Object 替��?ldnull
                    if (typeRefTokenMap.TryGetValue("System.Object", out var objectToken))
                    {
                        sourceInstructionToEmittedIndex[instructionIndex] = instructions.Count;
                        // ldc.i4.0 推入数组长度 0
                        instructions.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                        instructions.Add(new ClrInstruction
                        {
                            opcode = ClrOpcode.newarr,
                            operand = new ClrTokenOperand { value = objectToken }
                        });
                        continue;
                    }
            }

            sourceInstructionToEmittedIndex[instructionIndex] = instructions.Count;
            if (function.name.Contains("legion") && inst.opcode == NyarHeadCode.@const)
            {
                var opStr = inst.operands.Count > 0 ? inst.operands[0].GetType().Name : "?";
            }

            emit_instruction(instructions, inst, function, module, methodTokenMap,
                externalRefTokenMap, externalCtorFunctionNames, newObjectTokenMap, typeRefTokenMap, fieldTokenMap,
                userStrings, userStringTokens, pendingBranches, syntheticLabelOffsets, instructionIndex, usesSwap,
                ref usesLengthTemp);
            // 防御性�测：如果刚发射的指令是 pop，且下一�?Nyar 指令。call_static �?store_local（需要栈值）。
            // 则��?pop 替换为 nop（�€�非移除），以保�?sourceInstructionToEmittedIndex 映射有效
            if (instructions.Count > 0 && instructions[^1].opcode == ClrOpcode.pop)
                if (has_next_consuming_instruction(function, instructionIndex))
                    instructions[^1] = new ClrInstruction { opcode = ClrOpcode.nop };
            // 追踪 void call 和参数数量：�?Nyar 指令发射为 CLR call/callvirt 时�。
            // 记录指令索引和参数弹出数量，供栈仿真精确计算栈深度�€?
            // 覆盖两�来源。
            //   1. NyarHeadCode.call / call_static（�€���?FuncRef 签名获取参数数量。
            //   2. 内置操作码（例如 utf8_concat、math_sin 等），通过 get_builtin_call_info 获取。
            if (instructions.Count > 0 &&
                instructions[^1].opcode is ClrOpcode.call or ClrOpcode.callvirt)
            {
                var emittedIdx = instructions.Count - 1;
                if ((inst.opcode == NyarHeadCode.call || inst.opcode == NyarHeadCode.call_static) &&
                    inst.operands.Count > 0 &&
                    inst.operands[0] is GenerateOperand.FuncRef funcRef)
                {
                    var paramCount = funcRef.signature.parameters.Count;
                    callPopCounts[emittedIdx] = paramCount;
                    if (funcRef.signature.results.Count == 0 ||
                        funcRef.signature.results[0] == GenerateValueType.@void ||
                        funcRef.signature.results[0] == GenerateValueType.unit)
                        voidCallIndices.Add(emittedIdx);
                    // 返回值为 object 元素类型的非 void 调用，记录到 objectCallIndices。
                    // 用于 fix_comparison_type_mismatch 在 `call(object) + ldc_i4 + ceq` 模式下插入 unbox.any。
                    else if (is_object_element_value_type(funcRef.signature.results[0]))
                        objectCallIndices.Add(emittedIdx);
                }
                else
                {
                    var (popCount, isVoid) = get_builtin_call_info(inst.opcode);
                    if (popCount >= 0)
                    {
                        callPopCounts[emittedIdx] = popCount;
                        if (isVoid) voidCallIndices.Add(emittedIdx);
                    }
                }
            }

            // 在 void 方法���如果�?void 调用的返回�€�未����€�?Nyar 指令消费，自动插�?pop。
            // NyarVM 栈：IR 允�函数返回时整������弃，�?CLR 验证器��?void 方��?ret 时栈深度为 0。
            if (is_void_return_type(function.return_type_ref) &&
                (inst.opcode == NyarHeadCode.call || inst.opcode == NyarHeadCode.call_static) &&
                inst.operands.Count > 0 &&
                inst.operands[0] is GenerateOperand.FuncRef { signature.results.Count: > 0 } nonVoidRef &&
                nonVoidRef.signature.results[0] != GenerateValueType.@void &&
                nonVoidRef.signature.results[0] != GenerateValueType.unit &&
                !next_nyar_instruction_consumes_value(function, instructionIndex))
                instructions.Add(new ClrInstruction { opcode = ClrOpcode.pop });
        }

        TraceStage("emit_instruction_loop");
        if (instructions.Count == 0 || instructions[^1].opcode != ClrOpcode.ret)
            emit_default_return(instructions, function.return_type_ref);
        TraceStage("emit_default_return");
        // 规范�?void 返回：void/unit 方法 ret 前求值栈必须为空。
        // 从后向前����€�?ret 指令（包������的早期返回），将每��?ret 前的默�值推入指令替��� nop。
        // 对于"默�值推�?+ pop"对，两�€�都替换为 nop（推入和消费互相抵消）�€?
        // 对于单独�?pop（消费非默�值指令的结果，��?call），保留 pop 以维持栈平衡。
        // 使��?nop 替换而非 RemoveAt，以保持 sourceInstructionToEmittedIndex 分支映射有效。
        if (is_void_return_type(function.return_type_ref))
            for (var i = instructions.Count - 1; i >= 0; i--)
            {
                if (instructions[i].opcode != ClrOpcode.ret) continue;
                // 从��?ret 向前���，清理默认�€�推入和与之配��?pop
                while (i > 0)
                {
                    var before = instructions[i - 1];
                    if (is_default_value_push(before))
                    {
                        // 默�值推入：直接替换为 nop
                        instructions[i - 1] = new ClrInstruction { opcode = ClrOpcode.nop };
                        i--;
                    }
                    else if (before.opcode == ClrOpcode.pop && i >= 2 &&
                             is_default_value_push(instructions[i - 2]))
                    {
                        // pop 消费的是默�值推入：将两者都替换为 nop
                        instructions[i - 1] = new ClrInstruction { opcode = ClrOpcode.nop };
                        instructions[i - 2] = new ClrInstruction { opcode = ClrOpcode.nop };
                        i -= 2;
                    }
                    else
                    {
                        // 其他指令（��?call + pop）：保留以维持栈平衡
                        break;
                    }
                }
            }

        TraceStage("normalize_void_ret_prefix");
        // 清��?void 方法��� ldnull; pop; br/brfalse/brtrue 冗余模式
        // match 分支降级时会在分���生��?ldnull; pop，这些指令�栈无影响但属于冗。
        if (is_void_return_type(function.return_type_ref))
            clean_void_branch_nops(instructions);
        else
            // 在 void 方法规范化：���每��?ret 前求值栈有�€��。
            // 如果 infer_return_type ����?any 时，方法签名返回 object。
            // 但部分分����?match arm 调��?void 函数后）�?ret 前栈为空。
            // 导��?CLR 验证失败（InvalidProgramException）�。
            // �?ret 前��?nop 替换�?ldnull，确保栈上有返回值�。
            normalize_non_void_returns(instructions, voidCallIndices, pendingBranches);
        TraceStage("normalize_returns");
        var signatureReturnType = function.return_type_ref;
        if (isEntryPoint && !is_valid_clr_entry_return_type(function.return_type_ref))
        {
            signatureReturnType = GenerateTypeReference.i32;
            normalize_entry_point_return(instructions);
        }

        // void 方法栈深度�范化：确保每�?ret 前求值栈为空。
        // NyarVM 栈：IR 允�函数调用返回值滞留在栈上（函数返回时整个栈�丢弃），
        // �?CLR 验证器严格��?void 方法 ret 时栈深度为 0。
        // 主��已在指令发射������成（�?void 调用后自动插�?pop）�。
        // 此�作为安全网，仅�理指令循���覆盖的边缘情况�€?
        // 注意：线性栈模拟 call/callvirt 的栈效果估算不准���不扣除参数弹出）。
        // 因�仅在深度明显异常时才插��?pop，避免过度插入�致栈下溢。
        if (is_void_return_type(function.return_type_ref))
            normalize_void_returns_stack_depth(instructions, sourceInstructionToEmittedIndex, pendingBranches,
                voidCallIndices, callPopCounts, function);
        TraceStage("normalize_void_stack_depth");
        // �比较指令的类型不匹配：��?ldloc(object) + ldc_i4 + ceq 模式出现时，
        // �?ldloc 后插�?unbox.any System.Int32 �?object 拆��?i32
        var orderedLocals = function.local_variables
            .OrderBy(local => local.index)
            .ToList();
        var localTypeRefs = orderedLocals
            .Select(local => local.type_ref)
            .ToList();
        var localVariableNames = orderedLocals
            .Select(local => string.IsNullOrWhiteSpace(local.name) ? $"local_{local.index}" : local.name)
            .ToList();
        fix_comparison_type_mismatch(instructions, localTypeRefs, typeRefTokenMap,
            sourceInstructionToEmittedIndex, pendingBranches, objectCallIndices);
        TraceStage("fix_comparison_type_mismatch");
        sourceInstructionToEmittedIndex[^1] = instructions.Count;
        patch_branch_targets(function, instructions, sourceInstructionToEmittedIndex, pendingBranches,
            syntheticLabelOffsets);
        TraceStage("patch_branch_targets");
        // �€测是否使用��?swap 指令，需要追加临时变量槽。
        if (usesSwap)
        {
            // swap �€要两�?object 类型的临时变。
            localTypeRefs.Add(GenerateTypeReference.@object);
            localVariableNames.Add("__swap_left");
            localTypeRefs.Add(GenerateTypeReference.@object);
            localVariableNames.Add("__swap_right");
        }

        // 临时变量顺序：swap、length �?backward field
        // 必须与发射阶段�算的索引�€。
        if (usesLengthTemp)
            // length 指令�€�?object 类型的临时变量保存�象引。
            localTypeRefs.Add(GenerateTypeReference.@object);
        if (usesLengthTemp)
            localVariableNames.Add("__length_tmp");
        if (usesBackwardField)
            // backward scan 字�写入�€要一�?object 类型的临时变。
            localTypeRefs.Add(GenerateTypeReference.@object);
        if (usesBackwardField)
            localVariableNames.Add("__field_tmp");
        var clrInstructions = assign_offsets(instructions);
        return new ClrMethodDef
        {
            name = function.name,
            signature = build_method_signature(function, module, signatureReturnType),
            flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static | ClrMethodAttributes.hide_by_sig,
            max_stack = compute_max_stack(instructions),
            local_variable_types = [.. localTypeRefs.Select(type => type.display_name)],
            local_variable_names = localVariableNames,
            init_locals = localTypeRefs.Count > 0,
            instructions = clrInstructions
        };
    }

    private static byte[] build_method_signature(
        GenerateFunction function,
        GenerateModule module,
        GenerateTypeReference returnType)
    {
        var parameterCount = function.parameters.Count;
        var result = new List<byte>(4 + parameterCount)
        {
            0x00 // DEFAULT calling convention
        };
        append_compressed_unsigned(result, (uint)parameterCount);
        result.Add(map_element_type(resolve_clr_signature_type_name(module, returnType)));
        foreach (var parameter in function.parameters)
            result.Add(map_element_type(resolve_clr_signature_type_name(module, parameter.type_ref)));
        return [.. result];
    }

    private static bool is_valid_clr_entry_return_type(GenerateTypeReference returnType)
    {
        return returnType.kind is GenerateTypeKind.@void or GenerateTypeKind.unit or GenerateTypeKind.i32;
    }

    private static bool is_object_element_value_type(GenerateValueType valueType)
    {
        return valueType is not (GenerateValueType.@void or GenerateValueType.unit
            or GenerateValueType.@bool or GenerateValueType.@char or GenerateValueType.i8
            or GenerateValueType.i16 or GenerateValueType.i32 or GenerateValueType.i64
            or GenerateValueType.i128 or GenerateValueType.f32 or GenerateValueType.f64
            or GenerateValueType.utf8 or GenerateValueType.utf16);
    }

    /// <summary>
    ///     判断返回类型����?void/unit（无返回值�。
    /// </summary>
    private static bool is_void_return_type(GenerateTypeReference returnType)
    {
        return returnType.is_void_like;
    }

    private static void normalize_entry_point_return(List<ClrInstruction> instructions)
    {
        for (var i = 0; i < instructions.Count; i++)
            if (instructions[i].opcode == ClrOpcode.ret)
            {
                instructions.Insert(i, new ClrInstruction { opcode = ClrOpcode.conv_i4 });
                i++;
            }
    }

    private static void emit_entry_wrapper_default_argument(ICollection<ClrInstruction> instructions,
        GenerateTypeReference parameterType)
    {
        switch (parameterType.kind)
        {
            case GenerateTypeKind.i64:
                instructions.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.ldc_i8,
                    operand = new ClrInt64Operand { value = 0 }
                });
                return;
            case GenerateTypeKind.f32:
                instructions.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.ldc_r4,
                    operand = new ClrFloat32Operand { value = 0f }
                });
                return;
            case GenerateTypeKind.f64:
                instructions.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.ldc_r8,
                    operand = new ClrFloat64Operand { value = 0d }
                });
                return;
            case GenerateTypeKind.utf8:
            case GenerateTypeKind.utf16:
            case GenerateTypeKind.@object:
            case GenerateTypeKind.any:
            case GenerateTypeKind.external_ref:
                instructions.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
                return;
            default:
                // 数组类型（� [utf8]、[i32]）和引用类型都应推��?ldnull
                // �€测方���前缀表示数组类型，下划线前缀表示用户���义类。
                if (parameterType.defaults_to_null_reference)
                {
                    instructions.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
                    return;
                }

                instructions.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                return;
        }
    }

    private static void append_entry_wrapper_return_normalization(List<ClrInstruction> instructions,
        GenerateTypeReference returnType)
    {
        switch (returnType.kind)
        {
            case GenerateTypeKind.@void:
            case GenerateTypeKind.unit:
            case GenerateTypeKind.i32:
                return;
            case GenerateTypeKind.utf8:
            case GenerateTypeKind.utf16:
            case GenerateTypeKind.@object:
            case GenerateTypeKind.any:
            case GenerateTypeKind.external_ref:
                instructions.Add(new ClrInstruction { opcode = ClrOpcode.pop });
                instructions.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                return;
            default:
                instructions.Add(new ClrInstruction { opcode = ClrOpcode.conv_i4 });
                return;
        }
    }

    private static byte map_element_type(GenerateTypeReference typeName)
    {
        return typeName.kind switch
        {
            GenerateTypeKind.@void => 0x01,
            GenerateTypeKind.unit => 0x01,
            GenerateTypeKind.@bool => 0x02,
            GenerateTypeKind.i8 => 0x04,
            GenerateTypeKind.i16 => 0x06,
            GenerateTypeKind.i32 => 0x08,
            GenerateTypeKind.i64 => 0x0A,
            GenerateTypeKind.f32 => 0x0C,
            GenerateTypeKind.f64 => 0x0D,
            GenerateTypeKind.utf8 => 0x0E,
            GenerateTypeKind.utf16 => 0x0E,
            _ => 0x1C // object
        };
    }

    private static void append_compressed_unsigned(ICollection<byte> buffer, uint value)
    {
        if (value <= 0x7F)
        {
            buffer.Add((byte)value);
            return;
        }

        if (value <= 0x3FFF)
        {
            buffer.Add((byte)((value >> 8) | 0x80));
            buffer.Add((byte)(value & 0xFF));
            return;
        }

        buffer.Add((byte)((value >> 24) | 0xC0));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)(value & 0xFF));
    }

    private static IReadOnlyList<ClrInstruction> assign_offsets(List<ClrInstruction> instructions)
    {
        var result = new List<ClrInstruction>(instructions.Count);
        uint offset = 0;
        foreach (var instruction in instructions)
        {
            result.Add(new ClrInstruction
            {
                offset = offset,
                opcode = instruction.opcode,
                operand = instruction.operand
            });
            offset += (uint)get_instruction_size(instruction);
        }

        return result;
    }

    private static void emit_instruction(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        GenerateFunction function,
        GenerateModule module,
        IReadOnlyDictionary<string, uint> methodTokenMap,
        IReadOnlyDictionary<string, uint> externalRefTokenMap,
        IReadOnlySet<string> externalCtorFunctionNames,
        IReadOnlyDictionary<string, uint> newObjectTokenMap,
        IReadOnlyDictionary<string, uint> typeRefTokenMap,
        IDictionary<string, uint> fieldTokenMap,
        ICollection<string> userStrings,
        IDictionary<string, uint> userStringTokens,
        ICollection<PendingClrBranch> pendingBranches,
        IDictionary<string, int> syntheticLabelOffsets,
        int instructionIndex,
        bool usesSwap,
        ref bool usesLengthTemp)
    {
        switch (instruction.opcode)
        {
            case NyarHeadCode.nop:
                output.Add(new ClrInstruction { opcode = ClrOpcode.nop });
                return;
            case NyarHeadCode.pop:
                output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
                return;
            case NyarHeadCode.dup:
                output.Add(new ClrInstruction { opcode = ClrOpcode.dup });
                return;
            case NyarHeadCode.swap:
                // CLR 无直�?swap 指令，�€�过�€部变量模拟：
                // 栈：value_a, value_b) → (value_b, value_a)
                // stloc swap_tmp_b  // 弹��?value_b 存入临时变�。
                // stloc swap_tmp_a  // 弹��?value_a 存入临时变�。
                // ldloc swap_tmp_b  // 推��?value_b（现在在栈底。
                // ldloc swap_tmp_a  // 推��?value_a（现在在栈顶。
                // 注意：swap 使用的临时变量索引需要超出函数声明的�€部变量范。
                output.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.stloc,
                    operand = new ClrLocalIndexOperand { index = (uint)swap_temp_index(function) }
                });
                output.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.stloc,
                    operand = new ClrLocalIndexOperand { index = (uint)(swap_temp_index(function) + 1) }
                });
                output.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.ldloc,
                    operand = new ClrLocalIndexOperand { index = (uint)swap_temp_index(function) }
                });
                output.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.ldloc,
                    operand = new ClrLocalIndexOperand { index = (uint)(swap_temp_index(function) + 1) }
                });
                return;
            case NyarHeadCode.load_arg:
                emit_load_arg(output, instruction);
                return;
            case NyarHeadCode.store_arg:
                emit_store_arg(output, instruction);
                return;
            case NyarHeadCode.load_local:
                emit_load_local(output, instruction);
                return;
            case NyarHeadCode.store_local:
                emit_store_local(output, instruction);
                return;
            case NyarHeadCode.@const:
                emit_const(output, instruction.operands[0], module, userStrings, userStringTokens);
                return;
            case NyarHeadCode.i32_add:
            case NyarHeadCode.i64_add:
            case NyarHeadCode.f32_add:
            case NyarHeadCode.f64_add:
                output.Add(new ClrInstruction { opcode = ClrOpcode.add });
                return;
            case NyarHeadCode.i32_sub:
            case NyarHeadCode.i64_sub:
            case NyarHeadCode.f32_sub:
            case NyarHeadCode.f64_sub:
                output.Add(new ClrInstruction { opcode = ClrOpcode.sub });
                return;
            case NyarHeadCode.i32_mul:
            case NyarHeadCode.i64_mul:
            case NyarHeadCode.f32_mul:
            case NyarHeadCode.f64_mul:
                output.Add(new ClrInstruction { opcode = ClrOpcode.mul });
                return;
            case NyarHeadCode.i32_div_s:
            case NyarHeadCode.i32_div_u:
            case NyarHeadCode.i64_div_s:
            case NyarHeadCode.i64_div_u:
            case NyarHeadCode.f32_div:
            case NyarHeadCode.f64_div:
                output.Add(new ClrInstruction { opcode = ClrOpcode.div });
                return;
            case NyarHeadCode.i32_rem_s:
            case NyarHeadCode.i32_rem_u:
            case NyarHeadCode.i64_rem_s:
            case NyarHeadCode.i64_rem_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.rem });
                return;
            case NyarHeadCode.i32_neg:
            case NyarHeadCode.i64_neg:
            case NyarHeadCode.f32_neg:
            case NyarHeadCode.f64_neg:
                output.Add(new ClrInstruction { opcode = ClrOpcode.neg });
                return;
            case NyarHeadCode.f64_eq:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.f64_ne:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.f64_lt:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                return;
            case NyarHeadCode.f64_gt:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                return;
            case NyarHeadCode.f64_le:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.f64_ge:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.f64_sqrt:
                // CLR 无直接 `sqrt` 指令，这里只走源码中显式声明的 `math_sqrt` 绑定。
                if (try_get_external_ref_token(externalRefTokenMap, "math_sqrt", out var sqrtToken))
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.call,
                        operand = new ClrTokenOperand { value = sqrtToken }
                    });
                else
                    output.Add(new ClrInstruction
                        { opcode = ClrOpcode.ldc_r8, operand = new ClrFloat64Operand { value = 0 } });
                return;
            case NyarHeadCode.i32_and:
            case NyarHeadCode.i64_and:
                output.Add(new ClrInstruction { opcode = ClrOpcode.and });
                return;
            case NyarHeadCode.i32_or:
            case NyarHeadCode.i64_or:
                output.Add(new ClrInstruction { opcode = ClrOpcode.or });
                return;
            case NyarHeadCode.i32_xor:
            case NyarHeadCode.i64_xor:
                output.Add(new ClrInstruction { opcode = ClrOpcode.xor });
                return;
            case NyarHeadCode.i32_shl:
            case NyarHeadCode.i64_shl:
                output.Add(new ClrInstruction { opcode = ClrOpcode.shl });
                return;
            case NyarHeadCode.i32_shr_s:
            case NyarHeadCode.i64_shr_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.shr });
                return;
            case NyarHeadCode.i32_shr_u:
            case NyarHeadCode.i64_shr_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.shr_un });
                return;
            case NyarHeadCode.i32_not:
            case NyarHeadCode.i64_not:
                output.Add(new ClrInstruction { opcode = ClrOpcode.not });
                return;
            case NyarHeadCode.i32_extend_i64_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.conv_i8 });
                return;
            case NyarHeadCode.any_to_i32:
            {
                // 将栈上的 any（object）拆�?����?i32。
                // 发射：unbox.any System.Int32
                if (!typeRefTokenMap.TryGetValue("System.Int32", out var int32Token))
                    throw new InvalidOperationException("CLR 后�����?System.Int32 类型引用");
                output.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.unbox_any,
                    operand = new ClrTokenOperand { value = int32Token }
                });
                return;
            }
            case NyarHeadCode.any_to_utf8:
            {
                // 将栈上的 any（object）转��� utf8（string）�。
                // 发射：castclass System.String
                if (!typeRefTokenMap.TryGetValue("System.String", out var stringToken))
                    throw new InvalidOperationException("CLR 后�����?System.String 类型引用");
                output.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.castclass,
                    operand = new ClrTokenOperand { value = stringToken }
                });
                return;
            }
            case NyarHeadCode.i64_trunc_i32_s:
            case NyarHeadCode.f64_to_i32:
                output.Add(new ClrInstruction { opcode = ClrOpcode.conv_i4 });
                return;
            case NyarHeadCode.i32_to_f32_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.conv_r4 });
                return;
            case NyarHeadCode.i32_to_f64_s:
            case NyarHeadCode.i64_to_f64:
                output.Add(new ClrInstruction { opcode = ClrOpcode.conv_r8 });
                return;
            case NyarHeadCode.f64_to_i64:
                output.Add(new ClrInstruction { opcode = ClrOpcode.conv_i8 });
                return;
            case NyarHeadCode.i32_eq:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i32_ne:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i32_lt_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                return;
            case NyarHeadCode.i32_gt_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                return;
            case NyarHeadCode.i32_le_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i32_ge_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i32_lt_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt_un });
                return;
            case NyarHeadCode.i32_gt_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt_un });
                return;
            case NyarHeadCode.i32_le_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt_un });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i32_ge_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt_un });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i64_eq:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i64_ne:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i64_lt_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                return;
            case NyarHeadCode.i64_gt_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                return;
            case NyarHeadCode.i64_le_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i64_ge_s:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i64_lt_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt_un });
                return;
            case NyarHeadCode.i64_le_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt_un });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.i64_gt_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt_un });
                return;
            case NyarHeadCode.i64_ge_u:
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt_un });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.ref_eq:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.ref_ne:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                if (function.name == "legion.legion_canonical_target")
                {
                    var fr = instruction.operands.Count > 0 ? instruction.operands[0] : null;
                }

                emit_call(output, instruction, methodTokenMap, externalRefTokenMap, externalCtorFunctionNames);
                return;
            case NyarHeadCode.call_dynamic:
                emit_call_dynamic(output, instruction, methodTokenMap);
                return;
            case NyarHeadCode.new_object:
                emit_new_object(output, instruction, newObjectTokenMap, typeRefTokenMap);
                return;
            case NyarHeadCode.get_field:
                emit_get_field(output, instruction, fieldTokenMap);
                return;
            case NyarHeadCode.set_field:
                if (function.name.Contains("legion"))
                {
                }

                emit_set_field(output, instruction, fieldTokenMap);
                return;
            case NyarHeadCode.jump:
                emit_branch(output, instruction, pendingBranches, ClrOpcode.br);
                return;
            case NyarHeadCode.jump_if_true:
                emit_branch(output, instruction, pendingBranches, ClrOpcode.brtrue);
                return;
            case NyarHeadCode.jump_if_false:
                emit_branch(output, instruction, pendingBranches, ClrOpcode.brfalse);
                return;
            case NyarHeadCode.@return:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ret });
                return;
            case NyarHeadCode.enter_effect_handler:
            case NyarHeadCode.enter_try:
                output.Add(new ClrInstruction { opcode = ClrOpcode.nop });
                return;
            case NyarHeadCode.exit_effect_handler:
            case NyarHeadCode.exit_try:
                output.Add(new ClrInstruction { opcode = ClrOpcode.nop });
                return;
            case NyarHeadCode.perform_effect:
                output.Add(new ClrInstruction { opcode = ClrOpcode.@throw });
                return;
            case NyarHeadCode.resume:
                output.Add(new ClrInstruction { opcode = ClrOpcode.nop });
                return;
            case NyarHeadCode.length:
                // 栈：object_ref) → (i32)
                // `length` 在 CLR 后端仅为字符串和数组提供通用分派。
                // `List<T>.get_Count()` 必须通过源文件中的显式 `[clr]` 绑定调用，不再走后端偷偷补票。
                // 这里使用局部变量暂存对象引用，避免 `dup + isinst + brfalse` 导致栈不平衡。
                if (typeRefTokenMap.TryGetValue("System.String", out var strTypeToken) &&
                    try_get_external_ref_token(externalRefTokenMap, _clr_string_length_binding, out var strLenToken) &&
                    typeRefTokenMap.TryGetValue("System.Array", out var arrayTypeToken))
                {
                    usesLengthTemp = true;
                    // 临时变量索引计算。
                    //   - swap 占��?[n, n+1]（��?usesSwap。
                    //   - length 占��?n+2（��?usesSwap）��?n（��?!usesSwap。
                    var tempIdx = swap_temp_index(function) + (usesSwap ? 2 : 0);
                    // 创建合成标�名，用��?patch_branch_targets 解析分支�。
                    var arrayPathLabel = $"__synthetic_length_array_{instructionIndex}";
                    var fallbackLabel = $"__synthetic_length_fallback_{instructionIndex}";
                    var endLabel = $"__synthetic_length_end_{instructionIndex}";
                    // stloc_s tmp; ldloc_s tmp; isinst System.String; brfalse ARRAY_PATH
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.stloc_s,
                        operand = new ClrLocalIndexOperand { index = (uint)tempIdx }
                    });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.ldloc_s,
                        operand = new ClrLocalIndexOperand { index = (uint)tempIdx }
                    });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.isinst,
                        operand = new ClrTokenOperand { value = strTypeToken }
                    });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.brfalse,
                        operand = new ClrBranchTarget32Operand { offset = 0 }
                    });
                    pendingBranches.Add(new PendingClrBranch(output.Count - 1, arrayPathLabel));
                    // STRING_PATH: ldloc_s tmp; callvirt String.get_Length(); br END
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.ldloc_s,
                        operand = new ClrLocalIndexOperand { index = (uint)tempIdx }
                    });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.callvirt,
                        operand = new ClrTokenOperand { value = strLenToken }
                    });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.br,
                        operand = new ClrBranchTarget32Operand { offset = 0 }
                    });
                    pendingBranches.Add(new PendingClrBranch(output.Count - 1, endLabel));
                    // ARRAY_PATH: ldloc_s tmp; isinst System.Array; brfalse FALLBACK_PATH
                    syntheticLabelOffsets[arrayPathLabel] = output.Count;
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.ldloc_s,
                        operand = new ClrLocalIndexOperand { index = (uint)tempIdx }
                    });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.isinst,
                        operand = new ClrTokenOperand { value = arrayTypeToken }
                    });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.brfalse,
                        operand = new ClrBranchTarget32Operand { offset = 0 }
                    });
                    pendingBranches.Add(new PendingClrBranch(output.Count - 1, fallbackLabel));
                    // ARRAY_PATH: ldloc_s tmp; ldlen; conv_i4; br END
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.ldloc_s,
                        operand = new ClrLocalIndexOperand { index = (uint)tempIdx }
                    });
                    output.Add(new ClrInstruction { opcode = ClrOpcode.ldlen });
                    output.Add(new ClrInstruction { opcode = ClrOpcode.conv_i4 });
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.br,
                        operand = new ClrBranchTarget32Operand { offset = 0 }
                    });
                    pendingBranches.Add(new PendingClrBranch(output.Count - 1, endLabel));
                    // FALLBACK_PATH: 对非字符串/非数组值返回 0，保证栈形状稳定
                    syntheticLabelOffsets[fallbackLabel] = output.Count;
                    output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                    // END
                    syntheticLabelOffsets[endLabel] = output.Count;
                    return;
                }

                // 退化路径：当缺少 `System.String` 或 `System.Array` 的类型引用时，保留原始数组长度实现。
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldlen });
                output.Add(new ClrInstruction { opcode = ClrOpcode.conv_i4 });
                return;
            case NyarHeadCode.get_ordinal_index:
            case NyarHeadCode.get_offset_index:
                // 栈：array_ref, index_i32) → (element_ref)
                output.Add(new ClrInstruction
                {
                    opcode = get_array_load_opcode(get_array_index_element_type(instruction))
                });
                return;
            case NyarHeadCode.set_ordinal_index:
            case NyarHeadCode.set_offset_index:
            case NyarHeadCode.index_store:
                // 栈：array_ref, index_i32, value_ref) → ()
                output.Add(new ClrInstruction
                {
                    opcode = get_array_store_opcode(get_array_index_element_type(instruction))
                });
                return;
            case NyarHeadCode.utf8_eq:
                emit_explicit_external_call(
                    output,
                    externalRefTokenMap,
                    _clr_string_equals_binding,
                    ClrOpcode.call,
                    2,
                    GenerateValueType.@bool);
                return;
            case NyarHeadCode.utf8_ne:
                emit_explicit_external_call(
                    output,
                    externalRefTokenMap,
                    _clr_string_equals_binding,
                    ClrOpcode.call,
                    2,
                    GenerateValueType.@bool);
                // �?Equals 结果取反
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return;
            case NyarHeadCode.utf8_concat:
                emit_explicit_external_call(
                    output,
                    externalRefTokenMap,
                    _clr_string_concat_binding,
                    ClrOpcode.call,
                    2,
                    GenerateValueType.utf8);
                return;
            case NyarHeadCode.utf8_len_bytes:
            case NyarHeadCode.utf8_len_chars:
                // CLR �?string length �?UTF-16 字�数，�?Nyar ����€。
                emit_explicit_external_call(
                    output,
                    externalRefTokenMap,
                    _clr_string_length_binding,
                    ClrOpcode.callvirt,
                    1,
                    GenerateValueType.i32);
                return;
            case NyarHeadCode.utf8_substr:
                // 栈：string_ref, start_i32, length_i32) → (string_result)
                emit_explicit_external_call(
                    output,
                    externalRefTokenMap,
                    _clr_string_substring_binding,
                    ClrOpcode.callvirt,
                    3,
                    GenerateValueType.utf8);
                return;
            case NyarHeadCode.array_push:
                // 栈：list_ref, value_ref) → ()
                // 这里只走源码中的显式 CLR 绑定，不再偷塞 `List<T>.Add` 的元数据。
                if (try_get_external_ref_token(externalRefTokenMap, "__array_list_clr_add", out var addToken))
                {
                    output.Add(new ClrInstruction
                    {
                        opcode = ClrOpcode.callvirt,
                        operand = new ClrTokenOperand { value = addToken }
                    });
                }
                else
                {
                    output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
                    output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
                }

                return;
            case NyarHeadCode.i32_load:
                // 栈：address_i) → (i32)
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldind_i4 });
                return;
            case NyarHeadCode.i32_store:
                // 栈：address_i, value_i32) → ()
                output.Add(new ClrInstruction { opcode = ClrOpcode.stind_i4 });
                return;
            case NyarHeadCode.i64_load:
                // 栈：address_i) → (i64)
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldind_i8 });
                return;
            case NyarHeadCode.i64_store:
                // 栈：address_i, value_i64) → ()
                output.Add(new ClrInstruction { opcode = ClrOpcode.stind_i8 });
                return;
            case NyarHeadCode.load_global:
                emit_load_global(output, instruction, module, fieldTokenMap);
                return;
            case NyarHeadCode.store_global:
                emit_store_global(output, instruction, module, fieldTokenMap);
                return;
            case NyarHeadCode.new_closure:
                throw new NotSupportedException(
                    $"CLR ����ݲ�֧�ֲ����?`{instruction.opcode}`���հ�������δʵ��。");
            case NyarHeadCode.get_upvalue:
                throw new NotSupportedException(
                    $"CLR ����ݲ�֧�ֲ����?`{instruction.opcode}`����ֵ��ȡ��δʵ��。");
            case NyarHeadCode.set_upvalue:
                throw new NotSupportedException(
                    $"CLR ����ݲ�֧�ֲ����?`{instruction.opcode}`����ֵд����δʵ��。");
            case NyarHeadCode.field_store:
                // �?set_field 处�。
                emit_set_field(output, instruction, fieldTokenMap);
                return;
            default:
                throw new InvalidOperationException(
                    $"CLR ����ݲ�֧�ֲ����?`{instruction.opcode}`�����?`{function.name}` �д���δ����ָ��。");
        }
    }

    private static void emit_branch(ICollection<ClrInstruction> output, GenerateInstruction instruction,
        ICollection<PendingClrBranch> pendingBranches, ClrOpcode opcode)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Label label)
            throw new InvalidOperationException($"CLR ��ָ֧�� `{instruction.opcode}` ȱ�ٱ�ǩ������。");
        var emittedInstructionIndex = output.Count;
        output.Add(new ClrInstruction
        {
            opcode = opcode,
            operand = new ClrBranchTarget32Operand { offset = 0 }
        });
        pendingBranches.Add(new PendingClrBranch(emittedInstructionIndex, label.name));
    }

    private static void patch_branch_targets(GenerateFunction function, IList<ClrInstruction> instructions,
        IReadOnlyList<int> sourceInstructionToEmittedIndex, IReadOnlyList<PendingClrBranch> pendingBranches,
        IReadOnlyDictionary<string, int> syntheticLabelOffsets)
    {
        if (pendingBranches.Count == 0) return;
        var instructionOffsets = compute_instruction_offsets(instructions);
        var labelOffsets = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var label in function.labels)
        {
            if (label.instruction_index < 0 || label.instruction_index >= sourceInstructionToEmittedIndex.Count)
                throw new InvalidOperationException(
                    $"CLR ��ǩ `{label.name}` ��ָ������ `{label.instruction_index}` �������� `{function.name}` ��Χ��");
            var emittedInstructionIndex = sourceInstructionToEmittedIndex[label.instruction_index];
            labelOffsets[label.name] = instructionOffsets[emittedInstructionIndex];
        }

        foreach (var branch in pendingBranches)
        {
            // 先查正式标�，再查合成标签（�?length 指令降级动�€�生成的标�。
            if (!labelOffsets.TryGetValue(branch.label_name, out var targetOffset) &&
                !syntheticLabelOffsets.TryGetValue(branch.label_name, out targetOffset))
                throw new InvalidOperationException($"CLR ��֧Ŀ���ǩ `{branch.label_name}` δ���塣");
            // 合成标�的偏移是 instruction index，需要转�?byte offset
            if (syntheticLabelOffsets.ContainsKey(branch.label_name)) targetOffset = instructionOffsets[targetOffset];
            instructions[branch.emitted_instruction_index] = new ClrInstruction
            {
                opcode = instructions[branch.emitted_instruction_index].opcode,
                operand = new ClrBranchTarget32Operand { offset = targetOffset }
            };
        }
    }

    private static int[] compute_instruction_offsets(IList<ClrInstruction> instructions)
    {
        var offsets = new int[instructions.Count + 1];
        var offset = 0;
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            offsets[instructionIndex] = offset;
            offset += get_instruction_size(instructions[instructionIndex]);
        }

        offsets[^1] = offset;
        return offsets;
    }

    private static int get_instruction_size(ClrInstruction instruction)
    {
        return get_opcode_size(instruction.opcode) + get_operand_size(instruction.opcode, instruction.operand);
    }

    private static int get_opcode_size(ClrOpcode opcode)
    {
        return (ushort)opcode >= ClrConstants.two_byte_opcode_base ? 2 : 1;
    }

    private static int get_operand_size(ClrOpcode opcode, ClrOperand? operand)
    {
        return opcode switch
        {
            ClrOpcode.ldarg_s or ClrOpcode.ldarga_s or ClrOpcode.starg_s or ClrOpcode.ldloc_s or ClrOpcode.ldloca_s
                or ClrOpcode.stloc_s or ClrOpcode.ldc_i4_s or ClrOpcode.unaligned => 1,
            ClrOpcode.ldarg or ClrOpcode.ldarga or ClrOpcode.starg or ClrOpcode.ldloc or ClrOpcode.ldloca
                or ClrOpcode.stloc => 2,
            ClrOpcode.ldc_i4 => 4,
            ClrOpcode.ldc_i8 => 8,
            ClrOpcode.ldc_r4 => 4,
            ClrOpcode.ldc_r8 => 8,
            ClrOpcode.br_s or ClrOpcode.brfalse_s or ClrOpcode.brtrue_s or ClrOpcode.beq_s or ClrOpcode.bne_un_s
                or ClrOpcode.blt_s or ClrOpcode.ble_s or ClrOpcode.bgt_s or ClrOpcode.bge_s or ClrOpcode.blt_un_s
                or ClrOpcode.ble_un_s or ClrOpcode.bgt_un_s or ClrOpcode.bge_un_s or ClrOpcode.leave_s => 1,
            ClrOpcode.br or ClrOpcode.brfalse or ClrOpcode.brtrue or ClrOpcode.beq or ClrOpcode.bne_un
                or ClrOpcode.blt or ClrOpcode.ble or ClrOpcode.bgt or ClrOpcode.bge or ClrOpcode.blt_un
                or ClrOpcode.ble_un or ClrOpcode.bgt_un or ClrOpcode.bge_un or ClrOpcode.leave => 4,
            ClrOpcode.@switch => operand is ClrSwitchTargetsOperand sw ? 4 + sw.offsets.Count * 4 : 4,
            ClrOpcode.call or ClrOpcode.callvirt or ClrOpcode.newobj or ClrOpcode.ldstr or ClrOpcode.ldftn
                or ClrOpcode.ldvirtftn or ClrOpcode.castclass or ClrOpcode.isinst or ClrOpcode.unbox
                or ClrOpcode.unbox_any or ClrOpcode.box or ClrOpcode.newarr or ClrOpcode.ldelema or ClrOpcode.initobj
                or ClrOpcode.constrained or ClrOpcode.jmp or ClrOpcode.calli or ClrOpcode.ldobj or ClrOpcode.stobj
                or ClrOpcode.ldfld or ClrOpcode.ldflda or ClrOpcode.stfld or ClrOpcode.ldsfld or ClrOpcode.ldsflda
                or ClrOpcode.stsfld or ClrOpcode.@sizeof or ClrOpcode.ldelem_any or ClrOpcode.stelem_any
                or ClrOpcode.cpobj or ClrOpcode.mkrefany or ClrOpcode.refanyval or ClrOpcode.ldtoken => 4,
            _ => 0
        };
    }

    private static void emit_load_arg(ICollection<ClrInstruction> output, GenerateInstruction instruction)
    {
        var operand = (GenerateOperand.Param)instruction.operands[0];
        output.Add(operand.index switch
        {
            0 => new ClrInstruction { opcode = ClrOpcode.ldarg_0 },
            1 => new ClrInstruction { opcode = ClrOpcode.ldarg_1 },
            2 => new ClrInstruction { opcode = ClrOpcode.ldarg_2 },
            3 => new ClrInstruction { opcode = ClrOpcode.ldarg_3 },
            <= byte.MaxValue => new ClrInstruction
                { opcode = ClrOpcode.ldarg_s, operand = new ClrArgumentIndexOperand { index = (uint)operand.index } },
            _ => new ClrInstruction
                { opcode = ClrOpcode.ldarg, operand = new ClrArgumentIndexOperand { index = (uint)operand.index } }
        });
    }

    private static void emit_store_arg(ICollection<ClrInstruction> output, GenerateInstruction instruction)
    {
        var operand = (GenerateOperand.Param)instruction.operands[0];
        output.Add(operand.index switch
        {
            <= byte.MaxValue => new ClrInstruction
                { opcode = ClrOpcode.starg_s, operand = new ClrArgumentIndexOperand { index = (uint)operand.index } },
            _ => new ClrInstruction
                { opcode = ClrOpcode.starg, operand = new ClrArgumentIndexOperand { index = (uint)operand.index } }
        });
    }

    private static void emit_load_local(ICollection<ClrInstruction> output, GenerateInstruction instruction)
    {
        var operand = (GenerateOperand.Local)instruction.operands[0];
        output.Add(operand.index switch
        {
            0 => new ClrInstruction { opcode = ClrOpcode.ldloc_0 },
            1 => new ClrInstruction { opcode = ClrOpcode.ldloc_1 },
            2 => new ClrInstruction { opcode = ClrOpcode.ldloc_2 },
            3 => new ClrInstruction { opcode = ClrOpcode.ldloc_3 },
            <= byte.MaxValue => new ClrInstruction
                { opcode = ClrOpcode.ldloc_s, operand = new ClrLocalIndexOperand { index = (uint)operand.index } },
            _ => new ClrInstruction
                { opcode = ClrOpcode.ldloc, operand = new ClrLocalIndexOperand { index = (uint)operand.index } }
        });
    }

    private static void emit_store_local(ICollection<ClrInstruction> output, GenerateInstruction instruction)
    {
        var operand = (GenerateOperand.Local)instruction.operands[0];
        output.Add(operand.index switch
        {
            0 => new ClrInstruction { opcode = ClrOpcode.stloc_0 },
            1 => new ClrInstruction { opcode = ClrOpcode.stloc_1 },
            2 => new ClrInstruction { opcode = ClrOpcode.stloc_2 },
            3 => new ClrInstruction { opcode = ClrOpcode.stloc_3 },
            <= byte.MaxValue => new ClrInstruction
                { opcode = ClrOpcode.stloc_s, operand = new ClrLocalIndexOperand { index = (uint)operand.index } },
            _ => new ClrInstruction
                { opcode = ClrOpcode.stloc, operand = new ClrLocalIndexOperand { index = (uint)operand.index } }
        });
    }

    private static void emit_const(ICollection<ClrInstruction> output, GenerateOperand operand, GenerateModule module,
        ICollection<string> userStrings, IDictionary<string, uint> userStringTokens)
    {
        switch (operand)
        {
            case GenerateOperand.I32 i32:
                emit_i32_const(output, i32.value);
                break;
            case GenerateOperand.I64 i64:
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldc_i8, operand = new ClrInt64Operand { value = i64.value } });
                break;
            case GenerateOperand.F32 f32:
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldc_r4, operand = new ClrFloat32Operand { value = f32.value } });
                break;
            case GenerateOperand.F64 f64:
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldc_r8, operand = new ClrFloat64Operand { value = f64.value } });
                break;
            case GenerateOperand.Str str:
            {
                var token = register_user_string(userStrings, userStringTokens, str.value);
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldstr, operand = new ClrTokenOperand { value = token } });
                break;
            }
            case GenerateOperand.Const { type: GenerateValueType.utf8 } c:
            {
                var value = c.pool_index < module.constants.strings.Count
                    ? module.constants.strings[c.pool_index]
                    : string.Empty;
                var token = register_user_string(userStrings, userStringTokens, value);
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldstr, operand = new ClrTokenOperand { value = token } });
                break;
            }
            case GenerateOperand.Const { type: GenerateValueType.i32 or GenerateValueType.@bool } c:
            {
                var value = c.pool_index < module.constants.int64_s.Count
                    ? (int)module.constants.int64_s[c.pool_index]
                    : 0;
                emit_i32_const(output, value);
                break;
            }
            case GenerateOperand.Null:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
                break;
            default:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                break;
        }
    }

    /// <summary>
    ///     注册用户字�串并返��?Ldstr 使用�?Token。自动去重，相同字�串返回相�?Token。
    /// </summary>
    /// <param name="userStrings">字�串收集器。</param>
    /// <param name="userStringTokens">字�串到 Token 的去重映射�。</param>
    /// <param name="value">
    ///     字�串�€��€?/param>
    ///     <returns>用户字��?Token�?x70000001 起�）�€?/returns>
    private static uint register_user_string(ICollection<string> userStrings,
        IDictionary<string, uint> userStringTokens,
        string value)
    {
        if (userStringTokens.TryGetValue(value, out var existingToken)) return existingToken;
        var token = 0x70000001u + (uint)userStrings.Count;
        userStringTokens[value] = token;
        userStrings.Add(value);
        return token;
    }

    private static void emit_i32_const(ICollection<ClrInstruction> output, int value)
    {
        if (value is >= -1 and <= 8)
        {
            output.Add(new ClrInstruction
            {
                opcode = value switch
                {
                    -1 => ClrOpcode.ldc_i4_m1,
                    0 => ClrOpcode.ldc_i4_0,
                    1 => ClrOpcode.ldc_i4_1,
                    2 => ClrOpcode.ldc_i4_2,
                    3 => ClrOpcode.ldc_i4_3,
                    4 => ClrOpcode.ldc_i4_4,
                    5 => ClrOpcode.ldc_i4_5,
                    6 => ClrOpcode.ldc_i4_6,
                    7 => ClrOpcode.ldc_i4_7,
                    _ => ClrOpcode.ldc_i4_8
                }
            });
            return;
        }

        if (value is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            output.Add(new ClrInstruction
                { opcode = ClrOpcode.ldc_i4_s, operand = new ClrInt8Operand { value = (sbyte)value } });
            return;
        }

        output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4, operand = new ClrInt32Operand { value = value } });
    }

    private static void emit_call(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        IReadOnlyDictionary<string, uint> methodTokenMap,
        IReadOnlyDictionary<string, uint> externalRefTokenMap,
        IReadOnlySet<string> externalCtorFunctionNames)
    {
        var operand = (GenerateOperand.FuncRef)instruction.operands[0];
        // HIR 生成的未解析 `*.infix OP` 伪调用不会出现在链接表中。
        // 如果这里直接落到缺失 token 的兜底，会把真实比较/算术降成 `pop/pop/default`，
        // 最终生成坏 IL。对这类伪调用直接映射为对应的 CLR 指令。
        if (try_emit_unresolved_infix_call(output, operand)) return;
        if (externalRefTokenMap.TryGetValue(operand.name, out var memberRefToken))
        {
            output.Add(new ClrInstruction
            {
                opcode = externalCtorFunctionNames.Contains(operand.name) ? ClrOpcode.newobj : ClrOpcode.call,
                operand = new ClrTokenOperand { value = memberRefToken }
            });
            return;
        }

        if (methodTokenMap.TryGetValue(operand.name, out var token))
        {
            output.Add(new ClrInstruction
            {
                opcode = ClrOpcode.call,
                operand = new ClrTokenOperand { value = token }
            });
            return;
        }

        for (var i = 0; i < operand.signature.parameters.Count; i++)
            output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
        if (operand.signature.results.Count > 0
            && operand.signature.results[0] != GenerateValueType.@void
            && operand.signature.results[0] != GenerateValueType.unit)
            emit_default_value(output, operand.signature.results[0]);
    }

    /// <summary>
    ///     处理未解析的 `*.infix OP` 伪调用。
    ///     这些调用在链接阶段找不到真实方法时，需要直接发射对应的 CLR 运算指令。
    /// </summary>
    private static bool try_emit_unresolved_infix_call(
        ICollection<ClrInstruction> output,
        GenerateOperand.FuncRef operand)
    {
        var functionName = operand.name;
        var dotInfixIndex = functionName.LastIndexOf(".infix ", StringComparison.Ordinal);
        if (dotInfixIndex < 0) return false;
        if (!supports_unresolved_infix_primitive_emit(operand.signature)) return false;
        var op = functionName[(dotInfixIndex + ".infix ".Length)..];
        switch (op)
        {
            case "==":
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return true;
            case "!=":
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return true;
            case "<":
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                return true;
            case "<=":
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return true;
            case ">":
                output.Add(new ClrInstruction { opcode = ClrOpcode.cgt });
                return true;
            case ">=":
                output.Add(new ClrInstruction { opcode = ClrOpcode.clt });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ceq });
                return true;
            case "+":
                output.Add(new ClrInstruction { opcode = ClrOpcode.add });
                return true;
            case "-":
                output.Add(new ClrInstruction { opcode = ClrOpcode.sub });
                return true;
            case "*":
                output.Add(new ClrInstruction { opcode = ClrOpcode.mul });
                return true;
            case "/":
                output.Add(new ClrInstruction { opcode = ClrOpcode.div });
                return true;
            case "%":
                output.Add(new ClrInstruction { opcode = ClrOpcode.rem });
                return true;
            case "&&":
                output.Add(new ClrInstruction { opcode = ClrOpcode.and });
                return true;
            case "||":
                output.Add(new ClrInstruction { opcode = ClrOpcode.or });
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     仅为原始数值/布尔参数启用 `*.infix OP` 直发射。
    ///     避免误把字符串或引用类型比较降成 CLR 的引用比较。
    /// </summary>
    private static bool supports_unresolved_infix_primitive_emit(GenerateFunctionType signature)
    {
        if (signature.parameters.Count < 2) return false;
        for (var i = 0; i < 2; i++)
            switch (signature.parameters[i])
            {
                case GenerateValueType.i32:
                case GenerateValueType.i64:
                case GenerateValueType.f32:
                case GenerateValueType.f64:
                case GenerateValueType.@bool:
                    continue;
                default:
                    return false;
            }

        return true;
    }

    /// <summary>
    ///     为�部�入函数构�?CLR MethodDefSig。
    ///     使��?CLR 原生类型映射：utf8 �?string (0x0E)，unit �?void (0x01)。
    ///     格式：calling_convention(param_count, return_type, param_types...)
    /// </summary>
    private static byte[] build_import_signature(GenerateModule module, GenerateFunction function, string methodName)
    {
        var hasThis = function.parameters.Count > 0 &&
                      string.Equals(function.parameters[0].name, "self", StringComparison.Ordinal);
        var parameterStartIndex = hasThis ? 1 : 0;
        var parameterCount = function.parameters.Count - parameterStartIndex;
        var isCtor = string.Equals(methodName, ".ctor", StringComparison.Ordinal);
        var result = new List<byte>(4 + parameterCount)
        {
            hasThis || isCtor ? (byte)0x20 : (byte)0x00
        };
        append_compressed_unsigned(result, (uint)parameterCount);
        result.Add(isCtor
            ? (byte)0x01
            : map_clr_import_element_type(resolve_clr_signature_type_name(module, function.return_type_ref)));
        for (var i = parameterStartIndex; i < function.parameters.Count; i++)
            result.Add(map_clr_import_element_type(resolve_clr_signature_type_name(module,
                function.parameters[i].type_ref)));
        return [.. result];
    }

    /// <summary>
    ///     CLR 导入函数的元素类型映射。
    ///     �?Valkyrie 类型映射到�应的 CLR 原生类型，
    /// </summary>
    private static byte map_clr_import_element_type(GenerateTypeReference typeName)
    {
        return typeName.kind switch
        {
            GenerateTypeKind.@void => 0x01,
            GenerateTypeKind.unit => 0x01,
            GenerateTypeKind.@bool => 0x02,
            GenerateTypeKind.@char => 0x03,
            GenerateTypeKind.i8 => 0x04,
            GenerateTypeKind.i16 => 0x06,
            GenerateTypeKind.i32 => 0x08,
            GenerateTypeKind.i64 => 0x0A,
            GenerateTypeKind.f32 => 0x0C,
            GenerateTypeKind.f64 => 0x0D,
            GenerateTypeKind.utf8 => 0x0E,
            GenerateTypeKind.utf16 => 0x0E,
            _ => 0x1C
        };
    }
}