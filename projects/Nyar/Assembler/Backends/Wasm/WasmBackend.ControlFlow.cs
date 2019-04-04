using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     <see cref="WasmBackend" /> 的分部实现：负责 WASM 结构化控制流的发射逻辑与控制流计划的构建。
///     包含从线性 Nyar IR 指令序列推导出 WASM 结构化 block/loop 嵌套的算法，
///     以及向 WASM 字节流写入 block/loop/br/br_if 等结构化控制指令的方法。
/// </summary>
public sealed partial class WasmBackend
{
    private static void emit_structured_instructions(
        ref ByteBufferWriter writer,
        IReadOnlyList<GenerateInstruction> instructions,
        WasmFunctionEmitContext context,
        WasmStructuredControlFlowPlan plan)
    {
        if (plan.target_indices.Count == 0)
        {
            context.instructions = instructions;
            for (var index = 0; index < instructions.Count; index++)
            {
                if (context.unreachable) return;

                context.instruction_index = index;
                var inst = instructions[index];
                emit_instruction(ref writer, inst, context);
                // return 之后，标记不可达，后续指令全部跳过
                if (inst.head_code is NyarHeadCode.@return)
                {
                    context.unreachable = true;
                    context.unreachable_by_return = true;
                    return;
                }
            }

            return;
        }

        emit_structured_range(
            ref writer,
            instructions,
            context,
            plan,
            0,
            instructions.Count,
            [],
            null,
            []);
    }

    /// <summary>
    ///     判断目标指令是否为条件跳转（jump_if_false / jump_if_true）。
    ///     条件跳转目标需要 block i32 类型，因为条件值在 block 内计算并通过 end 逃逸到外层。
    /// </summary>
    private static bool is_conditional_jump_target(
        IReadOnlyList<GenerateInstruction> instructions,
        int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= instructions.Count) return false;

        return instructions[targetIndex].head_code
            is NyarHeadCode.jump_if_false or NyarHeadCode.jump_if_true;
    }

    /// <summary>
    ///     发射结构化 block 指令，根据目标指令类型自动选择 block 类型。
    ///     条件跳转目标使用 block i32 (0x7F)，允许条件值通过 end 逃逸；
    ///     其他目标使用 block void (0x40)，块内栈值在 end 时丢弃。
    ///     同时向 block_depth_stack 和 block_type_stack 压入现场，供 end 时恢复。
    /// </summary>
    private static void emit_structured_block(
        ref ByteBufferWriter writer,
        IReadOnlyList<GenerateInstruction> instructions,
        WasmFunctionEmitContext context,
        int targetIndex)
    {
        var isI32 = is_conditional_jump_target(instructions, targetIndex);
        writer.write_u8((byte)WasmOpcode.block);
        writer.write_u8(isI32 ? (byte)0x7F : (byte)0x40);
        context.block_depth_stack.Push(context.stack_depth);
        context.block_type_stack.Push(isI32);
    }

    /// <summary>
    ///     发射结构化 loop 指令（始终为 void 类型）。
    ///     loop 的 end 不产生值，br 到 loop 携带值进入循环体而非逃逸。
    /// </summary>
    private static void emit_structured_loop(
        ref ByteBufferWriter writer,
        WasmFunctionEmitContext context)
    {
        writer.write_u8((byte)WasmOpcode.loop);
        writer.write_u8(0x40);
        context.block_depth_stack.Push(context.stack_depth);
        context.block_type_stack.Push(false);
    }

    /// <summary>
    ///     发射 end 指令并恢复栈深度现场。
    ///     i32 block 的 end 产生 1 个 i32 值到外层栈（stack_depth = entry + 1）；
    ///     void block/loop 的 end 丢弃块内栈值（stack_depth = entry）。
    ///     同时重置 unreachable 状态（br/br_if 路径可达 end 后的代码），
    ///     但不重置 unreachable_by_return（return 引起的函数级不可达不可恢复）。
    /// </summary>
    private static void emit_structured_end(
        ref ByteBufferWriter writer,
        WasmFunctionEmitContext context)
    {
        writer.write_u8((byte)WasmOpcode.end);
        var entryDepth = context.block_depth_stack.Count > 0
            ? context.block_depth_stack.Pop()
            : context.stack_depth;
        var isI32 = context.block_type_stack.Count > 0 && context.block_type_stack.Pop();
        context.stack_depth = isI32 ? entryDepth + 1 : entryDepth;
        context.unreachable = false;
    }

    private static void emit_structured_range(
        ref ByteBufferWriter writer,
        IReadOnlyList<GenerateInstruction> instructions,
        WasmFunctionEmitContext context,
        WasmStructuredControlFlowPlan plan,
        int startIndex,
        int endIndex,
        List<int> openTargets,
        int? ignoredLoopStartIndex,
        HashSet<string> activeRanges)
    {
        if (endIndex <= startIndex) return;

        // 如果当前函数已通过 return 提前终止，跳过所有后续段（死代码）。
        // 父调用方已创建的 block 仍会由父调用方发出 end 指令关闭。
        if (context.unreachable) return;

        var activeRangeKey = $"{startIndex}:{endIndex}:{ignoredLoopStartIndex?.ToString() ?? "_"}";
        if (!activeRanges.Add(activeRangeKey))
            throw new InvalidOperationException(
                $"WASM 结构化控制流出现重复区间递归：{activeRangeKey}；开放块栈：[{string.Join(", ", openTargets)}]");

        try
        {
            if (try_get_next_loop_in_range(plan, startIndex, endIndex, ignoredLoopStartIndex, out var loopRegion))
            {
                // 收集所有超出循环的分支目标（pre-loop + 循环体内），去重并排序
                var farTargets = new SortedSet<int>();

                // 扫描 pre-loop 范围内的分支
                if (startIndex < loopRegion.start_index)
                    for (var i = startIndex; i < loopRegion.start_index; i++)
                    {
                        var inst = instructions[i];
                        if (inst.head_code is NyarHeadCode.jump or NyarHeadCode.jump_if_false
                            or NyarHeadCode.jump_if_true)
                            if (inst.operands.FirstOrDefault() is GenerateOperand.Label lbl &&
                                plan.label_targets.TryGetValue(lbl.name, out var branchTarget) &&
                                branchTarget > loopRegion.end_index)
                                farTargets.Add(branchTarget);
                    }

                // 扫描循环体内超出循环的分支目标
                // 同时扫描 loopRegion.end_index 处的指令（循环退出后的第一条指令），
                // 它也可能包含跳出后续代码块的分支
                for (var i = loopRegion.start_index; i <= loopRegion.end_index && i < instructions.Count; i++)
                {
                    var inst = instructions[i];
                    if (inst.head_code is NyarHeadCode.jump or NyarHeadCode.jump_if_false or NyarHeadCode.jump_if_true)
                        if (inst.operands.FirstOrDefault() is GenerateOperand.Label lbl &&
                            plan.label_targets.TryGetValue(lbl.name, out var branchTarget) &&
                            branchTarget > loopRegion.end_index)
                            farTargets.Add(branchTarget);
                }

                // 包含循环后所有 plan 中的目标索引（即使没有直接的显式分支指令引用）
                // 因为嵌套结构可能通过多层间接跳转到这些目标，它们在 post-loop 处理时需要对应的 block
                foreach (var targetIndex in plan.target_indices)
                    if (targetIndex > loopRegion.end_index)
                        farTargets.Add(targetIndex);

                // 创建嵌套 block：从最外层到最内层（最远的目标在最外层，最近的在最内层）
                // 每个 block 都会包裹 pre-loop、loop 以及对应目标之前的 post-loop 代码
                // 排除已在 openTargets 中的目标，避免重复创建 block（来自父级的 block 已覆盖）
                var farTargetList = farTargets
                    .Where(t => !openTargets.Contains(t))
                    .ToList();

                // 确定最外层 block 的目标位置
                var outerBlockTarget = farTargetList.Count > 0
                    ? farTargetList.Max()
                    : loopRegion.end_index;

                for (var t = farTargetList.Count - 1; t >= 0; t--)
                {
                    emit_structured_block(ref writer, instructions, context, farTargetList[t]);
                    openTargets.Add(farTargetList[t]);
                }

                // 如果没有 pre-loop 跨循环分支，仍需创建循环的基础外层 block
                // 检查 loopRegion.end_index 是否已在 openTargets 中（可能由父调用创建），避免重复
                var createdBaseBlock = false;
                if (farTargetList.Count == 0 && !openTargets.Contains(loopRegion.end_index))
                {
                    emit_structured_block(ref writer, instructions, context, loopRegion.end_index);
                    openTargets.Add(loopRegion.end_index);
                    createdBaseBlock = true;
                }

                // 处理 pre-loop 范围
                if (startIndex < loopRegion.start_index)
                {
                    var hasForwardTargetInPreLoopRange = plan.target_indices
                        .Any(targetIndex => targetIndex > startIndex && targetIndex <= loopRegion.start_index);
                    if (hasForwardTargetInPreLoopRange)
                        emit_structured_range(ref writer, instructions, context, plan, startIndex,
                            loopRegion.start_index, openTargets, loopRegion.start_index, activeRanges);
                    else
                        emit_linear_range(ref writer, instructions, context, plan, startIndex,
                            loopRegion.start_index, openTargets);
                }

                // 写入循环结构
                emit_structured_loop(ref writer, context);

                openTargets.Add(loopRegion.start_index);
                emit_structured_range(
                    ref writer,
                    instructions,
                    context,
                    plan,
                    loopRegion.start_index,
                    loopRegion.end_index,
                    openTargets,
                    loopRegion.start_index,
                    activeRanges);
                openTargets.RemoveAt(openTargets.Count - 1);

                // end 关闭 loop：恢复栈深度到 loop 创建时的值
                // void loop 的 end 会丢弃 loop 体内的残留栈值
                emit_structured_end(ref writer, context);

                // 关闭基础外层 block，必须在 post-loop 处理之前关闭
                // 避免 base block 包裹 post-loop 代码导致栈不平衡（fallthru 栈不为空）
                if (createdBaseBlock)
                {
                    openTargets.RemoveAt(openTargets.Count - 1);
                    emit_structured_end(ref writer, context);
                }

                // 检查 post-loop 范围内是否有与当前范围重叠的循环
                // 重叠条件：存在另一个循环，其 start_index 在 [loopRegion.end_index, endIndex) 内
                // 这种情况发生在连续循环中：外层循环的 far target 段包含了内层循环
                var hasOverlappingLoop = false;
                foreach (var l in plan.loops)
                    if (l.start_index > loopRegion.end_index
                        && l.start_index < endIndex
                        && l.start_index != loopRegion.start_index)
                    {
                        hasOverlappingLoop = true;
                        break;
                    }

                if (hasOverlappingLoop)
                {
                    // post-loop 范围内存在重叠循环，递归处理整个 post-loop 范围
                    // 递归调用会找到内层循环并正确处理其 loop 指令和回边分支
                    emit_structured_range(
                        ref writer,
                        instructions,
                        context,
                        plan,
                        loopRegion.end_index,
                        endIndex,
                        openTargets,
                        ignoredLoopStartIndex,
                        activeRanges);

                    // 递归调用返回后，关闭本调用创建的 far target block
                    for (var t = 0; t < farTargetList.Count; t++)
                    {
                        openTargets.RemoveAt(openTargets.Count - 1);
                        emit_structured_end(ref writer, context);
                    }
                }
                else
                {
                    // 分段处理 post-loop：在每个 far target 处关闭对应的 block
                    var postLoopStart = loopRegion.end_index;
                    for (var t = 0; t < farTargetList.Count; t++)
                    {
                        var targetPos = farTargetList[t];
                        if (postLoopStart < targetPos)
                            emit_structured_range(
                                ref writer,
                                instructions,
                                context,
                                plan,
                                postLoopStart,
                                targetPos,
                                openTargets,
                                ignoredLoopStartIndex,
                                activeRanges);

                        openTargets.RemoveAt(openTargets.Count - 1);
                        emit_structured_end(ref writer, context);
                        postLoopStart = targetPos;
                    }

                    // 处理所有 block 之后的部分
                    if (outerBlockTarget < endIndex)
                        emit_structured_range(
                            ref writer,
                            instructions,
                            context,
                            plan,
                            outerBlockTarget,
                            endIndex,
                            openTargets,
                            ignoredLoopStartIndex,
                            activeRanges);
                }

                return;
            }

            // 排除已存在于开放块栈中的目标索引，避免递归创建重复 block
            // 例如：父调用已为 37 创建了 block，子区间 (17..37) 不应再次为 37 创建 block
            var targetIndicesInRange = plan.target_indices
                .Where(targetIndex => targetIndex > startIndex && targetIndex <= endIndex)
                .Where(targetIndex => !openTargets.Contains(targetIndex))
                .OrderBy(t => t)
                .ToArray();
            if (targetIndicesInRange.Length == 0)
            {
                emit_linear_range(ref writer, instructions, context, plan, startIndex, endIndex, openTargets);
                return;
            }

            // 简化策略：从最外层到最内层为范围内所有目标创建嵌套 block
            // 这保证每个目标位置都有对应的 block，分支总能正确解析
            // block 从最外层（最大 target）到最内层（最小 target）创建
            for (var t = targetIndicesInRange.Length - 1; t >= 0; t--)
            {
                emit_structured_block(ref writer, instructions, context, targetIndicesInRange[t]);
                openTargets.Add(targetIndicesInRange[t]);
            }

            // 分段处理每个子范围并逐步关闭 block
            // 使用 emit_linear_range 而非 emit_structured_range，因为：
            // 1. 当前范围内确定无循环（否则走循环处理路径）
            // 2. 所有目标 block 已创建完毕（在 openTargets 中）
            // 3. 避免递归导致重复区间元组（如 110:128:110）
            var segmentStart = startIndex;
            for (var t = 0; t < targetIndicesInRange.Length; t++)
            {
                var targetPos = targetIndicesInRange[t];
                if (segmentStart < targetPos) emit_linear_range(ref writer, instructions, context, plan, segmentStart, targetPos, openTargets);

                openTargets.RemoveAt(openTargets.Count - 1);
                emit_structured_end(ref writer, context);
                segmentStart = targetPos;
            }

            // 处理最后一个目标之后的残余范围
            if (segmentStart < endIndex) emit_linear_range(ref writer, instructions, context, plan, segmentStart, endIndex, openTargets);
        }
        finally
        {
            activeRanges.Remove(activeRangeKey);
        }
    }

    private static void emit_linear_range(
        ref ByteBufferWriter writer,
        IReadOnlyList<GenerateInstruction> instructions,
        WasmFunctionEmitContext context,
        WasmStructuredControlFlowPlan plan,
        int startIndex,
        int endIndex,
        List<int> openTargets)
    {
        context.instructions = instructions;
        for (var instructionIndex = startIndex; instructionIndex < endIndex; instructionIndex++)
        {
            if (context.unreachable) return;

            var instruction = instructions[instructionIndex];
            context.instruction_index = instructionIndex;
            switch (instruction.head_code)
            {
                case NyarHeadCode.jump:
                    emit_structured_branch(
                        ref writer,
                        instruction,
                        instructionIndex,
                        plan,
                        openTargets,
                        context,
                        false,
                        false);
                    break;
                case NyarHeadCode.jump_if_true:
                    emit_structured_branch(
                        ref writer,
                        instruction,
                        instructionIndex,
                        plan,
                        openTargets,
                        context,
                        false,
                        true);
                    break;
                case NyarHeadCode.jump_if_false:
                    emit_structured_branch(
                        ref writer,
                        instruction,
                        instructionIndex,
                        plan,
                        openTargets,
                        context,
                        true,
                        true);
                    break;
                case NyarHeadCode.@return:
                    emit_instruction(ref writer, instruction, context);
                    // return 之后，标记不可达，后续指令全部跳过
                    context.unreachable = true;
                    context.unreachable_by_return = true;
                    return;
                default:
                    emit_instruction(ref writer, instruction, context);
                    break;
            }
        }
    }

    private static void emit_structured_branch(
        ref ByteBufferWriter writer,
        GenerateInstruction instruction,
        int instructionIndex,
        WasmStructuredControlFlowPlan plan,
        List<int> openTargets,
        WasmFunctionEmitContext context,
        bool negateCondition,
        bool conditional)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Label label ||
            !plan.label_targets.TryGetValue(label.name, out var targetInstructionIndex))
            throw new InvalidOperationException($"WASM 结构化分支缺少可解析的标签目标（函数：{context.function.name}）。");

        if (targetInstructionIndex <= instructionIndex &&
            !openTargets.Contains(targetInstructionIndex))
            throw new InvalidOperationException(
                $"WASM 结构化分支仅支持前向跳转，函数 '{context.function.name}' 标签 `{label.name}` 指向 `{targetInstructionIndex}`，当前位置为 `{instructionIndex}`，开放块栈：[{string.Join(", ", openTargets)}]。");

        var branchDepth = resolve_branch_depth(openTargets, targetInstructionIndex, label.name, context.function.name);
        if (conditional)
        {
            if (negateCondition) writer.write_u8((byte)WasmOpcode.i32_eqz);

            writer.write_u8((byte)WasmOpcode.br_if);
            writer.write_leb128_u32((uint)branchDepth);
            return;
        }

        writer.write_u8((byte)WasmOpcode.br);
        writer.write_leb128_u32((uint)branchDepth);
    }

    private static int resolve_branch_depth(List<int> openTargets, int targetInstructionIndex, string labelName,
        string functionName)
    {
        for (var reverseIndex = openTargets.Count - 1; reverseIndex >= 0; reverseIndex--)
            if (openTargets[reverseIndex] == targetInstructionIndex)
                return openTargets.Count - 1 - reverseIndex;

        throw new InvalidOperationException(
            $"WASM 结构化分支无法为标签 `{labelName}` 解析 block 深度（函数：{functionName}）；目标索引 `{targetInstructionIndex}` 不在当前开放块栈中。开放块栈：[{string.Join(", ", openTargets)}]");
    }

    private static bool try_create_structured_control_flow_plan(
        GenerateFunction function,
        out WasmStructuredControlFlowPlan plan,
        out string? error)
    {
        return try_create_structured_control_flow_plan(function, function.instructions.Count, out plan, out error);
    }

    private static bool try_create_structured_control_flow_plan(
        GenerateFunction function,
        int instructionCount,
        out WasmStructuredControlFlowPlan plan,
        out string? error)
    {
        var labelTargets = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var label in function.labels)
        {
            var targetIndex = label.instruction_index;
            if (targetIndex < 0 || targetIndex > function.instructions.Count)
            {
                plan = WasmStructuredControlFlowPlan.empty;
                error = $"WASM 标签 `{label.name}` 指向非法指令索引 `{targetIndex}`。";
                return false;
            }

            if (targetIndex > instructionCount) targetIndex = instructionCount;

            labelTargets[label.name] = targetIndex;
        }

        var targetIndices = new SortedSet<int>();
        var loopRegions = new List<WasmLoopRegion>();
        for (var instructionIndex = 0; instructionIndex < instructionCount; instructionIndex++)
        {
            var instruction = function.instructions[instructionIndex];
            if (instruction.head_code is not NyarHeadCode.jump and not NyarHeadCode.jump_if_true
                and not NyarHeadCode.jump_if_false) continue;

            if (instruction.operands.FirstOrDefault() is not GenerateOperand.Label label)
            {
                plan = WasmStructuredControlFlowPlan.empty;
                error = $"WASM 分支指令 `{instruction.head_code}` 缺少标签操作数。";
                return false;
            }

            if (!labelTargets.TryGetValue(label.name, out var targetIndex))
            {
                plan = WasmStructuredControlFlowPlan.empty;
                error = $"WASM 分支目标标签 `{label.name}` 未定义。";
                return false;
            }

            if (targetIndex < instructionIndex)
            {
                if (instruction.head_code is not NyarHeadCode.jump)
                {
                    plan = WasmStructuredControlFlowPlan.empty;
                    error = $"WASM 当前仅支持结构化循环回边；条件跳转 `{instruction.head_code}` 不能回跳到 `{label.name}`。";
                    return false;
                }

                loopRegions.Add(new WasmLoopRegion(targetIndex, instructionIndex + 1));
                continue;
            }

            targetIndices.Add(targetIndex);
        }

        plan = new WasmStructuredControlFlowPlan(labelTargets, [.. targetIndices], [.. loopRegions]);

        error = null;
        return true;
    }

    private static bool try_get_next_loop_in_range(
        WasmStructuredControlFlowPlan plan,
        int startIndex,
        int endIndex,
        int? ignoredLoopStartIndex,
        out WasmLoopRegion loopRegion)
    {
        WasmLoopRegion? bestCandidate = null;
        foreach (var candidate in plan.loops)
        {
            if (candidate.start_index < startIndex ||
                candidate.end_index > endIndex ||
                ignoredLoopStartIndex == candidate.start_index)
                continue;

            if (bestCandidate is null || candidate.start_index < bestCandidate.Value.start_index)
                bestCandidate = candidate;
        }

        if (bestCandidate is not null)
        {
            loopRegion = bestCandidate.Value;
            return true;
        }

        loopRegion = default;
        return false;
    }
}