using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     <see cref="WasmBackend" /> 的 partial 文件，承载主指令发射方法 <see cref="emit_instruction" />。
///     其他辅助发射方法位于 WasmBackend.Emit.Helpers.cs 中。
/// </summary>
public sealed partial class WasmBackend
{
    private static void emit_instruction(ref ByteBufferWriter writer, GenerateInstruction instruction,
        WasmFunctionEmitContext context)
    {
        // pop 指令需要在重置 previous_was_void_call 之前检查，
        // 因为 call_static(void) 设置的标志应在紧接着的 pop 中生效。
        if (instruction.opcode == NyarHeadCode.pop)
        {
            var isNextStore = context.is_next_instruction_store();
            var isPrevVoidCall = context.is_previous_void_call() || context.previous_was_void_call;
            context.previous_was_void_call = false;
            if (isNextStore || isPrevVoidCall)
                // void 调用的 pop 不发射 drop，栈深度不变
                return;

            writer.write_u8((byte)WasmOpcode.drop);
            context.stack_depth = Math.Max(0, context.stack_depth - 1);
            return;
        }

        // 所有非 pop 指令均重置 previous_was_void_call 标志。
        // 避免 void call 的标志被后续值产生指令（如 const）携带到不应跳过的 pop 处。
        // call_static 处理函数内部会根据实际返回类型重新设置此标志。
        context.previous_was_void_call = false;

        switch (instruction.head_code)
        {
            case NyarHeadCode.nop:
                writer.write_u8((byte)WasmOpcode.nop);
                break;
            case NyarHeadCode.@const:
                emit_const(ref writer, instruction, context);
                break;
            case NyarHeadCode.load_arg:
                emit_load_arg(ref writer, instruction);
                break;
            case NyarHeadCode.store_arg:
                emit_store_arg(ref writer, instruction);
                break;
            case NyarHeadCode.load_local:
                emit_load_local(ref writer, instruction, context.function);
                break;
            case NyarHeadCode.store_local:
                emit_store_local(ref writer, instruction, context.function);
                break;
            case NyarHeadCode.i32_add:
                writer.write_u8((byte)WasmOpcode.i32_add);
                break;
            case NyarHeadCode.i32_sub:
                writer.write_u8((byte)WasmOpcode.i32_sub);
                break;
            case NyarHeadCode.i32_mul:
                writer.write_u8((byte)WasmOpcode.i32_mul);
                break;
            case NyarHeadCode.i32_div_s:
                writer.write_u8((byte)WasmOpcode.i32_div_s);
                break;
            case NyarHeadCode.i32_rem_s:
                writer.write_u8((byte)WasmOpcode.i32_rem_s);
                break;
            case NyarHeadCode.i32_neg:
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                writer.write_u8((byte)WasmOpcode.i32_sub);
                break;
            case NyarHeadCode.i32_eq:
                writer.write_u8((byte)WasmOpcode.i32_eq);
                break;
            case NyarHeadCode.i32_ne:
                writer.write_u8((byte)WasmOpcode.i32_ne);
                break;
            case NyarHeadCode.i32_lt_s:
                writer.write_u8((byte)WasmOpcode.i32_lt_s);
                break;
            case NyarHeadCode.i32_le_s:
                writer.write_u8((byte)WasmOpcode.i32_le_s);
                break;
            case NyarHeadCode.i32_gt_s:
                writer.write_u8((byte)WasmOpcode.i32_gt_s);
                break;
            case NyarHeadCode.i32_ge_s:
                writer.write_u8((byte)WasmOpcode.i32_ge_s);
                break;
            case NyarHeadCode.i32_and:
                writer.write_u8((byte)WasmOpcode.i32_and);
                break;
            case NyarHeadCode.i32_or:
                writer.write_u8((byte)WasmOpcode.i32_or);
                break;
            case NyarHeadCode.i32_xor:
                writer.write_u8((byte)WasmOpcode.i32_xor);
                break;
            case NyarHeadCode.i32_shl:
                writer.write_u8((byte)WasmOpcode.i32_shl);
                break;
            case NyarHeadCode.i32_shr_s:
                writer.write_u8((byte)WasmOpcode.i32_shr_s);
                break;
            case NyarHeadCode.i32_shr_u:
                writer.write_u8((byte)WasmOpcode.i32_shr_u);
                break;
            case NyarHeadCode.i32_not:
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(-1);
                writer.write_u8((byte)WasmOpcode.i32_xor);
                break;
            case NyarHeadCode.i32_div_u:
                writer.write_u8((byte)WasmOpcode.i32_div_u);
                break;
            case NyarHeadCode.i32_rem_u:
                writer.write_u8((byte)WasmOpcode.i32_rem_u);
                break;
            case NyarHeadCode.i32_lt_u:
                writer.write_u8((byte)WasmOpcode.i32_lt_u);
                break;
            case NyarHeadCode.i32_le_u:
                writer.write_u8((byte)WasmOpcode.i32_le_u);
                break;
            case NyarHeadCode.i32_gt_u:
                writer.write_u8((byte)WasmOpcode.i32_gt_u);
                break;
            case NyarHeadCode.i32_ge_u:
                writer.write_u8((byte)WasmOpcode.i32_ge_u);
                break;
            case NyarHeadCode.any_to_i32:
            case NyarHeadCode.any_to_utf8:
                // WASM 后端中所有引用类型（any/object/utf8）均映射为 i32。
                // 因此 any_to_* 转换操作在 WASM 上为无操作（值已在栈上为 i32）
                break;

            // ---- 内存操作 ----
            case NyarHeadCode.i32_load:
                // 栈：(addr) → (value_i32)
                writer.write_u8((byte)WasmOpcode.i32_load);
                writer.write_leb128_u32(2);
                writer.write_leb128_u32(0);
                break;

            case NyarHeadCode.i32_store:
                emit_mem_store(ref writer, WasmOpcode.i32_store, 2, context);
                break;

            case NyarHeadCode.i64_load:
                // 栈：(addr) → (value_i64)
                writer.write_u8((byte)WasmOpcode.i64_load);
                writer.write_leb128_u32(3);
                writer.write_leb128_u32(0);
                break;

            case NyarHeadCode.i64_store:
                emit_mem_store(ref writer, WasmOpcode.i64_store, 3, context);
                break;

            case NyarHeadCode.alloc:
                emit_alloc(ref writer, context);
                break;
            case NyarHeadCode.free:
                // 简化实现：释放操作不改变线性内存，仅弹出地址
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.load_global:
                // 简化实现：push 0 占位（完整实现需查找全局变量索引）
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;
            case NyarHeadCode.store_global:
                // 简化实现：弹出值（完整实现需查找全局变量索引）
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.i64_eq:
                writer.write_u8((byte)WasmOpcode.i64_eq);
                break;
            case NyarHeadCode.i64_ne:
                writer.write_u8((byte)WasmOpcode.i64_ne);
                break;
            case NyarHeadCode.i64_lt_s:
                writer.write_u8((byte)WasmOpcode.i64_lt_s);
                break;
            case NyarHeadCode.i64_le_s:
                writer.write_u8((byte)WasmOpcode.i64_le_s);
                break;
            case NyarHeadCode.i64_gt_s:
                writer.write_u8((byte)WasmOpcode.i64_gt_s);
                break;
            case NyarHeadCode.i64_ge_s:
                writer.write_u8((byte)WasmOpcode.i64_ge_s);
                break;
            case NyarHeadCode.i64_lt_u:
                writer.write_u8((byte)WasmOpcode.i64_lt_u);
                break;
            case NyarHeadCode.i64_le_u:
                writer.write_u8((byte)WasmOpcode.i64_le_u);
                break;
            case NyarHeadCode.i64_gt_u:
                writer.write_u8((byte)WasmOpcode.i64_gt_u);
                break;
            case NyarHeadCode.i64_ge_u:
                writer.write_u8((byte)WasmOpcode.i64_ge_u);
                break;
            case NyarHeadCode.i64_add:
                writer.write_u8((byte)WasmOpcode.i64_add);
                break;
            case NyarHeadCode.i64_sub:
                writer.write_u8((byte)WasmOpcode.i64_sub);
                break;
            case NyarHeadCode.i64_mul:
                writer.write_u8((byte)WasmOpcode.i64_mul);
                break;
            case NyarHeadCode.i64_div_s:
                writer.write_u8((byte)WasmOpcode.i64_div_s);
                break;
            case NyarHeadCode.i64_rem_s:
                writer.write_u8((byte)WasmOpcode.i64_rem_s);
                break;
            case NyarHeadCode.i64_neg:
                writer.write_u8((byte)WasmOpcode.i64_const);
                writer.write_leb128_i64(0);
                writer.write_u8((byte)WasmOpcode.i64_sub);
                break;
            case NyarHeadCode.i64_and:
                writer.write_u8((byte)WasmOpcode.i64_and);
                break;
            case NyarHeadCode.i64_or:
                writer.write_u8((byte)WasmOpcode.i64_or);
                break;
            case NyarHeadCode.i64_xor:
                writer.write_u8((byte)WasmOpcode.i64_xor);
                break;
            case NyarHeadCode.i64_shl:
                writer.write_u8((byte)WasmOpcode.i64_shl);
                break;
            case NyarHeadCode.i64_shr_s:
                writer.write_u8((byte)WasmOpcode.i64_shr_s);
                break;
            case NyarHeadCode.i64_shr_u:
                writer.write_u8((byte)WasmOpcode.i64_shr_u);
                break;
            case NyarHeadCode.i64_not:
                writer.write_u8((byte)WasmOpcode.i64_const);
                writer.write_leb128_i64(-1);
                writer.write_u8((byte)WasmOpcode.i64_xor);
                break;
            case NyarHeadCode.i64_div_u:
                writer.write_u8((byte)WasmOpcode.i64_div_u);
                break;
            case NyarHeadCode.i64_rem_u:
                writer.write_u8((byte)WasmOpcode.i64_rem_u);
                break;
            case NyarHeadCode.ref_eq:
                // WASM 后端中引用类型（any/object/external_ref/utf8）映射为 i32。
                // 因此使用 i32.eq 而非 ref.eq（后者要求 eqref 类型操作数）
                writer.write_u8((byte)WasmOpcode.i32_eq);
                break;
            case NyarHeadCode.ref_ne:
                writer.write_u8((byte)WasmOpcode.i32_ne);
                break;
            case NyarHeadCode.i32_extend_i64_s:
                writer.write_u8((byte)WasmOpcode.i64_extend_i32_s);
                break;
            case NyarHeadCode.i32_extend_i64_u:
                writer.write_u8((byte)WasmOpcode.i64_extend_i32_u);
                break;
            case NyarHeadCode.i64_trunc_i32_s:
                writer.write_u8((byte)WasmOpcode.i32_wrap_i64);
                break;
            case NyarHeadCode.i64_trunc_i32_u:
                writer.write_u8((byte)WasmOpcode.i32_wrap_i64);
                break;
            case NyarHeadCode.i32_to_f32_s:
                writer.write_u8((byte)WasmOpcode.f32_convert_i32_s);
                break;
            case NyarHeadCode.i32_to_f64_s:
                writer.write_u8((byte)WasmOpcode.f64_convert_i32_s);
                break;
            case NyarHeadCode.i64_to_f64:
                writer.write_u8((byte)WasmOpcode.f64_convert_i64_s);
                break;
            case NyarHeadCode.f64_to_i32:
                writer.write_u8((byte)WasmOpcode.i32_trunc_f64_s);
                break;
            case NyarHeadCode.f64_to_i64:
                writer.write_u8((byte)WasmOpcode.i64_trunc_f64_s);
                break;
            case NyarHeadCode.f64_add:
                writer.write_u8((byte)WasmOpcode.f64_add);
                break;
            case NyarHeadCode.f64_sub:
                writer.write_u8((byte)WasmOpcode.f64_sub);
                break;
            case NyarHeadCode.f64_mul:
                writer.write_u8((byte)WasmOpcode.f64_mul);
                break;
            case NyarHeadCode.f64_div:
                writer.write_u8((byte)WasmOpcode.f64_div);
                break;
            case NyarHeadCode.f32_add:
                writer.write_u8((byte)WasmOpcode.f32_add);
                break;
            case NyarHeadCode.f32_sub:
                writer.write_u8((byte)WasmOpcode.f32_sub);
                break;
            case NyarHeadCode.f32_mul:
                writer.write_u8((byte)WasmOpcode.f32_mul);
                break;
            case NyarHeadCode.f32_div:
                writer.write_u8((byte)WasmOpcode.f32_div);
                break;
            case NyarHeadCode.f32_neg:
                writer.write_u8((byte)WasmOpcode.f32_neg);
                break;
            case NyarHeadCode.f64_sqrt:
                writer.write_u8((byte)WasmOpcode.f64_sqrt);
                break;
            case NyarHeadCode.f64_neg:
                writer.write_u8((byte)WasmOpcode.f64_neg);
                break;
            case NyarHeadCode.f64_eq:
                writer.write_u8((byte)WasmOpcode.f64_eq);
                break;
            case NyarHeadCode.f64_ne:
                writer.write_u8((byte)WasmOpcode.f64_ne);
                break;
            case NyarHeadCode.f64_lt:
                writer.write_u8((byte)WasmOpcode.f64_lt);
                break;
            case NyarHeadCode.f64_gt:
                writer.write_u8((byte)WasmOpcode.f64_gt);
                break;
            case NyarHeadCode.f64_le:
                writer.write_u8((byte)WasmOpcode.f64_le);
                break;
            case NyarHeadCode.f64_ge:
                writer.write_u8((byte)WasmOpcode.f64_ge);
                break;
            case NyarHeadCode.simd:
                emit_prefixed_opcode(ref writer, WasmOpcode.v128_const);
                for (var i = 0; i < 16; i++) writer.write_u8(0);

                break;
            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                emit_call_static(ref writer, instruction, context);
                break;
            case NyarHeadCode.@return:
                writer.write_u8((byte)WasmOpcode.@return);
                // return 之后，标记不可达，后续指令全部跳过
                // 此标记同时被 emit_linear_range / emit_structured_instructions / build_code_internal 的 else 分支消费
                context.unreachable = true;
                context.unreachable_by_return = true;
                break;

            case NyarHeadCode.dup:
                emit_dup(ref writer, context);
                break;

            case NyarHeadCode.new_object:
                emit_new_object(ref writer, instruction, context);
                break;

            case NyarHeadCode.get_field:
                emit_get_field(ref writer, instruction, context);
                break;

            case NyarHeadCode.set_field:
                emit_set_field(ref writer, instruction, context);
                break;

            case NyarHeadCode.array_get:
            case NyarHeadCode.get_offset_index:
            case NyarHeadCode.get_ordinal_index:
                emit_get_index(ref writer, context);
                break;

            case NyarHeadCode.array_set:
            case NyarHeadCode.set_offset_index:
            case NyarHeadCode.set_ordinal_index:
                emit_set_index(ref writer, context);
                break;

            case NyarHeadCode.enter_effect_handler:
                writer.write_u8((byte)WasmOpcode.nop);
                break;

            case NyarHeadCode.exit_effect_handler:
                writer.write_u8((byte)WasmOpcode.nop);
                break;

            case NyarHeadCode.perform_effect:
                writer.write_u8((byte)WasmOpcode.nop);
                break;

            case NyarHeadCode.resume:
                writer.write_u8((byte)WasmOpcode.nop);
                break;

            case NyarHeadCode.enter_try:
                writer.write_u8((byte)WasmOpcode.nop);
                break;

            case NyarHeadCode.array_push:
                // 栈：(array_ref, value_ref) → (array_ref)
                // WASM 简化实现：弹出 value 保留 array_ref（保持栈效果匹配）
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.exit_try:
                writer.write_u8((byte)WasmOpcode.nop);
                break;

            case NyarHeadCode.length:
                // 栈：(ref) → (i32)
                // WASM 简化实现：弹出引用，推入 0（占位）
                // 完整实现需要从对象/数组/字符串头部读取长度字段
                writer.write_u8((byte)WasmOpcode.drop);
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;

            case NyarHeadCode.new_closure:
                // 简化实现：推入 0（占位闭包引用）
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;
            case NyarHeadCode.get_upvalue:
                // 简化实现：推入 0（占位上值引用）
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;
            case NyarHeadCode.set_upvalue:
                // 简化实现：弹出值。
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.call_witness:
            case NyarHeadCode.call_dynamic:
                // WASM 后端不支持 witness 分派和动态分派，使用 fallback 保持栈平衡。
                if (instruction.operands.FirstOrDefault() is GenerateOperand.FuncRef funcRef)
                {
                    // 丢弃所有参数。
                    foreach (var _ in funcRef.signature.parameters) writer.write_u8((byte)WasmOpcode.drop);

                    // 压入默认返回值。
                    foreach (var result in funcRef.signature.results)
                        if (result != GenerateValueType.@void && result != GenerateValueType.unit)
                            emit_default_value(ref writer, map_value_type(result));
                }
                else
                {
                    // 无法确定签名时，使用 unreachable
                    writer.write_u8((byte)WasmOpcode.unreachable);
                }

                break;

            case NyarHeadCode.access_static:
            case NyarHeadCode.access_witness:
            case NyarHeadCode.access_dynamic:
                // 简化实现：推入 0（占位字段值）
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;

            case NyarHeadCode.field_store:
                // 字段写入（字段名由常量池索引指定），栈效果：弹出 1 个值
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.utf8_eq:
                // 栈：(str1, str2) → (i32)
                // WASM 简化实现：弹出两个字符串引用，做指针相等比较。
                // 完整实现需要逐字节比较或调用导入的字符串比较函数
                writer.write_u8((byte)WasmOpcode.i32_eq);
                break;

            case NyarHeadCode.utf8_ne:
                // 栈：(str1, str2) → (i32)
                writer.write_u8((byte)WasmOpcode.i32_ne);
                break;

            case NyarHeadCode.utf8_concat:
                // 栈：(str1, str2) → (ref)
                // WASM 简化实现：弹出第二个字符串，保留第一个作为占位
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.utf8_len_bytes:
            case NyarHeadCode.utf8_len_chars:
                // 栈：(str) → (i32)
                // WASM 简化实现：弹出字符串引用，推入 0（占位长度）
                writer.write_u8((byte)WasmOpcode.drop);
                writer.write_u8((byte)WasmOpcode.i32_const);
                writer.write_leb128_i32(0);
                break;

            case NyarHeadCode.utf8_substr:
                // 栈：(str, start, end) → (str)
                // WASM 简化实现：弹出 start 和 end，保留原始字符串引用
                writer.write_u8((byte)WasmOpcode.drop);
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.index_store:
                // 索引写入（对象、索引、值从栈弹出），栈效果：弹出 3 个值
                writer.write_u8((byte)WasmOpcode.drop);
                writer.write_u8((byte)WasmOpcode.drop);
                writer.write_u8((byte)WasmOpcode.drop);
                break;

            case NyarHeadCode.call_intrinsic:
                emit_call_intrinsic_stub(ref writer, instruction, context);
                break;

            case NyarHeadCode.swap:
                // 当前 WASM 后端先忽略 swap，依赖前端尽量消除该指令。
                Console.Error.WriteLine($"[WASM] 警告: swap 指令在 {context.function.name} 中被忽略");
                break;

            default:
                // 防御性处理：未实现的 opcode 静默忽略可能导致栈不平衡
                // 这里记录错误以便调试，但不抛异常以保持构建可继续
                Console.Error.WriteLine($"[WASM] 未实现的 opcode: {instruction.head_code}");
                break;
        }

        // 更新栈深度估计（用于末尾兜底逻辑）。
        update_stack_depth(instruction, context);
    }
}