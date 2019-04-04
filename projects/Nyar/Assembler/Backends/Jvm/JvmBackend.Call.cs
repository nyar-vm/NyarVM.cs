using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm;
using Std.Data.Binary.Jvm.Data;
using Std.Data.Binary.NyarIR.Data;
using static Nyar.Assembler.Backends.Jvm.JvmConstantPoolHelper;
using static Nyar.Assembler.Backends.Jvm.JvmTypeMap;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 后端 partial：函数调用相关字节码发射（call / jvm 外部调用 / 返回值调整）。
/// </summary>
public partial class JvmBackend
{
    /// <summary>
    ///     检查操作数名称是否匹配外部函数名集合。
    ///     操作数名称可能为短名（如 "console_write_line"）或全限定名（如 "test.console_write_line"），
    ///     而外部函数集合中可能存相反形式，需做双向模糊匹配。
    /// </summary>
    /// <returns>净栈深度变化量（正值=推入，负值=弹出）。</returns>
    private static int emit_call(ref ByteBufferWriter writer, GenerateInstruction instruction,
        List<JvmConstant> constantPool, string className,
        JvmExternalLinkTable linkTable,
        IReadOnlyList<GenerateFunction> allFunctions,
        GenerateFunction function, int instructionIndex)
    {
        var operand = (GenerateOperand.FuncRef)instruction.operands[0];
        var signature = operand.signature;
        var returnType = get_signature_return_type(signature);
        var parameterTypes = signature.parameters.Select(to_jvm_type_name);
        var descriptor = JvmTypeMapper.map_method_descriptor(returnType, parameterTypes);
        var paramCount = signature.parameters.Count;

        // 处理未解析的 infix 运算符调用（如 "text.infix +"、"index.infix <"）
        // 这些函数在链接表中不存在，需要直接发射对应字节码
        var dotInfixIdx = operand.name.IndexOf(".infix ", StringComparison.Ordinal);
        if (dotInfixIdx >= 0)
            return emit_unresolved_infix_call(ref writer, constantPool, operand, signature,
                function, instructionIndex, linkTable);

        // 判断签名是否期望 unit 或 void 返回值
        var signatureExpectsNoResult = signature.results.Count == 0
                                       || signature.results[0] == GenerateValueType.unit;

        // 在模块函数列表中查找目标函数，使用其实际参数类型（而非 FuncRef.signature 的 fallback）
        // 匹配时允许自定义类型（映射为 @null）通配任意引用类型，避免因类型解析不完整导致匹配失败
        // 同时支持短名匹配（FuncRef 可能使用短名，而函数定义使用全限定名）
        var operandShortName = operand.name;
        var operandDot = operand.name.LastIndexOf('.');
        if (operandDot >= 0) operandShortName = operand.name[(operandDot + 1)..];

        var resolvedParamTypes = parameterTypes.ToArray();
        var resolvedReturnType = returnType;
        var resolvedFuncName = operand.name;
        // 记录签名参数数与目标函数参数数的差异（前端隐式推入的模块对象数量）
        var normalParamCountDiff = 0;
        for (var fi = 0; fi < allFunctions.Count; fi++)
        {
            var targetFunc = allFunctions[fi];

            // 名称匹配：精确匹配或短名匹配
            var nameMatch = string.Equals(targetFunc.name, operand.name, StringComparison.Ordinal);
            if (!nameMatch)
            {
                var funcLastDot = targetFunc.name.LastIndexOf('.');
                var funcShortName = funcLastDot >= 0 ? targetFunc.name[(funcLastDot + 1)..] : targetFunc.name;
                nameMatch = string.Equals(funcShortName, operandShortName, StringComparison.Ordinal);
            }

            if (!nameMatch) continue;

            // 参数数量不匹配时，可能是前端隐式推入的模块对象导致 signature 多出参数。
            // 模块对象通常在参数列表开头，尝试跳过开头的差异部分进行比较。
            var paramCountDiff = signature.parameters.Count - targetFunc.parameters.Count;
            if (paramCountDiff < 0) continue;

            // 逐一比较参数类型，自定义类型（映射为 @null）和 any 通配任意类型
            // 跳过 signature 开头的 paramCountDiff 个参数（模块对象）
            var paramsMatch = true;
            for (var j = 0; j < targetFunc.parameters.Count; j++)
            {
                var actualParamType = to_value_type(targetFunc.parameters[j].type_ref);
                var sigParamType = signature.parameters[j + paramCountDiff];
                if (actualParamType != sigParamType
                    && actualParamType != GenerateValueType.@null
                    && actualParamType != GenerateValueType.any
                    && sigParamType != GenerateValueType.any)
                {
                    paramsMatch = false;
                    break;
                }
            }

            if (!paramsMatch) continue;

            // 比较返回类型，自定义类型（映射为 @null）和 any 通配任意类型
            var expectedReturn = signature.results.Count == 0
                ? GenerateValueType.@void
                : signature.results[0];

            var actualReturnType = to_value_type(targetFunc.return_type_ref);
            if (actualReturnType != expectedReturn
                && actualReturnType != GenerateValueType.@null
                && actualReturnType != GenerateValueType.any
                && expectedReturn != GenerateValueType.any)
                continue;

            resolvedParamTypes = [.. targetFunc.parameters.Select(p => to_jvm_type_name(p.type_ref))];
            resolvedReturnType = to_jvm_type_name(targetFunc.return_type_ref);
            resolvedFuncName = targetFunc.name;
            normalParamCountDiff = paramCountDiff;
            break;
        }

        var resolvedDescriptor = JvmTypeMapper.map_method_descriptor(resolvedReturnType, resolvedParamTypes);

        var targetClass = className;
        var targetMethod = operand.name;

        // 识别 [jvm] 属性外部函数：直接调用指定的 Java 类方法
        if (linkTable.try_get_jvm_external_ref(operand.name, out var jvmRef))
        {
            // [jvm] 外部函数的签名可能包含前端隐式推入的模块对象参数。
            // 从 allFunctions 查找函数定义，获取实际参数类型，构建正确的描述符。
            // 如果找不到函数定义，回退到 signature 的参数类型。
            var externalParamTypes = resolvedParamTypes;
            var externalReturnType = resolvedReturnType;
            for (var fi = 0; fi < allFunctions.Count; fi++)
            {
                var targetFunc = allFunctions[fi];
                var nameMatch = string.Equals(targetFunc.name, operand.name, StringComparison.Ordinal);
                if (!nameMatch)
                {
                    var funcLastDot = targetFunc.name.LastIndexOf('.');
                    var funcShortName = funcLastDot >= 0 ? targetFunc.name[(funcLastDot + 1)..] : targetFunc.name;
                    nameMatch = string.Equals(funcShortName, operandShortName, StringComparison.Ordinal);
                }

                if (nameMatch)
                {
                    externalParamTypes = [.. targetFunc.parameters.Select(p => to_jvm_type_name(p.type_ref))];
                    externalReturnType = to_jvm_type_name(targetFunc.return_type_ref);
                    break;
                }
            }

            var externalResolvedDescriptor =
                JvmTypeMapper.map_method_descriptor(externalReturnType, externalParamTypes);

            // 构建有效的 JVM 描述符：
            // 1. 如果 [jvm] 属性提供了第 3 个参数（描述符覆盖），直接使用
            // 2. 否则，当返回类型为 any/external_ref/object 时，使用 [jvm] 属性的类名替代 Object
            var effectiveDescriptor = jvmRef.DescriptorOverride ?? externalResolvedDescriptor;
            if (jvmRef.DescriptorOverride == null)
                effectiveDescriptor =
                    apply_jvm_descriptor_heuristic(externalResolvedDescriptor, jvmRef.ClassName, signature);

            // 当签名返回 unit 时，Java 标准库方法（如 println）通常返回 void 而非 int。
            // 将描述符的返回类型从 I（int）改为 V（void），避免 VerifyError。
            if (signature.results.Count > 0 && signature.results[0] == GenerateValueType.unit
                                            && effectiveDescriptor.EndsWith(")I", StringComparison.Ordinal))
                effectiveDescriptor = effectiveDescriptor[..^1] + "V";

            // 跨模块调用时，build_dot_call_expression 的回退路径可能将模块路径对象
            // （如 std.adaptor.jvm.console）作为额外参数推入栈中。
            // [jvm] 外部函数不需要这些模块对象作为参数，需要弹出多余的参数。
            // 注意：signature.parameters.Count 不包含前端隐式推入的模块对象，
            // 因此需要通过回溯指令流推断 call 前栈上的实际值数量。
            var expectedParamCount = count_descriptor_parameters(effectiveDescriptor);
            var actualStackValues = count_actual_stack_values_before_call(function, instructionIndex);
            var extraParamCount = actualStackValues - expectedParamCount;

            if (extraParamCount > 0)
            {
                // 弹出多余的参数（模块对象在栈底，需要先弹出栈顶的实际参数，再弹出模块对象，再恢复实际参数）
                // 简单方案：使用 pop 指令弹出栈底的多余参数
                // 但 JVM 没有直接弹出栈底元素的指令，需要用 dup_x2 + pop 等复杂操作
                // 更简单的方案：将多余参数存入临时局部变量，调用后再丢弃
                // 但这里采用最简单的方案：弹出栈顶的 expectedParamCount 个参数，再弹出 extraParamCount 个多余参数，再恢复
                // 由于复杂度较高，这里先只处理 extraParamCount == 1 且 expectedParamCount == 1 的常见情况
                // （即模块对象在栈底，实际参数在栈顶）
                if (extraParamCount == 1 && expectedParamCount == 1)
                {
                    // 栈：[module_obj, arg0]
                    // swap → [arg0, module_obj]
                    // pop → [arg0]
                    writer.write_u8((byte)JvmOpcode.swap);
                    writer.write_u8((byte)JvmOpcode.pop);
                }
                else
                {
                    // 通用方案：逐个弹出栈顶参数到临时变量，再弹出多余参数，再恢复
                    // 这里先弹出所有参数到临时变量（逆序）
                    // 注意：这是简化实现，假设参数都是引用类型（aload/astore）
                    // 实际需要根据参数类型选择正确的指令
                    // 由于当前场景主要是模块对象（引用类型），这里用 pop2 处理
                    // 栈：[module_obj, arg0, arg1, ..., argN-1]
                    // 需要变成：[arg0, arg1, ..., argN-1]
                    // 用 dup_x2 + pop 逐个将栈底元素移除
                    for (var i = 0; i < expectedParamCount; i++)
                    {
                        // dup_x2: 复制栈顶值并插入到栈底元素下方
                        // 栈：[module_obj, arg_i, ..., argN-1] → [argN-1, module_obj, arg_i, ..., argN-1]
                        // pop: [argN-1, module_obj, arg_i, ..., argN-2]
                        // 这个操作太复杂，暂时不支持
                    }
                    // 暂时不处理复杂情况，让验证器报错
                }
            }

            emit_jvm_external_call(ref writer, constantPool, jvmRef, effectiveDescriptor, signature);
            // emit_call_result_adjustment 在签名期望 unit/void 时不会保留返回值
            // 返回值使用 actualStackValues（实际消费的栈上值数），而非 paramCount（签名参数数），
            // 因为前端可能隐式推入了模块对象等额外参数。
            return -actualStackValues + (signatureExpectsNoResult ? 0 : 1);
        }

        // 跨后端外部函数 fallback：弹出参数，压入默认返回值
        if (linkTable.is_other_external_name(operand.name))
        {
            foreach (var paramType in resolvedParamTypes)
                if (paramType is "long" or "double")
                    writer.write_u8((byte)JvmOpcode.pop2);
                else
                    writer.write_u8((byte)JvmOpcode.pop);

            emit_default_value(ref writer, resolvedReturnType, constantPool);
            // 弹出所有参数，压入默认返回值
            return -paramCount + 1;
        }

        targetMethod = sanitize_method_name(resolvedFuncName);

        // 参数类型调整：统一处理 checkcast（引用类型窄化）和 int→Object 装箱（原始类型提升）。
        // 对于多参数调用，使用"存储到临时局部变量 → 重新加载并应用转换"的方式，
        // 确保每个参数都能独立应用正确的转换，避免栈顶位置错误。
        // 对于单参数调用，直接对栈顶应用转换。
        if (resolvedParamTypes.Length > 0)
        {
            // 推断栈上实际参数类型（考虑 pop 指令消费值等复杂情况）
            var actualParamJvmTypes = infer_actual_param_types(function, instructionIndex, resolvedParamTypes.Length);

            // 调试日志：输出函数名、参数类型推断结果
            Console.Error.WriteLine($"[CALL-DEBUG] func={function.name} target={resolvedFuncName} callIdx={instructionIndex} paramCount={resolvedParamTypes.Length}");
            if (function.name.Contains("emit_single_project") || function.name.Contains("read_project_manifest"))
            {
                Console.Error.WriteLine($"[CALL-DEBUG] resolvedParamTypes=[{string.Join(", ", resolvedParamTypes)}]");
                Console.Error.WriteLine($"[CALL-DEBUG] actualParamJvmTypes=[{string.Join(", ", actualParamJvmTypes)}]");
            }

            // 计算每个参数需要的转换类型
            var needsCheckcast = new bool[resolvedParamTypes.Length];
            var needsIntToObject = new bool[resolvedParamTypes.Length];
            var needsObjectToInt = new bool[resolvedParamTypes.Length];
            for (var pi = 0; pi < resolvedParamTypes.Length; pi++)
            {
                var resType = resolvedParamTypes[pi];
                // checkcast：引用类型比 Object 更具体时
                if (resType != "java/lang/Object" && resType != "int" && resType != "boolean"
                    && resType != "byte" && resType != "short" && resType != "char"
                    && resType != "long" && resType != "float" && resType != "double"
                    && resType != "void")
                    needsCheckcast[pi] = true;

                if (pi < actualParamJvmTypes.Length)
                {
                    var actualType = actualParamJvmTypes[pi];
                    // int→Object：栈上是原始类型但方法期望 Object/String
                    if (actualType is "int" or "boolean" or "long" or "float" or "double"
                        && resType is "java/lang/Object" or "java/lang/String")
                        needsIntToObject[pi] = true;
                    // Object→int：栈上是 Object 但方法期望 int/boolean
                    // 例如：HashMap.get 返回 Object（any），但 peek_von_token 期望 int（i32）
                    if (actualType is "java/lang/Object"
                        && resType is "int" or "boolean")
                        needsObjectToInt[pi] = true;
                }
            }

            var hasAnyAdjustment = needsCheckcast.Any(b => b) || needsIntToObject.Any(b => b) ||
                                   needsObjectToInt.Any(b => b);

            // 调试日志：输出转换需求
            if (function.name.Contains("emit_single_project") || function.name.Contains("read_project_manifest"))
            {
                Console.Error.WriteLine($"[CALL-DEBUG] needsCheckcast=[{string.Join(", ", needsCheckcast.Select(b => b.ToString()))}]");
                Console.Error.WriteLine($"[CALL-DEBUG] needsIntToObject=[{string.Join(", ", needsIntToObject.Select(b => b.ToString()))}]");
                Console.Error.WriteLine($"[CALL-DEBUG] needsObjectToInt=[{string.Join(", ", needsObjectToInt.Select(b => b.ToString()))}]");
            }

            if (hasAnyAdjustment)
            {
                if (resolvedParamTypes.Length > 1)
                {
                    // 多参数：存储到临时局部变量，再重新加载并应用转换
                    // 基址 = 参数槽位 + 局部变量槽位 + set_field 预留（不含 call 预留）
                    var totalSlots = compute_parameter_slots(function);
                    foreach (var local in function.local_variables)
                        totalSlots += is_wide_type(to_jvm_type_name(local.type_ref)) ? 2 : 1;

                    if (function.instructions.Any(i => i.head_code is NyarHeadCode.set_field
                            or NyarHeadCode.set_offset_index
                            or NyarHeadCode.set_ordinal_index)) totalSlots += 2;

                    var baseLocalSlot = totalSlots;

                    // 第一遍：从栈上存储到临时局部变量（使用实际栈上类型）
                    for (var pi = resolvedParamTypes.Length - 1; pi >= 0; pi--)
                    {
                        var actualType = pi < actualParamJvmTypes.Length
                            ? actualParamJvmTypes[pi]
                            : "java/lang/Object";
                        emit_store_to_temp_local(ref writer, actualType, baseLocalSlot + pi);
                    }

                    // 第二遍：从临时局部变量重新加载并应用转换
                    for (var pi = 0; pi < resolvedParamTypes.Length; pi++)
                    {
                        var actualType = pi < actualParamJvmTypes.Length
                            ? actualParamJvmTypes[pi]
                            : "java/lang/Object";
                        emit_load_from_temp_local(ref writer, actualType, baseLocalSlot + pi);

                        // 应用 Object→int 拆箱
                        if (needsObjectToInt[pi]) emit_unbox_to_int(ref writer, constantPool);

                        // 应用 int→Object 装箱
                        if (needsIntToObject[pi]) emit_box_primitive(ref writer, actualType, constantPool);

                        // 应用 checkcast
                        if (needsCheckcast[pi])
                        {
                            var classIdx = add_class(constantPool, resolvedParamTypes[pi]);
                            writer.write_u8((byte)JvmOpcode.checkcast);
                            writer.write_u16_be(classIdx);
                        }
                    }
                }
                else
                {
                    // 单参数：直接对栈顶应用转换
                    // 应用 Object→int 拆箱
                    if (needsObjectToInt[0]) emit_unbox_to_int(ref writer, constantPool);

                    if (needsIntToObject[0])
                    {
                        var actualType = actualParamJvmTypes.Length > 0
                            ? actualParamJvmTypes[0]
                            : "java/lang/Object";
                        emit_box_primitive(ref writer, actualType, constantPool);
                    }

                    if (needsCheckcast[0])
                    {
                        var classIdx = add_class(constantPool, resolvedParamTypes[0]);
                        writer.write_u8((byte)JvmOpcode.checkcast);
                        writer.write_u16_be(classIdx);
                    }
                }
            }
        }

        // 跨模块调用时，build_dot_call_expression 的回退路径可能将模块路径对象
        // （如 std.io）作为额外参数推入栈中。普通 Valkyrie 函数不需要这些模块对象作为参数，
        // 需要弹出多余的参数。
        // 模块对象数量通过签名参数数与目标函数参数数的差异确定（normalParamCountDiff），
        // 而不是通过回溯指令流推断总栈深度（会误判其他指令推入的值）。
        var normalExpectedParamCount = resolvedParamTypes.Length;
        var normalExtraParamCount = normalParamCountDiff;

        if (normalExtraParamCount > 0)
        {
            // 弹出多余的模块对象参数
            // 简单方案：只处理 extraParamCount == 1 且 expectedParamCount == 1 的情况
            // （即模块对象在栈底，实际参数在栈顶）
            if (normalExtraParamCount == 1 && normalExpectedParamCount == 1)
            {
                // 栈：[module_obj, arg0]
                // swap → [arg0, module_obj]
                // pop → [arg0]
                writer.write_u8((byte)JvmOpcode.swap);
                writer.write_u8((byte)JvmOpcode.pop);
            }
            else if (normalExtraParamCount == 1 && normalExpectedParamCount == 0)
            {
                // 栈：[module_obj]
                // pop → []
                writer.write_u8((byte)JvmOpcode.pop);
            }
            // 其他复杂情况暂不处理，让验证器报错
        }

        var methodRefIdx = add_method_ref(constantPool, targetClass, targetMethod, resolvedDescriptor);
        writer.write_u8((byte)JvmOpcode.invokestatic);
        writer.write_u16_be(methodRefIdx);
        emit_call_result_adjustment(ref writer, signature, resolvedReturnType, constantPool);
        // emit_call_result_adjustment 在签名期望 unit/void 时不会保留返回值
        // 返回值使用 normalExpectedParamCount + normalParamCountDiff（实际参数 + 模块对象）
        return -(normalExpectedParamCount + normalParamCountDiff) + (signatureExpectsNoResult ? 0 : 1);
    }

    /// <summary>
    ///     发射 [jvm] 属性外部函数调用。
    ///     支持两种模式：
    ///     <list type="bullet">
    ///         <item><c>field.method</c> 模式：MethodName 含 '.' → 生成 getstatic + swap + invokevirtual 序列</item>
    ///         <item>普通模式：MethodName 不含 '.' → 生成 invokestatic</item>
    ///     </list>
    /// </summary>
    private static void emit_jvm_external_call(ref ByteBufferWriter writer, List<JvmConstant> constantPool,
        (string ClassName, string MethodName, string? DescriptorOverride) jvmRef, string descriptor,
        GenerateFunctionType signature)
    {
        // 当签名返回 unit 时，Java 标准库方法（如 println）通常返回 void 而非 int。
        // 将描述符的返回类型从 I（int）改为 V（void），避免 VerifyError。
        // emit_call_result_adjustment 会在 invokevirtual 之后推入 iconst_0 作为 unit 占位符。
        var effectiveDescriptor = descriptor;
        if (signature.results.Count > 0 && signature.results[0] == GenerateValueType.unit
                                        && descriptor.EndsWith(")I", StringComparison.Ordinal))
            effectiveDescriptor = descriptor[..^1] + "V";

        // 检测 MethodName 是否包含 '.' —— field.method 模式
        var dotIndex = jvmRef.MethodName.IndexOf('.');
        if (dotIndex >= 0)
        {
            var fieldName = jvmRef.MethodName[..dotIndex];
            var instanceMethodName = jvmRef.MethodName[(dotIndex + 1)..];
            var internalClassName = jvmRef.ClassName.Replace('.', '/');

            // getstatic 获取静态字段（如 System.out / System.err）
            var fieldDescriptor = "Ljava/io/PrintStream;";
            var fieldIdx = add_field_ref(constantPool, internalClassName, fieldName, fieldDescriptor);
            writer.write_u8((byte)JvmOpcode.getstatic);
            writer.write_u16_be(fieldIdx);

            // swap：将参数移到 PrintStream 下方，让 PrintStream 成为 invokevirtual 的接收者
            writer.write_u8((byte)JvmOpcode.swap);

            // invokevirtual 调用实例方法，目标类是字段类型（如 java/io/PrintStream），而非字段所在类
            var instanceClass = fieldDescriptor[1..^1];
            var methodIdx = add_method_ref(constantPool, instanceClass, instanceMethodName, effectiveDescriptor);
            writer.write_u8((byte)JvmOpcode.invokevirtual);
            writer.write_u16_be(methodIdx);
        }
        else
        {
            // 普通 invokestatic 行为
            var internalClassName = jvmRef.ClassName.Replace('.', '/');
            var methodIdx = add_method_ref(constantPool, internalClassName, jvmRef.MethodName, effectiveDescriptor);
            writer.write_u8((byte)JvmOpcode.invokestatic);
            writer.write_u16_be(methodIdx);
        }

        var externalReturnType = extract_return_type_from_descriptor(effectiveDescriptor);
        emit_call_result_adjustment(ref writer, signature, externalReturnType, constantPool);
    }

    /// <summary>
    ///     从 JVM 方法描述符中提取返回类型名称。
    ///     描述符格式为 (参数类型)返回类型，返回类型位于 ')' 之后。
    /// </summary>
    /// <param name="descriptor">JVM 方法描述符，如 "(Ljava/lang/String;)V"。</param>
    /// <returns>JVM 类型名（void / int / long / float / double / boolean / java/lang/Object）。</returns>
    private static string extract_return_type_from_descriptor(string descriptor)
    {
        var closeParenIndex = descriptor.IndexOf(')');
        if (closeParenIndex < 0 || closeParenIndex + 1 >= descriptor.Length) return "void";

        var returnTypeStr = descriptor[(closeParenIndex + 1)..];
        return returnTypeStr switch
        {
            "V" => "void",
            "I" => "int",
            "J" => "long",
            "F" => "float",
            "D" => "double",
            "Z" => "boolean",
            "B" => "byte",
            "C" => "char",
            "S" => "short",
            _ when returnTypeStr.StartsWith('L')
                   || returnTypeStr.StartsWith('[') => "java/lang/Object",
            _ => "void"
        };
    }

    /// <summary>
    ///     正向计算指令流，得到 call/call_static 指令前栈上的实际值数量。
    ///     用于检测前端是否隐式推入了模块对象等额外参数。
    ///     从函数开头正向追踪栈深度变化，直到 call 指令前一条。
    ///     注意：此方法假设函数体内无控制流分支（call 前无 jump/label），
    ///     对于复杂控制流可能不准确，但 [jvm] 外部函数调用通常在简单上下文中。
    /// </summary>
    /// <param name="function">当前函数。</param>
    /// <param name="callInstructionIndex">call 指令的索引。</param>
    /// <returns>call 指令前栈上的实际值数量。</returns>
    private static int count_actual_stack_values_before_call(GenerateFunction function, int callInstructionIndex)
    {
        var depth = 0;
        for (var idx = 0; idx < callInstructionIndex; idx++)
        {
            var instr = function.instructions[idx];
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

            depth = depth - pop + push;
            if (depth < 0) depth = 0;
        }

        return depth;
    }

    /// <summary>
    ///     从 JVM 方法描述符中解析参数数量。
    ///     描述符格式为 (参数类型)返回类型，参数类型位于 '(' 和 ')' 之间。
    /// </summary>
    /// <param name="descriptor">JVM 方法描述符，如 "(Ljava/lang/String;)V"。</param>
    /// <returns>参数数量。</returns>
    private static int count_descriptor_parameters(string descriptor)
    {
        var openParenIndex = descriptor.IndexOf('(');
        var closeParenIndex = descriptor.IndexOf(')');
        if (openParenIndex < 0 || closeParenIndex < 0 || closeParenIndex <= openParenIndex) return 0;

        var paramSection = descriptor[(openParenIndex + 1)..closeParenIndex];
        var count = 0;
        var i = 0;
        while (i < paramSection.Length)
            // long/double 占 2 个槽位，但算 1 个参数
            if (paramSection[i] is 'J' or 'D')
            {
                count++;
                i++;
            }
            else if (paramSection[i] == 'L')
            {
                // 对象类型：LClassName; 末尾以 ';' 结束
                var semiIndex = paramSection.IndexOf(';', i);
                if (semiIndex < 0) break;
                count++;
                i = semiIndex + 1;
            }
            else if (paramSection[i] == '[')
            {
                // 数组类型：[ 后跟元素类型
                i++;
                // 连续的 [ 表示多维数组
                while (i < paramSection.Length && paramSection[i] == '[') i++;

                // 跳过元素类型
                if (i < paramSection.Length && paramSection[i] == 'L')
                {
                    var semiIndex = paramSection.IndexOf(';', i);
                    if (semiIndex < 0) break;
                    i = semiIndex + 1;
                }
                else
                {
                    i++;
                }

                count++;
            }
            else
            {
                // 原始类型：I, Z, B, C, S, F
                count++;
                i++;
            }

        return count;
    }

    /// <summary>
    ///     对 JVM 外部调用应用描述符启发式修正。
    ///     当返回类型为 any / external_ref / object 时，将描述符中的返回类型
    ///     java/lang/Object 替换为 [jvm] 属性中声明的类名。
    ///     这处理了工厂方法（如 Path.of）返回自身类型的常见模式。
    /// </summary>
    /// <param name="descriptor">当前解析出的 JVM 方法描述符。</param>
    /// <param name="jvmClassName">[jvm] 属性中声明的 JVM 类名。</param>
    /// <param name="signature">函数签名，用于判断返回类型。</param>
    /// <returns>修正后的 JVM 方法描述符。</returns>
    private static string apply_jvm_descriptor_heuristic(string descriptor, string jvmClassName,
        GenerateFunctionType signature)
    {
        // 只对返回类型为 any / external_ref / object 的函数应用启发式修正
        if (signature.results.Count == 0) return descriptor;

        var returnType = signature.results[0];
        if (returnType is not (GenerateValueType.any or GenerateValueType.external_ref or GenerateValueType.@object))
            return descriptor;

        // 将类名中的点号替换为斜杠
        var internalClassName = jvmClassName.Replace('.', '/');

        // 描述符格式：(参数类型)返回类型
        var closeParenIndex = descriptor.IndexOf(')');
        if (closeParenIndex < 0) return descriptor;

        var originalReturnPart = descriptor[(closeParenIndex + 1)..];

        // 只有当原始返回类型为 Object 时才替换（安全保护）
        if (originalReturnPart == "Ljava/lang/Object;")
            return descriptor[..(closeParenIndex + 1)] + "L" + internalClassName + ";";

        return descriptor;
    }

    /// <summary>
    ///     根据方法签名期望的返回类型和实际解析的返回类型，发射调用后的栈调整指令。
    ///     处理签名返回类型与实际方法返回类型不一致的情况：
    ///     <list type="bullet">
    ///         <item>签名期望 unit + 实际返回 void → 压入 iconst_0 作为 unit 占位符</item>
    ///         <item>签名期望 unit + 实际返回非 void → 弹出返回值后压入 iconst_0</item>
    ///         <item>签名无返回值（void）+ 实际返回非 void → 弹出返回值</item>
    ///         <item>签名期望非 unit 返回值 + 实际返回非 void → 保持栈不变</item>
    ///         <item>签名期望非 unit 返回值 + 实际返回 void → 压入期望类型的默认值</item>
    ///     </list>
    /// </summary>
    private static void emit_call_result_adjustment(ref ByteBufferWriter writer, GenerateFunctionType signature,
        string resolvedReturnType, List<JvmConstant> constantPool)
    {
        var signatureExpectsUnit = signature.results.Count > 0 && signature.results[0] == GenerateValueType.unit;
        var signatureExpectsVoid = signature.results.Count == 0;
        var resolvedIsVoid = resolvedReturnType == "void";

        if (signatureExpectsUnit)
        {
            if (resolvedIsVoid)
            {
                // 方法无返回值，签名期望 unit：不推入占位符
                // unit 在 JVM 中映射为 int，但占位符由 return 语句或 store_local 提供
            }
            else
            {
                // 方法有返回值但签名期望 unit，弹出返回值，不推入占位符
                if (resolvedReturnType is "long" or "double")
                    writer.write_u8((byte)JvmOpcode.pop2);
                else
                    writer.write_u8((byte)JvmOpcode.pop);
            }
        }
        else if (signatureExpectsVoid)
        {
            if (!resolvedIsVoid)
            {
                // 方法有返回值但签名期望 void，弹出返回值
                if (resolvedReturnType is "long" or "double")
                    writer.write_u8((byte)JvmOpcode.pop2);
                else
                    writer.write_u8((byte)JvmOpcode.pop);
            }
            // 否则：方法无返回值且签名期望 void，无需调整
        }
        else
        {
            if (resolvedIsVoid)
            {
                // 方法无返回值但签名期望有返回值，压入默认值
                var expectedType = signature.results.Count > 0 ? to_jvm_type_name(signature.results[0]) : "void";
                emit_default_value(ref writer, expectedType, constantPool);
            }
            else
            {
                // 方法有返回值且签名期望返回值，检查类型是否兼容
                var expectedType = signature.results.Count > 0 ? to_jvm_type_name(signature.results[0]) : "void";

                // 签名期望 int/boolean 但方法返回 Object → 将 Object 转为 int
                // 使用 null 检查：非 null → 1，null → 0
                if (expectedType is "int" or "boolean" &&
                    resolvedReturnType is "java/lang/Object" or "java/lang/String")
                {
                    writer.write_u8((byte)JvmOpcode.ifnonnull);
                    writer.write_i16_be(7);
                    writer.write_u8((byte)JvmOpcode.iconst0);
                    writer.write_u8((byte)JvmOpcode.@goto);
                    writer.write_i16_be(4);
                    writer.write_u8((byte)JvmOpcode.iconst1);
                }
                // 签名期望 String 但方法返回 Object → checkcast String
                else if (expectedType == "java/lang/String" && resolvedReturnType == "java/lang/Object")
                {
                    var stringClassIdx = add_class(constantPool, "java/lang/String");
                    writer.write_u8((byte)JvmOpcode.checkcast);
                    writer.write_u16_be(stringClassIdx);
                }
                // 否则：类型兼容，保持栈不变
            }
        }
    }

    /// <summary>
    ///     将栈顶值存储到指定局部变量槽位，根据实际 JVM 类型选择正确的 store 操作码。
    /// </summary>
    private static void emit_store_to_temp_local(ref ByteBufferWriter writer, string jvmType, int slot)
    {
        switch (jvmType)
        {
            case "int" or "boolean" or "byte" or "short" or "char":
                writer.write_u8((byte)JvmOpcode.istore);
                writer.write_u8((byte)slot);
                break;
            case "long":
                writer.write_u8((byte)JvmOpcode.lstore);
                writer.write_u8((byte)slot);
                break;
            case "float":
                writer.write_u8((byte)JvmOpcode.fstore);
                writer.write_u8((byte)slot);
                break;
            case "double":
                writer.write_u8((byte)JvmOpcode.dstore);
                writer.write_u8((byte)slot);
                break;
            default:
                writer.write_u8((byte)JvmOpcode.astore);
                writer.write_u8((byte)slot);
                break;
        }
    }

    /// <summary>
    ///     从指定局部变量槽位加载值到栈顶，根据实际 JVM 类型选择正确的 load 操作码。
    /// </summary>
    private static void emit_load_from_temp_local(ref ByteBufferWriter writer, string jvmType, int slot)
    {
        switch (jvmType)
        {
            case "int" or "boolean" or "byte" or "short" or "char":
                writer.write_u8((byte)JvmOpcode.iload);
                writer.write_u8((byte)slot);
                break;
            case "long":
                writer.write_u8((byte)JvmOpcode.lload);
                writer.write_u8((byte)slot);
                break;
            case "float":
                writer.write_u8((byte)JvmOpcode.fload);
                writer.write_u8((byte)slot);
                break;
            case "double":
                writer.write_u8((byte)JvmOpcode.dload);
                writer.write_u8((byte)slot);
                break;
            default:
                writer.write_u8((byte)JvmOpcode.aload);
                writer.write_u8((byte)slot);
                break;
        }
    }

    /// <summary>
    ///     将栈顶的原始类型值装箱为对应的包装类型。
    ///     支持 int/boolean → Integer、long → Long、float → Float、double → Double。
    /// </summary>
    private static void emit_box_primitive(ref ByteBufferWriter writer, string jvmType,
        List<JvmConstant> constantPool)
    {
        switch (jvmType)
        {
            case "int" or "boolean":
            {
                var idx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(idx);
                break;
            }
            case "long":
            {
                var idx = add_method_ref(constantPool, "java/lang/Long", "valueOf",
                    "(J)Ljava/lang/Long;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(idx);
                break;
            }
            case "float":
            {
                var idx = add_method_ref(constantPool, "java/lang/Float", "valueOf",
                    "(F)Ljava/lang/Float;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(idx);
                break;
            }
            case "double":
            {
                var idx = add_method_ref(constantPool, "java/lang/Double", "valueOf",
                    "(D)Ljava/lang/Double;");
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(idx);
                break;
            }
        }
    }

    /// <summary>
    ///     处理未解析的 infix 运算符调用（如 "text.infix +"、"index.infix <"、"ch.infix =="）。
    ///     这些函数名由 HIR 生成但无法解析为具体方法分派，在链接表中不存在，
    ///     需要根据操作数类型直接发射对应字节码。
    /// </summary>
    /// <returns>净栈深度变化量（正值=推入，负值=弹出）。</returns>
    private static int emit_unresolved_infix_call(
        ref ByteBufferWriter writer, List<JvmConstant> constantPool,
        GenerateOperand.FuncRef operand, GenerateFunctionType signature,
        GenerateFunction function, int instructionIndex,
        JvmExternalLinkTable linkTable)
    {
        var dotInfixIdx = operand.name.LastIndexOf(".infix ", StringComparison.Ordinal);
        var op = operand.name[(dotInfixIdx + ".infix ".Length)..];
        var operandTypes = scan_binary_operand_types(function, instructionIndex, linkTable);
        var hasString = operandTypes[0] == "java/lang/String" || operandTypes[1] == "java/lang/String";
        var signatureExpectsNoResult = signature.results.Count == 0
                                       || signature.results[0] == GenerateValueType.unit;

        // 处理复合运算符（如 "==.infix ||"、"<.infix &&"）
        // 这些是短路逻辑运算符，操作数是两个 bool 值
        if (op == "||" || op == "&&")
        {
            // 栈上两个 Object（Boolean），需要拆箱为 int 后用 ior/iand
            emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
            writer.write_u8((byte)(op == "||" ? JvmOpcode.ior : JvmOpcode.iand));
            emit_box_primitive(ref writer, "int", constantPool);
            if (signatureExpectsNoResult)
            {
                writer.write_u8((byte)JvmOpcode.pop);
                return -2;
            }

            return -2 + 1;
        }

        switch (op)
        {
            case "+":
            {
                if (hasString)
                {
                    emit_string_concat(ref writer, function, instructionIndex, constantPool, linkTable);
                }
                else
                {
                    emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                    writer.write_u8((byte)JvmOpcode.iadd);
                    // iadd 产生原始 int，但签名返回 any（Object），需要装箱
                    emit_box_primitive(ref writer, "int", constantPool);
                }

                break;
            }
            case "-":
            {
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                writer.write_u8((byte)JvmOpcode.isub);
                emit_box_primitive(ref writer, "int", constantPool);
                break;
            }
            case "*":
            {
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                writer.write_u8((byte)JvmOpcode.imul);
                emit_box_primitive(ref writer, "int", constantPool);
                break;
            }
            case "/":
            {
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                writer.write_u8((byte)JvmOpcode.idiv);
                emit_box_primitive(ref writer, "int", constantPool);
                break;
            }
            case "==":
            {
                if (hasString)
                {
                    // String.equals：栈 [str1, str2] → invokevirtual String.equals(Object)Z
                    var stringClassIdx = add_class(constantPool, "java/lang/String");
                    writer.write_u8((byte)JvmOpcode.checkcast);
                    writer.write_u16_be(stringClassIdx);
                    writer.write_u8((byte)JvmOpcode.swap);
                    writer.write_u8((byte)JvmOpcode.checkcast);
                    writer.write_u16_be(stringClassIdx);
                    writer.write_u8((byte)JvmOpcode.swap);
                    var equalsIdx = add_method_ref(constantPool, "java/lang/String", "equals",
                        "(Ljava/lang/Object;)Z");
                    writer.write_u8((byte)JvmOpcode.invokevirtual);
                    writer.write_u16_be(equalsIdx);
                }
                else
                {
                    emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                    // if_icmpne 跳转模式：相等时 push 1，不等时 push 0
                    var eqIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                        "(I)Ljava/lang/Integer;");
                    writer.write_u8((byte)JvmOpcode.ificmpne);
                    writer.write_i16_be(7);
                    writer.write_u8((byte)JvmOpcode.iconst1);
                    writer.write_u8((byte)JvmOpcode.@goto);
                    writer.write_i16_be(4);
                    writer.write_u8((byte)JvmOpcode.iconst0);
                    writer.write_u8((byte)JvmOpcode.invokestatic);
                    writer.write_u16_be(eqIdx);
                }

                break;
            }
            case "!=":
            {
                if (hasString)
                {
                    var stringClassIdx = add_class(constantPool, "java/lang/String");
                    writer.write_u8((byte)JvmOpcode.checkcast);
                    writer.write_u16_be(stringClassIdx);
                    writer.write_u8((byte)JvmOpcode.swap);
                    writer.write_u8((byte)JvmOpcode.checkcast);
                    writer.write_u16_be(stringClassIdx);
                    writer.write_u8((byte)JvmOpcode.swap);
                    var equalsIdx = add_method_ref(constantPool, "java/lang/String", "equals",
                        "(Ljava/lang/Object;)Z");
                    writer.write_u8((byte)JvmOpcode.invokevirtual);
                    writer.write_u16_be(equalsIdx);
                    // equals 返回 true 时取反：ifne → 不等时 push 1
                    var neIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                        "(I)Ljava/lang/Integer;");
                    writer.write_u8((byte)JvmOpcode.ifne);
                    writer.write_i16_be(7);
                    writer.write_u8((byte)JvmOpcode.iconst1);
                    writer.write_u8((byte)JvmOpcode.@goto);
                    writer.write_i16_be(4);
                    writer.write_u8((byte)JvmOpcode.iconst0);
                    writer.write_u8((byte)JvmOpcode.invokestatic);
                    writer.write_u16_be(neIdx);
                }
                else
                {
                    emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                    var neIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                        "(I)Ljava/lang/Integer;");
                    writer.write_u8((byte)JvmOpcode.ificmpeq);
                    writer.write_i16_be(7);
                    writer.write_u8((byte)JvmOpcode.iconst1);
                    writer.write_u8((byte)JvmOpcode.@goto);
                    writer.write_i16_be(4);
                    writer.write_u8((byte)JvmOpcode.iconst0);
                    writer.write_u8((byte)JvmOpcode.invokestatic);
                    writer.write_u16_be(neIdx);
                }

                break;
            }
            case "<":
            {
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                var ltIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.ificmpge);
                writer.write_i16_be(7);
                writer.write_u8((byte)JvmOpcode.iconst1);
                writer.write_u8((byte)JvmOpcode.@goto);
                writer.write_i16_be(4);
                writer.write_u8((byte)JvmOpcode.iconst0);
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(ltIdx);
                break;
            }
            case "<=":
            {
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                var leIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.ificmpgt);
                writer.write_i16_be(7);
                writer.write_u8((byte)JvmOpcode.iconst1);
                writer.write_u8((byte)JvmOpcode.@goto);
                writer.write_i16_be(4);
                writer.write_u8((byte)JvmOpcode.iconst0);
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(leIdx);
                break;
            }
            case ">":
            {
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                var gtIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.ificmple);
                writer.write_i16_be(7);
                writer.write_u8((byte)JvmOpcode.iconst1);
                writer.write_u8((byte)JvmOpcode.@goto);
                writer.write_i16_be(4);
                writer.write_u8((byte)JvmOpcode.iconst0);
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(gtIdx);
                break;
            }
            case ">=":
            {
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                var geIdx = add_method_ref(constantPool, "java/lang/Integer", "valueOf",
                    "(I)Ljava/lang/Integer;");
                writer.write_u8((byte)JvmOpcode.ificmplt);
                writer.write_i16_be(7);
                writer.write_u8((byte)JvmOpcode.iconst1);
                writer.write_u8((byte)JvmOpcode.@goto);
                writer.write_i16_be(4);
                writer.write_u8((byte)JvmOpcode.iconst0);
                writer.write_u8((byte)JvmOpcode.invokestatic);
                writer.write_u16_be(geIdx);
                break;
            }
            case "%":
            {
                // 取模运算：irem
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                writer.write_u8((byte)JvmOpcode.irem);
                emit_box_primitive(ref writer, "int", constantPool);
                break;
            }
            case "||":
            {
                // 逻辑或：两个布尔值（int 0/1）进行按位或，非短路求值
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                writer.write_u8((byte)JvmOpcode.ior);
                emit_box_primitive(ref writer, "int", constantPool);
                break;
            }
            case "&&":
            {
                // 逻辑与：两个布尔值（int 0/1）进行按位与，非短路求值
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                writer.write_u8((byte)JvmOpcode.iand);
                emit_box_primitive(ref writer, "int", constantPool);
                break;
            }
            case "^":
            {
                // 按位异或：ixor
                emit_unbox_operands_for_binary_arith(ref writer, function, instructionIndex, constantPool, linkTable);
                writer.write_u8((byte)JvmOpcode.ixor);
                emit_box_primitive(ref writer, "int", constantPool);
                break;
            }
            default:
                throw new NotSupportedException($"未支持的未解析 infix 运算符：{operand.name}");
        }

        // 栈效果：弹出 2 个参数，推入 1 个结果
        // 签名期望 unit/void 时不保留返回值
        if (signatureExpectsNoResult)
        {
            writer.write_u8((byte)JvmOpcode.pop);
            return -2;
        }

        return -2 + 1;
    }

    /// <summary>
    ///     回溯二元运算前的两个栈值，推断它们的 JVM 类型。
    /// </summary>
    private static string[] scan_binary_operand_types(
        GenerateFunction function,
        int instructionIndex,
        JvmExternalLinkTable linkTable)
    {
        var types = new string[2] { "java/lang/Object", "java/lang/Object" };
        var foundCount = 0;
        var popSkipCount = 0;

        for (var idx = instructionIndex - 1; idx >= 0 && foundCount < 2; idx--)
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

        return types;
    }
}