using System.Collections.Immutable;
using Nyar.Assembler;
using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Rules;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Mir;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     承载调用发射与字段索引访问的 partial 类定义。
/// </summary>
public sealed partial class LirBuilder
{
    private static GenerateValueType emit_apply(Apply apply, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        // 先检查被调用者是否为 CoreBuiltin 内置函数字面量（效应降级后的 VmPerform/VmHandle）。
        var calleeNode = resolve_node(context.graph, apply.function);
        if (calleeNode is Literal<long> builtinLiteral &&
            is_standard_effect_builtin(builtinLiteral.value))
        {
            var builtin = (CoreBuiltin)builtinLiteral.value;
            return emit_standard_effect_builtin(builtin, apply.arguments, context, expectedType);
        }

        // 解析被调用函数名
        var functionName = calleeNode switch
        {
            Sym calleeSymbol => calleeSymbol.name,
            AlgebraNode.Symbol symbol => symbol.name,
            _ => "unknown"
        };

        // 语义层不应把 operator 概念泄漏到 LIR。
        // 但当前部分类型尚未提供完整的 infix imply 实现（如 utf8 的 ==、!=），
        // HIR 解糖后这些运算符无法解析方法分派，需要在此处兜底降级为对应指令。
        // 支持多种函数名形式：`infix OP`、`module.infix OP`、`module.__operator_infix__uXXXX`
        var extractedInfix = try_extract_infix_operator(functionName);
        if (extractedInfix is not null)
        {
            return emit_unresolved_infix(extractedInfix, apply.arguments, context);
        }

        for (var i = 0; i < apply.arguments.Length; i++)
        {
            var argumentId = apply.arguments[i];
            lower_expression(argumentId, context, null);
        }

        if (try_emit_intrinsic_call(functionName, apply.arguments.Length, context, expectedType,
                out var intrinsicType))
        {
            return intrinsicType;
        }

        var signature = build_static_call_signature(functionName, apply.arguments.Length, context, expectedType);

        context.instructions.Add(new GenerateInstruction(NyarHeadCode.call_static,
            new GenerateOperand.FuncRef(functionName, signature)));
        return signature.results.Count > 0 ? signature.results[0] : GenerateValueType.@void;
    }

    /// <summary>
    ///     发射 NewObject 指令，创建对象实例。
    /// </summary>
    private static GenerateValueType emit_new_object(AlgebraNode.NewObject newObj, FunctionLoweringContext context)
    {
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.new_object,
            new GenerateOperand.Str(newObj.type_name),
            new GenerateOperand.I32(newObj.field_count)));
        return GenerateValueType.@object;
    }

    private static GenerateValueType emit_array_literal(AlgebraNode.ArrayLiteral arrayLiteral,
        FunctionLoweringContext context)
    {
        var elements = arrayLiteral.elements.IsDefaultOrEmpty ? [] : arrayLiteral.elements;

        if (string.Equals(context.mir.target_backend, "wasm", StringComparison.Ordinal))
        {
            for (var i = 0; i < elements.Length; i++)
            {
                lower_expression(elements[i], context, null);
            }

            context.instructions.Add(new GenerateInstruction(NyarHeadCode.new_object,
                new GenerateOperand[]
                {
                    new GenerateOperand.Str("std.collection.Array"),
                    new GenerateOperand.I32(elements.Length)
                }));
            return GenerateValueType.@object;
        }

        if (is_clr_backend_target(context.mir.target_backend))
        {
            // CLR 数组字面量直接降级为固定长度数组创建，并保留元素类型信息，
            // 供后端选择 `newarr/stelem.*`。
            var elementType = infer_clr_array_literal_element_type(elements, context);
            var arrayTypeName = $"[{to_cg_type_name(elementType)}]";
            emit_const(context.instructions, new GenerateOperand.I32(elements.Length), GenerateValueType.i32);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.new_object,
                new GenerateOperand[]
                {
                    new GenerateOperand.Str(arrayTypeName),
                    new GenerateOperand.I32(elements.Length)
                }));
            for (var i = 0; i < elements.Length; i++)
            {
                context.instructions.Add(new GenerateInstruction(NyarHeadCode.dup));
                emit_const(context.instructions, new GenerateOperand.I32(i), GenerateValueType.i32);
                lower_expression(elements[i], context, elementType);
                context.instructions.Add(new GenerateInstruction(
                    NyarHeadCode.set_offset_index,
                    new GenerateOperand.Str(to_cg_type_name(elementType))));
            }

            return GenerateValueType.@object;
        }

        var functionName = "std.collection.__array_list_jvm_new";
        emit_const(context.instructions, new GenerateOperand.I32(0), GenerateValueType.i32);
        var signature = build_static_call_signature(functionName, 1, context, GenerateValueType.@object);
        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.call_static,
            new GenerateOperand.FuncRef(functionName, signature)));
        for (var i = 0; i < elements.Length; i++)
        {
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.dup));
            lower_expression(elements[i], context, null);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.array_push));
        }

        return GenerateValueType.@object;
    }

    private static bool is_clr_backend_target(string? targetBackend)
    {
        if (string.IsNullOrWhiteSpace(targetBackend))
        {
            return false;
        }

        return string.Equals(targetBackend, "clr", StringComparison.OrdinalIgnoreCase) ||
               targetBackend.StartsWith("clr-", StringComparison.OrdinalIgnoreCase);
    }

    private static GenerateValueType infer_clr_array_literal_element_type(
        IReadOnlyList<Id> elements,
        FunctionLoweringContext context)
    {
        if (elements.Count == 0)
        {
            return GenerateValueType.@object;
        }

        var firstType = infer_expression_type(elements[0], context);
        for (var i = 1; i < elements.Count; i++)
        {
            var currentType = infer_expression_type(elements[i], context);
            if (currentType != firstType)
            {
                return GenerateValueType.@object;
            }
        }

        return firstType;
    }

    /// <summary>
    ///     处理未识别的表达式节点，输出调试信息并返回 void。
    /// </summary>
    private static GenerateValueType emit_unhandled_expression(AlgebraNode node)
    {
#if DEBUG
        Console.Error.WriteLine(
            $"[LIR] 未处理的代数节点类型: {node.GetType().Name}，返回 void");
#endif
        return GenerateValueType.@void;
    }

    /// <summary>
    ///     发射 GetField 指令，获取对象字段。
    /// </summary>
    private static GenerateValueType emit_get_field(AlgebraNode.GetField getField, FunctionLoweringContext context)
    {
        lower_expression(getField.@object, context, null);
        emit_const(context.instructions, new GenerateOperand.Str(getField.field_name), GenerateValueType.utf8);
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.get_field));
        return GenerateValueType.any;
    }

    /// <summary>
    ///     发射 SetField 指令，设置对象字段。
    /// </summary>
    private static GenerateValueType emit_set_field(AlgebraNode.SetField setField, FunctionLoweringContext context)
    {
        lower_expression(setField.@object, context, null);
        lower_expression(setField.value, context, null);
        emit_const(context.instructions, new GenerateOperand.Str(setField.field_name), GenerateValueType.utf8);
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_field));
        return GenerateValueType.@void;
    }

    private static GenerateValueType emit_call(PhysicalNode.Call call, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        return call.dispatch switch
        {
            DispatchKind.@static => emit_static_call(call, context, expectedType),
            DispatchKind.witness => emit_witness_call(call, context, expectedType),
            DispatchKind.dynamic => emit_dynamic_call(call, context, expectedType),
            _ => throw new NotSupportedException($"不支持的分派策略：{call.dispatch}")
        };
    }

    private static GenerateValueType emit_static_call(PhysicalNode.Call call, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var calleeNode = resolve_node(context.graph, call.target);
        var functionName = calleeNode is Sym calleeSymbol ? calleeSymbol.name : "unknown";

        // 语义层不应把 operator 概念泄漏到 LIR。
        // 但当前部分类型尚未提供完整的 infix imply 实现（如 utf8 的 ==、!=），
        // HIR 解糖后这些运算符无法解析方法分派，需要在此处兜底降级为对应指令。
        // 支持多种函数名形式：`infix OP`、`module.infix OP`、`module.__operator_infix__uXXXX`
        var extractedInfix = try_extract_infix_operator(functionName);
        if (extractedInfix is not null)
        {
            return emit_unresolved_infix(extractedInfix, [.. call.arguments], context);
        }

        foreach (var argumentId in call.arguments) lower_expression(argumentId, context, null);

        if (try_emit_intrinsic_call(functionName, call.arguments.Length, context, expectedType,
                out var intrinsicType))
            return intrinsicType;

        var signature = build_static_call_signature(functionName, call.arguments.Length, context, expectedType);

#if DEBUG
        if (signature.results.Count > 0 && signature.results[0] == GenerateValueType.any)
            Console.Error.WriteLine($"[LIR] 未知签名回退到 any: {functionName}({call.arguments.Length} args)");
#endif

        context.instructions.Add(new GenerateInstruction(NyarHeadCode.call_static,
            new GenerateOperand.FuncRef(functionName, signature)));
        return signature.results.Count > 0 ? signature.results[0] : GenerateValueType.@void;
    }

    /// <summary>
    ///     兜底降级未解析的 infix 运算符。
    ///     当类型的 imply 实现不完整时（如 utf8 缺少 infix ==、bool 缺少 infix ||），
    ///     HIR 解糖后的方法调用无法找到分派目标，需要在此处直接映射为指令。
    /// </summary>
    private static GenerateValueType emit_unresolved_infix(
        string functionName,
        IReadOnlyList<Id> arguments,
        FunctionLoweringContext context)
    {
        // 捕获各操作数类型，用于 infix + 的文本/整数分派
        var operandTypes = new GenerateValueType[arguments.Count];
        for (var i = 0; i < arguments.Count; i++)
            operandTypes[i] = lower_expression(arguments[i], context, null);

        // infix + 需要根据操作数类型分派：文本类型或 any 走 utf8_concat，整数走 i32_add。
        // any 类型无法在 LIR 层确定具体类型，但 i32_add 对字符串会触发 CLR 校验失败，
        // 因此 any 也按文本拼接处理（字符串 + 是 any 上最常见的 + 用法）。
        if (functionName is "infix +" && arguments.Count == 2 &&
            (is_text_type(operandTypes[0]) || is_text_type(operandTypes[1]) ||
             operandTypes[0] == GenerateValueType.any || operandTypes[1] == GenerateValueType.any))
        {
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.utf8_concat));
            return GenerateValueType.utf8;
        }

        NyarHeadCode? headCode = functionName switch
        {
            "infix ==" => NyarHeadCode.i32_eq,
            "infix !=" => NyarHeadCode.i32_ne,
            "infix <" => NyarHeadCode.i32_lt_s,
            "infix <=" => NyarHeadCode.i32_le_s,
            "infix >" => NyarHeadCode.i32_gt_s,
            "infix >=" => NyarHeadCode.i32_ge_s,
            "infix +" => NyarHeadCode.i32_add,
            "infix -" => NyarHeadCode.i32_sub,
            "infix *" => NyarHeadCode.i32_mul,
            "infix /" => NyarHeadCode.i32_div_s,
            "infix %" => NyarHeadCode.i32_rem_s,
            "infix ||" => NyarHeadCode.i32_or,
            "infix &&" => NyarHeadCode.i32_and,
            "infix ^" => NyarHeadCode.i32_xor,
            _ => null
        };

        if (headCode is null)
        {
            throw new NotSupportedException(
                $"LIR 不应再接收到遗留 infix 调用 `{functionName}`；当前函数 `{context.function_name}` 仍未完成 `operator -> method call -> intrinsic/trait dispatch`。");
        }

        context.instructions.Add(new GenerateInstruction(headCode.Value));

        var isComparison = functionName is "infix ==" or "infix !=" or "infix <" or "infix <=" or "infix >" or "infix >=" or "infix ||" or "infix &&";
        return isComparison ? GenerateValueType.@bool : GenerateValueType.i32;
    }

    private static bool try_emit_intrinsic_call(
        string functionName,
        int argumentCount,
        FunctionLoweringContext context,
        GenerateValueType? expectedType,
        out GenerateValueType resultType)
    {
        if (argumentCount == 1 && string.Equals(functionName, "ExitCode", StringComparison.Ordinal))
        {
            // `ExitCode(x)` 在表达式位置只是对底层 `i32` 的语义包装，
            // 不能在这里直接发射 `return`。
            // 真正的返回时机由 `ReturnStatement` 统一处理，否则会把
            // `if smoke() != ExitCode(0)` 之类的比较条件错误地截断成提前返回。
            context.may_fall_through = false;
            context.may_fall_through = true;
            resultType = GenerateValueType.i32;
            return true;
        }

        // 直接 intrinsic 名（如 i32.add、i64.sub、f64.mul 等），绕过 mir 查找
        if (IntrinsicRegistry.try_get(functionName, out var directDescriptor))
        {
            if (argumentCount != directDescriptor.argument_count)
                throw new InvalidOperationException(
                    $"Intrinsic `{functionName}` 期望参数个数为 {directDescriptor.argument_count}，实际收到 {argumentCount}。");

            resultType = directDescriptor.result_type;
            return emit_intrinsic_descriptor(directDescriptor, context);
        }

        if (!context.mir.try_get_function_intrinsic(functionName, out var intrinsicName))
        {
            resultType = GenerateValueType.@void;
            return false;
        }

        if (!IntrinsicRegistry.try_get(intrinsicName, out var descriptor))
        {
            resultType = GenerateValueType.@void;
            return false;
        }

        if (argumentCount != descriptor.argument_count)
            throw new InvalidOperationException(
                $"Intrinsic `{intrinsicName}` 期望参数个数为 {descriptor.argument_count}，实际收到 {argumentCount}。");

        resultType = descriptor.result_type;
        return emit_intrinsic_descriptor(descriptor, context);
    }

    private static bool emit_intrinsic_descriptor(
        IntrinsicDescriptor descriptor,
        FunctionLoweringContext context)
    {
        switch (descriptor.lowering_kind)
        {
            case IntrinsicLoweringKind.opcode:
                // call_intrinsic 需要携带函数名（常量池索引）和参数数量，供运行时查找宿主 intrinsic
                if (descriptor.head_code == NyarHeadCode.call_intrinsic)
                {
                    context.instructions.Add(new GenerateInstruction(
                        descriptor.head_code,
                        new GenerateOperand.Str(descriptor.name),
                        new GenerateOperand.I32(descriptor.argument_count)));
                }
                else
                {
                    context.instructions.Add(new GenerateInstruction(descriptor.head_code));
                }

                return true;
            case IntrinsicLoweringKind.static_call:
                if (string.IsNullOrWhiteSpace(descriptor.static_call_target))
                {
                    return false;
                }

                var signature = build_static_call_signature(
                    descriptor.static_call_target,
                    descriptor.argument_count,
                    context,
                    descriptor.result_type);

                context.instructions.Add(new GenerateInstruction(
                    NyarHeadCode.call_static,
                    new GenerateOperand.FuncRef(descriptor.static_call_target, signature)));
                return true;
            default:
                return false;
        }
    }

    private static GenerateFunctionType build_static_call_signature(
        string functionName,
        int argumentCount,
        FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        if (context.mir.try_get_function_signature(functionName, out var parameterTypeNames, out var returnTypeName))
        {
            // MIR 中的参数数量可能与实际调用点的参数数量不一致（跨模块调用时，
            // 函数声明可能因 struct 展开等导致参数数量不同），此时以实际调用点为准，
            // 避免签名参数数量与实际栈上参数数量不匹配导致 WASM 栈验证失败。
#if DEBUG
            if (parameterTypeNames.Count != argumentCount)
            {
                Console.Error.WriteLine($"[LIR] 签名参数数量不匹配: {functionName} MIR={parameterTypeNames.Count} args={argumentCount}");
            }
#endif
            if (parameterTypeNames.Count == argumentCount)
            {
                var parameters = parameterTypeNames
                    .Select(map_hir_type_ref_to_value_type)
                    .ToArray();
                var resultType = map_hir_type_ref_to_value_type(returnTypeName);
                return new GenerateFunctionType
                {
                    parameters = parameters,
                    results = resultType != GenerateValueType.@void ? [resultType] : []
                };
            }
        }

        // 未知签名时使用 any（引用类型）而非 i32，避免将 utf8/object 等引用类型参数错误地
        // 映射为整数类型。any 在 JVM 端映射为 java/lang/Object，可兼容所有引用类型。
        // 返回类型：优先使用 expectedType，否则默认 any（object）。
        // 不能默认 void，因为跨模块调用时 MIR 中无被调函数签名。
        // void 在 normalize_storage_type 会变成 i32，导致 CommandApp 等结构体类型被错误映射为 i32。
        // CLR 验证器拒绝 object→i32 的类型不匹配。
        return new GenerateFunctionType
        {
            parameters = [.. Enumerable.Repeat(GenerateValueType.any, argumentCount)],
            results = expectedType is { } t && t != GenerateValueType.@void ? [t] : [GenerateValueType.any]
        };
    }

    /// <summary>
    ///     生成动态 trait object 分派调用指令。
    /// </summary>
    private static GenerateValueType emit_dynamic_call(PhysicalNode.Call call, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        lower_expression(call.target, context, null);
        foreach (var argumentId in call.arguments) lower_expression(argumentId, context, null);

        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.call_dynamic,
            new GenerateOperand.I32(call.method_index!.Value)));

        return expectedType ?? GenerateValueType.i32;
    }

    private static GenerateValueType emit_witness_call(PhysicalNode.Call call, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        if (call.method_index is not { } slotIndex)
            throw new InvalidOperationException("Witness 调用缺少方法槽索引，无法降级为 `CallWitness`。");

        if (call.witness is not { } witnessId ||
            resolve_node(context.graph, witnessId) is not Sym witnessTraitSymbol)
            throw new InvalidOperationException("Witness 调用缺少协议身份，无法降级为稳定的共享调用形态。");

        var interfaceId = GenerateWitnessIdentity.compute_interface_id(witnessTraitSymbol.name);

        lower_expression(call.target, context, null);
        foreach (var argumentId in call.arguments) lower_expression(argumentId, context, null);

        context.instructions.Add(
            new GenerateInstruction(
                NyarHeadCode.call_witness,
                new GenerateOperand.I32(interfaceId),
                new GenerateOperand.I32(slotIndex)));
        return expectedType ?? GenerateValueType.i32;
    }

    /// <summary>
    ///     发射序数索引读取指令。
    /// </summary>
    private static GenerateValueType emit_get_ordinal_index(GetOrdinalIdx getOrdinalIdx, FunctionLoweringContext context)
    {
        lower_expression(getOrdinalIdx.obj, context, null);
        lower_expression(getOrdinalIdx.index, context, null);
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.get_ordinal_index));
        return GenerateValueType.any;
    }

    /// <summary>
    ///     发射序数索引写入指令。
    /// </summary>
    private static GenerateValueType emit_set_ordinal_index(SetOrdinalIdx setOrdinalIdx, FunctionLoweringContext context)
    {
        lower_expression(setOrdinalIdx.obj, context, null);
        lower_expression(setOrdinalIdx.index, context, null);
        lower_expression(setOrdinalIdx.value, context, null);
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_ordinal_index));
        return GenerateValueType.@void;
    }

    /// <summary>
    ///     发射偏移索引读取指令。
    /// </summary>
    private static GenerateValueType emit_get_offset_index(GetOffsetIdx getOffsetIdx, FunctionLoweringContext context)
    {
        lower_expression(getOffsetIdx.obj, context, null);
        lower_expression(getOffsetIdx.index, context, null);
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.get_offset_index));
        return GenerateValueType.any;
    }

    /// <summary>
    ///     发射偏移索引写入指令。
    /// </summary>
    private static GenerateValueType emit_set_offset_index(SetOffsetIdx setOffsetIdx, FunctionLoweringContext context)
    {
        lower_expression(setOffsetIdx.obj, context, null);
        lower_expression(setOffsetIdx.index, context, null);
        lower_expression(setOffsetIdx.value, context, null);
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_offset_index));
        return GenerateValueType.@void;
    }

    /// <summary>
    ///     遗留二元运算旁路已被禁用。
    ///     所有 operator 语义必须在进入 LIR 前解糖为正常调用、intrinsic/trait 分派。
    /// </summary>
    [Obsolete("遗留二元运算旁路已被禁用；请先完成 `operator -> method call -> intrinsic/trait dispatch`。")]
    private static GenerateValueType emit_binary(string op, Id left, Id right, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        _ = left;
        _ = right;
        _ = context;
        _ = expectedType;
        throw new NotSupportedException(
            $"LIR 不应再调用遗留二元运算旁路 `{op}`；请先完成 `operator -> method call -> intrinsic/trait dispatch`。");
    }

    /// <summary>
    ///     遗留数组索引读取兼容入口已被禁用。
    /// </summary>
    [Obsolete("遗留 `Oa.GetIndex` 兼容入口已被禁用；请先显式降级到 `GetOrdinalIdx` 或 `GetOffsetIdx`。")]
    private static GenerateValueType emit_get_index_legacy(AlgebraNode.GetIndex getIndex, FunctionLoweringContext context)
    {
        _ = getIndex;
        _ = context;
        return reject_legacy_index_node(nameof(AlgebraNode.GetIndex));
    }

    /// <summary>
    ///     遗留数组索引写入兼容入口已被禁用。
    /// </summary>
    [Obsolete("遗留 `Oa.SetIndex` 兼容入口已被禁用；请先显式降级到 `SetOrdinalIdx` 或 `SetOffsetIdx`。")]
    private static GenerateValueType emit_set_index_legacy(AlgebraNode.SetIndex setIndex, FunctionLoweringContext context)
    {
        _ = setIndex;
        _ = context;
        return reject_legacy_index_node(nameof(AlgebraNode.SetIndex));
    }

    /// <summary>
    ///     发射统一字段访问指令（PhysicalNode.Access 节点）。
    /// </summary>
    private static GenerateValueType emit_field_access(PhysicalNode.Access access, FunctionLoweringContext context)
    {
        lower_expression(access.@object, context, null);
        if (access.field_name is not null)
        {
            emit_const(context.instructions, new GenerateOperand.Str(access.field_name), GenerateValueType.utf8);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.get_field));
            return GenerateValueType.any;
        }

        emit_integer_constant(context.instructions, access.field_index, GenerateValueType.i32);
        context.instructions.Add(new GenerateInstruction(NyarHeadCode.get_offset_index));
        return GenerateValueType.any;
    }

    /// <summary>
    ///     处理语义层未解析的 infix 运算符，将其降级为对应 intrinsic 指令。
    ///     当类型系统无法解析 infix imply（如 utf8 的 +）时，HIR 会保留 "infix XXX" 函数名，
    ///     此处将其映射到对应的 NyarHeadCode 指令。
    /// </summary>
    private static GenerateValueType emit_unresolved_infix(
        string functionName, Id[] arguments, FunctionLoweringContext context)
    {
        // 捕获各操作数类型，用于 infix + 的文本/整数分派
        var operandTypes = new GenerateValueType[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
            operandTypes[i] = lower_expression(arguments[i], context, null);

        // infix + 需要根据操作数类型分派：文本类型或 any 走 utf8_concat，整数走 i32_add。
        // any 类型无法在 LIR 层确定具体类型，但 i32_add 对字符串会触发 CLR 校验失败，
        // 因此 any 也按文本拼接处理（字符串 + 是 any 上最常见的 + 用法）。
        if (functionName is "infix +" && arguments.Length == 2 &&
            (is_text_type(operandTypes[0]) || is_text_type(operandTypes[1]) ||
             operandTypes[0] == GenerateValueType.any || operandTypes[1] == GenerateValueType.any))
        {
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.utf8_concat));
            return GenerateValueType.utf8;
        }

        NyarHeadCode? headCode = functionName switch
        {
            "infix ==" => NyarHeadCode.i32_eq,
            "infix !=" => NyarHeadCode.i32_ne,
            "infix <" => NyarHeadCode.i32_lt_s,
            "infix <=" => NyarHeadCode.i32_le_s,
            "infix >" => NyarHeadCode.i32_gt_s,
            "infix >=" => NyarHeadCode.i32_ge_s,
            "infix +" => NyarHeadCode.i32_add,
            "infix -" => NyarHeadCode.i32_sub,
            "infix *" => NyarHeadCode.i32_mul,
            "infix /" => NyarHeadCode.i32_div_s,
            "infix %" => NyarHeadCode.i32_rem_s,
            "infix ||" => NyarHeadCode.i32_or,
            "infix &&" => NyarHeadCode.i32_and,
            "infix ^" => NyarHeadCode.i32_xor,
            _ => null
        };

        if (headCode is null)
        {
            throw new NotSupportedException(
                $"LIR 不应再接收到遗留 infix 调用 `{functionName}`；当前函数 `{context.function_name}` 仍未完成 `operator -> method call -> intrinsic/trait dispatch`。");
        }

        context.instructions.Add(new GenerateInstruction(headCode.Value));

        var isComparison = functionName is "infix ==" or "infix !=" or "infix <" or "infix <=" or "infix >" or "infix >=" or "infix ||" or "infix &&";
        return isComparison ? GenerateValueType.@bool : GenerateValueType.i32;
    }

    /// <summary>
    ///     遗留 infix 运算符兼容入口已被禁用。
    /// </summary>
    [Obsolete("遗留 `infix XXX` 运算符兼容入口已被禁用；请先完成 `operator -> method call -> intrinsic/trait dispatch`。")]
    private static string? to_shared_operator_name_from_infix(string functionName)
    {
        if (!functionName.StartsWith("infix ", StringComparison.Ordinal))
        {
            return null;
        }

        throw new NotSupportedException(
            $"LIR 不应再接收到遗留 infix 调用 `{functionName}`；请先完成 `operator -> method call -> intrinsic/trait dispatch`。");
    }

    /// <summary>
    ///     尝试从函数名中提取标准 infix 运算符。
    ///     支持以下形式：
    ///     1. `infix OP`（如 `infix ==`）
    ///     2. `module.infix OP`（如 `cmd_name.infix ==`）
    ///     3. `module.infix OP.infix OP2`（嵌套形式，取最后一个 infix）
    ///     4. `module.__operator_infix__uXXXX_uXXXX`（unicode 编码形式）
    /// </summary>
    /// <returns>标准运算符字符串（如 `infix ==`），无法识别返回 null。</returns>
    private static string? try_extract_infix_operator(string functionName)
    {
        // 形式 1：直接以 "infix " 开头
        if (functionName.StartsWith("infix ", StringComparison.Ordinal))
        {
            return functionName;
        }

        // 形式 2/3：包含 ".infix "，取最后一个
        var lastDotInfix = functionName.LastIndexOf(".infix ", StringComparison.Ordinal);
        if (lastDotInfix >= 0)
        {
            // 提取从最后一个 ".infix " 开始到结尾或下一个 ".infix" 之前
            var suffix = functionName.Substring(lastDotInfix + 1); // 去掉前面的 "."
            // 如果后面还有嵌套的 ".infix"，只取第一段
            var nextDotInfix = suffix.IndexOf(".infix ", 1, StringComparison.Ordinal);
            if (nextDotInfix > 0)
            {
                suffix = suffix.Substring(0, nextDotInfix);
            }

            return suffix;
        }

        // 形式 4：__operator_infix__uXXXX_uXXXX 编码形式
        var opInfixIdx = functionName.IndexOf("__operator_infix__", StringComparison.Ordinal);
        if (opInfixIdx >= 0)
        {
            var encoded = functionName.Substring(opInfixIdx + "__operator_infix__".Length);
            return decode_infix_operator(encoded);
        }

        return null;
    }

    /// <summary>
    ///     解码 `__operator_infix__` 后的 unicode 编码运算符。
    ///     编码格式：`u003D_u003D` 表示 `==`（每个 `uXXXX` 是一个字符的 4 位十六进制 unicode）。
    /// </summary>
    private static string? decode_infix_operator(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
        {
            return null;
        }

        var parts = encoded.Split('_');
        var sb = new System.Text.StringBuilder();
        sb.Append("infix ");

        foreach (var part in parts)
        {
            if (part.Length < 2 || part[0] != 'u')
            {
                return null;
            }

            var hex = part.Substring(1);
            if (hex.Length != 4 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var code))
            {
                return null;
            }

            sb.Append((char)code);
        }

        return sb.ToString();
    }
}
