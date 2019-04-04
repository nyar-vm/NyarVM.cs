using Std.Data.Binary.Clr;
using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.NyarIR.Data;
using Nyar.Types.Externals;

namespace Nyar.Assembler.Backends.Clr;

/// <summary>
///     CLR 后端 partial：外部引用注册和类型相关辅助方法。
/// </summary>
public partial class ClrBackend
{
    /// <summary>
    ///     注册内置外部类型引用。
    ///     这些类型用于 `unbox.any` 等需要 `TypeRef` token 的指令，`TypeRef` token 从 `0x01000001` 开始分配。
    /// </summary>
    private static void register_builtin_external_type_refs(
        GenerateModule module,
        List<ClrExternalTypeRef> externalTypeRefs,
        Dictionary<string, uint> typeRefTokenMap)
    {
        var typeRefIndex = 0u;
        var typeRefTokenBase = 0x01000001u;
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Int32");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Int64");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Single");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Double");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Boolean");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Char");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.String");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Object");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "System.Runtime",
            "System.Array");
        add_builtin_type_ref(externalTypeRefs, typeRefTokenMap, ref typeRefIndex, typeRefTokenBase, "mscorlib",
            "System.Collections.Generic.List`1");
        foreach (var binding in module.type_external_imports)
        {
            if (binding.external_import_link is not ExternalClrTypeImport clrImportLink) continue;
            if (clrImportLink.clr_type is not ClrNamedType namedType) continue;
            var fullName = namedType.full_name;
            if (typeRefTokenMap.ContainsKey(fullName)) continue;
            externalTypeRefs.Add(new ClrExternalTypeRef
            {
                assembly_name = clrImportLink.assembly_name,
                type_full_name = fullName,
                type_namespace = namedType.@namespace,
                type_name = namedType.name
            });
            typeRefTokenMap[fullName] = typeRefTokenBase + typeRefIndex;
            typeRefIndex++;
        }
    }

    private static void add_builtin_type_ref(
        List<ClrExternalTypeRef> externalTypeRefs,
        Dictionary<string, uint> typeRefTokenMap,
        ref uint typeRefIndex,
        uint typeRefTokenBase,
        string assemblyName,
        string typeFullName)
    {
        var lastDot = typeFullName.LastIndexOf('.');
        var typeNamespace = lastDot >= 0 ? typeFullName[..lastDot] : string.Empty;
        var typeName = lastDot >= 0 ? typeFullName[(lastDot + 1)..] : typeFullName;
        externalTypeRefs.Add(new ClrExternalTypeRef
        {
            assembly_name = assemblyName,
            type_full_name = typeFullName,
            type_namespace = typeNamespace,
            type_name = typeName
        });
        typeRefTokenMap[typeFullName] = typeRefTokenBase + typeRefIndex;
        typeRefIndex++;
    }

    /// <summary>
    ///     解析数组类型名中的元素类型。
    ///     约定使用 `"[i32]"` 这样的显示名承载 CLR 数组字面量的元素类型。
    /// </summary>
    private static GenerateTypeReference get_clr_array_element_type(string typeName)
    {
        if (typeName.Length >= 3 && typeName[0] == '[' && typeName[^1] == ']') return GenerateTypeReference.parse(typeName[1..^1]);

        if (string.Equals(typeName, "std.collection.Array", StringComparison.Ordinal)) return GenerateTypeReference.@object;

        return GenerateTypeReference.parse(typeName);
    }

    /// <summary>
    ///     将数组元素类型映射为 `newarr` 所需的 `TypeRef` token。
    /// </summary>
    private static uint? try_get_clr_array_element_type_token(
        GenerateTypeReference elementType,
        IReadOnlyDictionary<string, uint> typeRefTokenMap)
    {
        var clrTypeName = elementType.kind switch
        {
            GenerateTypeKind.i32 => "System.Int32",
            GenerateTypeKind.i64 => "System.Int64",
            GenerateTypeKind.f32 => "System.Single",
            GenerateTypeKind.f64 => "System.Double",
            GenerateTypeKind.@bool => "System.Boolean",
            GenerateTypeKind.@char => "System.Char",
            GenerateTypeKind.utf8 or GenerateTypeKind.utf16 => "System.String",
            _ => "System.Object"
        };

        return typeRefTokenMap.TryGetValue(clrTypeName, out var token) ? token : null;
    }

    /// <summary>
    ///     注册 CLR 后端内部会直接依赖的外部方法引用。
    ///     这些绑定不一定总能从前端切片里保留下来，因此在后端补齐。
    /// </summary>
    private static void register_builtin_external_method_refs(
        GenerateModule module,
        List<ClrExternalMethodRef> externalRefs,
        Dictionary<string, uint> externalRefTokenMap,
        HashSet<string> externalCtorFunctionNames,
        ref int memberRefIndex)
    {
        if (!module.functions.SelectMany(function => function.instructions)
                .Any(instruction => instruction.opcode == NyarHeadCode.array_push))
            return;

        add_builtin_external_method_ref(
            externalRefs,
            externalRefTokenMap,
            "std.collection.__array_list_clr_new",
            "mscorlib",
            "System.Collections.ArrayList",
            ".ctor",
            [0x20, 0x01, 0x01, 0x08],
            ref memberRefIndex,
            externalCtorFunctionNames);
        add_builtin_external_method_ref(
            externalRefs,
            externalRefTokenMap,
            "__array_list_clr_add",
            "mscorlib",
            "System.Collections.ArrayList",
            "Add",
            [0x20, 0x01, 0x08, 0x1C],
            ref memberRefIndex);
    }

    private static void add_builtin_external_method_ref(
        List<ClrExternalMethodRef> externalRefs,
        Dictionary<string, uint> externalRefTokenMap,
        string functionName,
        string assemblyName,
        string typeFullName,
        string methodName,
        byte[] methodSignature,
        ref int memberRefIndex,
        HashSet<string>? externalCtorFunctionNames = null)
    {
        if (externalRefTokenMap.ContainsKey(functionName)) return;

        var lastDot = typeFullName.LastIndexOf('.');
        var typeNamespace = lastDot >= 0 ? typeFullName[..lastDot] : string.Empty;
        var shortTypeName = lastDot >= 0 ? typeFullName[(lastDot + 1)..] : typeFullName;
        externalRefs.Add(new ClrExternalMethodRef
        {
            assembly_name = assemblyName,
            type_full_name = typeFullName,
            type_namespace = typeNamespace,
            type_name = shortTypeName,
            method_name = methodName,
            method_signature = methodSignature
        });
        var token = 0x0A000001u + (uint)memberRefIndex;
        externalRefTokenMap[functionName] = token;
        var shortName = get_short_function_name(functionName);
        if (!string.Equals(shortName, functionName, StringComparison.Ordinal)) externalRefTokenMap.TryAdd(shortName, token);

        if (string.Equals(methodName, ".ctor", StringComparison.Ordinal))
        {
            externalCtorFunctionNames?.Add(functionName);
            externalCtorFunctionNames?.Add(shortName);
        }

        memberRefIndex++;
    }

    /// <summary>
    ///     扫描所有函数，收集被数组相关操作使用的字段。
    ///     返回值是 `类型名.字段名` 形式的限定 key 集合，用于区分 `[]` 空数组和 `null`。
    ///     传入的 `fieldTokenMap` 由 `collect_oop_references()` 预填充，用于稳定解析字段所属类型。
    /// </summary>
    private static HashSet<string> collect_array_field_names(
        GenerateModule module,
        IDictionary<string, uint> fieldTokenMap)
    {
        var arrayFieldKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var function in module.functions)
        {
            var valueTypeStack = new Stack<GenerateTypeReference>();
            for (var i = 0; i < function.instructions.Count; i++)
            {
                var instruction = function.instructions[i];
                // 跟踪 `load_local` 的类型，供后续 `get_field` 推断对象类型。
                if (instruction is { opcode: NyarHeadCode.load_local, operands.Count: > 0 } &&
                    instruction.operands[0] is GenerateOperand.Local localOp)
                {
                    var localVar = function.local_variables.FirstOrDefault(l => l.index == localOp.index);
                    if (localVar is { type_ref.is_empty: false }) valueTypeStack.Push(localVar.type_ref);
                }

                // 检测 `get_field` 后紧跟数组相关操作的模式。
                if (instruction.opcode == NyarHeadCode.get_field && i + 1 < function.instructions.Count)
                {
                    var nextOp = function.instructions[i + 1].opcode;
                    if (nextOp is NyarHeadCode.array_push or NyarHeadCode.array_get or NyarHeadCode.array_set
                        or NyarHeadCode.get_offset_index or NyarHeadCode.set_offset_index or NyarHeadCode.length)
                    {
                        var fieldName = find_field_name_backward(function, i);
                        if (fieldName != null)
                        {
                            var currentType = valueTypeStack.Count > 0
                                ? valueTypeStack.Peek().display_name
                                : null;
                            var qualifiedKey = build_field_qualified_key(currentType, fieldName, fieldTokenMap, function.name);
                            arrayFieldKeys.Add(qualifiedKey);
                        }
                    }
                }

                // 更新 `valueTypeStack`，保持与栈上对象类型同步。
                if (instruction.opcode == NyarHeadCode.get_field)
                {
                    if (valueTypeStack.Count > 0) valueTypeStack.Pop(); // 弹出对象引用。
                }
                else if (instruction.opcode == NyarHeadCode.set_field)
                {
                    if (valueTypeStack.Count > 0) valueTypeStack.Pop();
                    if (valueTypeStack.Count > 0) valueTypeStack.Pop();
                }
                else if (instruction.opcode == NyarHeadCode.pop)
                {
                    if (valueTypeStack.Count > 0) valueTypeStack.Pop();
                }
            }
        }

        return arrayFieldKeys;
    }

    /// <summary>
    ///     扫描所有函数中的 OOP 指令（`new_object/get_field/set_field`）。
    ///     这里会收集类型引用、构造器占位 token，以及每个类型的字段列表。
    ///     返回值用于后续生成 `ClrTypeDef`。
    /// </summary>
    private static Dictionary<string, List<string>> collect_oop_references(
        GenerateModule module,
        List<ClrExternalMethodRef> externalRefs,
        Dictionary<string, uint> newObjectTokenMap,
        Dictionary<string, uint> fieldTokenMap,
        ref int memberRefIndex
    )
    {
        var typeFields = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var function in module.functions)
        {
            var typeContextStack = new Stack<(string typeName, int remainingFields)>();
            var valueTypeStack = new Stack<GenerateTypeReference>();
            for (var i = 0; i < function.instructions.Count; i++)
            {
                var instruction = function.instructions[i];
                switch (instruction.opcode)
                {
                    case NyarHeadCode.new_object:
                    {
                        if (instruction.operands.Count < 2 ||
                            instruction.operands[0] is not GenerateOperand.Str typeNameOperand ||
                            instruction.operands[1] is not GenerateOperand.I32 fieldCountOperand)
                            continue;
                        var typeName = typeNameOperand.value;
                        var fieldCount = fieldCountOperand.value;
                        // 判断当前 `new_object` 是否只是某个字段赋值表达式中的值。
                        var isFieldValue = i > 0 &&
                                           function.instructions[i - 1].opcode == NyarHeadCode.@const &&
                                           function.instructions[i - 1].operands.Count > 0 &&
                                           function.instructions[i - 1].operands[0] is GenerateOperand.Str;
                        if (is_builtin_type_alias(typeName))
                            // 内建类型别名不入栈，避免后续 `set_field` 误推断字段宿主。
                            break;
                        if (!isFieldValue) typeContextStack.Push((typeName, fieldCount));
                        if (!typeFields.ContainsKey(typeName)) typeFields[typeName] = [];
                        if (!newObjectTokenMap.TryAdd(typeName, 0xFFFFFFFF))
                            continue;
                        // 本地类型的 `.ctor` 不走 `MemberRef`，稍后会替换成 `MethodDef` token。
                        // 这里只登记占位 token，避免污染 `externalRefs` 并打乱后续 token 映射。
                        // 注意：这里不会递增 `memberRefIndex`，也不会追加外部引用项。
                        break;
                    }
                    case NyarHeadCode.load_local:
                    {
                        if (instruction.operands.Count > 0 &&
                            instruction.operands[0] is GenerateOperand.Local localOp)
                        {
                            var localVar = function.local_variables.FirstOrDefault(l => l.index == localOp.index);
                            if (localVar is { type_ref.is_empty: false })
                                valueTypeStack.Push(localVar.type_ref);
                        }

                        break;
                    }
                    case NyarHeadCode.load_arg:
                    {
                        if (instruction.operands.Count > 0 &&
                            instruction.operands[0] is GenerateOperand.Param paramOp)
                        {
                            var paramIndex = paramOp.index;
                            var signatureType = paramIndex < function.parameters.Count
                                ? function.parameters[paramIndex].type_ref
                                : null;
                            if (signatureType is { is_empty: false, is_erased_object_type: false })
                                valueTypeStack.Push(signatureType);
                            else
                                valueTypeStack.Push(GenerateTypeReference.from_value_type(paramOp.type));
                        }

                        break;
                    }
                    case NyarHeadCode.get_field:
                    case NyarHeadCode.set_field:
                    {
                        // 复用发射阶段一致的反向扫描逻辑提取字段名，
                        // 正确处理以下模式：
                        // A: `const Str fieldName, set_field`
                        // A'：`const Str fieldName, const Str value, set_field`
                        // B: `const Str fieldName, <value>, set_field`
                        var fieldName = find_field_name_backward(function, i);
                        if (fieldName != null)
                        {
                            var currentType = typeContextStack.Count > 0
                                ? typeContextStack.Peek().typeName
                                : valueTypeStack.Count > 0
                                    ? valueTypeStack.Peek().display_name
                                    : null;
                            var qualifiedKey = build_field_qualified_key(currentType, fieldName, fieldTokenMap, function.name);
                            var resolvedType = qualifiedKey.Contains('.')
                                ? qualifiedKey[..qualifiedKey.LastIndexOf('.')]
                                : null;
                            if (resolvedType != null && !is_builtin_type_alias(resolvedType))
                            {
                                if (!typeFields.ContainsKey(resolvedType)) typeFields[resolvedType] = [];
                                if (!typeFields[resolvedType].Contains(fieldName))
                                    typeFields[resolvedType].Add(fieldName);
                                // 字段最终由 `ClrFieldDef` 定义，因此这里使用 `FieldDef` token。
                                if (fieldTokenMap.TryAdd(qualifiedKey, 0))
                                {
                                    // 先登记占位 token，稍后在 `build_clr_module()` 中替换成真实 `FieldDef` token。
                                }
                            }

                            if (instruction.opcode == NyarHeadCode.set_field && typeContextStack.Count > 0)
                            {
                                var (typeName, remaining) = typeContextStack.Pop();
                                remaining--;
                                if (remaining > 0) typeContextStack.Push((typeName, remaining));
                            }

                            // 更新 `valueTypeStack`：`set_field` 会弹出值和对象，`get_field` 会弹出对象。
                            if (instruction.opcode == NyarHeadCode.set_field)
                            {
                                if (valueTypeStack.Count > 0) valueTypeStack.Pop(); // value
                                if (valueTypeStack.Count > 0) valueTypeStack.Pop(); // object
                            }
                            else
                            {
                                if (valueTypeStack.Count > 0) valueTypeStack.Pop(); // object
                            }
                        }

                        break;
                    }
                }
            }
        }

        return typeFields;
    }

    /// <summary>
    ///     发射 `new_object` 指令，生成对应的 CLR `newobj`。
    ///     当前统一使用 `System.Object::.ctor()` 作为用户类型构造入口。
    ///     数组字面量会特殊映射为 CLR `newarr`。
    ///     其他内建类型别名直接降级为 `ldnull`，避免生成与签名不匹配的伪构造调用。
    /// </summary>
    private static void emit_new_object(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        IReadOnlyDictionary<string, uint> newObjectTokenMap,
        IReadOnlyDictionary<string, uint> typeRefTokenMap)
    {
        if (instruction.operands.Count < 1 ||
            instruction.operands[0] is not GenerateOperand.Str typeNameOperand)
            return;
        var typeName = typeNameOperand.value;
        if (string.Equals(typeName, "std.collection.Array", StringComparison.Ordinal) ||
            (typeName.Length >= 3 && typeName[0] == '[' && typeName[^1] == ']'))
        {
            var elementType = get_clr_array_element_type(typeName);
            var elementToken = try_get_clr_array_element_type_token(elementType, typeRefTokenMap);
            if (elementToken.HasValue)
            {
                output.Add(new ClrInstruction
                {
                    opcode = ClrOpcode.newarr,
                    operand = new ClrTokenOperand { value = elementToken.Value }
                });
            }
            else
            {
                output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
            }

            return;
        }

        // 内建类型别名直接压入 `ldnull`，不生成 `newobj`。
        // 这些类型在方法签名中会映射到 `ELEMENT_TYPE_OBJECT (0x1C)`，
        // 如果继续构造伪 `TypeDef`，容易触发 `ILVerify` 类型推断错误。
        if (is_builtin_type_alias(typeName))
        {
            output.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
            return;
        }

        if (newObjectTokenMap.TryGetValue(typeName, out var token))
            output.Add(new ClrInstruction
            {
                opcode = ClrOpcode.newobj,
                operand = new ClrTokenOperand { value = token }
            });
        else
            output.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
    }

    /// <summary>
    ///     发射 `get_field` 的保底实现。
    ///     当前先弹出对象和字段名，再压入 `null` 占位。
    /// </summary>
    private static void emit_get_field(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        IDictionary<string, uint> fieldTokenMap)
    {
        output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
        output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
        output.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
    }

    /// <summary>
    ///     发射 `set_field` 的保底实现。
    ///     当前直接弹出对象、字段名和值，保持栈平衡。
    /// </summary>
    private static void emit_set_field(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        IDictionary<string, uint> fieldTokenMap)
    {
        output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
        output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
        output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
    }

    /// <summary>
    ///     发射 `stfld` 指令，把值写入对象字段。
    ///     该方法由 `build_method()` 在识别到 `const Str + set_field` 模式时调用。
    ///     如果字段 token 尚未注册，这里会动态分配一个占位 token。
    /// </summary>
    private static void emit_set_field_stfld(
        ICollection<ClrInstruction> output,
        string fieldName,
        IDictionary<string, uint> fieldTokenMap,
        string functionName = "")
    {
        if (!fieldTokenMap.TryGetValue(fieldName, out var token))
        {
            token = 0x04000001u + (uint)fieldTokenMap.Count;
            fieldTokenMap[fieldName] = token;
        }

        output.Add(new ClrInstruction
        {
            opcode = ClrOpcode.stfld,
            operand = new ClrTokenOperand { value = token }
        });
    }

    /// <summary>
    ///     发射 `ldfld` 指令，从对象字段读取值。
    ///     该方法由 `build_method()` 在识别到 `const Str + get_field` 模式时调用。
    ///     如果字段 token 尚未注册，这里会动态分配一个占位 token。
    /// </summary>
    private static void emit_get_field_ldfld(
        ICollection<ClrInstruction> output,
        string fieldName,
        IDictionary<string, uint> fieldTokenMap,
        string functionName = "")
    {
        if (!fieldTokenMap.TryGetValue(fieldName, out var token))
        {
            token = 0x04000001u + (uint)fieldTokenMap.Count;
            fieldTokenMap[fieldName] = token;
        }

        output.Add(new ClrInstruction
        {
            opcode = ClrOpcode.ldfld,
            operand = new ClrTokenOperand { value = token }
        });
    }

    /// <summary>
    ///     发射 `call_dynamic` 的保底实现。
    ///     当前 CLR 后端还不支持真正的动态分派，因此这里只移除 `self` 参数占位。
    /// </summary>
    private static void emit_call_dynamic(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        IReadOnlyDictionary<string, uint> methodTokenMap)
    {
        // 弹出 `self` 参数占位。
        output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
    }

    private static bool try_get_external_ref_token(
        IReadOnlyDictionary<string, uint> externalRefTokenMap,
        string functionName,
        out uint token)
    {
        if (externalRefTokenMap.TryGetValue(functionName, out token)) return true;
        return externalRefTokenMap.TryGetValue(get_short_function_name(functionName), out token);
    }

    private static void emit_explicit_external_call(
        ICollection<ClrInstruction> output,
        IReadOnlyDictionary<string, uint> externalRefTokenMap,
        string functionName,
        ClrOpcode opcode,
        int fallbackPopCount,
        GenerateValueType fallbackResultType)
    {
        if (try_get_external_ref_token(externalRefTokenMap, functionName, out var token))
        {
            output.Add(new ClrInstruction
            {
                opcode = opcode,
                operand = new ClrTokenOperand { value = token }
            });
            return;
        }

        for (var i = 0; i < fallbackPopCount; i++) output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
        emit_default_value(output, fallbackResultType);
    }

    /// <summary>
    ///     发射 `load_global` 指令，生成 CLR `ldsfld`，从静态字段读取全局变量。
    ///     全局变量名从指令操作数中的字符串引用提取。
    /// </summary>
    private static void emit_load_global(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        GenerateModule module,
        IDictionary<string, uint> fieldTokenMap)
    {
        var globalName = extract_global_name(instruction, module);
        if (globalName == null)
        {
            output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
            return;
        }

        if (!fieldTokenMap.TryGetValue(globalName, out var token))
        {
            token = 0x04000001u + (uint)fieldTokenMap.Count;
            fieldTokenMap[globalName] = token;
        }

        output.Add(new ClrInstruction
        {
            opcode = ClrOpcode.ldsfld,
            operand = new ClrTokenOperand { value = token }
        });
    }

    /// <summary>
    ///     发射 `store_global` 指令，生成 CLR `stsfld`，把值写回静态字段。
    ///     全局变量名从指令操作数中的字符串引用提取。
    /// </summary>
    private static void emit_store_global(
        ICollection<ClrInstruction> output,
        GenerateInstruction instruction,
        GenerateModule module,
        IDictionary<string, uint> fieldTokenMap)
    {
        var globalName = extract_global_name(instruction, module);
        if (globalName == null)
        {
            output.Add(new ClrInstruction { opcode = ClrOpcode.pop });
            return;
        }

        if (!fieldTokenMap.TryGetValue(globalName, out var token))
        {
            token = 0x04000001u + (uint)fieldTokenMap.Count;
            fieldTokenMap[globalName] = token;
        }

        output.Add(new ClrInstruction
        {
            opcode = ClrOpcode.stsfld,
            operand = new ClrTokenOperand { value = token }
        });
    }

    /// <summary>
    ///     从指令操作数中提取全局变量名。
    ///     同时支持 `GenerateOperand.Const(utf8)` 和 `GenerateOperand.Str` 两种格式。
    /// </summary>
    private static string? extract_global_name(GenerateInstruction instruction, GenerateModule module)
    {
        if (instruction.operands.Count == 0) return null;
        return instruction.operands[0] switch
        {
            GenerateOperand.Str str => str.value,
            GenerateOperand.Const { type: GenerateValueType.utf8 } c =>
                c.pool_index < module.constants.strings.Count
                    ? module.constants.strings[c.pool_index]
                    : null,
            _ => null
        };
    }

    private static void emit_default_return(ICollection<ClrInstruction> output, GenerateTypeReference returnType)
    {
        if (!returnType.is_void_like)
        {
            var valueType = returnType.value_type switch
            {
                GenerateValueType.i64 => GenerateValueType.i64,
                GenerateValueType.f32 => GenerateValueType.f32,
                GenerateValueType.f64 => GenerateValueType.f64,
                GenerateValueType.utf8 => GenerateValueType.utf8,
                GenerateValueType.utf16 => GenerateValueType.utf16,
                GenerateValueType.@bool => GenerateValueType.@bool,
                _ => GenerateValueType.i32
            };
            emit_default_value(output, valueType);
        }

        output.Add(new ClrInstruction { opcode = ClrOpcode.ret });
    }

    private static void emit_default_value(ICollection<ClrInstruction> output, GenerateValueType valueType)
    {
        switch (valueType)
        {
            case GenerateValueType.unit:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                return;
            case GenerateValueType.i64:
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldc_i8, operand = new ClrInt64Operand { value = 0 } });
                return;
            case GenerateValueType.f32:
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldc_r4, operand = new ClrFloat32Operand { value = 0f } });
                return;
            case GenerateValueType.f64:
                output.Add(new ClrInstruction
                    { opcode = ClrOpcode.ldc_r8, operand = new ClrFloat64Operand { value = 0d } });
                return;
            case GenerateValueType.utf8:
            case GenerateValueType.@object:
            case GenerateValueType.any:
            case GenerateValueType.external_ref:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldnull });
                return;
            default:
                output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
                return;
        }
    }

    private static void emit_synthetic_unit_result(ICollection<ClrInstruction> output, GenerateFunctionType signature)
    {
        if (signature.results.Count > 0 && signature.results[0] == GenerateValueType.unit)
            output.Add(new ClrInstruction { opcode = ClrOpcode.ldc_i4_0 });
    }
}
