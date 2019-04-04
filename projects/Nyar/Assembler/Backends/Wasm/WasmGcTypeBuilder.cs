using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     WASM GC 类型构建器，扫描模块中的 new_object 指令并生成对应的 GC 结构体类型定义。
/// </summary>
/// <remarks>
///     完整实现（替代旧版 Phase 1/2 临时方案）：
///     - 扫描 new_object 指令收集类型定义
///     - 从 new_object 操作数中提取字段类型注解（如果 LIR 已携带）
///     - 回退：从 set_field 值指令推断字段类型
///     - 所有字段默认为 eq_ref（GC 引用），而非 i32
///     - 类型索引 = existingFuncTypes.Count + gcSubTypes 中的位置
/// </remarks>
internal static class WasmGcTypeBuilder
{
    /// <summary>
    ///     扫描模块中所有函数的 new_object 和 set_field/get_field 指令，
    ///     构建 GC 子类型列表、类型名到索引的映射、字段名到索引的映射。
    /// </summary>
    /// <param name="module">元编译模块。</param>
    /// <param name="existingFuncTypes">已有的函数类型列表，用于计算 GC 类型的起始 typeidx。</param>
    /// <returns>
    ///     返回元组：
    ///     gc_sub_types 为 GC 子类型列表；
    ///     type_name_to_index 为类型名到 typeidx 的映射字典；
    ///     field_name_to_index 为全局字段名到字段索引的映射字典。
    ///     typeidx 计算规则为 existingFuncTypes.Count + 在 gc_sub_types 中的位置。
    /// </returns>
    public static (
        IReadOnlyList<WasmSubType> gc_sub_types,
        Dictionary<string, uint> type_name_to_index,
        Dictionary<string, uint> field_name_to_index
        ) build(GenerateModule module, IReadOnlyList<WasmFunctionType> existingFuncTypes)
    {
        var typeDefs = collect_type_definitions(module);
        var fieldNameToIndex = collect_field_names(module);

        if (typeDefs.Count == 0) return ([], [], []);

        var gcSubTypes = new List<WasmSubType>(typeDefs.Count);
        var typeNameToIndex = new Dictionary<string, uint>(StringComparer.Ordinal);
        var baseIndex = (uint)existingFuncTypes.Count;

        foreach (var typeDef in typeDefs)
        {
            var fields = build_struct_fields(typeDef);
            var subType = new WasmSubType
            {
                final = true,
                super_type_index = null,
                type = new WasmCompositeType
                {
                    kind = WasmCompositeTypeKind.@struct,
                    fields = fields
                }
            };

            var typeIndex = baseIndex + (uint)gcSubTypes.Count;
            gcSubTypes.Add(subType);
            typeNameToIndex[typeDef.type_name] = typeIndex;
        }

        if (!typeNameToIndex.ContainsKey("std.collection.Array"))
        {
            var arraySubType = new WasmSubType
            {
                final = true,
                super_type_index = null,
                type = new WasmCompositeType
                {
                    kind = WasmCompositeTypeKind.array,
                    element_type = new WasmStorageType
                    {
                        packed_type = null,
                        value_type = WasmValueType.eq_ref
                    }
                }
            };

            var typeIndex = baseIndex + (uint)gcSubTypes.Count;
            gcSubTypes.Add(arraySubType);
            typeNameToIndex["std.collection.Array"] = typeIndex;
        }

        return (gcSubTypes, typeNameToIndex, fieldNameToIndex);
    }

    /// <summary>
    ///     扫描模块中所有函数，收集每个唯一类型名及其字段类型。
    ///     字段类型来源优先级：
    ///     1. new_object 指令中嵌入的字段类型注解（operands[2..]）
    ///     2. 分析 set_field 值指令推断类型
    ///     3. 回退：eq_ref
    /// </summary>
    private static List<TypeDefinition> collect_type_definitions(GenerateModule module)
    {
        var typeFieldCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var typeFieldTypes = new Dictionary<string, List<WasmValueType>>(StringComparer.Ordinal);
        var orderedTypeNames = new List<string>();

        foreach (var function in module.functions)
        {
            string? currentTypeName = null;

            foreach (var instruction in function.instructions)
            {
                if (instruction.head_code == NyarHeadCode.new_object)
                {
                    if (instruction.operands.Count < 2) continue;
                    if (instruction.operands[0] is not GenerateOperand.Str typeNameOp) continue;
                    if (instruction.operands[1] is not GenerateOperand.I32 fieldCountOp) continue;

                    var typeName = typeNameOp.value;
                    var fieldCount = fieldCountOp.value;
                    currentTypeName = typeName;

                    // 只在首次遇到此类型名时记录
                    if (!typeFieldCounts.TryAdd(typeName, fieldCount)) continue;

                    orderedTypeNames.Add(typeName);

                    var fieldTypes = new List<WasmValueType>(fieldCount);

                    // 优先级 1: 检查 new_object 是否携带字段类型注解 (operands[2..])
                    if (instruction.operands.Count >= 2 + fieldCount)
                    {
                        var hasTypeAnnotations = true;
                        for (var j = 0; j < fieldCount; j++)
                            if (instruction.operands[2 + j] is GenerateOperand.Str fieldTypeOp)
                            {
                                fieldTypes.Add(map_type_name_to_wasm_value_type(fieldTypeOp.value));
                            }
                            else
                            {
                                hasTypeAnnotations = false;
                                break;
                            }

                        if (hasTypeAnnotations)
                        {
                            typeFieldTypes[typeName] = fieldTypes;
                            continue;
                        }

                        fieldTypes.Clear(); // 重置，走回退路径
                    }

                    // 暂不填充 fieldTypes — 等待 set_field 指令收集
                    typeFieldTypes[typeName] = fieldTypes;
                    continue;
                }

                if (instruction.head_code == NyarHeadCode.set_field && currentTypeName != null)
                {
                    // 优先级 2: 从 set_field 值指令推断字段类型
                    if (!typeFieldTypes.TryGetValue(currentTypeName, out var fieldTypes))
                    {
                        fieldTypes = [];
                        typeFieldTypes[currentTypeName] = fieldTypes;
                    }

                    // 如果字段类型数已满，跳过
                    if (typeFieldCounts.TryGetValue(currentTypeName, out var expectedCount) &&
                        fieldTypes.Count >= expectedCount)
                        continue;

                    var fieldType = infer_field_value_type(function.instructions, instruction, function);
                    fieldTypes.Add(fieldType);
                }
            }
        }

        // 构建结果列表
        var result = new List<TypeDefinition>(orderedTypeNames.Count);
        foreach (var typeName in orderedTypeNames)
        {
            var fieldCount = typeFieldCounts[typeName];
            var fieldTypes = typeFieldTypes.GetValueOrDefault(typeName) ?? new List<WasmValueType>(fieldCount);

            // 确保 fieldTypes 长度与 fieldCount 一致，不足部分用 eq_ref 填充
            while (fieldTypes.Count < fieldCount) fieldTypes.Add(WasmValueType.eq_ref);

            result.Add(new TypeDefinition(typeName, fieldCount, fieldTypes.AsReadOnly()));
        }

        return result;
    }

    /// <summary>
    ///     从 set_field 指令前的指令流推断字段值的 WASM 类型。
    ///     指令模式: ... [value instructions] @const "fieldName" set_field
    ///     <paramref name="setFieldInst" /> 是 set_field 指令本身，
    ///     <paramref name="function" /> 是所属函数（用于局部变量/参数类型查找）。
    /// </summary>
    private static WasmValueType infer_field_value_type(
        IReadOnlyList<GenerateInstruction> instructions,
        GenerateInstruction setFieldInst,
        GenerateFunction function)
    {
        var setFieldIdx = -1;
        for (var idx = 0; idx < instructions.Count; idx++)
            if (ReferenceEquals(instructions[idx], setFieldInst))
            {
                setFieldIdx = idx;
                break;
            }

        if (setFieldIdx <= 0) return WasmValueType.eq_ref;

        // 向前扫描：跳过 @const "fieldName"，找到值指令
        // 值指令在 @const "fieldName" 之前（如果 @const 存在），
        // 或在 set_field 之前（如果没有 @const）
        for (var i = setFieldIdx - 1; i >= 0; i--)
        {
            var inst = instructions[i];

            // 跳过 @const Str（字段名），它的类型是 utf8，不是字段值
            if (inst.head_code == NyarHeadCode.@const &&
                inst.operands.FirstOrDefault() is GenerateOperand.Str)
                continue;

            // 跳过 dup（它复制对象引用，不是字段值）
            if (inst.head_code == NyarHeadCode.dup) continue;

            // 这条指令产生字段值 — 分析它的类型
            return infer_instruction_result_type(inst, function, instructions, i);
        }

        return WasmValueType.eq_ref;
    }

    /// <summary>
    ///     分析指令产生的结果类型。
    /// </summary>
    private static WasmValueType infer_instruction_result_type(
        GenerateInstruction inst,
        GenerateFunction function,
        IReadOnlyList<GenerateInstruction> allInstructions,
        int index)
    {
        switch (inst.head_code)
        {
            case NyarHeadCode.@const:
            {
                var operand = inst.operands.FirstOrDefault();
                return operand switch
                {
                    GenerateOperand.I32 => WasmValueType.int32,
                    GenerateOperand.I64 => WasmValueType.int64,
                    GenerateOperand.F32 => WasmValueType.float32,
                    GenerateOperand.F64 => WasmValueType.float64,
                    GenerateOperand.Str => WasmValueType.eq_ref, // 字符串常量 → eq_ref
                    _ => WasmValueType.eq_ref
                };
            }

            case NyarHeadCode.new_object:
                return WasmValueType.eq_ref;

            case NyarHeadCode.load_local:
            {
                if (inst.operands.FirstOrDefault() is GenerateOperand.Local localOp)
                {
                    var localVar = function.local_variables
                        .FirstOrDefault(lv => lv.index == localOp.index);
                    if (localVar != null)
                        return map_type_name_to_wasm_value_type(localVar.type_ref);
                }

                return WasmValueType.eq_ref;
            }

            case NyarHeadCode.load_arg:
            {
                if (inst.operands.FirstOrDefault() is GenerateOperand.Param paramOp)
                    if (paramOp.index < function.parameters.Count)
                        return map_type_name_to_wasm_value_type(function.parameters[paramOp.index].type_ref);
                return WasmValueType.eq_ref;
            }

            case NyarHeadCode.call_static:
            {
                // 对于函数调用，使用返回类型（如可用）
                // call_static 操作数: FuncRef(name, signature) 或 Str(name)
                if (inst.operands.FirstOrDefault() is GenerateOperand.FuncRef { signature.results.Count: > 0 } funcRef)
                    return map_type_name_to_wasm_value_type(funcRef.signature.results[0]);

                // 回退：在模块中查找同名函数
                return WasmValueType.eq_ref;
            }

            // 二元运算产生 i32（比较运算）
            case NyarHeadCode.i32_eq:
            case NyarHeadCode.i32_ne:
            case NyarHeadCode.i32_lt_s:
            case NyarHeadCode.i32_le_s:
            case NyarHeadCode.i32_gt_s:
            case NyarHeadCode.i32_ge_s:
                return WasmValueType.int32;

            // 默认：保守回退到 eq_ref
            default:
                return WasmValueType.eq_ref;
        }
    }

    /// <summary>
    ///     将 Valkyrie 代码生成类型名映射为 WASM 值类型。
    ///     用于 GC 结构体字段的类型映射。
    /// </summary>
    private static WasmValueType map_type_name_to_wasm_value_type(string typeName)
    {
        return map_type_name_to_wasm_value_type(GenerateTypeReference.parse(typeName));
    }

    private static WasmValueType map_type_name_to_wasm_value_type(GenerateValueType typeName)
    {
        return map_type_name_to_wasm_value_type(GenerateTypeReference.from_value_type(typeName));
    }

    private static WasmValueType map_type_name_to_wasm_value_type(GenerateTypeReference typeName)
    {
        return typeName.kind switch
        {
            GenerateTypeKind.unit or GenerateTypeKind.@bool or GenerateTypeKind.i8 or GenerateTypeKind.i16
                or GenerateTypeKind.i32 => WasmValueType.int32,
            GenerateTypeKind.i64 => WasmValueType.int64,
            GenerateTypeKind.f32 => WasmValueType.float32,
            GenerateTypeKind.f64 => WasmValueType.float64,
            GenerateTypeKind.v128 => WasmValueType.v128,
            GenerateTypeKind.function_ref => WasmValueType.func_ref,
            GenerateTypeKind.external_ref => WasmValueType.extern_ref,
            GenerateTypeKind.utf8 or GenerateTypeKind.utf16 => WasmValueType.eq_ref,
            GenerateTypeKind.@object or GenerateTypeKind.any => WasmValueType.eq_ref,
            _ => WasmValueType.eq_ref // 未知类型默认为 eq_ref（GC 引用）
        };
    }

    /// <summary>
    ///     构建结构体字段列表，使用推断的字段类型。
    ///     所有字段当前为 mutable，待后续阶段支持 readonly 字段。
    /// </summary>
    private static List<WasmFieldType> build_struct_fields(TypeDefinition typeDef)
    {
        var fields = new List<WasmFieldType>(typeDef.field_count);
        for (var i = 0; i < typeDef.field_count; i++)
        {
            var valueType = i < typeDef.field_types.Count
                ? typeDef.field_types[i]
                : WasmValueType.eq_ref;

            fields.Add(new WasmFieldType
            {
                storage_type = new WasmStorageType { value_type = valueType },
                mutable = true
            });
        }

        return fields;
    }

    /// <summary>
    ///     扫描模块中所有函数，收集 set_field/get_field 指令中使用的字段名，
    ///     为每个首次出现的字段名分配一个递增的索引。
    ///     用于 WASM GC struct.get/struct.set 指令的字段索引立即数。
    /// </summary>
    /// <remarks>
    ///     由于 LIR 中 get_field/set_field 不携带类型信息，只能通过栈上的对象引用隐式关联类型。
    ///     我们使用全局字段名到索引映射，假设字段名在不同类型间不会冲突。
    ///     字段索引按照首次遇到的顺序分配：第一个遇到的字段名为索引 0，第二个为 1，以此类推。
    /// </remarks>
    private static Dictionary<string, uint> collect_field_names(GenerateModule module)
    {
        var fieldNameToIndex = new Dictionary<string, uint>(StringComparer.Ordinal);
        var nextIndex = 0u;

        foreach (var function in module.functions)
        {
            var instructions = function.instructions;
            for (var i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                if (instruction.head_code != NyarHeadCode.set_field &&
                    instruction.head_code != NyarHeadCode.get_field)
                    continue;

                var fieldName = find_preceding_field_name(instructions, i);
                if (fieldName is null) continue;

                if (!fieldNameToIndex.ContainsKey(fieldName)) fieldNameToIndex[fieldName] = nextIndex++;
            }
        }

        return fieldNameToIndex;
    }

    /// <summary>
    ///     从给定指令索引向前查找最近的 @const "fieldName" 指令，提取字段名。
    /// </summary>
    private static string? find_preceding_field_name(
        IReadOnlyList<GenerateInstruction> instructions, int currentIndex)
    {
        for (var i = currentIndex - 1; i >= 0; i--)
        {
            var prevInstruction = instructions[i];

            if (prevInstruction.head_code == NyarHeadCode.new_object ||
                prevInstruction.head_code == NyarHeadCode.call ||
                prevInstruction.head_code == NyarHeadCode.call_static)
                break;

            if (prevInstruction.head_code == NyarHeadCode.@const)
                if (prevInstruction.operands.FirstOrDefault() is GenerateOperand.Str strOp)
                    return strOp.value;
        }

        return null;
    }

    /// <summary>
    ///     从 new_object / set_field 指令流中提取的类型定义。
    /// </summary>
    private sealed record TypeDefinition(
        string type_name,
        int field_count,
        IReadOnlyList<WasmValueType> field_types);
}