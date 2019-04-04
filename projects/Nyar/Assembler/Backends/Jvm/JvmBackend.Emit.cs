using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm.Data;
using Std.Data.Binary.NyarIR.Data;
using static Nyar.Assembler.Backends.Jvm.JvmConstantPoolHelper;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 后端的 partial 文件，承载主指令发射方法 <see cref="emit_instruction" />。
///     所有辅助发射方法位于 JvmBackend.EmitHelpers.cs 中。
/// </summary>
public partial class JvmBackend
{
    private static void emit_instruction(ref ByteBufferWriter writer, ref int stackDepth,
        IReadOnlyDictionary<string, int> labelEntryDepths, GenerateInstruction instruction,
        GenerateFunction function, List<JvmConstant> constantPool, IReadOnlyList<string> moduleStrings,
        List<JvmBootstrapMethod> bootstrapMethods, string className, int instructionIndex,
        IReadOnlyList<int> instructionOffsets,
        IReadOnlyDictionary<string, int> labelOffsets,
        JvmExternalLinkTable linkTable,
        IReadOnlyList<GenerateFunction> allFunctions)
    {
        // 特殊处理：跳转到不可达标签时，重置栈深度
        if (instruction.head_code == NyarHeadCode.jump && instruction.operands.Count > 0
                                                       && instruction.operands[0] is GenerateOperand.Label jumpLabel
                                                       && labelEntryDepths.TryGetValue(jumpLabel.name, out var entryDepth))
            stackDepth = entryDepth;

        // 无条件跳转不更新栈深度
        if (instruction.head_code == NyarHeadCode.jump) return;

        // pop 指令
        if (instruction.head_code == NyarHeadCode.pop)
        {
            stackDepth = Math.Max(0, stackDepth - 1);
            writer.write_u8((byte)JvmOpcode.pop);
            return;
        }

        // dup 指令
        if (instruction.head_code == NyarHeadCode.dup)
        {
            stackDepth++;
            writer.write_u8((byte)JvmOpcode.dup);
            return;
        }

        // swap 指令
        if (instruction.head_code == NyarHeadCode.swap)
        {
            writer.write_u8((byte)JvmOpcode.swap);
            return;
        }

        // nop 指令
        if (instruction.head_code == NyarHeadCode.nop)
        {
            writer.write_u8((byte)JvmOpcode.nop);
            return;
        }

        // return 指令
        // 注意：返回类型必须与方法描述符一致，方法描述符使用 function.return_type_ref，
        // 因此这里也使用 function.return_type_ref 而非 function.signature。
        // 对于 unit 返回类型，JVM 映射为 int，但 LIR 不会在栈上留返回值，
        // 因此需要先压入默认 int 值 0 再 ireturn。
        if (instruction.head_code == NyarHeadCode.@return)
        {
            var returnType = JvmTypeMap.to_jvm_type_name(function.return_type_ref);
            if (returnType == "void")
                writer.write_u8((byte)JvmOpcode.@return);
            else if (returnType == "int" || returnType == "boolean" || returnType == "char" || returnType == "short" || returnType == "byte")
            {
                // unit 映射为 int，LIR 不会在栈上留值，需要压入默认值
                if (function.return_type_ref.kind == GenerateTypeKind.unit)
                    emit_i32_const(ref writer, 0, constantPool);
                writer.write_u8((byte)JvmOpcode.ireturn);
            }
            else if (returnType == "long")
            {
                if (function.return_type_ref.kind == GenerateTypeKind.unit)
                    emit_i64_const(ref writer, 0, constantPool);
                writer.write_u8((byte)JvmOpcode.lreturn);
            }
            else if (returnType == "float")
            {
                if (function.return_type_ref.kind == GenerateTypeKind.unit)
                    writer.write_u8((byte)JvmOpcode.fconst_0);
                writer.write_u8((byte)JvmOpcode.freturn);
            }
            else if (returnType == "double")
            {
                if (function.return_type_ref.kind == GenerateTypeKind.unit)
                    writer.write_u8((byte)JvmOpcode.dconst_0);
                writer.write_u8((byte)JvmOpcode.dreturn);
            }
            else
            {
                if (function.return_type_ref.kind == GenerateTypeKind.unit)
                    writer.write_u8((byte)JvmOpcode.aconst_null);
                writer.write_u8((byte)JvmOpcode.areturn);
            }

            return;
        }

        // const 常量指令
        if (instruction.head_code == NyarHeadCode.@const)
        {
            if (instruction.operands.Count > 0 && instruction.operands[0] is GenerateOperand.Const constOp)
                emit_jvm_const(ref writer, constOp, function, moduleStrings, constantPool, bootstrapMethods, className);
            else if (instruction.operands.Count > 0) emit_jvm_const(ref writer, instruction.operands[0], function, moduleStrings, constantPool, bootstrapMethods, className);

            stackDepth++;
            return;
        }

        // load_arg 加载参数
        if (instruction.head_code == NyarHeadCode.load_arg)
        {
            emit_load_arg(ref writer, instruction);
            stackDepth++;
            return;
        }

        // store_arg 存储参数
        if (instruction.head_code == NyarHeadCode.store_arg)
        {
            emit_store_arg(ref writer, instruction);
            stackDepth = Math.Max(0, stackDepth - 1);
            return;
        }

        // load_local 加载局部变量
        if (instruction.head_code == NyarHeadCode.load_local)
        {
            emit_load_local(ref writer, instruction);
            stackDepth++;
            return;
        }

        // store_local 存储局部变量
        if (instruction.head_code == NyarHeadCode.store_local)
        {
            emit_store_local(ref writer, instruction);
            stackDepth = Math.Max(0, stackDepth - 1);
            return;
        }

        // load_global 加载全局变量
        if (instruction.head_code == NyarHeadCode.load_global)
        {
            emit_load_global(ref writer, instruction);
            stackDepth++;
            return;
        }

        // store_global 存储全局变量（JVM 使用 putstatic）
        if (instruction.head_code == NyarHeadCode.store_global)
        {
            stackDepth = Math.Max(0, stackDepth - 1);
            return;
        }

        // 处理各种运算和操作指令
        switch (instruction.head_code)
        {
            // ---- i32 算术运算 ----
            case NyarHeadCode.i32_add:
                writer.write_u8((byte)JvmOpcode.iadd);
                break;
            case NyarHeadCode.i32_sub:
                writer.write_u8((byte)JvmOpcode.isub);
                break;
            case NyarHeadCode.i32_mul:
                writer.write_u8((byte)JvmOpcode.imul);
                break;
            case NyarHeadCode.i32_div_s:
                writer.write_u8((byte)JvmOpcode.idiv);
                break;
            case NyarHeadCode.i32_rem_s:
                writer.write_u8((byte)JvmOpcode.irem);
                break;
            case NyarHeadCode.i32_and:
                writer.write_u8((byte)JvmOpcode.iand);
                break;
            case NyarHeadCode.i32_or:
                writer.write_u8((byte)JvmOpcode.ior);
                break;
            case NyarHeadCode.i32_xor:
                writer.write_u8((byte)JvmOpcode.ixor);
                break;
            case NyarHeadCode.i32_shl:
                writer.write_u8((byte)JvmOpcode.ishl);
                break;
            case NyarHeadCode.i32_shr_s:
                writer.write_u8((byte)JvmOpcode.ishr);
                break;
            case NyarHeadCode.i32_shr_u:
                writer.write_u8((byte)JvmOpcode.iushr);
                break;
            case NyarHeadCode.i32_not:
                emit_i32_const(ref writer, -1, constantPool);
                writer.write_u8((byte)JvmOpcode.ixor);
                break;
            case NyarHeadCode.i32_neg:
                writer.write_u8((byte)JvmOpcode.ineg);
                break;

            // ---- i64 算术运算 ----
            case NyarHeadCode.i64_add:
                writer.write_u8((byte)JvmOpcode.ladd);
                break;
            case NyarHeadCode.i64_sub:
                writer.write_u8((byte)JvmOpcode.lsub);
                break;
            case NyarHeadCode.i64_mul:
                writer.write_u8((byte)JvmOpcode.lmul);
                break;
            case NyarHeadCode.i64_div_s:
                writer.write_u8((byte)JvmOpcode.ldiv);
                break;
            case NyarHeadCode.i64_rem_s:
                writer.write_u8((byte)JvmOpcode.lrem);
                break;
            case NyarHeadCode.i64_and:
                writer.write_u8((byte)JvmOpcode.land);
                break;
            case NyarHeadCode.i64_or:
                writer.write_u8((byte)JvmOpcode.lor);
                break;
            case NyarHeadCode.i64_xor:
                writer.write_u8((byte)JvmOpcode.lxor);
                break;
            case NyarHeadCode.i64_shl:
                writer.write_u8((byte)JvmOpcode.lshl);
                break;
            case NyarHeadCode.i64_shr_s:
                writer.write_u8((byte)JvmOpcode.lshr);
                break;
            case NyarHeadCode.i64_shr_u:
                writer.write_u8((byte)JvmOpcode.lushr);
                break;
            case NyarHeadCode.i64_neg:
                writer.write_u8((byte)JvmOpcode.lneg);
                break;

            // ---- 类型转换 ----
            case NyarHeadCode.i64_trunc_i32_s:
                writer.write_u8((byte)JvmOpcode.i2l);
                break;
            case NyarHeadCode.i32_extend_i64_s:
                writer.write_u8((byte)JvmOpcode.l2i);
                break;
            case NyarHeadCode.i32_to_f64_s:
                writer.write_u8((byte)JvmOpcode.i2d);
                break;
            case NyarHeadCode.i64_to_f64:
                writer.write_u8((byte)JvmOpcode.l2d);
                break;
            case NyarHeadCode.f64_to_i32:
                writer.write_u8((byte)JvmOpcode.d2i);
                break;
            case NyarHeadCode.f64_to_i64:
                writer.write_u8((byte)JvmOpcode.d2l);
                break;
            case NyarHeadCode.any_to_i32:
                // JVM 中 any 到 i32 的转换：拆箱为 int
                emit_unbox_to_int(ref writer, constantPool);
                break;
            case NyarHeadCode.any_to_utf8:
                // JVM 中 any 到 utf8 的转换：checkcast 为 String
                var stringClassIdx = add_class(constantPool, "java/lang/String");
                writer.write_u8((byte)JvmOpcode.checkcast);
                writer.write_u16_be(stringClassIdx);
                break;

            // ---- 浮点运算 ----
            case NyarHeadCode.f64_add:
                writer.write_u8((byte)JvmOpcode.dadd);
                break;
            case NyarHeadCode.f64_sub:
                writer.write_u8((byte)JvmOpcode.dsub);
                break;
            case NyarHeadCode.f64_mul:
                writer.write_u8((byte)JvmOpcode.dmul);
                break;
            case NyarHeadCode.f64_div:
                writer.write_u8((byte)JvmOpcode.ddiv);
                break;
            case NyarHeadCode.f64_neg:
                writer.write_u8((byte)JvmOpcode.dneg);
                break;

            // ---- 比较运算 ----
            case NyarHeadCode.i32_eq:
                emit_i32_comparison(ref writer, JvmOpcode.if_icmpne, function, instructionIndex, constantPool, linkTable);
                break;
            case NyarHeadCode.i32_ne:
                emit_i32_comparison(ref writer, JvmOpcode.if_icmpeq, function, instructionIndex, constantPool, linkTable);
                break;
            case NyarHeadCode.i32_lt_s:
                emit_i32_comparison(ref writer, JvmOpcode.if_icmpge, function, instructionIndex, constantPool, linkTable);
                break;
            case NyarHeadCode.i32_le_s:
                emit_i32_comparison(ref writer, JvmOpcode.if_icmpgt, function, instructionIndex, constantPool, linkTable);
                break;
            case NyarHeadCode.i32_gt_s:
                emit_i32_comparison(ref writer, JvmOpcode.if_icmple, function, instructionIndex, constantPool, linkTable);
                break;
            case NyarHeadCode.i32_ge_s:
                emit_i32_comparison(ref writer, JvmOpcode.if_icmplt, function, instructionIndex, constantPool, linkTable);
                break;
            case NyarHeadCode.i64_eq:
                emit_i64_comparison(ref writer, JvmOpcode.lcmp);
                break;
            case NyarHeadCode.i64_ne:
                emit_i64_comparison(ref writer, JvmOpcode.lcmp);
                break;
            case NyarHeadCode.i64_lt_s:
                emit_i64_comparison(ref writer, JvmOpcode.lcmp);
                break;
            case NyarHeadCode.i64_le_s:
                emit_i64_comparison(ref writer, JvmOpcode.lcmp);
                break;
            case NyarHeadCode.i64_gt_s:
                emit_i64_comparison(ref writer, JvmOpcode.lcmp);
                break;
            case NyarHeadCode.i64_ge_s:
                emit_i64_comparison(ref writer, JvmOpcode.lcmp);
                break;
            case NyarHeadCode.f64_eq:
                emit_f64_comparison(ref writer, JvmOpcode.ifne);
                break;
            case NyarHeadCode.f64_ne:
                emit_f64_comparison(ref writer, JvmOpcode.ifeq);
                break;
            case NyarHeadCode.f64_lt:
                emit_f64_comparison(ref writer, JvmOpcode.ifge);
                break;
            case NyarHeadCode.f64_le:
                emit_f64_comparison(ref writer, JvmOpcode.ifgt);
                break;
            case NyarHeadCode.f64_gt:
                emit_f64_comparison(ref writer, JvmOpcode.ifle);
                break;
            case NyarHeadCode.f64_ge:
                emit_f64_comparison(ref writer, JvmOpcode.iflt);
                break;
            case NyarHeadCode.ref_eq:
                // JVM 中引用相等使用 if_acmpeq
                emit_ref_comparison(ref writer, JvmOpcode.if_acmpne);
                break;
            case NyarHeadCode.ref_ne:
                emit_ref_comparison(ref writer, JvmOpcode.if_acmpeq);
                break;

            // ---- 分支指令 ----
            case NyarHeadCode.jump_if_true:
                emit_branch(ref writer, instruction, instructionIndex, instructionOffsets, labelOffsets, JvmOpcode.ifeq);
                break;
            case NyarHeadCode.jump_if_false:
                emit_branch(ref writer, instruction, instructionIndex, instructionOffsets, labelOffsets, JvmOpcode.ifne);
                break;

            // ---- 字符串操作 ----
            case NyarHeadCode.utf8_concat:
                emit_string_concat(ref writer, function, instructionIndex, constantPool, linkTable);
                break;
            case NyarHeadCode.utf8_eq:
                // JVM 中字符串相等使用 String.equals()
                var refEqIdx = add_method_ref(constantPool, "java/lang/String", "equals", "(Ljava/lang/Object;)Z");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(refEqIdx);
                break;
            case NyarHeadCode.utf8_ne:
                // 使用 String.equals() 后取反
                refEqIdx = add_method_ref(constantPool, "java/lang/String", "equals", "(Ljava/lang/Object;)Z");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(refEqIdx);
                writer.write_u8((byte)JvmOpcode.ifeq);
                writer.write_i16_be(7);
                writer.write_u8((byte)JvmOpcode.iconst0);
                writer.write_u8((byte)JvmOpcode.@goto);
                writer.write_i16_be(4);
                writer.write_u8((byte)JvmOpcode.iconst1);
                break;
            case NyarHeadCode.utf8_len_bytes:
            case NyarHeadCode.utf8_len_chars:
                // JVM 中字符串长度使用 String.length()
                var strLenIdx = add_method_ref(constantPool, "java/lang/String", "length", "()I");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(strLenIdx);
                break;
            case NyarHeadCode.utf8_substr:
            {
                // 栈：(str, start, end) → (str)
                // String.substring(start, end)
                // 将 end 设为 int 类型
                var substrMethodIdx = add_method_ref(constantPool,
                    "java/lang/String", "substring", "(II)Ljava/lang/String;");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(substrMethodIdx);
                break;
            }
            // ---- 调用指令 ----
            // call/call_static 的发射逻辑在 JvmBackend.Call.cs 中通过专门的
            // emit_call 方法处理，此处调用 emit_call 发射 invokestatic 及参数类型调整。
            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
            {
                emit_call(ref writer, instruction, constantPool, className, linkTable, allFunctions,
                    function, instructionIndex);
                break;
            }

            // ---- 字段访问 ----
            case NyarHeadCode.get_field:
            {
                // 栈：(obj, fieldName) → (value)
                // JVM 中结构体表示为 HashMap，使用 HashMap.get(fieldName) 读取字段。
                // 序列：swap → checkcast HashMap → swap → invokevirtual HashMap.get
                writer.write_u8((byte)JvmOpcode.swap);
                var hashMapClassIdx = add_class(constantPool, "java/util/HashMap");
                writer.write_u8((byte)JvmOpcode.checkcast);
                writer.write_u16_be(hashMapClassIdx);
                writer.write_u8((byte)JvmOpcode.swap);
                var getMethodIdx = add_method_ref(constantPool, "java/util/HashMap", "get",
                    "(Ljava/lang/Object;)Ljava/lang/Object;");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(getMethodIdx);
                break;
            }
            case NyarHeadCode.set_field:
            {
                // 栈：(obj, fieldName, value) → ()
                // JVM 中结构体表示为 HashMap，使用 HashMap.put(fieldName, value) 写入字段。
                // 使用临时局部变量暂存 value 和 fieldName，再重新排列调用 HashMap.put。
                var baseLocalSlot = compute_set_temp_base_slot(function);
                // 暂存 value（Object 类型）
                writer.write_u8((byte)JvmOpcode.astore);
                writer.write_u8((byte)(baseLocalSlot + 1));
                // 暂存 fieldName（Object 类型）
                writer.write_u8((byte)JvmOpcode.astore);
                writer.write_u8((byte)baseLocalSlot);
                // checkcast obj → HashMap
                var hashMapClassIdx = add_class(constantPool, "java/util/HashMap");
                writer.write_u8((byte)JvmOpcode.checkcast);
                writer.write_u16_be(hashMapClassIdx);
                // 加载 fieldName
                writer.write_u8((byte)JvmOpcode.aload);
                writer.write_u8((byte)baseLocalSlot);
                // 加载 value
                writer.write_u8((byte)JvmOpcode.aload);
                writer.write_u8((byte)(baseLocalSlot + 1));
                // invokevirtual HashMap.put
                var putMethodIdx = add_method_ref(constantPool, "java/util/HashMap", "put",
                    "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(putMethodIdx);
                // HashMap.put 返回旧值，需要 pop 掉
                writer.write_u8((byte)JvmOpcode.pop);
                break;
            }
            case NyarHeadCode.get_ordinal_index:
            case NyarHeadCode.get_offset_index:
            {
                // 栈：(obj, index) → (value)
                // JVM 中结构体表示为 HashMap，index 为 i32，需要装箱为 Integer 后调用 HashMap.get。
                // 序列：swap → checkcast HashMap → swap → invokestatic Integer.valueOf → invokevirtual HashMap.get
                writer.write_u8((byte)JvmOpcode.swap);
                var hashMapClassIdx = add_class(constantPool, "java/util/HashMap");
                writer.write_u8((byte)JvmOpcode.checkcast);
                writer.write_u16_be(hashMapClassIdx);
                writer.write_u8((byte)JvmOpcode.swap);
                var valueOfIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(valueOfIdx);
                var getMethodIdx = add_method_ref(constantPool, "java/util/HashMap", "get",
                    "(Ljava/lang/Object;)Ljava/lang/Object;");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(getMethodIdx);
                break;
            }
            case NyarHeadCode.set_ordinal_index:
            case NyarHeadCode.set_offset_index:
            {
                // 栈：(obj, index, value) → ()
                // JVM 中结构体表示为 HashMap，index 为 i32，需要装箱为 Integer 后调用 HashMap.put。
                // 使用临时局部变量暂存 value 和 index，再重新排列调用 HashMap.put。
                var baseLocalSlot = compute_set_temp_base_slot(function);
                // 暂存 value（Object 类型）
                writer.write_u8((byte)JvmOpcode.astore);
                writer.write_u8((byte)(baseLocalSlot + 1));
                // 暂存 index（int 类型）
                writer.write_u8((byte)JvmOpcode.istore);
                writer.write_u8((byte)baseLocalSlot);
                // checkcast obj → HashMap
                var hashMapClassIdx = add_class(constantPool, "java/util/HashMap");
                writer.write_u8((byte)JvmOpcode.checkcast);
                writer.write_u16_be(hashMapClassIdx);
                // 加载 index 并装箱为 Integer
                writer.write_u8((byte)JvmOpcode.iload);
                writer.write_u8((byte)baseLocalSlot);
                var valueOfIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(valueOfIdx);
                // 加载 value
                writer.write_u8((byte)JvmOpcode.aload);
                writer.write_u8((byte)(baseLocalSlot + 1));
                // invokevirtual HashMap.put
                var putMethodIdx = add_method_ref(constantPool, "java/util/HashMap", "put",
                    "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
                writer.write_u8((byte)JvmOpcode.invokevirtual);
                writer.write_u16_be(putMethodIdx);
                // HashMap.put 返回旧值，需要 pop 掉
                writer.write_u8((byte)JvmOpcode.pop);
                break;
            }

            // ---- 异常处理 ----
            case NyarHeadCode.enter_effect_handler:
            case NyarHeadCode.exit_effect_handler:
            case NyarHeadCode.enter_try:
            case NyarHeadCode.exit_try:
            case NyarHeadCode.resume:
                // JVM 中这些用 try-catch 块处理，指令级别不做特殊处理
                break;

            case NyarHeadCode.perform_effect:
                // 抛出异常
                writer.write_u8((byte)JvmOpcode.athrow);
                break;

            // ---- 数组/集合操作 ----
            case NyarHeadCode.array_push:
                // 栈：(array, value) → (array)
                writer.write_u8((byte)JvmOpcode.pop);
                break;

            case NyarHeadCode.length:
                // 栈：(obj) → (i32)
                // 使用 arraylength（假设是数组）
                writer.write_u8((byte)JvmOpcode.arraylength);
                break;

            // ---- 闭包 ----
            case NyarHeadCode.new_closure:
                // 简化实现：push null
                writer.write_u8((byte)JvmOpcode.aconst_null);
                stackDepth++;
                break;

            case NyarHeadCode.get_upvalue:
                // 简化实现：push null
                writer.write_u8((byte)JvmOpcode.aconst_null);
                stackDepth++;
                break;

            case NyarHeadCode.set_upvalue:
                // 简化实现：pop 值
                writer.write_u8((byte)JvmOpcode.pop);
                stackDepth = Math.Max(0, stackDepth - 1);
                break;

            // ---- 内存操作 ----
            case NyarHeadCode.i32_load:
                // 栈：(addr) → (value)
                writer.write_u8((byte)JvmOpcode.aload);
                writer.write_u8(0); // 简化：从局部变量表加载
                stackDepth++;
                break;

            case NyarHeadCode.i32_store:
                // 栈：(addr, value) → ()
                writer.write_u8((byte)JvmOpcode.istore);
                writer.write_u8(0); // 简化：存储到局部变量表
                stackDepth = Math.Max(0, stackDepth - 2);
                break;

            // ---- 其他 ----
            case NyarHeadCode.alloc:
                // 简化实现：push 0 (占位)
                writer.write_u8((byte)JvmOpcode.iconst0);
                stackDepth++;
                break;

            case NyarHeadCode.free:
                // 简化实现：pop
                writer.write_u8((byte)JvmOpcode.pop);
                stackDepth = Math.Max(0, stackDepth - 1);
                break;

            case NyarHeadCode.new_object:
            {
                // 简化实现：push null
                writer.write_u8((byte)JvmOpcode.aconst_null);
                stackDepth++;
                break;
            }
        }

        // 更新栈深度（无条件跳转已在上面处理）
        if (instruction.head_code != NyarHeadCode.jump)
        {
            var (push, pop) = get_nyar_stack_effect(instruction);
            // call/call_static 的推入值取决于签名是否期望 unit/void
            // emit_call_result_adjustment 会弹出 unit 返回值，实际推入 0
            if (instruction.head_code is NyarHeadCode.call or NyarHeadCode.call_static
                && instruction.operands.Count > 0
                && instruction.operands[0] is GenerateOperand.FuncRef funcRef)
            {
                var sigResultsCount = funcRef.signature.results.Count;
                var sigExpectsUnit = sigResultsCount > 0 && funcRef.signature.results[0] == GenerateValueType.unit;
                var sigExpectsNoResult = sigResultsCount == 0 || sigExpectsUnit;
                if (sigExpectsNoResult) push = 0;
            }

            var oldDepth = stackDepth;
            stackDepth = stackDepth - pop + push;
            if (stackDepth < 0) stackDepth = 0;

            if (function.name.Contains("legion_emit") ||
                function.name.Contains("find_cycle_dfs")) // debug: log stack depth
                File.AppendAllText(@"E:\RiderProjects\stack_depth_debug.log",
                    $"[emit] method={function.name}, idx={instructionIndex}, opcode={instruction.head_code}, push={push}, pop={pop}, depth: {oldDepth}->{stackDepth}\n");
        }
    }
}