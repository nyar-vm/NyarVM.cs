using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm.Data;
using Std.Data.Binary.NyarIR.Data;
using static Nyar.Assembler.Backends.Jvm.JvmConstantPoolHelper;
using static Nyar.Assembler.Backends.Jvm.JvmTypeMap;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 后端的 partial 文件，承载所有辅助指令发射方法。
///     主发射方法 <see cref="emit_instruction" /> 位于 JvmBackend.Emit.cs 中。
/// </summary>
public partial class JvmBackend
{
    private static void emit_branch(ref ByteBufferWriter writer, GenerateInstruction instruction, int instructionIndex,
        IReadOnlyList<int> instructionOffsets, IReadOnlyDictionary<string, int> labelOffsets, JvmOpcode opcode)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Label label)
            throw new InvalidOperationException($"JVM 分支指令 `{instruction.head_code}` 缺少标签操作数。");

        if (!labelOffsets.TryGetValue(label.name, out var targetOffset))
            throw new InvalidOperationException($"JVM 分支目标标签 `{label.name}` 未定义。");

        writer.write_u8((byte)opcode);
        // 使用 writer.position 计算分支偏移量，避免 jump 指令前面的 pop 导致偏移错误
        // writer.position 此时指向 opcode 之后，减 1 得到 opcode 自身的地址
        var branchOffset = targetOffset - (writer.position - 1);
        if (branchOffset is < short.MinValue or > short.MaxValue)
            throw new InvalidOperationException($"JVM 分支到标签 `{label.name}` 的偏移 `{branchOffset}` 超出 16 位范围。");

        writer.write_i16_be((short)branchOffset);
    }

    private static void emit_i32_comparison(ref ByteBufferWriter writer, JvmOpcode comparisonOpcode,
        GenerateFunction function, int instructionIndex, List<JvmConstant> constantPool,
        JvmExternalLinkTable linkTable)
    {
        // 在比较之前，将栈上的 Object 值拆箱为 int
        emit_unbox_i32_comparison_operands(ref writer, function, instructionIndex, constantPool, linkTable);
        writer.write_u8((byte)comparisonOpcode);
        writer.write_i16_be(7);
        writer.write_u8((byte)JvmOpcode.iconst0);
        writer.write_u8((byte)JvmOpcode.@goto);
        writer.write_i16_be(4);
        writer.write_u8((byte)JvmOpcode.iconst1);
    }

    /// <summary>
    ///     在 i32 比较指令之前，检查栈上两个值是否为 Object 类型。
    ///     如果是则拆箱为 int（checkcast Integer + invokevirtual intValue）。
    ///     通过回溯指令流推断栈值类型。
    /// </summary>
    private static void emit_unbox_i32_comparison_operands(ref ByteBufferWriter writer,
        GenerateFunction function, int compareInstructionIndex, List<JvmConstant> constantPool,
        JvmExternalLinkTable linkTable)
    {
        // 找到栈顶两个推入值的指令类型
        var types = new string[2] { "java/lang/Object", "java/lang/Object" };
        var foundCount = 0;
        var popSkipCount = 0;
        for (var idx = compareInstructionIndex - 1; idx >= 0 && foundCount < 2; idx--)
        {
            var instr = function.instructions[idx];

            if (instr.head_code is NyarHeadCode.nop or NyarHeadCode.store_local
                or NyarHeadCode.store_arg or NyarHeadCode.jump
                or NyarHeadCode.@return or NyarHeadCode.exit_effect_handler or NyarHeadCode.exit_try
                or NyarHeadCode.swap)
                continue;

            if (instr.head_code == NyarHeadCode.pop)
            {
                popSkipCount++;
                continue;
            }

            if (popSkipCount > 0)
            {
                popSkipCount--;
                // 被跳过的指令可能也消费了栈上的值（如 call 消费参数），
                // 需要将该 pop 数累加到 popSkipCount，以跳过它消费的值。
                var (_, skippedPop) = get_nyar_stack_effect(instr);
                popSkipCount += skippedPop;
                continue;
            }

            if (instr.head_code is NyarHeadCode.set_field or NyarHeadCode.set_offset_index or NyarHeadCode.set_ordinal_index)
            {
                idx -= 3;
                continue;
            }

            types[1 - foundCount] = get_instruction_result_jvm_type(instr, linkTable);
            foundCount++;

            // 该指令可能消费了栈上的值（如 length 消费 HashMap），需要跳过这些被消费的值。
            // any_to_i32 等转换指令已自行处理类型转换，其 pop 对应的被消费值不应影响后续回溯。
            var (_, pop) = get_nyar_stack_effect(instr);
            if (instr.head_code is NyarHeadCode.any_to_i32 or NyarHeadCode.any_to_utf8) pop = 0;
            popSkipCount += pop;
        }

        // 栈布局：[value0, value1]，value1 在栈顶。
        // 如果 value0 是 Object 而 value1 不是 Object，需要拆箱 value0
        if (types[0] == "java/lang/Object" && types[1] != "java/lang/Object")
        {
            // 栈：[Object, int/long/etc]
            // 需要拆箱 value0（位于 value1 下方）
            // dup_x1: Object, int → int, Object, int
            // pop:    int, Object
            // ...
            // 下面的英文注释为原始 JVM 字节码说明
            // 先处理右侧值（栈顶第二个值）
            emit_unbox_to_int(ref writer, constantPool);
            // 现在栈顶是拆箱后的 int，但顺序反了，用 swap 恢复
            writer.write_u8((byte)JvmOpcode.swap);
        }
        else if (types[0] != "java/lang/Object" && types[1] == "java/lang/Object")
        {
            // 栈：[int, Object]
            // 处理左侧值（栈顶第一个值）
            // swap: Object, int
            emit_unbox_to_int(ref writer, constantPool);
            writer.write_u8((byte)JvmOpcode.swap);
        }
        else if (types[0] == "java/lang/Object" && types[1] == "java/lang/Object")
        {
            // 两个都是 Object，需要分别拆箱
            emit_unbox_to_int(ref writer, constantPool);
            writer.write_u8((byte)JvmOpcode.swap);
            emit_unbox_to_int(ref writer, constantPool);
            writer.write_u8((byte)JvmOpcode.swap);
        }
    }

    /// <summary>
    ///     将栈顶的 Object 值拆箱为 int（checkcast Integer + invokevirtual intValue）。
    ///     如果栈顶值已经是原始 int 类型，则不做处理。
    /// </summary>
    private static void emit_unbox_to_int(ref ByteBufferWriter writer, List<JvmConstant> constantPool)
    {
        var integerClassIdx = add_class(constantPool, "java/lang/Integer");
        var intValueMethodIdx = add_method_ref(constantPool, "java/lang/Integer", "intValue", "()I");

        writer.write_u8((byte)JvmOpcode.checkcast);
        writer.write_u16_be(integerClassIdx);
        writer.write_u8((byte)JvmOpcode.invokevirtual);
        writer.write_u16_be(intValueMethodIdx);
    }

    private static void emit_unbox_operands_for_binary_arith(
        ref ByteBufferWriter writer, GenerateFunction function, int arithInstructionIndex,
        List<JvmConstant> constantPool, JvmExternalLinkTable linkTable)
    {
        // 类似于 i32 比较操作，用于二元算术操作前的拆箱
        var types = new string[2] { "java/lang/Object", "java/lang/Object" };
        var foundCount = 0;
        var popSkipCount = 0;

        for (var idx = arithInstructionIndex - 1; idx >= 0 && foundCount < 2; idx--)
        {
            var instr = function.instructions[idx];

            if (instr.head_code is NyarHeadCode.nop or NyarHeadCode.store_local
                or NyarHeadCode.store_arg or NyarHeadCode.jump
                or NyarHeadCode.@return or NyarHeadCode.exit_effect_handler or NyarHeadCode.exit_try
                or NyarHeadCode.swap)
                continue;

            if (instr.head_code == NyarHeadCode.pop)
            {
                popSkipCount++;
                continue;
            }

            if (popSkipCount > 0)
            {
                popSkipCount--;
                var (_, skippedPop) = get_nyar_stack_effect(instr);
                popSkipCount += skippedPop;
                continue;
            }

            if (instr.head_code is NyarHeadCode.set_field or NyarHeadCode.set_offset_index or NyarHeadCode.set_ordinal_index)
            {
                idx -= 3;
                continue;
            }

            types[1 - foundCount] = get_instruction_result_jvm_type(instr, linkTable);
            foundCount++;

            var (_, pop) = get_nyar_stack_effect(instr);
            if (instr.head_code is NyarHeadCode.any_to_i32 or NyarHeadCode.any_to_utf8) pop = 0;
            popSkipCount += pop;
        }

        // 对 Object 类型的操作数进行拆箱
        if (types[0] == "java/lang/Object" && types[1] != "java/lang/Object")
        {
            emit_unbox_to_int(ref writer, constantPool);
            writer.write_u8((byte)JvmOpcode.swap);
        }
        else if (types[0] != "java/lang/Object" && types[1] == "java/lang/Object")
        {
            writer.write_u8((byte)JvmOpcode.swap);
            emit_unbox_to_int(ref writer, constantPool);
            writer.write_u8((byte)JvmOpcode.swap);
        }
        else if (types[0] == "java/lang/Object" && types[1] == "java/lang/Object")
        {
            emit_unbox_to_int(ref writer, constantPool);
            writer.write_u8((byte)JvmOpcode.swap);
            emit_unbox_to_int(ref writer, constantPool);
        }
    }

    private static void emit_string_concat(
        ref ByteBufferWriter writer, GenerateFunction function, int instructionIndex,
        List<JvmConstant> constantPool, JvmExternalLinkTable linkTable)
    {
        // 栈：(str1, str2) → (result)
        // 使用 StringBuilder 或 String.concat()
        var concatMethodIdx = add_method_ref(constantPool, "java/lang/String", "concat",
            "(Ljava/lang/String;)Ljava/lang/String;");
        writer.write_u8((byte)JvmOpcode.invokevirtual);
        writer.write_u16_be(concatMethodIdx);
    }

    private static void emit_to_string(
        ref ByteBufferWriter writer, GenerateFunction function, int instructionIndex,
        List<JvmConstant> constantPool, JvmExternalLinkTable linkTable)
    {
        // 栈：(obj) → (String)
        // 使用 String.valueOf()
        var valueOfMethodIdx = add_method_ref(constantPool, "java/lang/String", "valueOf",
            "(Ljava/lang/Object;)Ljava/lang/String;");
        writer.write_u8((byte)JvmOpcode.invokestatic);
        writer.write_u16_be(valueOfMethodIdx);
    }

    private static bool is_jvm_reference_type(string jvmType)
    {
        return jvmType == "java/lang/Object"
               || jvmType == "java/lang/String"
               || jvmType == "java/lang/Integer"
               || jvmType == "java/lang/Boolean"
               || jvmType == "java/lang/Long"
               || jvmType == "java/lang/Float"
               || jvmType == "java/lang/Double"
               || jvmType.StartsWith("java/", StringComparison.Ordinal)
               || jvmType.StartsWith("[", StringComparison.Ordinal);
    }

    private static void emit_i64_comparison(ref ByteBufferWriter writer, JvmOpcode comparisonOpcode)
    {
        // lcmp: 比较两个 long 值，栈上弹出两个 long，推入 int 结果（-1, 0, 1）
        writer.write_u8((byte)JvmOpcode.lcmp);
        // 根据 lcmp 结果进行分支
        writer.write_u8((byte)comparisonOpcode);
        writer.write_i16_be(7);
        writer.write_u8((byte)JvmOpcode.iconst0);
        writer.write_u8((byte)JvmOpcode.@goto);
        writer.write_i16_be(4);
        writer.write_u8((byte)JvmOpcode.iconst1);
    }

    private static void emit_ref_comparison(ref ByteBufferWriter writer, JvmOpcode comparisonOpcode)
    {
        // 引用比较（if_acmpeq / if_acmpne）
        writer.write_u8((byte)comparisonOpcode);
        writer.write_i16_be(7);
        writer.write_u8((byte)JvmOpcode.iconst0);
        writer.write_u8((byte)JvmOpcode.@goto);
        writer.write_i16_be(4);
        writer.write_u8((byte)JvmOpcode.iconst1);
    }

    private static void emit_f64_comparison(ref ByteBufferWriter writer, JvmOpcode branchOpcode)
    {
        // dcmpg: 比较两个 double 值
        writer.write_u8((byte)JvmOpcode.dcmpg);
        // ifeq/ifne/iflt/ifge/ifgt/ifle
        writer.write_u8((byte)branchOpcode);
        writer.write_i16_be(7);
        writer.write_u8((byte)JvmOpcode.iconst0);
        writer.write_u8((byte)JvmOpcode.@goto);
        writer.write_i16_be(4);
        writer.write_u8((byte)JvmOpcode.iconst1);
    }

    private static void emit_load_local(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Local local) return;

        var index = local.index;
        switch (index)
        {
            case 0: writer.write_u8((byte)JvmOpcode.iload_0); break;
            case 1: writer.write_u8((byte)JvmOpcode.iload_1); break;
            case 2: writer.write_u8((byte)JvmOpcode.iload_2); break;
            case 3: writer.write_u8((byte)JvmOpcode.iload_3); break;
            default:
                if (index <= byte.MaxValue)
                {
                    writer.write_u8((byte)JvmOpcode.iload);
                    writer.write_u8((byte)index);
                }
                else
                {
                    writer.write_u8((byte)JvmOpcode.wide);
                    writer.write_u8((byte)JvmOpcode.iload);
                    writer.write_u16_be((ushort)index);
                }

                break;
        }
    }

    private static void emit_store_local(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Local local) return;

        var index = local.index;
        switch (index)
        {
            case 0: writer.write_u8((byte)JvmOpcode.istore_0); break;
            case 1: writer.write_u8((byte)JvmOpcode.istore_1); break;
            case 2: writer.write_u8((byte)JvmOpcode.istore_2); break;
            case 3: writer.write_u8((byte)JvmOpcode.istore_3); break;
            default:
                if (index <= byte.MaxValue)
                {
                    writer.write_u8((byte)JvmOpcode.istore);
                    writer.write_u8((byte)index);
                }
                else
                {
                    writer.write_u8((byte)JvmOpcode.wide);
                    writer.write_u8((byte)JvmOpcode.istore);
                    writer.write_u16_be((ushort)index);
                }

                break;
        }
    }

    private static void emit_load_arg(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Param param) return;

        // JVM 中参数从 0 开始（实例方法中 0 为 this），使用 aload 加载引用类型参数
        var index = param.index;
        if (index == 0)
        {
            writer.write_u8((byte)JvmOpcode.aload_0);
        }
        else if (index == 1)
        {
            writer.write_u8((byte)JvmOpcode.aload_1);
        }
        else if (index == 2)
        {
            writer.write_u8((byte)JvmOpcode.aload_2);
        }
        else if (index == 3)
        {
            writer.write_u8((byte)JvmOpcode.aload_3);
        }
        else if (index <= byte.MaxValue)
        {
            writer.write_u8((byte)JvmOpcode.aload);
            writer.write_u8((byte)index);
        }
        else
        {
            writer.write_u8((byte)JvmOpcode.wide);
            writer.write_u8((byte)JvmOpcode.aload);
            writer.write_u16_be((ushort)index);
        }
    }

    private static void emit_store_arg(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.FirstOrDefault() is not GenerateOperand.Param param) return;

        var index = param.index;
        if (index <= byte.MaxValue)
        {
            writer.write_u8((byte)JvmOpcode.astore);
            writer.write_u8((byte)index);
        }
        else
        {
            writer.write_u8((byte)JvmOpcode.wide);
            writer.write_u8((byte)JvmOpcode.astore);
            writer.write_u16_be((ushort)index);
        }
    }

    private static void emit_load_global(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        // 简化实现：使用 getstatic 加载全局变量
        // 完整实现需要查找全局变量对应的字段引用
        writer.write_u8((byte)JvmOpcode.getstatic);
        writer.write_u16_be(0); // 占位，需要字段引用
    }

    private static void emit_jvm_const(ref ByteBufferWriter writer, GenerateOperand operand,
        GenerateFunction function, IReadOnlyList<string> moduleStrings, List<JvmConstant> constantPool,
        List<JvmBootstrapMethod> bootstrapMethods, string className)
    {
        switch (operand)
        {
            case GenerateOperand.I32 i32:
                emit_i32_const(ref writer, i32.value, constantPool);
                break;
            case GenerateOperand.I64 i64:
                emit_i64_const(ref writer, i64.value, constantPool);
                break;
            case GenerateOperand.F32 f32:
            {
                var constIdx = add_float(constantPool, f32.value);
                writer.write_u8((byte)JvmOpcode.ldc);
                writer.write_u8((byte)constIdx);
                break;
            }
            case GenerateOperand.F64 f64:
            {
                var constIdx = add_double(constantPool, f64.value);
                writer.write_u8((byte)JvmOpcode.ldc2_w);
                writer.write_u16_be(constIdx);
                break;
            }
            case GenerateOperand.Str str:
            {
                var constIdx = add_string(constantPool, str.value);
                writer.write_u8((byte)JvmOpcode.ldc);
                writer.write_u8((byte)constIdx);
                break;
            }
            case GenerateOperand.Null:
                writer.write_u8((byte)JvmOpcode.aconst_null);
                break;
            case GenerateOperand.Const { type: GenerateValueType.i32 } c:
            {
                var value = c.pool_index < moduleStrings.Count
                    ? c.pool_index
                    : 0;
                emit_i32_const(ref writer, value, constantPool);
                break;
            }
            case GenerateOperand.Const { type: GenerateValueType.utf8 } c:
            {
                var value = c.pool_index < moduleStrings.Count
                    ? moduleStrings[c.pool_index]
                    : string.Empty;
                var constIdx = add_string(constantPool, value);
                writer.write_u8((byte)JvmOpcode.ldc);
                writer.write_u8((byte)constIdx);
                break;
            }
            default:
                // 未知常量类型，推入 0 作为占位
                emit_i32_const(ref writer, 0, constantPool);
                break;
        }
    }

    private static void emit_i32_const(ref ByteBufferWriter writer, int value, List<JvmConstant> constantPool)
    {
        switch (value)
        {
            case -1: writer.write_u8((byte)JvmOpcode.iconst_m1); break;
            case 0: writer.write_u8((byte)JvmOpcode.iconst_0); break;
            case 1: writer.write_u8((byte)JvmOpcode.iconst_1); break;
            case 2: writer.write_u8((byte)JvmOpcode.iconst_2); break;
            case 3: writer.write_u8((byte)JvmOpcode.iconst_3); break;
            case 4: writer.write_u8((byte)JvmOpcode.iconst_4); break;
            case 5: writer.write_u8((byte)JvmOpcode.iconst_5); break;
            default:
                if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                {
                    writer.write_u8((byte)JvmOpcode.bipush);
                    writer.write_u8((byte)(sbyte)value);
                }
                else if (value >= short.MinValue && value <= short.MaxValue)
                {
                    writer.write_u8((byte)JvmOpcode.sipush);
                    writer.write_i16_be((short)value);
                }
                else
                {
                    var constIdx = add_int(constantPool, value);
                    writer.write_u8((byte)JvmOpcode.ldc);
                    writer.write_u8((byte)constIdx);
                }

                break;
        }
    }

    private static void emit_i64_const(ref ByteBufferWriter writer, long value, List<JvmConstant> constantPool)
    {
        if (value == 0)
        {
            writer.write_u8((byte)JvmOpcode.lconst_0);
        }
        else if (value == 1)
        {
            writer.write_u8((byte)JvmOpcode.lconst_1);
        }
        else
        {
            var constIdx = add_long(constantPool, value);
            writer.write_u8((byte)JvmOpcode.ldc2_w);
            writer.write_u16_be(constIdx);
        }
    }

    private static string sanitize_method_name(string methodName)
    {
        // 将全限定名转换为 JVM 内部方法名
        // 只保留最后一个 '/' 之后的部分（如果有），并清理 JVM 非法字符
        // JVM 方法名禁止包含: '.' ';' '[' '/' '<' '>'
        var lastSlash = methodName.LastIndexOf('/');
        var name = lastSlash >= 0 ? methodName[(lastSlash + 1)..] : methodName;
        // 将 '.' 替换为 '∷'（U+2237，与 IR 中的命名空间分隔符一致，且为 JVM 合法字符）
        name = name.Replace('.', '∷');
        return name;
    }

    /// <summary>
    ///     为没有显式 return 的路径发射隐式 return 指令。
    /// </summary>
    private static void emit_implicit_return(ref ByteBufferWriter writer, GenerateTypeReference returnType,
        List<JvmConstant> constantPool)
    {
        var returnTypeName = to_jvm_type_name(returnType);
        emit_implicit_return(ref writer, returnTypeName, constantPool);
    }

    private static void emit_implicit_return(ref ByteBufferWriter writer, string returnType,
        List<JvmConstant> constantPool)
    {
        switch (returnType)
        {
            case "void":
            case "unit":
                writer.write_u8((byte)JvmOpcode.@return);
                break;
            case "int":
            case "boolean":
            case "char":
            case "short":
            case "byte":
                emit_i32_const(ref writer, 0, constantPool);
                writer.write_u8((byte)JvmOpcode.ireturn);
                break;
            case "long":
                emit_i64_const(ref writer, 0, constantPool);
                writer.write_u8((byte)JvmOpcode.lreturn);
                break;
            case "float":
                writer.write_u8((byte)JvmOpcode.fconst_0);
                writer.write_u8((byte)JvmOpcode.freturn);
                break;
            case "double":
                writer.write_u8((byte)JvmOpcode.dconst_0);
                writer.write_u8((byte)JvmOpcode.dreturn);
                break;
            default:
                writer.write_u8((byte)JvmOpcode.aconst_null);
                writer.write_u8((byte)JvmOpcode.areturn);
                break;
        }
    }

    private static void emit_default_value(ref ByteBufferWriter writer, GenerateTypeReference returnType,
        List<JvmConstant> constantPool)
    {
        emit_default_value(ref writer, to_jvm_type_name(returnType), constantPool);
    }

    private static void emit_default_value(ref ByteBufferWriter writer, string returnType,
        List<JvmConstant> constantPool)
    {
        switch (returnType)
        {
            case "int":
            case "boolean":
            case "char":
            case "short":
            case "byte":
                emit_i32_const(ref writer, 0, constantPool);
                break;
            case "long":
                emit_i64_const(ref writer, 0, constantPool);
                break;
            case "float":
                writer.write_u8((byte)JvmOpcode.fconst_0);
                break;
            case "double":
                writer.write_u8((byte)JvmOpcode.dconst_0);
                break;
            default:
                writer.write_u8((byte)JvmOpcode.aconst_null);
                break;
        }
    }

    private static string get_signature_return_type(GenerateFunctionType signature)
    {
        if (signature.results.Count == 0) return "void";

        // 多返回值（元组）在 JVM 中映射为 Object
        if (signature.results.Count > 1) return "java/lang/Object";

        return to_jvm_type_name(signature.results[0]);
    }

    /// <summary>
    ///     检查栈顶值是否为 JVM 原始类型（int/long/float/double/boolean），
    ///     如果是则发射对应的装箱指令（Integer.valueOf / Long.valueOf 等）。
    ///     通过回溯 <paramref name="setInstructionIndex" />
    ///     前一条指令来确定值的 JVM 类型。
    /// </summary>
    /// <remarks>
    ///     当前仅处理栈顶的单个值。对于 set_field / set_index，栈在调用此方法时为
    ///     [(HashMap)obj, fieldName/index, value]，其中 value 在栈顶。
    /// </remarks>
    private static void emit_box_value_for_hashmap(ref ByteBufferWriter writer, GenerateFunction function,
        int setInstructionIndex, List<JvmConstant> constantPool,
        JvmExternalLinkTable linkTable)
    {
        // 找到 set_field/set_index 前一条推入值的指令
        var valueType = "java/lang/Object";
        for (var idx = setInstructionIndex - 1; idx >= 0; idx--)
        {
            var instr = function.instructions[idx];

            // 跳过不推入值的指令
            if (instr.head_code is NyarHeadCode.nop or NyarHeadCode.pop
                or NyarHeadCode.store_local or NyarHeadCode.store_arg)
                continue;

            // 跳过 set_field/set_index（消费值而非推入值）
            if (instr.head_code is NyarHeadCode.set_field or NyarHeadCode.set_offset_index or NyarHeadCode.set_ordinal_index) break;

            valueType = get_instruction_result_jvm_type(instr, linkTable);
            break;
        }

        switch (valueType)
        {
            case "int" or "boolean":
            {
                var integerValueOfIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(integerValueOfIdx);
                break;
            }
            case "long":
            {
                var longValueOfIdx = add_method_ref(constantPool, "java/lang/Long", "valueOf",
                    "(J)Ljava/lang/Long;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(longValueOfIdx);
                break;
            }
            case "float":
            {
                var floatValueOfIdx = add_method_ref(constantPool, "java/lang/Float", "valueOf",
                    "(F)Ljava/lang/Float;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(floatValueOfIdx);
                break;
            }
            case "double":
            {
                var doubleValueOfIdx = add_method_ref(constantPool, "java/lang/Double", "valueOf",
                    "(D)Ljava/lang/Double;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(doubleValueOfIdx);
                break;
            }
        }
    }

    /// <summary>
    ///     根据 LIR 指令推断其栈顶推入值的 JVM 类型。
    ///     返回的对象类型（如 "java/lang/String"）与 <see cref="to_jvm_type_name" /> 一致。
    ///     对于 call/call_static，需要区分普通 Valkyrie 函数和 [jvm] 外部函数。
    ///     普通函数发射为 invokestatic 返回 Object（装箱），[jvm] 外部函数
    ///     直接调用 Java 标准库，返回原始类型就是原始类型，不会装箱。
    /// </summary>
    private static string get_instruction_result_jvm_type(GenerateInstruction instr,
        JvmExternalLinkTable? linkTable = null)
    {
        if (instr.head_code is NyarHeadCode.@const && instr.operands.Count > 0)
            return instr.operands[0] switch
            {
                GenerateOperand.I32 => "int",
                GenerateOperand.I64 => "long",
                GenerateOperand.F32 => "float",
                GenerateOperand.F64 => "double",
                GenerateOperand.Str => "java/lang/String",
                GenerateOperand.Null => "java/lang/Object",
                _ => "java/lang/Object"
            };

        if (instr.head_code is NyarHeadCode.load_arg && instr.operands.Count > 0
                                                     && instr.operands[0] is GenerateOperand.Param param)
            return to_jvm_type_name(param.type);

        if (instr.head_code is NyarHeadCode.load_local && instr.operands.Count > 0
                                                       && instr.operands[0] is GenerateOperand.Local local)
            return to_jvm_type_name(local.type);

        // call / call_static 的返回值类型取决于被调用函数的签名。
        // JVM 后端的方法描述符基于声明的返回类型（build_method 使用 to_jvm_type_name(return_type)），
        // 因此 invokestatic 的返回类型与签名一致：
        // - 声明返回 bool/i32/long 等原始类型时，栈上就是原始类型。
        // - 声明返回 String/Object/any 等引用类型时，栈上是对应的引用类型。
        // 注意：[jvm] 外部函数直接调用 Java 标准库，同样遵循声明类型。
        if (instr.opcode is NyarHeadCode.call or NyarHeadCode.call_static
            && instr.operands.Count > 0
            && instr.operands[0] is GenerateOperand.FuncRef { signature.results.Count: > 0 } funcRef)
            return to_jvm_type_name(funcRef.signature.results[0]);

        return instr.head_code switch
        {
            NyarHeadCode.i32_add or NyarHeadCode.i32_sub or NyarHeadCode.i32_mul
                or NyarHeadCode.i32_div_s or NyarHeadCode.i32_rem_s or NyarHeadCode.i32_and
                or NyarHeadCode.i32_or or NyarHeadCode.i32_xor or NyarHeadCode.i32_shl
                or NyarHeadCode.i32_shr_s or NyarHeadCode.i32_shr_u or NyarHeadCode.i32_not
                or NyarHeadCode.i32_div_u or NyarHeadCode.i32_rem_u
                or NyarHeadCode.i32_eq or NyarHeadCode.i32_ne or NyarHeadCode.i32_lt_s
                or NyarHeadCode.i32_le_s or NyarHeadCode.i32_gt_s or NyarHeadCode.i32_ge_s
                or NyarHeadCode.i32_lt_u or NyarHeadCode.i32_le_u or NyarHeadCode.i32_gt_u or NyarHeadCode.i32_ge_u
                or NyarHeadCode.i64_trunc_i32_s or NyarHeadCode.any_to_i32 or NyarHeadCode.length
                => "int",
            NyarHeadCode.i64_add or NyarHeadCode.i64_sub or NyarHeadCode.i64_mul
                or NyarHeadCode.i64_div_s or NyarHeadCode.i64_rem_s or NyarHeadCode.i64_and
                or NyarHeadCode.i64_or or NyarHeadCode.i64_xor
                or NyarHeadCode.i64_eq or NyarHeadCode.i64_ne or NyarHeadCode.i64_lt_s
                or NyarHeadCode.i64_le_s or NyarHeadCode.i64_gt_s or NyarHeadCode.i64_ge_s
                => "long",
            NyarHeadCode.f64_add or NyarHeadCode.f64_sub or NyarHeadCode.f64_mul or NyarHeadCode.f64_div
                or NyarHeadCode.f64_eq or NyarHeadCode.f64_ne or NyarHeadCode.f64_lt
                or NyarHeadCode.f64_le or NyarHeadCode.f64_gt or NyarHeadCode.f64_ge
                => "double",
            NyarHeadCode.ref_eq or NyarHeadCode.ref_ne => "int",
            NyarHeadCode.utf8_eq or NyarHeadCode.utf8_ne => "int",
            NyarHeadCode.utf8_concat => "java/lang/String",
            NyarHeadCode.utf8_substr => "java/lang/String",
            NyarHeadCode.any_to_utf8 => "java/lang/String",
            NyarHeadCode.utf8_len_bytes or NyarHeadCode.utf8_len_chars => "int",
            _ => "java/lang/Object"
        };
    }
}