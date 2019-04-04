using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     <see cref="WasmBackend" /> 的 partial 文件，承载所有辅助指令发射方法和栈效果计算。
///     主发射方法 <see cref="emit_instruction" /> 位于 <see cref="WasmBackend.Emit" /> 中。
/// </summary>
public sealed partial class WasmBackend
{
    /// <summary>
    ///     根据指令的栈效果更新 emit 上下文的栈深度估计。
    ///     仅用于简单线性路径（无控制流块）的末尾兜底逻辑。
    ///     不处理 block/loop/if 等结构化控制流的栈深度变化。
    /// </summary>
    private static void update_stack_depth(GenerateInstruction instruction, WasmFunctionEmitContext context)
    {
        var (pop, push) = get_stack_effect(instruction, context);
        context.stack_depth = Math.Max(0, context.stack_depth - pop + push);
    }

    /// <summary>
    ///     获取指令的栈效果 (pop, push)。
    ///     对于 call_static/call，根据被调函数签名计算；
    ///     对于其他指令，根据 opcode 静态映射。
    /// </summary>
    private static (int pop, int push) get_stack_effect(GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        switch (instruction.head_code)
        {
            // 无栈效果
            case NyarHeadCode.nop:
            case NyarHeadCode.any_to_i32:
            case NyarHeadCode.any_to_utf8:
            case NyarHeadCode.enter_effect_handler:
            case NyarHeadCode.exit_effect_handler:
            case NyarHeadCode.perform_effect:
            case NyarHeadCode.resume:
            case NyarHeadCode.enter_try:
            case NyarHeadCode.exit_try:
            case NyarHeadCode.swap:
                return (0, 0);

            // push 1
            case NyarHeadCode.@const:
            case NyarHeadCode.load_arg:
            case NyarHeadCode.load_local:
            case NyarHeadCode.load_global:
            case NyarHeadCode.dup:
            case NyarHeadCode.new_object:
            case NyarHeadCode.new_closure:
            case NyarHeadCode.get_upvalue:
            case NyarHeadCode.access_static:
            case NyarHeadCode.access_witness:
            case NyarHeadCode.access_dynamic:
            case NyarHeadCode.simd:
            case NyarHeadCode.alloc:
                return (0, 1);

            // pop 1
            case NyarHeadCode.store_arg:
            case NyarHeadCode.store_local:
            case NyarHeadCode.store_global:
            case NyarHeadCode.free:
            case NyarHeadCode.set_upvalue:
            case NyarHeadCode.field_store:
                return (1, 0);

            // (1) → (1)
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
            case NyarHeadCode.i32_load:
            case NyarHeadCode.i64_load:
            case NyarHeadCode.array_get:
            case NyarHeadCode.get_offset_index:
            case NyarHeadCode.get_ordinal_index:
            case NyarHeadCode.utf8_len_bytes:
            case NyarHeadCode.utf8_len_chars:
            case NyarHeadCode.length:
                return (1, 1);

            case NyarHeadCode.get_field:
                return (2, 1);

            // (2) → (1)
            case NyarHeadCode.i32_add:
            case NyarHeadCode.i32_sub:
            case NyarHeadCode.i32_mul:
            case NyarHeadCode.i32_div_s:
            case NyarHeadCode.i32_rem_s:
            case NyarHeadCode.i32_div_u:
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
            case NyarHeadCode.i32_le_s:
            case NyarHeadCode.i32_gt_s:
            case NyarHeadCode.i32_ge_s:
            case NyarHeadCode.i32_lt_u:
            case NyarHeadCode.i32_le_u:
            case NyarHeadCode.i32_gt_u:
            case NyarHeadCode.i32_ge_u:
            case NyarHeadCode.i64_add:
            case NyarHeadCode.i64_sub:
            case NyarHeadCode.i64_mul:
            case NyarHeadCode.i64_div_s:
            case NyarHeadCode.i64_rem_s:
            case NyarHeadCode.i64_div_u:
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
            case NyarHeadCode.i64_le_s:
            case NyarHeadCode.i64_gt_s:
            case NyarHeadCode.i64_ge_s:
            case NyarHeadCode.i64_lt_u:
            case NyarHeadCode.i64_le_u:
            case NyarHeadCode.i64_gt_u:
            case NyarHeadCode.i64_ge_u:
            case NyarHeadCode.ref_eq:
            case NyarHeadCode.ref_ne:
            case NyarHeadCode.f64_add:
            case NyarHeadCode.f64_sub:
            case NyarHeadCode.f64_mul:
            case NyarHeadCode.f64_div:
            case NyarHeadCode.f64_eq:
            case NyarHeadCode.f64_ne:
            case NyarHeadCode.f64_lt:
            case NyarHeadCode.f64_gt:
            case NyarHeadCode.f64_le:
            case NyarHeadCode.f64_ge:
            case NyarHeadCode.f32_add:
            case NyarHeadCode.f32_sub:
            case NyarHeadCode.f32_mul:
            case NyarHeadCode.f32_div:
            case NyarHeadCode.utf8_eq:
            case NyarHeadCode.utf8_ne:
            case NyarHeadCode.utf8_concat:
            case NyarHeadCode.array_push:
                return (2, 1);

            // (2) → (1) → mem_store 保留地址
            case NyarHeadCode.i32_store:
            case NyarHeadCode.i64_store:
                return (2, 1);

            // (3) → (1) → string_substr 保留原字符串
            case NyarHeadCode.utf8_substr:
                return (3, 1);

            // (3) → (0)
            case NyarHeadCode.set_field:
            case NyarHeadCode.array_set:
            case NyarHeadCode.set_offset_index:
            case NyarHeadCode.set_ordinal_index:
            case NyarHeadCode.index_store:
                return (3, 0);

            // call/call_static：根据签名计算
            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                return get_call_stack_effect(instruction, context);

            // call_intrinsic：根据操作数中的参数数量和返回类型计算栈效果
            case NyarHeadCode.call_intrinsic:
                return get_call_intrinsic_stack_effect(instruction);

            // pop：消费 1 个值（除非前一条是 void call，由 emit_instruction 特殊处理）
            case NyarHeadCode.pop:
                return (1, 0);

            // return：消费返回值（按函数签名）
            case NyarHeadCode.@return:
                return (1, 0);

            // call_witness/call_dynamic：保守估计：丢弃所有参数，push 1（若有返回）
            case NyarHeadCode.call_witness:
            case NyarHeadCode.call_dynamic:
                return get_call_stack_effect(instruction, context);

            default:
                return (0, 0);
        }
    }

    /// <summary>
    ///     获取 call/call_static 指令的栈效果。
    ///     pop 参数数量，push 返回值数量（void/unit 为 0）。
    /// </summary>
    private static (int pop, int push) get_call_stack_effect(GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.FuncRef funcRef) return (0, 0);

        var popCount = funcRef.signature.parameters.Count;

        // 优先使用 try_get_function_return_is_void 检查实际函数返回类型，
        // 因为调用的 signature 可能与实际 WASM 函数类型不一。
        var isActuallyVoid = context.build_context.try_get_function_return_is_void(funcRef.name, funcRef.signature);
        if (isActuallyVoid) return (popCount, 0);

        var pushCount = funcRef.signature.results.Count > 0
                        && funcRef.signature.results[0] != GenerateValueType.@void
                        && funcRef.signature.results[0] != GenerateValueType.unit
            ? 1
            : 0;

        return (popCount, pushCount);
    }

    private static void emit_const(ref ByteBufferWriter writer, GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        if (instruction.operands.Count == 0) return;

        switch (instruction.operands[0])
        {
            case GenerateOperand.I32 value:
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(value.value);
                break;
            case GenerateOperand.I64 value:
                writer.write_u8((byte)WasmOpcode.i64_const);
                writer.write_leb128_i64(value.value);
                break;
            case GenerateOperand.F32 value:
                writer.write_u8((byte)WasmOpcode.f32_const);
                writer.write_f32_le(value.value);
                break;
            case GenerateOperand.F64 value:
                writer.write_u8((byte)WasmOpcode.f64_const);
                writer.write_f64_le(value.value);
                break;
            case GenerateOperand.Str value:
                var stringLiteral = context.build_context.register_string_literal(value.value);
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(stringLiteral.offset);
                break;
            case GenerateOperand.Null value:
                emit_default_value(ref writer, map_value_type(value.type));
                break;
            case GenerateOperand.Const { type: GenerateValueType.i32 } value:
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(value.pool_index);
                break;
        }
    }

    private static void emit_load_arg(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Param param) return;

        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32((uint)param.index);
    }

    private static void emit_store_arg(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Param param) return;

        writer.write_u8((byte)WasmOpcode.local_set);
        writer.write_leb128_u32((uint)param.index);
    }

    private static void emit_load_local(ref ByteBufferWriter writer, GenerateInstruction instruction,
        GenerateFunction function)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Local local) return;

        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32(get_wasm_local_index(function, local.index));
    }

    private static void emit_store_local(ref ByteBufferWriter writer, GenerateInstruction instruction,
        GenerateFunction function)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Local local) return;

        writer.write_u8((byte)WasmOpcode.local_set);
        writer.write_leb128_u32(get_wasm_local_index(function, local.index));
    }

    private static void emit_call_static(ref ByteBufferWriter writer, GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.FuncRef funcRef) return;

        // 识别 [wasm]/[import("wasm",...)] 外部导入函数
        if (context.build_context.wasm_import_function_index.TryGetValue(funcRef.name, out var importIndex))
        {
            writer.write_u8((byte)WasmOpcode.call);
            writer.write_leb128_u32(importIndex);
            emit_synthetic_unit_result(ref writer, funcRef.signature);

            // 如果导入函数返回 void/unit，标记 previous_was_void_call 以便后续 pop 跳过 drop
            if (funcRef.signature.results.Count == 0
                || funcRef.signature.results[0] == GenerateValueType.@void
                || funcRef.signature.results[0] == GenerateValueType.unit)
                context.previous_was_void_call = true;

            return;
        }

        // 跨后端外部函数 fallback：丢弃参数，压入默认返回值，保持栈平衡。
        if (context.build_context.cross_backend_external_names.Contains(funcRef.name) ||
            context.build_context.cross_backend_external_names.Contains(get_short_name(funcRef.name)))
        {
            foreach (var _ in funcRef.signature.parameters) writer.write_u8((byte)WasmOpcode.drop);

            if (funcRef.signature.results.Count > 0
                && funcRef.signature.results[0] != GenerateValueType.@void
                && funcRef.signature.results[0] != GenerateValueType.unit)
                emit_default_value(ref writer, map_value_type(funcRef.signature.results[0]));
            else
                // 返回类型为 void/unit 时，FALLBACK 不压入任何值，
                // 标记 previous_was_void_call 以便后续 pop 跳过 drop，避免栈下溢
                context.previous_was_void_call = true;

            return;
        }

        if (context.build_context.try_get_function_index(funcRef.name, funcRef.signature, out var functionIndex))
        {
            var wasmCallIndex = context.build_context.import_function_count + (uint)functionIndex;
            writer.write_u8((byte)WasmOpcode.call);
            writer.write_leb128_u32(wasmCallIndex);

            // 如果被调函数的实际返回类型是 void/unit，标记以便后续 pop 跳过 drop
            if (context.build_context.try_get_function_return_is_void(funcRef.name, funcRef.signature))
                context.previous_was_void_call = true;

            return;
        }

        foreach (var _ in funcRef.signature.parameters) writer.write_u8((byte)WasmOpcode.drop);

        if (funcRef.signature.results.Count > 0
            && funcRef.signature.results[0] != GenerateValueType.@void
            && funcRef.signature.results[0] != GenerateValueType.unit)
            emit_default_value(ref writer, map_value_type(funcRef.signature.results[0]));
        else
            // 返回类型为 void/unit 时，FALLBACK 不压入任何值，
            // 标记 previous_was_void_call 以便后续 pop 跳过 drop，避免栈下溢
            context.previous_was_void_call = true;
    }

    /// <summary>
    ///     判断 call_intrinsic 是否为 void 返回。
    ///     基于 intrinsic 名称前缀：io.print / io.println / io.eprintln 为 void，
    ///     其余（__nyar_typecheck / __nyar_asconvert / fs.* / host.*）均返回值。
    /// </summary>
    private static bool is_intrinsic_void(string intrinsicName)
    {
        return intrinsicName.StartsWith("io.", StringComparison.Ordinal);
    }

    /// <summary>
    ///     获取 call_intrinsic 指令的栈效果。
    ///     operand[0] 为 Str（intrinsic 名称），operand[1] 为 I32（参数数量）。
    /// </summary>
    private static (int pop, int push) get_call_intrinsic_stack_effect(GenerateInstruction instruction)
    {
        var argCount = 0;
        var isIntrinsicVoid = true;

        if (instruction.operands.Count >= 2
            && instruction.operands[0] is GenerateOperand.Str nameOperand
            && instruction.operands[1] is GenerateOperand.I32 countOperand)
        {
            argCount = countOperand.value;
            isIntrinsicVoid = is_intrinsic_void(nameOperand.value);
        }

        return (argCount, isIntrinsicVoid ? 0 : 1);
    }

    /// <summary>
    ///     发射 call_intrinsic 的存根代码。
    ///     当前 WASM 后端尚未实现 intrinsic 的 import 机制，
    ///     因此丢弃所有参数，非 void 时压入默认返回值，以保持栈平衡。
    ///     栈深度跟踪由 update_stack_depth 通过 get_call_intrinsic_stack_effect 完成。
    /// </summary>
    private static void emit_call_intrinsic_stub(ref ByteBufferWriter writer, GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        var argCount = 0;
        var isIntrinsicVoid = true;

        if (instruction.operands.Count >= 2
            && instruction.operands[0] is GenerateOperand.Str nameOperand
            && instruction.operands[1] is GenerateOperand.I32 countOperand)
        {
            argCount = countOperand.value;
            isIntrinsicVoid = is_intrinsic_void(nameOperand.value);
        }

        // 丢弃所有参数
        for (var i = 0; i < argCount; i++)
            writer.write_u8((byte)WasmOpcode.drop);

        // 非 void 返回时压入默认值（i32 0）
        if (!isIntrinsicVoid)
        {
            writer.write_u8((byte)WasmOpcode.i32_const);
            writer.write_leb128_i32(0);
        }
        else
        {
            // void 返回时标记 previous_was_void_call，以便后续 pop 跳过 drop
            context.previous_was_void_call = true;
        }
    }

    private static void emit_default_value(ref ByteBufferWriter writer, WasmValueType valueType)
    {
        switch (valueType)
        {
            case WasmValueType.int32:
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;
            case WasmValueType.int64:
                writer.write_u8((byte)WasmOpcode.i64_const);
                writer.write_leb128_i64(0);
                break;
            case WasmValueType.float32:
                writer.write_u8((byte)WasmOpcode.f32_const);
                writer.write_f32_le(0);
                break;
            case WasmValueType.float64:
                writer.write_u8((byte)WasmOpcode.f64_const);
                writer.write_f64_le(0);
                break;
            case WasmValueType.v128:
                emit_prefixed_opcode(ref writer, WasmOpcode.v128_const);
                for (var i = 0; i < 16; i++) writer.write_u8(0);

                break;
            case WasmValueType.func_ref:
            case WasmValueType.extern_ref:
            case WasmValueType.any_ref:
                writer.write_u8((byte)WasmOpcode.ref_null);
                writer.write_u8((byte)valueType);
                break;
            default:
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;
        }
    }

    private static void emit_prefixed_opcode(ref ByteBufferWriter writer, WasmOpcode opcode)
    {
        var rawOpcode = (ushort)opcode;
        writer.write_u8((byte)(rawOpcode >> 8));
        writer.write_leb128_u32((uint)(rawOpcode & 0xFF));
    }

    /// <summary>
    ///     发射 dup 指令，使用临时局部变量复制栈顶值。
    ///     栈效果：+1（复制栈顶）。
    /// </summary>
    private static void emit_dup(ref ByteBufferWriter writer, WasmFunctionEmitContext context)
    {
        if (context.scratch_local_index == uint.MaxValue)
            throw new InvalidOperationException(
                $"WASM 函数 `{context.function.name}` 使用了 dup 指令但未分配临时局部变量。");

        writer.write_u8((byte)WasmOpcode.local_tee);
        writer.write_leb128_u32(context.scratch_local_index);
        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32(context.scratch_local_index);
    }

    /// <summary>
    ///     发射 new_object 指令，使用 WASM GC struct.new_default 创建对象引用。
    ///     栈效果：+1（对象引用）。
    ///     如果类型未注册为 GC 类型，则回退到 bump allocator 线性内存分配。
    /// </summary>
    private static void emit_new_object(ref ByteBufferWriter writer, GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        var typeName = "";
        if (instruction.operands.Count > 0 && instruction.operands[0] is GenerateOperand.Str strOp)
            typeName = strOp.value;

        if (string.Equals(typeName, "std.collection.Array", StringComparison.Ordinal))
            if (context.gc_type_name_to_index.TryGetValue(typeName, out var arrayTypeIdx))
            {
                var count = 0;
                if (instruction.operands.Count > 1 && instruction.operands[1] is GenerateOperand.I32 countOp)
                    count = countOp.value;

                emit_prefixed_opcode(ref writer, WasmOpcode.array_new_fixed);
                writer.write_leb128_u32(arrayTypeIdx);
                writer.write_leb128_u32((uint)count);
                return;
            }

        if (context.gc_type_name_to_index.TryGetValue(typeName, out var typeIdx))
        {
            // ref.null any → 栈：[null_any]
            writer.write_u8(unchecked((int)WasmOpcode.ref_null));
            writer.write_u8(0x6E); // anyref

            // struct.new_default typeidx → 栈：[new_obj]
            emit_prefixed_opcode(ref writer, WasmOpcode.struct_new_default);
            writer.write_leb128_u32(typeIdx);
        }
        else
        {
            // 回退：bump allocator（原始行为）
            if (context.heap_ptr_global_index == uint.MaxValue)
                throw new InvalidOperationException(
                    $"WASM 函数 `{context.function.name}` 使用了 new_object 指令但未分配堆指针全局变量。");

            if (context.scratch_local_index == uint.MaxValue)
                throw new InvalidOperationException(
                    $"WASM 函数 `{context.function.name}` 使用了 new_object 指令但未分配临时局部变量。");

            // 计算对象大小：field_count * 4 字节（每个字段假定为 i32）。
            var fieldCount = 0;
            if (instruction.operands.Count > 1 && instruction.operands[1] is GenerateOperand.I32 countOp)
                fieldCount = countOp.value;

            var objectSize = fieldCount * 4;

            // 堆分配（bump allocator）：
            // global_get heap_ptr      → [old_ptr]
            // local_tee scratch        → [old_ptr], scratch = old_ptr
            // i32.const objectSize     → [old_ptr, size]
            // i32.add                  → [old_ptr + size]
            // global_set heap_ptr      → [], heap_ptr = old_ptr + size
            // local_get scratch        → [old_ptr]
            writer.write_u8((byte)WasmOpcode.global_get);
            writer.write_leb128_u32(context.heap_ptr_global_index);
            writer.write_u8((byte)WasmOpcode.local_tee);
            writer.write_leb128_u32(context.scratch_local_index);
            writer.write_u8((byte)WasmOpcode.i32_const);
            writer.write_leb128_i32(objectSize);
            writer.write_u8((byte)WasmOpcode.i32_add);
            writer.write_u8((byte)WasmOpcode.global_set);
            writer.write_leb128_u32(context.heap_ptr_global_index);
            writer.write_u8((byte)WasmOpcode.local_get);
            writer.write_leb128_u32(context.scratch_local_index);
        }
    }

    /// <summary>
    ///     发射 get_field 指令，使用 WASM GC struct.get 读取对象字段。
    ///     栈效果：-2（对象引用 + 字段名字符串）→ 1（字段值）。
    ///     如果 GC 类型不可用，则回退：drop 两个值。
    /// </summary>
    private static void emit_get_field(ref ByteBufferWriter writer, GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        // 查找 preceding const(Str(fieldName)) 指令中的字段索引
        var fieldIndex = 0u;
        var fieldTypeName = "";
        var foundFieldName = false;

        if (context is { instructions: not null, instruction_index: > 0 })
        {
            // 向前查找 const(Str(fieldName)) 指令
            for (var i = context.instruction_index - 1; i >= 0; i--)
            {
                var prevInst = context.instructions[i];
                if (prevInst.head_code == NyarHeadCode.@const &&
                    prevInst.operands.FirstOrDefault() is GenerateOperand.Str fieldOp)
                {
                    if (context.gc_field_name_to_index.TryGetValue(fieldOp.value, out var fi))
                    {
                        fieldIndex = fi;
                        foundFieldName = true;
                    }

                    break;
                }

                // 遇到值产生指令就停止回溯
                if (prevInst.head_code == NyarHeadCode.load_arg ||
                    prevInst.head_code == NyarHeadCode.load_local ||
                    prevInst.head_code == NyarHeadCode.call_static)
                    break;
            }

            // gc_field_op_type_name 通过指令 hash 查找类型
            var key = instruction.operands.GetHashCode();
            if (context.gc_field_op_type_name.TryGetValue(key, out var tn)) fieldTypeName = tn;
        }

        if (foundFieldName && fieldTypeName != "" &&
            context.gc_type_name_to_index.TryGetValue(fieldTypeName, out var typeIdx))
        {
            // 栈：[object_ref, field_name_str]
            // 丢弃 field_name_str
            writer.write_u8((byte)WasmOpcode.drop);
            // 栈：[object_ref]

            // 保存 object_ref → scratch
            writer.write_u8((byte)WasmOpcode.local_tee);
            writer.write_leb128_u32(context.scratch_local_index);

            // 类型检查：local_get scratch ref.cast typeidx
            writer.write_u8((byte)WasmOpcode.local_get);
            writer.write_leb128_u32(context.scratch_local_index);
            emit_prefixed_opcode(ref writer, WasmOpcode.ref_cast);
            writer.write_leb128_u32(typeIdx);

            // 读取字段：struct.get typeidx fieldidx
            emit_prefixed_opcode(ref writer, WasmOpcode.struct_get);
            writer.write_leb128_u32(typeIdx);
            writer.write_leb128_u32(fieldIndex);
            // 栈：[field_value]
        }
        else
        {
            // 回退：丢弃 object_ref、field_name_str，压入默认值保持栈平衡
            writer.write_u8((byte)WasmOpcode.drop);
            writer.write_u8((byte)WasmOpcode.drop);
            writer.write_u8((byte)WasmOpcode.i32_const);
            writer.write_leb128_i32(0);
        }
    }

    /// <summary>
    ///     发射 set_field 指令，使用 WASM GC struct.set 写入对象字段。
    ///     栈效果：-3（对象引用 + 字段名字符串 + 值）。
    ///     如果 GC 类型不可用，则回退：drop 三个值。
    /// </summary>
    private static void emit_set_field(ref ByteBufferWriter writer, GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        // 查找 preceding const(Str(fieldName)) 指令中的字段索引
        var fieldIndex = 0u;
        var fieldTypeName = "";
        var foundFieldName = false;

        if (context is { instructions: not null, instruction_index: > 0 })
        {
            // 向前查找 const(Str(fieldName)) 指令
            for (var i = context.instruction_index - 1; i >= 0; i--)
            {
                var prevInst = context.instructions[i];
                if (prevInst.head_code == NyarHeadCode.@const &&
                    prevInst.operands.FirstOrDefault() is GenerateOperand.Str fieldOp)
                {
                    if (context.gc_field_name_to_index.TryGetValue(fieldOp.value, out var fi))
                    {
                        fieldIndex = fi;
                        foundFieldName = true;
                    }

                    break;
                }

                // 遇到值产生指令就停止回溯
                if (prevInst.head_code == NyarHeadCode.load_arg ||
                    prevInst.head_code == NyarHeadCode.load_local ||
                    prevInst.head_code == NyarHeadCode.call_static)
                    break;
            }

            // gc_field_op_type_name 通过指令 hash 查找类型
            var key = instruction.operands.GetHashCode();
            if (context.gc_field_op_type_name.TryGetValue(key, out var tn)) fieldTypeName = tn;
        }

        if (foundFieldName && fieldTypeName != "" &&
            context.gc_type_name_to_index.TryGetValue(fieldTypeName, out var typeIdx))
        {
            // 栈：[object_ref, field_name_str, value]
            // 保存 value → scratch
            writer.write_u8((byte)WasmOpcode.local_set);
            writer.write_leb128_u32(context.scratch_local_index);
            // 栈：[object_ref, field_name_str], scratch=value

            // 丢弃 field_name_str
            writer.write_u8((byte)WasmOpcode.drop);
            // 栈：[object_ref], scratch=value

            // 保存 object_ref → scratch2（local_set 消费栈顶，不保留）
            writer.write_u8((byte)WasmOpcode.local_set);
            writer.write_leb128_u32(context.scratch_local_index2);
            // 栈：[], scratch=value, scratch2=object_ref

            // 类型检查：local_get scratch2 ref.cast typeidx
            writer.write_u8((byte)WasmOpcode.local_get);
            writer.write_leb128_u32(context.scratch_local_index2);
            emit_prefixed_opcode(ref writer, WasmOpcode.ref_cast);
            writer.write_leb128_u32(typeIdx);

            // 设置字段：local_get scratch struct.set typeidx fieldidx
            writer.write_u8((byte)WasmOpcode.local_get);
            writer.write_leb128_u32(context.scratch_local_index);
            emit_prefixed_opcode(ref writer, WasmOpcode.struct_set);
            writer.write_leb128_u32(typeIdx);
            writer.write_leb128_u32(fieldIndex);
            // 栈：[]（set_field 净效果为 0，与 LIR 预期一致）
        }
        else
        {
            // 回退：无法确定 GC 类型时，丢弃三个栈值保持栈平衡
            // 栈效果：-3（对象引用 + 字段名字符串 + 值）
            writer.write_u8((byte)WasmOpcode.drop);
            writer.write_u8((byte)WasmOpcode.drop);
            writer.write_u8((byte)WasmOpcode.drop);
        }
    }

    /// <summary>
    ///     发射 get_index 指令，使用 WASM GC array.get 读取数组元素。
    ///     栈效果：-2（数组引用 + 索引）→ 1（元素值）。
    /// </summary>
    private static void emit_get_index(ref ByteBufferWriter writer, WasmFunctionEmitContext context)
    {
        // 栈：[array_ref, index]
        // 手动计算 i32 load 地址: array_ref + (index * 4)
        // 无需 scratch，直接计算：
        //   i32_const 4   → [ref, idx, 4]
        //   i32_mul       → [ref, idx*4]
        //   i32_add       → [ref + idx*4]
        //   i32.load      → [result]
        writer.write_u8((byte)WasmOpcode.i32_const);
        writer.write_leb128_i32(4);
        writer.write_u8((byte)WasmOpcode.i32_mul);
        writer.write_u8((byte)WasmOpcode.i32_add);
        writer.write_u8((byte)WasmOpcode.i32_load);
        writer.write_leb128_u32(0);
        writer.write_leb128_u32(0);
    }

    /// <summary>
    ///     发射 set_index 指令，使用 i32.store 写入数组元素。
    ///     栈效果：-3（数组引用 + 索引 + 值）。
    /// </summary>
    private static void emit_set_index(ref ByteBufferWriter writer, WasmFunctionEmitContext context)
    {
        // 栈：[array_ref, index, value]
        // 需要 1 个 scratch 暂存 value
        writer.write_u8((byte)WasmOpcode.local_set);
        writer.write_leb128_u32(context.scratch_local_index);
        writer.write_u8((byte)WasmOpcode.i32_const);
        writer.write_leb128_i32(4);
        writer.write_u8((byte)WasmOpcode.i32_mul);
        writer.write_u8((byte)WasmOpcode.i32_add);
        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32(context.scratch_local_index);
        writer.write_u8((byte)WasmOpcode.i32_store);
        writer.write_leb128_u32(0);
        writer.write_leb128_u32(0);
    }

    /// <summary>
    ///     发射内存存储指令（i32_store / i64_store），保留地址在栈上以支持循环写入。
    ///     栈效果：(addr, value) → (addr)
    ///     使用两个临时局部变量：scratch_local_index 保存地址，scratch_local_index2 保存值。
    /// </summary>
    private static void emit_mem_store(ref ByteBufferWriter writer, WasmOpcode storeOpcode, uint alignment,
        WasmFunctionEmitContext context)
    {
        if (context.scratch_local_index == uint.MaxValue || context.scratch_local_index2 == uint.MaxValue)
            throw new InvalidOperationException(
                $"WASM 函数 `{context.function.name}` 使用了 {storeOpcode} 指令但未分配两个临时局部变量。");

        // 栈：[addr, value]
        // 保存 value → scratch2，栈：[addr]
        writer.write_u8((byte)WasmOpcode.local_set);
        writer.write_leb128_u32(context.scratch_local_index2);

        // 保存 addr → scratch1 并保持在栈上，栈：[addr]
        writer.write_u8((byte)WasmOpcode.local_tee);
        writer.write_leb128_u32(context.scratch_local_index);

        // 恢复 value，栈：[addr, value]
        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32(context.scratch_local_index2);

        // 执行存储，栈：[]
        writer.write_u8((byte)storeOpcode);
        writer.write_leb128_u32(alignment);
        writer.write_leb128_u32(0);

        // 恢复地址，栈：[addr]
        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32(context.scratch_local_index);
    }

    /// <summary>
    ///     为 void/unit 返回类型的 import 调用生成合成返回值。
    ///     注意：WASM 中 void/unit 函数在栈上不生成任何值，因此此方法为空操作。
    ///     保留此方法以保持调用点一致，避免歧义。
    ///     实际 void/unit 的 import 调用后不需要任何额外的栈操作。
    /// </summary>
    private static void emit_synthetic_unit_result(ref ByteBufferWriter writer, GenerateFunctionType signature)
    {
        // void/unit 返回类型不需要在栈上推入任何值。
    }

    /// <summary>
    ///     发射 alloc 指令，使用 bump allocator 从线性内存分配空间。
    ///     栈效果：(size) → (address)
    ///     需要两个临时局部变量：scratch_local_index 暂存旧指针，scratch_local_index2 暂存 size。
    /// </summary>
    private static void emit_alloc(ref ByteBufferWriter writer, WasmFunctionEmitContext context)
    {
        if (context.heap_ptr_global_index == uint.MaxValue)
            throw new InvalidOperationException(
                $"WASM 函数 `{context.function.name}` 使用了 alloc 指令但未分配堆指针全局变量。");

        if (context.scratch_local_index == uint.MaxValue || context.scratch_local_index2 == uint.MaxValue)
            throw new InvalidOperationException(
                $"WASM 函数 `{context.function.name}` 使用了 alloc 指令但未分配两个临时局部变量。");

        // 栈：[size]
        // 保存 size → scratch2，栈：[]
        writer.write_u8((byte)WasmOpcode.local_set);
        writer.write_leb128_u32(context.scratch_local_index2);

        // global_get heap_ptr   → [old_ptr]
        writer.write_u8((byte)WasmOpcode.global_get);
        writer.write_leb128_u32(context.heap_ptr_global_index);

        // 保存 old_ptr → scratch1 并保持在栈上   → [old_ptr]
        writer.write_u8((byte)WasmOpcode.local_tee);
        writer.write_leb128_u32(context.scratch_local_index);

        // local_get scratch2   → [old_ptr, size]
        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32(context.scratch_local_index2);

        // i32.add   → [new_ptr]
        writer.write_u8((byte)WasmOpcode.i32_add);

        // global_set heap_ptr   → [], heap_ptr = new_ptr
        writer.write_u8((byte)WasmOpcode.global_set);
        writer.write_leb128_u32(context.heap_ptr_global_index);

        // local_get scratch1   → [old_ptr]（返回分配的内存地址）
        writer.write_u8((byte)WasmOpcode.local_get);
        writer.write_leb128_u32(context.scratch_local_index);
    }
}