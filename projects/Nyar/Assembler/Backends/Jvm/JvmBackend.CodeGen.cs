using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm.Data;
using Std.Data.Binary.NyarIR.Data;
using static Nyar.Assembler.Backends.Jvm.JvmTypeMap;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 后端 partial：代码生成辅助方法（指令偏移、槽位计算、栈深度分析）。
/// </summary>
public sealed partial class JvmBackend
{
    private static int[] compute_instruction_offsets(GenerateFunction function, List<JvmConstant> constantPool,
        IReadOnlyList<string> moduleStrings, List<JvmBootstrapMethod> bootstrapMethods, string className,
        JvmExternalLinkTable linkTable,
        IReadOnlyList<GenerateFunction> allFunctions,
        IReadOnlyDictionary<string, int> labelEntryDepths)
    {
        var offsets = new int[function.instructions.Count + 1];
        var currentOffset = 0;
        var depth = 0;
        var skipUntilLabel = false;

        // 按指令索引排序的标签列表，用于检测标签目标。
        var sortedLabels = function.labels
            .OrderBy(l => l.instruction_index)
            .ToList();
        var nextLabelIdx = 0;

        for (var instructionIndex = 0; instructionIndex < function.instructions.Count; instructionIndex++)
        {
            var instr = function.instructions[instructionIndex];

            // 检查当前指令是否是标签目标
            while (nextLabelIdx < sortedLabels.Count && sortedLabels[nextLabelIdx].instruction_index == instructionIndex)
            {
                var labelName = sortedLabels[nextLabelIdx].name;
                if (labelEntryDepths.TryGetValue(labelName, out var labelDepth))
                {
                    // 当到达标签时，如果实际栈深度与预期不一致，需要插入 pop/aconst_null 调整
                    var extraPops = Math.Max(0, depth - labelDepth);
                    var extraPushes = Math.Max(0, labelDepth - depth);
                    currentOffset += extraPops; // 每个 pop 1 字节
                    currentOffset += extraPushes; // 每个 aconst_null 1 字节
                    // 同步调整追踪深度
                    depth = labelDepth;
                }

                skipUntilLabel = false;
                nextLabelIdx++;
            }

            if (skipUntilLabel) continue;

            offsets[instructionIndex] = currentOffset;

            if (instr.head_code is NyarHeadCode.jump or NyarHeadCode.jump_if_true or NyarHeadCode.jump_if_false)
            {
                // 分支指令基础大小为 3 字节（opcode + 2 字节偏移）。
                currentOffset += 3;

                // 无条件跳转需要额外 POP 字节（栈深度修复），同步减少跟踪深度
                if (instr.head_code == NyarHeadCode.jump &&
                    instr.operands[0] is GenerateOperand.Label label &&
                    labelEntryDepths.TryGetValue(label.name, out var expectedDepth))
                {
                    var extraPops = Math.Max(0, depth - expectedDepth);
                    currentOffset += extraPops;
                    depth -= extraPops; // POP 发射后栈深度减少，与 emission 保持一致。
                }

                // 无条件跳转或返回后，跳过后续指令直到下一个标签。
                if (instr.head_code == NyarHeadCode.jump || instr.head_code == NyarHeadCode.@return) skipUntilLabel = true;
            }
            else
            {
                currentOffset += measure_instruction_size(instr, function, constantPool,
                    moduleStrings, bootstrapMethods, className, linkTable, allFunctions, instructionIndex,
                    labelEntryDepths);
            }

            // 更新追踪栈深度。
            // call/call_static 的推入值取决于签名是否期望 unit/void
            var (push, pop) = get_nyar_stack_effect(instr);
            if (instr.head_code is NyarHeadCode.call or NyarHeadCode.call_static
                && instr.operands.Count > 0
                && instr.operands[0] is GenerateOperand.FuncRef funcRef)
            {
                var sigResultsCount = funcRef.signature.results.Count;
                var sigExpectsUnit = sigResultsCount > 0 && funcRef.signature.results[0] == GenerateValueType.unit;
                var sigExpectsNoResult = sigResultsCount == 0 || sigExpectsUnit;
                // 签名期望 unit/void 时，emit_call_result_adjustment 会弹出返回值，
                // 实际推入为 0 而非 get_nyar_stack_effect 报告的 1
                if (sigExpectsNoResult) push = 0;
            }

            depth = depth - pop + push;
            if (depth < 0) depth = 0;
        }

        offsets[^1] = currentOffset;
        return offsets;
    }

    private static int measure_instruction_size(GenerateInstruction instruction, GenerateFunction function,
        List<JvmConstant> constantPool, IReadOnlyList<string> moduleStrings, List<JvmBootstrapMethod> bootstrapMethods,
        string className,
        JvmExternalLinkTable linkTable,
        IReadOnlyList<GenerateFunction> allFunctions,
        int instructionIndex,
        IReadOnlyDictionary<string, int> labelEntryDepths)
    {
        if (instruction.head_code is NyarHeadCode.jump or NyarHeadCode.jump_if_true or NyarHeadCode.jump_if_false) return 3;

        var writer = new ByteBufferWriter(64);
        var dummyDepth = 0;
        emit_instruction(ref writer, ref dummyDepth, labelEntryDepths, instruction, function, constantPool, moduleStrings, bootstrapMethods, className, instructionIndex,
            [0],
            new Dictionary<string, int>(StringComparer.Ordinal), linkTable, allFunctions);
        return writer.position;
    }

    private static IReadOnlyDictionary<string, int> build_label_offsets(GenerateFunction function,
        IReadOnlyList<int> instructionOffsets)
    {
        var labelOffsets = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var label in function.labels)
        {
            if (label.instruction_index < 0 || label.instruction_index >= instructionOffsets.Count)
                throw new InvalidOperationException(
                    $"JVM 标签 `{label.name}` 的指令索引 `{label.instruction_index}` 超出函数 `{function.name}` 范围。");

            labelOffsets[label.name] = instructionOffsets[label.instruction_index];
        }

        return labelOffsets;
    }

    /// <summary>
    ///     计算函数所有参数占用的 JVM 局部变量表槽位总数。
    ///     long 和 double 类型每个参数占 2 个槽位，其余类型占 1 个。
    ///     用于 max_locals 计算，get_argument_slot/get_local_slot 的基址偏移。
    /// </summary>
    private static int compute_parameter_slots(GenerateFunction function)
    {
        var slots = 0;
        foreach (var param in function.parameters)
        {
            var jvmType = to_jvm_type_name(param.type_ref);
            slots += is_wide_type(jvmType) ? 2 : 1;
        }

        return slots;
    }

    /// <summary>
    ///     计算函数的 max_locals：参数总槽位数 + 局部变量总槽位数。
    ///     long 和 double 类型各占 2 个槽位，其余类型占 1 个。
    /// </summary>
    private static ushort compute_max_locals(GenerateFunction function)
    {
        var totalSlots = compute_parameter_slots(function);
        foreach (var local in function.local_variables)
        {
            var jvmType = to_jvm_type_name(local.type_ref);
            totalSlots += is_wide_type(jvmType) ? 2 : 1;
        }

        // set_field/set_offset_index/set_ordinal_index 需要两个临时局部变量槽位
        // （暂存 value 和 key/index）。
        if (function.instructions.Any(i => i.head_code is NyarHeadCode.set_field
                or NyarHeadCode.set_offset_index
                or NyarHeadCode.set_ordinal_index))
            totalSlots += 2;

        // call/call_static 的多参数 checkcast 需要临时局部变量槽位。
        // 最多需要 max_param_count 个槽位来暂存参数
        var maxCallParams = 0;
        foreach (var instr in function.instructions)
            if (instr.head_code is NyarHeadCode.call or NyarHeadCode.call_static)
                if (instr.operands.Count > 0 && instr.operands[0] is GenerateOperand.FuncRef funcRef)
                    maxCallParams = Math.Max(maxCallParams, funcRef.signature.parameters.Count);

        if (maxCallParams > 1) totalSlots += maxCallParams;

        return (ushort)totalSlots;
    }

    /// <summary>
    ///     计算 set_field/set_offset_index/set_ordinal_index 使用的临时局部变量基址槽位。
    ///     布局：[参数槽位][局部变量槽位][set 临时槽位 ×2][call 临时槽位...]
    /// </summary>
    private static int compute_set_temp_base_slot(GenerateFunction function)
    {
        var totalSlots = compute_parameter_slots(function);
        foreach (var local in function.local_variables)
            totalSlots += is_wide_type(to_jvm_type_name(local.type_ref)) ? 2 : 1;
        return totalSlots;
    }


    /// <summary>
    ///     预分析：计算每个标签在进入时的预期栈深度（第一次经过时记录）。
    ///     用于控制流回边处的栈深度修复。
    /// </summary>
    private static IReadOnlyDictionary<string, int> compute_label_entry_depths(
        GenerateFunction function)
    {
        var labelToDepth = new Dictionary<string, int>(StringComparer.Ordinal);

        // 按指令索引排序的标签列表
        var sortedLabels = function.labels
            .OrderBy(l => l.instruction_index)
            .ToList();
        var nextLabelIdx = 0;

        var depth = 0;
        var skipUntilLabel = false;

        for (var i = 0; i < function.instructions.Count; i++)
        {
            // 检查当前指令是否是标签目标
            while (nextLabelIdx < sortedLabels.Count && sortedLabels[nextLabelIdx].instruction_index == i)
            {
                var labelName = sortedLabels[nextLabelIdx].name;
                if (!labelToDepth.ContainsKey(labelName)) labelToDepth[labelName] = depth;

                skipUntilLabel = false;
                nextLabelIdx++;
            }

            if (skipUntilLabel) continue;

            var instr = function.instructions[i];
            var (push, pop) = get_nyar_stack_effect(instr);

            // call/call_static 的推入值取决于签名是否期望 unit/void
            if (instr.head_code is NyarHeadCode.call or NyarHeadCode.call_static
                && instr.operands.Count > 0
                && instr.operands[0] is GenerateOperand.FuncRef funcRef)
            {
                var sigResultsCount = funcRef.signature.results.Count;
                var sigExpectsUnit = sigResultsCount > 0 && funcRef.signature.results[0] == GenerateValueType.unit;
                var sigExpectsNoResult = sigResultsCount == 0 || sigExpectsUnit;
                if (sigExpectsNoResult) push = 0;
            }

            // 处理分支
            if (instr.head_code == NyarHeadCode.jump)
            {
                if (instr.operands[0] is GenerateOperand.Label jumpLabel && !labelToDepth.ContainsKey(jumpLabel.name)) labelToDepth[jumpLabel.name] = depth;

                skipUntilLabel = true;
            }
            else if (instr.head_code == NyarHeadCode.@return)
            {
                // 返回后的指令（const Null + pop 等）是死代码，必须跳过，
                // 否则栈深度计算会包含它们，导致标签入口栈深度不一致。
                skipUntilLabel = true;
            }
            else if (instr.head_code is NyarHeadCode.jump_if_true or NyarHeadCode.jump_if_false)
            {
                if (instr.operands[0] is GenerateOperand.Label branchLabel)
                {
                    var targetDepth = Math.Max(0, depth - pop); // 条件值在目标处已被弹出。
                    if (!labelToDepth.ContainsKey(branchLabel.name)) labelToDepth[branchLabel.name] = targetDepth;
                }
            }

            depth -= pop;
            if (depth < 0) depth = 0;

            depth += push;
        }

        return labelToDepth;
    }

    /// <summary>
    ///     计算局部变量在 JVM 局部变量表中的槽位编号。
    ///     先累加所有参数占用的槽位（long/double 占 2），再累加前面局部变量占用的槽位。
    /// </summary>
    /// <param name="index">局部变量在 local_variables 列表中的索引（从 0 开始）。</param>
    /// <param name="function">目标函数。</param>
    private static int get_local_slot(int index, GenerateFunction function)
    {
        var slot = compute_parameter_slots(function);
        for (var i = 0; i < index; i++)
        {
            var jvmType = to_jvm_type_name(function.local_variables[i].type_ref);
            slot += is_wide_type(jvmType) ? 2 : 1;
        }

        return slot;
    }

    /// <summary>
    ///     计算参数降级为 JVM 局部变量表中的槽位编号。
    ///     遍历前面所有参数，累加槽位（long/double 占 2 个槽位）。
    /// </summary>
    /// <param name="index">参数�?parameters 列表中的索引（从 0 开始）。</param>
    /// <param name="function">目标函数。</param>
    private static int get_argument_slot(int index, GenerateFunction function)
    {
        var slot = 0;
        for (var i = 0; i < index; i++)
        {
            var jvmType = to_jvm_type_name(function.parameters[i].type_ref);
            slot += is_wide_type(jvmType) ? 2 : 1;
        }

        return slot;
    }

    private static JvmOpcode get_specialized_load_opcode(JvmOpcode opcode, int slot)
    {
        return opcode switch
        {
            JvmOpcode.iload => slot switch
            {
                0 => JvmOpcode.iload0,
                1 => JvmOpcode.iload1,
                2 => JvmOpcode.iload2,
                _ => JvmOpcode.iload3
            },
            JvmOpcode.lload => slot switch
            {
                0 => JvmOpcode.lload0,
                1 => JvmOpcode.lload1,
                2 => JvmOpcode.lload2,
                _ => JvmOpcode.lload3
            },
            JvmOpcode.fload => slot switch
            {
                0 => JvmOpcode.fload0,
                1 => JvmOpcode.fload1,
                2 => JvmOpcode.fload2,
                _ => JvmOpcode.fload3
            },
            JvmOpcode.dload => slot switch
            {
                0 => JvmOpcode.dload0,
                1 => JvmOpcode.dload1,
                2 => JvmOpcode.dload2,
                _ => JvmOpcode.dload3
            },
            JvmOpcode.aload => slot switch
            {
                0 => JvmOpcode.aload0,
                1 => JvmOpcode.aload1,
                2 => JvmOpcode.aload2,
                _ => JvmOpcode.aload3
            },
            _ => opcode
        };
    }

    private static JvmOpcode get_specialized_store_opcode(JvmOpcode opcode, int slot)
    {
        return opcode switch
        {
            JvmOpcode.istore => slot switch
            {
                0 => JvmOpcode.istore0,
                1 => JvmOpcode.istore1,
                2 => JvmOpcode.istore2,
                _ => JvmOpcode.istore3
            },
            JvmOpcode.lstore => slot switch
            {
                0 => JvmOpcode.lstore0,
                1 => JvmOpcode.lstore1,
                2 => JvmOpcode.lstore2,
                _ => JvmOpcode.lstore3
            },
            JvmOpcode.fstore => slot switch
            {
                0 => JvmOpcode.fstore0,
                1 => JvmOpcode.fstore1,
                2 => JvmOpcode.fstore2,
                _ => JvmOpcode.fstore3
            },
            JvmOpcode.dstore => slot switch
            {
                0 => JvmOpcode.dstore0,
                1 => JvmOpcode.dstore1,
                2 => JvmOpcode.dstore2,
                _ => JvmOpcode.dstore3
            },
            JvmOpcode.astore => slot switch
            {
                0 => JvmOpcode.astore0,
                1 => JvmOpcode.astore1,
                2 => JvmOpcode.astore2,
                _ => JvmOpcode.astore3
            },
            _ => opcode
        };
    }

    private static bool is_return_opcode(NyarHeadCode opcode)
    {
        return opcode == NyarHeadCode.@return;
    }

    /// <summary>
    ///     检查当前指令的前一条 LIR 指令是否为 @const I32(0)。
    ///     用于 @return 处理中判�?return; 语句是否已经推入了 unit 默认值。
    /// </summary>
    private static bool is_previous_const_i32_zero(GenerateFunction function, int instructionIndex)
    {
        if (instructionIndex == 0) return false;

        var prev = function.instructions[instructionIndex - 1];
        if (prev.head_code != NyarHeadCode.@const || prev.operands.Count == 0) return false;

        return prev.operands[0] is GenerateOperand.I32 i32 && i32.value == 0;
    }

    /// <summary>
    ///     检查当前指令的前一条 LIR 指令是否为 @const Null。
    ///     用于 @return 处理中判断多返回值（元组）是否用 null 占位。
    /// </summary>
    private static bool is_previous_const_null(GenerateFunction function, int instructionIndex)
    {
        if (instructionIndex == 0) return false;

        var prev = function.instructions[instructionIndex - 1];
        if (prev.head_code != NyarHeadCode.@const || prev.operands.Count == 0) return false;

        return prev.operands[0] is GenerateOperand.Null;
    }

    /// <summary>
    ///     回溯 call_static 之前的 LIR 指令，推断每个参数实际推入的 JVM 类型。
    ///     通过从当前位置向前扫描，识别每个参数值的来源指令并映射为 JVM 类型。
    /// </summary>
    private static string[] infer_actual_param_types(GenerateFunction function, int callInstructionIndex, int paramCount)
    {
        var result = new string[paramCount];
        for (var i = 0; i < paramCount; i++) result[i] = "java/lang/Object";

        // 从 call_static 前一条指令开始，向前回溯 paramCount �?值推�?指令
        // 每遇到一个推入值的指令（@const、二元运算、比较、load 等），记录其 JVM 类型
        var foundCount = 0;
        // pop 指令会消耗栈顶的值，回溯时需要跳过被 pop 消费的值推入指。
        var popSkipCount = 0;
        for (var idx = callInstructionIndex - 1; idx >= 0 && foundCount < paramCount; idx--)
        {
            var instr = function.instructions[idx];

            // 跳过不推入值的指令，pop 需要额外处理（跳过被消费的值推入指令）
            if (instr.head_code is NyarHeadCode.nop or NyarHeadCode.store_local
                or NyarHeadCode.store_arg or NyarHeadCode.jump
                or NyarHeadCode.@return or NyarHeadCode.exit_effect_handler or NyarHeadCode.exit_try)
                continue;

            // pop 指令消耗栈顶的一个值，记录需要跳过的值推入指令数。
            if (instr.head_code == NyarHeadCode.pop)
            {
                popSkipCount++;
                continue;
            }

            // 如果之前�?pop 指令，跳过被消费的值推入指。
            if (popSkipCount > 0)
            {
                popSkipCount--;
                continue;
            }

            // set_field/set_index 不推入值到栈上，反而消�?3 个栈值。
            // 向后回溯时需要额外跳过这 3 个被消耗的推入指令（值表达式、字段名、对象引用）。
            // 否则它们会被错误地识别为 call_static 的参数推入。
            if (instr.head_code is NyarHeadCode.set_field or NyarHeadCode.set_offset_index or NyarHeadCode.set_ordinal_index or NyarHeadCode.array_set)
            {
                idx -= 3;
                continue;
            }

            // 推入 int 类型的指。
            if (instr.head_code is NyarHeadCode.@const && instr.operands.Count > 0)
            {
                var operand = instr.operands[0];
                var jvmType = operand switch
                {
                    GenerateOperand.I32 => "int",
                    GenerateOperand.I64 => "long",
                    GenerateOperand.F32 => "float",
                    GenerateOperand.F64 => "double",
                    GenerateOperand.Str => "java/lang/String",
                    GenerateOperand.Null => "java/lang/Object",
                    _ => "java/lang/Object"
                };
                result[paramCount - 1 - foundCount] = jvmType;
                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            if (instr.head_code is NyarHeadCode.i32_add or NyarHeadCode.i32_sub or NyarHeadCode.i32_mul
                or NyarHeadCode.i32_div_s or NyarHeadCode.i32_rem_s or NyarHeadCode.i32_and
                or NyarHeadCode.i32_or or NyarHeadCode.i32_xor or NyarHeadCode.i32_shl
                or NyarHeadCode.i32_shr_s or NyarHeadCode.i32_shr_u or NyarHeadCode.i32_not
                or NyarHeadCode.i32_eq or NyarHeadCode.i32_ne or NyarHeadCode.i32_lt_s
                or NyarHeadCode.i32_le_s or NyarHeadCode.i32_gt_s or NyarHeadCode.i32_ge_s
                or NyarHeadCode.i32_extend_i64_s or NyarHeadCode.i32_to_f32_s
                or NyarHeadCode.i32_to_f64_s or NyarHeadCode.i32_div_u or NyarHeadCode.i32_rem_u
                or NyarHeadCode.i64_trunc_i32_s
                or NyarHeadCode.length
                or NyarHeadCode.any_to_i32)
            {
                result[paramCount - 1 - foundCount] = "int";
                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            if (instr.head_code is NyarHeadCode.any_to_utf8)
            {
                result[paramCount - 1 - foundCount] = "java/lang/String";
                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            if (instr.head_code is NyarHeadCode.i64_add or NyarHeadCode.i64_sub or NyarHeadCode.i64_mul
                or NyarHeadCode.i64_div_s or NyarHeadCode.i64_rem_s or NyarHeadCode.i64_and
                or NyarHeadCode.i64_or or NyarHeadCode.i64_xor)
            {
                result[paramCount - 1 - foundCount] = "long";
                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            if (instr.head_code is NyarHeadCode.f64_add or NyarHeadCode.f64_sub or NyarHeadCode.f64_mul
                or NyarHeadCode.f64_div)
            {
                result[paramCount - 1 - foundCount] = "double";
                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            if (instr.head_code is NyarHeadCode.ref_eq or NyarHeadCode.ref_ne)
            {
                result[paramCount - 1 - foundCount] = "int";
                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            // utf8_eq / utf8_ne 在 JVM 端降级为 String.equals，返回 int（boolean）。
            if (instr.head_code is NyarHeadCode.utf8_eq or NyarHeadCode.utf8_ne)
            {
                result[paramCount - 1 - foundCount] = "int";
                foundCount++;
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            // call/call_static 推入其返回类型。
            if (instr.head_code is NyarHeadCode.call or NyarHeadCode.call_static)
            {
                if (instr.operands.Count > 0 && instr.operands[0] is GenerateOperand.FuncRef funcRef)
                {
                    var returnType = funcRef.signature.results.Count == 0
                        ? "void"
                        : to_jvm_type_name(funcRef.signature.results[0]);
                    result[paramCount - 1 - foundCount] = returnType == "void" ? "java/lang/Object" : returnType;
                }

                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            // load_local/load_arg 的类型取决于变量类型
            if (instr.head_code is NyarHeadCode.load_local or NyarHeadCode.load_arg)
            {
                result[paramCount - 1 - foundCount] = get_instruction_result_jvm_type(instr);
                foundCount++;
                // 该指令可能消费了栈上的值，回溯时需要跳过被消费的值推入指。
                var (_, instrPop) = get_nyar_stack_effect(instr);
                popSkipCount += instrPop;
                continue;
            }

            // 其他推入值的指令（dup、swap、new_object、get_field 等）
            var (_, catchallPop) = get_nyar_stack_effect(instr);
            popSkipCount += catchallPop;
            foundCount++;
        }

        return result;
    }

    /// <summary>
    ///     基于 Nyar IR 指令流计算方法的 max_stack。    ///     线性扫描每条 Nyar 指令的栈效果（推�?弹出），跟踪峰值深度。
    /// </summary>
    private static ushort compute_jvm_max_stack(GenerateFunction function)
    {
        var depth = 0;
        var maxDepth = 0;

        for (var i = 0; i < function.instructions.Count; i++)
        {
            var instr = function.instructions[i];
            var (push, pop) = get_nyar_stack_effect(instr);

            depth -= pop;
            if (depth < 0) depth = 0;

            depth += push;
            if (depth > maxDepth) maxDepth = depth;
        }

        // 至少保留 8 作为安全下限
        return (ushort)Math.Max(maxDepth, 8);
    }

    /// <summary>
    ///     返回 Nyar IR 指令的栈效果 (push, pop)。
    ///     push = 该指令向求值栈推入的值数量。    ///     pop = 该指令从求值栈弹出的值数量。    ///
    /// </summary>
    private static (int push, int pop) get_nyar_stack_effect(GenerateInstruction instr)
    {
        switch (instr.head_code)
        {
            // 无栈效果
            case NyarHeadCode.nop:
                return (0, 0);

            // 推入 1 个值。            case NyarHeadCode.load_arg:
            case NyarHeadCode.load_local:
            case NyarHeadCode.load_global:
            case NyarHeadCode.@const:
            case NyarHeadCode.new_object:
            case NyarHeadCode.new_closure:
            case NyarHeadCode.get_upvalue:
            case NyarHeadCode.alloc:
            case NyarHeadCode.i32_load:
            case NyarHeadCode.i64_load:
                return (1, 0);

            // 弹出 1 个值，推入 1 个值（length: Object → int。
            case NyarHeadCode.length:
            case NyarHeadCode.any_to_i32:
            case NyarHeadCode.any_to_utf8:
                return (1, 1);

            // 弹出 2 推入 1（读取字段/索引）。
            case NyarHeadCode.get_field:
            case NyarHeadCode.array_get:
            case NyarHeadCode.get_offset_index:
            case NyarHeadCode.get_ordinal_index:
                return (1, 2);

            // 弹出 1 个值。            case NyarHeadCode.store_local:
            case NyarHeadCode.store_arg:
            case NyarHeadCode.store_global:
            case NyarHeadCode.pop:
            case NyarHeadCode.set_upvalue:
            case NyarHeadCode.free:
            case NyarHeadCode.i32_store:
            case NyarHeadCode.i64_store:
            case NyarHeadCode.field_store:
            case NyarHeadCode.index_store:
                return (0, 1);

            // 弹出数组和元素，推入 0
            case NyarHeadCode.array_push:
                return (0, 2);

            // 弹出 3 推入 0（写入字段/索引）。
            case NyarHeadCode.set_field:
            case NyarHeadCode.array_set:
            case NyarHeadCode.set_offset_index:
            case NyarHeadCode.set_ordinal_index:
                return (0, 3);

            // 栈操。
            case NyarHeadCode.dup:
                return (1, 0);
            case NyarHeadCode.swap:
                return (0, 0);

            // 二元算术/比较：弹�?2 推入 1
            case NyarHeadCode.i32_add:
            case NyarHeadCode.i32_sub:
            case NyarHeadCode.i32_mul:
            case NyarHeadCode.i32_div_s:
            case NyarHeadCode.i32_div_u:
            case NyarHeadCode.i32_rem_s:
            case NyarHeadCode.i32_rem_u:
            case NyarHeadCode.i32_and:
            case NyarHeadCode.i32_or:
            case NyarHeadCode.i32_xor:
            case NyarHeadCode.i32_shl:
            case NyarHeadCode.i32_shr_s:
            case NyarHeadCode.i32_shr_u:
            case NyarHeadCode.i32_eq:
            case NyarHeadCode.i32_ne:
            case NyarHeadCode.i32_lt_s:
            case NyarHeadCode.i32_lt_u:
            case NyarHeadCode.i32_le_s:
            case NyarHeadCode.i32_le_u:
            case NyarHeadCode.i32_gt_s:
            case NyarHeadCode.i32_gt_u:
            case NyarHeadCode.i32_ge_s:
            case NyarHeadCode.i32_ge_u:
            case NyarHeadCode.i64_add:
            case NyarHeadCode.i64_sub:
            case NyarHeadCode.i64_mul:
            case NyarHeadCode.i64_div_s:
            case NyarHeadCode.i64_div_u:
            case NyarHeadCode.i64_rem_s:
            case NyarHeadCode.i64_rem_u:
            case NyarHeadCode.i64_and:
            case NyarHeadCode.i64_or:
            case NyarHeadCode.i64_xor:
            case NyarHeadCode.i64_shl:
            case NyarHeadCode.i64_shr_s:
            case NyarHeadCode.i64_shr_u:
            case NyarHeadCode.i64_eq:
            case NyarHeadCode.i64_ne:
            case NyarHeadCode.i64_lt_s:
            case NyarHeadCode.i64_lt_u:
            case NyarHeadCode.i64_le_s:
            case NyarHeadCode.i64_le_u:
            case NyarHeadCode.i64_gt_s:
            case NyarHeadCode.i64_gt_u:
            case NyarHeadCode.i64_ge_s:
            case NyarHeadCode.i64_ge_u:
            case NyarHeadCode.f32_add:
            case NyarHeadCode.f32_sub:
            case NyarHeadCode.f32_mul:
            case NyarHeadCode.f32_div:
            case NyarHeadCode.f64_add:
            case NyarHeadCode.f64_sub:
            case NyarHeadCode.f64_mul:
            case NyarHeadCode.f64_div:
            case NyarHeadCode.f64_eq:
            case NyarHeadCode.f64_ne:
            case NyarHeadCode.f64_lt:
            case NyarHeadCode.f64_le:
            case NyarHeadCode.f64_gt:
            case NyarHeadCode.f64_ge:
            case NyarHeadCode.ref_eq:
            case NyarHeadCode.ref_ne:
            case NyarHeadCode.utf8_eq:
            case NyarHeadCode.utf8_ne:
            case NyarHeadCode.utf8_concat:
            case NyarHeadCode.big_int_add:
            case NyarHeadCode.big_int_sub:
            case NyarHeadCode.big_int_mul:
                return (1, 2);

            // 一元运算：弹出 1 推入 1
            case NyarHeadCode.i32_neg:
            case NyarHeadCode.i32_not:
            case NyarHeadCode.i64_neg:
            case NyarHeadCode.i64_not:
            case NyarHeadCode.f32_neg:
            case NyarHeadCode.f64_neg:
            case NyarHeadCode.f64_sqrt:
            case NyarHeadCode.i32_extend_i64_s:
            case NyarHeadCode.i32_extend_i64_u:
            case NyarHeadCode.i64_trunc_i32_s:
            case NyarHeadCode.i64_trunc_i32_u:
            case NyarHeadCode.i32_to_f32_s:
            case NyarHeadCode.i32_to_f64_s:
            case NyarHeadCode.i64_to_f64:
            case NyarHeadCode.f64_to_i32:
            case NyarHeadCode.f64_to_i64:
            case NyarHeadCode.utf8_len_bytes:
            case NyarHeadCode.utf8_len_chars:
                return (1, 1);

            // 分支：弹出 0 或 1
            case NyarHeadCode.jump:
                return (0, 0);

            case NyarHeadCode.jump_if_true:
            case NyarHeadCode.jump_if_false:
                return (0, 1);

            // 调用：推�?1（返回值），弹出参数数。
            // call_static 的参数计数从 FuncRef.signature 获取
            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
            case NyarHeadCode.call_dynamic:
            case NyarHeadCode.call_witness:
            case NyarHeadCode.tail_call:
            case NyarHeadCode.call_intrinsic:
            case NyarHeadCode.call_native:
            {
                var paramCount = 0;
                if (instr.operands.Count > 0 && instr.operands[0] is GenerateOperand.FuncRef funcRef) paramCount = funcRef.signature.parameters.Count;
                return (1, paramCount);
            }

            // 返回（ireturn/areturn 等弹出栈顶返回值）
            case NyarHeadCode.@return:
                return (0, 1);

            // 效果处理。            case NyarHeadCode.enter_effect_handler:
            case NyarHeadCode.exit_effect_handler:
            case NyarHeadCode.enter_try:
            case NyarHeadCode.exit_try:
            case NyarHeadCode.effect_handle:
            case NyarHeadCode.yield:
            case NyarHeadCode.rethrow:
                return (0, 0);

            case NyarHeadCode.perform_effect:
            case NyarHeadCode.resume:
                return (1, 0);

            // 异常
            case NyarHeadCode.@throw:
            case NyarHeadCode.@catch:
                return (0, 0);

            // UTF-8 文本操作。
            case NyarHeadCode.utf8_substr:
                return (1, 2); // 弹出字符�?范围推入子串

            // FFI
            case NyarHeadCode.load_native_lib:
                return (0, 1);
            case NyarHeadCode.get_native_func:
                return (1, 1);

            // 静�?动态访。
            case NyarHeadCode.access_static:
            case NyarHeadCode.access_witness:
            case NyarHeadCode.access_dynamic:
                return (1, 0);
            case NyarHeadCode.inline_cache_update:
                return (0, 0);

            // SIMD 操作：弹出操作数推入结果
            case NyarHeadCode.simd:
                return (1, 0);
                return (1, 1);
                return (0, 2);
                return (1, 2);
                return (1, 1);
                return (1, 3);
                return (1, 1);
                return (1, 1);
                return (1, 2);
                return (1, 2);
                return (1, 1);

            default:
                return (0, 0);
        }
    }
}