using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     <see cref="WasmBackend" /> 的分部文件，承载单函数代码生成逻辑。
///     包含 <see cref="build_code" /> 及其内部实现、指令序列修剪、
///     栈效果分析等代码生成辅助方法。
/// </summary>
public sealed partial class WasmBackend
{
    /// <summary>
    ///     构建单个函数的 WASM 代码段，捕获并转换底层异常为编译诊断信息。
    /// </summary>
    private static WasmCode build_code(GenerateFunction function, WasmBuildContext buildContext)
    {
        try
        {
            return build_code_internal(function, buildContext);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"WASM 编译函数 `{function.name}` 失败：{ex}", ex);
        }
    }

    /// <summary>
    ///     实际的代码生成实现：扫描指令、分配临时局部变量、
    ///     按结构化或线性方式发射指令，并在函数末尾处理栈平衡。
    /// </summary>
    private static WasmCode build_code_internal(GenerateFunction function, WasmBuildContext buildContext)
    {
        var writer = new ByteBufferWriter(256);
        var effectiveInstructions = trim_trailing_return(function.instructions);
        var emitContext = new WasmFunctionEmitContext(buildContext, function);
        emitContext.unreachable = false;
        emitContext.unreachable_by_return = false;
        var locals = build_locals(function);

        // 诊断：dump LIR
        // 环境变量 LEGION_SPY_LIR_DUMP=1 强制开启 dump
        // 环境变量 LEGION_SPY_LIR_FUNC=<func> 指定只 dump 该函数（函数名包含匹配）
        var spyDumpEnabled = Environment.GetEnvironmentVariable("LEGION_SPY_LIR_DUMP") == "1";
        var spyFuncFilter = Environment.GetEnvironmentVariable("LEGION_SPY_LIR_FUNC");
        var diagFuncIndex = buildContext.function_count++;
        var shouldDiag = spyDumpEnabled
            ? string.IsNullOrEmpty(spyFuncFilter) || function.name.Contains(spyFuncFilter)
            : false;

        if (shouldDiag)
        {
            Console.Error.WriteLine(
                $"\n=== LIR dump: {function.name} (#{diagFuncIndex}, {effectiveInstructions.Count} instrs, return={function.return_type_ref}) ===");
            for (var di = 0; di < effectiveInstructions.Count; di++)
            {
                var instr = effectiveInstructions[di];
                var operands = string.Join(", ", instr.operands);
                Console.Error.WriteLine($"  [{di:D3}] {instr.head_code} {operands}");
            }

            Console.Error.WriteLine("=== labels ===");
            foreach (var label in function.labels)
                Console.Error.WriteLine($"  label {label.name} -> instr {label.instruction_index}");
        }

        // 检测是否需要临时局部变量（dup / new_object / set_field / set_index / get_index 需要）
        var needsScratchLocal = effectiveInstructions.Any(i =>
            i.head_code == NyarHeadCode.dup ||
            i.head_code == NyarHeadCode.new_object ||
            i.head_code == NyarHeadCode.set_field ||
            i.head_code == NyarHeadCode.array_set ||
            i.head_code == NyarHeadCode.set_offset_index ||
            i.head_code == NyarHeadCode.array_get ||
            i.head_code == NyarHeadCode.get_offset_index);
        // 检测是否需要第二个临时局部变量（i32_store / i64_store 地址保留需要，alloc 需要）
        var needsScratchLocal2 = effectiveInstructions.Any(i =>
            i.head_code == NyarHeadCode.i32_store ||
            i.head_code == NyarHeadCode.i64_store ||
            i.head_code == NyarHeadCode.alloc);
        if (needsScratchLocal || needsScratchLocal2)
        {
            var scratchIndex = (uint)(function.parameters.Count + function.local_variables.Count);
            emitContext.scratch_local_index = scratchIndex;
            var mutableLocals = new List<WasmLocal>(locals)
            {
                new() { count = 1, type = WasmValueType.int32 }
            };

            if (needsScratchLocal2)
            {
                var scratchIndex2 = (uint)(function.parameters.Count + function.local_variables.Count + 1);
                emitContext.scratch_local_index2 = scratchIndex2;
                mutableLocals.Add(new WasmLocal { count = 1, type = WasmValueType.int32 });
            }

            locals = mutableLocals;
        }

        // 传递堆指针全局变量索引
        emitContext.heap_ptr_global_index = buildContext.heap_ptr_global_index;

        // 传递 GC 类型映射字典
        emitContext.gc_type_name_to_index = buildContext.gc_type_name_to_index;
        emitContext.gc_field_name_to_index = buildContext.gc_field_name_to_index;
        emitContext.gc_field_op_type_name = buildContext.gc_field_op_type_name;

        if (try_create_structured_control_flow_plan(function, effectiveInstructions.Count, out var structuredPlan,
                out _))
        {
            emit_structured_instructions(ref writer, effectiveInstructions, emitContext, structuredPlan);
        }
        else
        {
            emitContext.instructions = effectiveInstructions;
            for (var idx = 0; idx < effectiveInstructions.Count; idx++)
            {
                if (emitContext.unreachable) break;

                emitContext.instruction_index = idx;
                emit_instruction(ref writer, effectiveInstructions[idx], emitContext);
            }
        }

        // 如果函数已通过 return 提前终止，后续兜底逻辑全部跳过（WASM 字节流中已有正确的 return 指令）
        // return 指令已消费栈上的返回值，非 void 函数无需额外压入返回值
        if (emitContext.unreachable)
        {
            writer.write_u8((byte)WasmOpcode.end);
            var earlyBody = writer.to_array();

            return new WasmCode
            {
                locals = locals,
                body = earlyBody,
                max_length = (uint)earlyBody.Length
            };
        }

        // 函数末尾栈平衡兜底逻辑：
        // - void/unit 函数：fallthru 要求栈为空
        // - 非 void 函数：fallthru 要求栈上有且仅有 1 个返回值
        // 若函数已通过 return 提前退出（unreachable_by_return=true），
        // 末尾 fallthru 不可达，插入 unreachable 指令即可。
        // stack_depth 在结构化控制流路径下也可信：
        // emit_structured_range 在 block/loop 创建时保存 stack_depth，
        // 关闭时恢复，以抵消 void block 结束时丢弃块内栈值的语义。
        var isVoidReturnFallthru = function.return_type_ref.is_void_like;

        if (isVoidReturnFallthru)
        {
            // void/unit 函数：drop 所有残留值使栈为空
            for (var i = 0; i < emitContext.stack_depth; i++)
                writer.write_u8((byte)WasmOpcode.drop);
        }
        else
        {
            // 非 void 函数：调整栈到恰好 1 个返回值
            if (emitContext.stack_depth == 0)
                // 栈为空，压入默认返回值
                emit_default_value(ref writer, map_value_type(function.return_type_ref));
            else if (emitContext.stack_depth > 1)
                // 栈上有多余值，drop 到只剩 1 个
                for (var i = 1; i < emitContext.stack_depth; i++)
                    writer.write_u8((byte)WasmOpcode.drop);

            // stack_depth >= 1 时插入 return 消费返回值
            if (emitContext.stack_depth >= 1)
                writer.write_u8((byte)WasmOpcode.@return);
        }

        writer.write_u8((byte)WasmOpcode.end);
        var body = writer.to_array();

        return new WasmCode
        {
            locals = locals,
            body = body,
            max_length = (uint)body.Length
        };
    }

    /// <summary>
    ///     裁剪指令序列末尾的 return 指令（WASM 通过 fallthru 隐式返回，无需显式 return）。
    /// </summary>
    private static IReadOnlyList<GenerateInstruction> trim_trailing_return(
        IReadOnlyList<GenerateInstruction> instructions)
    {
        if (instructions.Count > 0 && instructions[^1].head_code == NyarHeadCode.@return)
            return [.. instructions.Take(instructions.Count - 1)];

        return instructions;
    }

    /// <summary>
    ///     判断指令序列末尾是否以值产生指令结束。
    ///     用于 void/unit 函数末尾的栈平衡检查。
    /// </summary>
    private static bool ends_with_value_producer(IReadOnlyList<GenerateInstruction> instructions)
    {
        if (instructions.Count == 0) return false;

        return instructions[^1].head_code switch
        {
            NyarHeadCode.@const => true,
            NyarHeadCode.load_arg => true,
            NyarHeadCode.load_local => true,
            NyarHeadCode.i32_add => true,
            NyarHeadCode.i32_sub => true,
            NyarHeadCode.i32_mul => true,
            NyarHeadCode.i32_eq => true,
            NyarHeadCode.i32_ne => true,
            NyarHeadCode.i32_lt_s => true,
            NyarHeadCode.i32_le_s => true,
            NyarHeadCode.i32_gt_s => true,
            NyarHeadCode.i32_ge_s => true,
            NyarHeadCode.i64_eq => true,
            NyarHeadCode.i64_ne => true,
            NyarHeadCode.i64_lt_s => true,
            NyarHeadCode.i64_le_s => true,
            NyarHeadCode.i64_gt_s => true,
            NyarHeadCode.i64_ge_s => true,
            NyarHeadCode.ref_eq => true,
            NyarHeadCode.ref_ne => true,
            NyarHeadCode.f64_add => true,
            NyarHeadCode.f64_sub => true,
            NyarHeadCode.f64_mul => true,
            NyarHeadCode.f64_div => true,
            NyarHeadCode.f64_sqrt => true,
            NyarHeadCode.simd => true,
            NyarHeadCode.call_static => true,
            NyarHeadCode.dup => true,
            NyarHeadCode.new_object => true,
            NyarHeadCode.get_field => true,
            NyarHeadCode.get_offset_index => true,
            NyarHeadCode.any_to_i32 => true,
            NyarHeadCode.any_to_utf8 => true,
            NyarHeadCode.alloc => true,
            NyarHeadCode.load_global => true,
            NyarHeadCode.new_closure => true,
            NyarHeadCode.get_upvalue => true,
            NyarHeadCode.access_static => true,
            NyarHeadCode.access_witness => true,
            NyarHeadCode.access_dynamic => true,
            NyarHeadCode.utf8_len_bytes => true,
            NyarHeadCode.utf8_len_chars => true,
            NyarHeadCode.utf8_substr => true,
            NyarHeadCode.i32_extend_i64_u => true,
            NyarHeadCode.i64_trunc_i32_u => true,
            NyarHeadCode.i32_or => true,
            NyarHeadCode.i32_and => true,
            NyarHeadCode.i32_xor => true,
            NyarHeadCode.i32_rem_s => true,
            NyarHeadCode.i32_div_s => true,
            NyarHeadCode.i32_div_u => true,
            NyarHeadCode.i32_rem_u => true,
            NyarHeadCode.utf8_eq => true,
            NyarHeadCode.utf8_ne => true,
            NyarHeadCode.utf8_concat => true,
            _ => false
        };
    }

    /// <summary>
    ///     判断单条指令是否在 WASM 栈上产生值。
    ///     对 call_static 做精确判断：检查被调函数的签名是否具有非 void 返回类型。
    /// </summary>
    private static bool last_instruction_produces_value(GenerateInstruction? instruction)
    {
        if (instruction == null) return false;

        // call_static 需要检查被调函数签名
        if (instruction.head_code == NyarHeadCode.call_static)
        {
            if (instruction.operands.FirstOrDefault() is GenerateOperand.FuncRef funcRef)
                return funcRef.signature.results.Count > 0 &&
                       funcRef.signature.results[0] != GenerateValueType.@void &&
                       funcRef.signature.results[0] != GenerateValueType.unit;

            return false;
        }

        // 其他操作码复用 ends_with_value_producer 的逻辑（排除 call_static）
        return instruction.head_code switch
        {
            NyarHeadCode.@const => true,
            NyarHeadCode.load_arg => true,
            NyarHeadCode.load_local => true,
            NyarHeadCode.i32_add => true,
            NyarHeadCode.i32_sub => true,
            NyarHeadCode.i32_mul => true,
            NyarHeadCode.i32_eq => true,
            NyarHeadCode.i32_ne => true,
            NyarHeadCode.i32_lt_s => true,
            NyarHeadCode.i32_le_s => true,
            NyarHeadCode.i32_gt_s => true,
            NyarHeadCode.i32_ge_s => true,
            NyarHeadCode.i64_eq => true,
            NyarHeadCode.i64_ne => true,
            NyarHeadCode.i64_lt_s => true,
            NyarHeadCode.i64_le_s => true,
            NyarHeadCode.i64_gt_s => true,
            NyarHeadCode.i64_ge_s => true,
            NyarHeadCode.ref_eq => true,
            NyarHeadCode.ref_ne => true,
            NyarHeadCode.f64_add => true,
            NyarHeadCode.f64_sub => true,
            NyarHeadCode.f64_mul => true,
            NyarHeadCode.f64_div => true,
            NyarHeadCode.f64_sqrt => true,
            NyarHeadCode.simd => true,
            NyarHeadCode.dup => true,
            NyarHeadCode.new_object => true,
            NyarHeadCode.get_field => true,
            NyarHeadCode.get_offset_index => true,
            NyarHeadCode.any_to_i32 => true,
            NyarHeadCode.any_to_utf8 => true,
            NyarHeadCode.alloc => true,
            NyarHeadCode.load_global => true,
            NyarHeadCode.new_closure => true,
            NyarHeadCode.get_upvalue => true,
            NyarHeadCode.access_static => true,
            NyarHeadCode.access_witness => true,
            NyarHeadCode.access_dynamic => true,
            NyarHeadCode.utf8_len_bytes => true,
            NyarHeadCode.utf8_len_chars => true,
            NyarHeadCode.utf8_substr => true,
            NyarHeadCode.i32_extend_i64_u => true,
            NyarHeadCode.i64_trunc_i32_u => true,
            NyarHeadCode.i32_or => true,
            NyarHeadCode.i32_and => true,
            NyarHeadCode.i32_xor => true,
            NyarHeadCode.i32_rem_s => true,
            NyarHeadCode.i32_div_s => true,
            NyarHeadCode.i32_div_u => true,
            NyarHeadCode.i32_rem_u => true,
            NyarHeadCode.utf8_eq => true,
            NyarHeadCode.utf8_ne => true,
            NyarHeadCode.utf8_concat => true,
            _ => false
        };
    }
}