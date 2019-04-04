using System.Collections.Immutable;
using System.Text;
using Nyar.Assembler;
using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Rules;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Language.Valkyrie.Compiler.Mir;
using Nyar.Types.Externals;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     将最终 MIR（EGraph + Oa）降级为 `GenerateModule`。
/// </summary>
public sealed partial class LirBuilder
{
    #region 入口

    public LirModule build(MirModule mir)
    {
        var module = new GenerateModule(mir.name);

        foreach (var (typeName, externalImportLinks) in mir.enumerate_type_external_import_links())
        {
            foreach (var externalImportLink in externalImportLinks)
            {
                module.add_type_external_import(typeName, externalImportLink);
            }
        }

        foreach (var binding in mir.witness_bindings)
            module.add_witness_entry(new GenerateWitnessDispatchEntry(
                binding.trait_name,
                binding.slot_index,
                binding.method_name,
                binding.target_type_name,
                binding.implementation_function_name
            ));

        if (!mir.root.HasValue) return new LirModule(module);

        var rootNode = resolve_node(mir.graph, mir.root.Value);
        if (rootNode is not AlgebraNode.Module moduleNode) return new LirModule(module);

        var emittedFunctionNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var memberId in moduleNode.children)
        {
            if (resolve_node(mir.graph, memberId) is not AlgebraNode.Export exportNode) continue;

            if (resolve_node(mir.graph, exportNode.value) is not AlgebraNode.Lambda lambdaNode) continue;

            var function = lower_function(mir, exportNode.name, lambdaNode);
            var functionIndex = module.functions.Count;
            module.add_function(function);
            emittedFunctionNames.Add(exportNode.name);
            if (mir.is_exported_function(exportNode.name))
                module.add_export(new GenerateModuleExport(exportNode.name, GenerateExportKind.function,
                    functionIndex));
        }

        // 为无函数体的外部导入声明补充 GenerateFunction，使后端能将其注册为导入符号。
        foreach (var functionName in mir.enumerate_function_names())
        {
            if (emittedFunctionNames.Contains(functionName)) continue;
            module.add_function(lower_external_declaration(mir, functionName));
        }

        return new LirModule(module);
    }

    #endregion

    #region 鍑芥暟闄嶇骇

    private static GenerateFunction lower_function(MirModule mir, string functionName, AlgebraNode.Lambda lambdaNode)
    {
        mir.try_get_parameter_types(functionName, out var parameterTypes);
        var resolvedTypes = parameterTypes ?? [];
        var returnType = infer_return_type(mir.graph, lambdaNode.body);
        if (mir.try_get_return_type(functionName, out var returnTypeRef))
        {
            try
            {
                returnType = map_hir_type_ref_to_value_type(returnTypeRef);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"LIR 降级函数 `{functionName}` 时返回类型 `{returnTypeRef.name}` 非法。", ex);
            }
        }

        var context =
            new FunctionLoweringContext(mir, functionName, [.. lambdaNode.parameters], resolvedTypes, returnType);

        lower_statement(lambdaNode.body, context);
        ensure_return(context);

        var function = new GenerateFunction(functionName, GenerateTypeReference.from_value_type(context.return_type));
        for (var i = 0; i < lambdaNode.parameters.Length; i++)
        {
            // 优先保留 HIR 中的原始结构体类型名（如 LegionProjectManifest），
            // 供 CLR 后端在 get_field/set_field 时解析字段所属类型；
            // 仅当原始类型名为内建类型或为空时，才回退到擦除后的 GenerateValueType 名称。
            var parameterType = GenerateTypeReference.from_value_type(GenerateValueType.any);
            if (i < resolvedTypes.Count)
            {
                var originalName = resolvedTypes[i].name;
                if (!string.IsNullOrEmpty(originalName) && !is_builtin_value_type_name(originalName))
                {
                    parameterType = GenerateTypeReference.parse(originalName);
                }
                else
                {
                    parameterType = GenerateTypeReference.from_value_type(map_hir_type_ref_to_value_type(resolvedTypes[i]));
                }
            }

            function.add_parameter(lambdaNode.parameters[i], parameterType);
        }

        foreach (var local in context.local_variables) function.add_local_variable(local.name, local.type_ref, local.index);

        remove_pop_before_call_cleanup(context, functionName);

        var labelsByInstructionIndex = context.labels
            .GroupBy(label => label.instruction_index)
            .ToDictionary(group => group.Key, group => group.ToArray());
        for (var instructionIndex = 0; instructionIndex <= context.instructions.Count; instructionIndex++)
        {
            if (labelsByInstructionIndex.TryGetValue(instructionIndex, out var labels))
                foreach (var label in labels)
                    function.add_label(label.name);

            if (instructionIndex < context.instructions.Count)
                function.add_instruction(context.instructions[instructionIndex]);
        }

        if (mir.try_get_function_attributes(functionName, out var hirAttributes))
            foreach (var attr in hirAttributes)
                function.add_attribute(new GenerateAttribute(attr.name, attr.arguments));

        if (mir.try_get_function_external_import_links(functionName, out var externalImportLinks))
            foreach (var externalImportLink in externalImportLinks)
                function.add_external_import_link(externalImportLink);

        return function;
    }

    /// <summary>
    ///     为无函数体的外部导入声明创建 GenerateFunction。
    ///     不包含任何指令，仅保留参数、返回类型、属性和外部导入链接，
    ///     供后端（如 WASM）将其注册为导入符号。
    /// </summary>
    private static GenerateFunction lower_external_declaration(MirModule mir, string functionName)
    {
        var returnTypeRef = mir.try_get_return_type(functionName, out var returnType)
            ? returnType
            : HirTypeRef.named("unit");

        GenerateValueType returnTypeValue;
        try
        {
            returnTypeValue = map_hir_type_ref_to_value_type(returnTypeRef);
        }
        catch (InvalidOperationException)
        {
            returnTypeValue = GenerateValueType.@null;
        }

        var function = new GenerateFunction(functionName, GenerateTypeReference.from_value_type(returnTypeValue));

        if (mir.try_get_parameter_types(functionName, out var parameterTypes) && parameterTypes is not null)
        {
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                var parameterTypeRef = parameterTypes[i];
                var parameterType = GenerateTypeReference.from_value_type(GenerateValueType.any);
                if (!string.IsNullOrEmpty(parameterTypeRef.name) && !is_builtin_value_type_name(parameterTypeRef.name))
                {
                    parameterType = GenerateTypeReference.parse(parameterTypeRef.name);
                }
                else
                {
                    try
                    {
                        parameterType = GenerateTypeReference.from_value_type(map_hir_type_ref_to_value_type(parameterTypeRef));
                    }
                    catch (InvalidOperationException)
                    {
                        parameterType = GenerateTypeReference.from_value_type(GenerateValueType.any);
                    }
                }
                function.add_parameter($"__arg{i}", parameterType);
            }
        }

        if (mir.try_get_function_attributes(functionName, out var hirAttributes))
            foreach (var attr in hirAttributes)
                function.add_attribute(new GenerateAttribute(attr.name, attr.arguments));

        if (mir.try_get_function_external_import_links(functionName, out var externalImportLinks))
            foreach (var externalImportLink in externalImportLinks)
                function.add_external_import_link(externalImportLink);

        return function;
    }

    #endregion

    #region 涓婁笅鏂?

    private sealed class FunctionLoweringContext
    {
        public FunctionLoweringContext(MirModule mir, string functionName, ImmutableArray<string> parameterNames,
            IReadOnlyList<HirTypeRef> parameterTypes, GenerateValueType returnType)
        {
            this.mir = mir;
            this.functionName = functionName;
            graph = mir.graph;
            return_type = returnType;
            parameters = new Dictionary<string, int>(StringComparer.Ordinal);
            parameter_types = new Dictionary<string, GenerateValueType>(StringComparer.Ordinal);
            locals = new Dictionary<string, int>(StringComparer.Ordinal);
            local_types = new Dictionary<string, GenerateValueType>(StringComparer.Ordinal);
            local_variables = [];
            labels = [];
            shared_state = new SharedLoweringState();
            for (var i = 0; i < parameterNames.Length; i++)
            {
                parameters[parameterNames[i]] = i;
                var parameterType = i < parameterTypes.Count
                    ? map_hir_type_ref_to_value_type(parameterTypes[i])
                    : GenerateValueType.any;
                parameter_types[parameterNames[i]] = parameterType;
            }
        }

        private FunctionLoweringContext(FunctionLoweringContext parent)
        {
            mir = parent.mir;
            functionName = parent.functionName;
            graph = parent.graph;
            return_type = parent.return_type;
            parameters = parent.parameters;
            parameter_types = parent.parameter_types;
            locals = new Dictionary<string, int>(parent.locals, StringComparer.Ordinal);
            local_types = new Dictionary<string, GenerateValueType>(parent.local_types, StringComparer.Ordinal);
            local_variables = parent.local_variables;
            labels = [];
            shared_state = parent.shared_state;
            break_label = parent.break_label;
            continue_label = parent.continue_label;
            may_fall_through = true;
        }

        public MirModule mir { get; }
        private string functionName { get; }
        public EGraph<AlgebraNode> graph { get; }
        public GenerateValueType return_type { get; set; }
        public List<GenerateInstruction> instructions { get; } = [];
        public Dictionary<string, int> parameters { get; }
        public Dictionary<string, GenerateValueType> parameter_types { get; }
        public Dictionary<string, int> locals { get; }
        public Dictionary<string, GenerateValueType> local_types { get; }
        public List<GenerateLocalVariable> local_variables { get; }
        public List<GenerateLabelMarker> labels { get; }
        public SharedLoweringState shared_state { get; }
        public string function_name => functionName;
        public string? break_label { get; private set; }
        public string? continue_label { get; private set; }
        public bool may_fall_through { get; set; } = true;
        public bool may_return { get; set; }
        public bool may_break { get; set; }
        public bool may_continue { get; set; }
        public bool terminated => !may_fall_through;

        public FunctionLoweringContext create_branch_context()
        {
            return new FunctionLoweringContext(this);
        }

        public FunctionLoweringContext create_loop_context(string breakLabel, string continueLabel)
        {
            var context = new FunctionLoweringContext(this);
            context.break_label = breakLabel;
            context.continue_label = continueLabel;
            return context;
        }
    }

    private sealed class SharedLoweringState
    {
        public int next_label_id;
        public int next_temp_id;
    }

    #endregion

    #region 璇彞涓庤〃杈惧紡

    private static void lower_statement(Id statementId, FunctionLoweringContext context)
    {
        if (context.terminated) return;

        var resolvedNode = resolve_node(context.graph, statementId);
        switch (resolvedNode)
        {
            case AlgebraNode.Seq seq:
                if (is_struct_literal_sequence(seq, context.graph))
                {
                    // 结构体字面量：new_object + dup + set_field*
                    // 语句上下文需要 pop 最终的对象引用
                    lower_struct_literal(seq, context);
                    context.instructions.Add(new GenerateInstruction(NyarHeadCode.pop));
                    break;
                }

                foreach (var childId in seq.children) lower_statement(childId, context);

                break;
            case Choice choice:
                lower_choice_statement(choice, context);
                break;
            case AlgebraNode.Repeat repeat:
                lower_repeat_statement(repeat, context);
                break;
            case AlgebraNode.Break:
                lower_break_statement(context);
                break;
            case AlgebraNode.Continue:
                lower_continue_statement(context);
                break;
            case AlgebraNode.Return ret:
                if (resolve_node(context.graph, ret.value) is AlgebraNode.None)
                {
                    // return; 无返回值表达式：如果函数返回非 void 类型，
                    // 需要推入默认返回值供 @return 使用
                    if (context.return_type != GenerateValueType.@void)
                    {
                        emit_default_value(context.instructions, context.return_type);
                    }
                }
                else if (resolve_node(context.graph, ret.value) is not Literal<object?>)
                {
                    lower_expression(ret.value, context, context.return_type);
                    // 如果 lower_expression 已经触发了 ExitCode 等终止性 intrinsic，则跳过重复的 return 指令
                    if (context.terminated)
                        break;
                }
                else if (context.return_type != GenerateValueType.@void)
                {
                    emit_default_value(context.instructions, context.return_type);
                }

                context.instructions.Add(new GenerateInstruction(NyarHeadCode.@return));
                context.may_fall_through = false;
                context.may_return = true;
                break;
            case AlgebraNode.Raise:
                reject_legacy_effect_statement("Raise");
                break;
            case AlgebraNode.Resume:
                reject_legacy_effect_statement("Resume");
                break;
            case AlgebraNode.Try:
                reject_legacy_effect_statement("Try");
                break;
            case VarDecl declaration:
                lower_variable_declaration(declaration, context);
                break;
            case AlgebraNode.VarDecl ikunDecl:
                lower_variable_declaration_ikun(ikunDecl, context);
                break;
            default:
                var valueType = lower_expression(statementId, context, null);
                if (!context.terminated && valueType != GenerateValueType.@void && valueType != GenerateValueType.unit)
                    context.instructions.Add(new GenerateInstruction(NyarHeadCode.pop));

                break;
        }
    }

    /// <summary>
    ///     清理 `pop` 后紧接需要消费栈值的指令的有害模式。
    ///     当 `lower_statement` 的 default 分支在语句位置为表达式结果插入 `pop`，
    ///     而后续指令（store_local / store_arg）需要该值作为参数时，移除多余的 `pop` 指令。
    ///     注意：call_static / call 不会消费被 pop 的值，它们消费的是自己的参数，
    ///     因此 pop + call 的组合中 pop 不可移除，否则调用参数数量会错乱。
    /// </summary>
    private static void remove_pop_before_call_cleanup(FunctionLoweringContext context, string functionName)
    {
        if (context.instructions.Count < 2)
        {
            return;
        }

        // 第一遍：标记需要移除的 pop 索引
        var removedIndices = new HashSet<int>();
        for (var i = 0; i < context.instructions.Count - 1; i++)
        {
            if (context.instructions[i].head_code != NyarHeadCode.pop)
            {
                continue;
            }

            var nextOpcode = context.instructions[i + 1].head_code;
            if (nextOpcode == NyarHeadCode.store_local ||
                nextOpcode == NyarHeadCode.store_arg)
            {
                removedIndices.Add(i);
            }
        }

        if (removedIndices.Count == 0)
        {
            return;
        }

        // 第二遍：构建清理后的指令列表，并计算旧索引到新索引的映射
        var cleaned = new List<GenerateInstruction>(context.instructions.Count - removedIndices.Count);
        var oldToNew = new int[context.instructions.Count];
        var newIdx = 0;
        for (var oldIdx = 0; oldIdx < context.instructions.Count; oldIdx++)
        {
            if (removedIndices.Contains(oldIdx))
            {
                oldToNew[oldIdx] = -1;
                continue;
            }

            oldToNew[oldIdx] = newIdx;
            cleaned.Add(context.instructions[oldIdx]);
            newIdx++;
        }

        context.instructions.Clear();
        context.instructions.AddRange(cleaned);

        // 第三遍：修正标签的 instruction_index
        for (var labelIdx = 0; labelIdx < context.labels.Count; labelIdx++)
        {
            var label = context.labels[labelIdx];
            var newIndex = oldToNew[label.instruction_index];
            if (newIndex >= 0 && newIndex != label.instruction_index)
            {
                context.labels[labelIdx] = new GenerateLabelMarker(label.name, newIndex);
            }
        }
    }

    private static GenerateValueType lower_expression(Id id, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var resolvedNode = resolve_node(context.graph, id);
        if (resolvedNode is null)
        {
#if DEBUG
            Console.Error.WriteLine($"[LIR] 无法解析节点 id={id}，返回 void");
#endif
            return GenerateValueType.@void;
        }
        return resolvedNode switch
        {
            Literal<long> c => emit_integer_constant(context.instructions, c.value, expectedType),
            AlgebraNode.Constant c => emit_integer_constant(context.instructions, c.value, expectedType),
            Literal<double> f => emit_float_constant(context.instructions, f.value, expectedType),
            AlgebraNode.FloatConstant f => emit_float_constant(context.instructions, f.value, expectedType),
            Literal<string> s => emit_const(context.instructions, new GenerateOperand.Str(s.value),
                GenerateValueType.utf8),
            AlgebraNode.StringConstant s => emit_const(context.instructions, new GenerateOperand.Str(s.value),
                GenerateValueType.utf8),
            Literal<bool> b => emit_const(context.instructions, new GenerateOperand.I32(b.value ? 1 : 0),
                GenerateValueType.@bool),
            AlgebraNode.BooleanConstant b => emit_const(context.instructions, new GenerateOperand.I32(b.value ? 1 : 0),
                GenerateValueType.@bool),
            Literal<object?> => emit_none(context, expectedType),
            AlgebraNode.None => emit_none(context, expectedType),
            Sym symbol => emit_symbol(symbol.name, context),
            AlgebraNode.Symbol legacySymbol => emit_symbol(legacySymbol.name, context),
            Cast cast => emit_cast(cast, context, expectedType),
            AlgebraNode.Cast legacyCast => emit_cast(new Cast(legacyCast.value, legacyCast.target_type), context, expectedType),
            Neg neg => emit_neg(neg, context, expectedType),
            Not notNode => emit_not_bool(notNode, context, expectedType),
            StateUp update => emit_state_update(update, context),
            AlgebraNode.StateUpdate stateUpdate => emit_state_update_ikun(stateUpdate, context),
            Add => reject_legacy_binary_operator("+", context),
            Sub => reject_legacy_binary_operator("-", context),
            Mul => reject_legacy_binary_operator("*", context),
            Div => reject_legacy_binary_operator("/", context),
            Rem => reject_legacy_binary_operator("%", context),
            And andNode => emit_and_bool(andNode, context, expectedType),
            Or orNode => emit_or_bool(orNode, context, expectedType),
            Cmp cmpNode => emit_cmp(cmpNode, context, expectedType),
            Perform performNode => emit_perform(performNode, context),
            Apply apply => emit_apply(apply, context, expectedType),
            PhysicalNode.Call call => emit_call(call, context, expectedType),
            PhysicalNode.Access access => emit_field_access(access, context),
            GetOrdinalIdx getOrdinalIdx => emit_get_ordinal_index(getOrdinalIdx, context),
            SetOrdinalIdx setOrdinalIdx => emit_set_ordinal_index(setOrdinalIdx, context),
            GetOffsetIdx getOffsetIdx => emit_get_offset_index(getOffsetIdx, context),
            SetOffsetIdx setOffsetIdx => emit_set_offset_index(setOffsetIdx, context),
            AlgebraNode.GetIndex getIndex => emit_get_index_legacy(getIndex, context),
            AlgebraNode.SetIndex setIndex => emit_set_index_legacy(setIndex, context),
            AlgebraNode.GetField getField => emit_get_field(getField, context),
            AlgebraNode.SetField setField => emit_set_field(setField, context),
            AlgebraNode.NewObject newObject => emit_new_object(newObject, context),
            AlgebraNode.Catch => reject_legacy_effect_expression("Catch"),
            AlgebraNode.Return ret => lower_expression(ret.value, context, expectedType),
            AlgebraNode.Seq seq => lower_seq_expression(seq, context, expectedType),
            AlgebraNode.ArrayLiteral arrayLiteral => emit_array_literal(arrayLiteral, context),
            Choice choice => emit_choice_expression(choice, context, expectedType),
            _ => emit_unhandled_expression(resolvedNode)
        };
    }

    private static GenerateValueType reject_legacy_binary_operator(
        string operatorName,
        FunctionLoweringContext context)
    {
        throw new NotSupportedException(
            $"LIR 不应再接收到遗留二元运算 `{operatorName}`；当前函数 `{context.function_name}` 仍未完成 `operator -> method call -> intrinsic/trait dispatch`。");
    }

    private static GenerateValueType reject_legacy_index_node(string nodeName)
    {
        throw new NotSupportedException(
            $"LIR 不应再接收到遗留统一索引节点 `{nodeName}`；请先明确降级到 `GetOrdinalIdx/SetOrdinalIdx` 或 `GetOffsetIdx/SetOffsetIdx`。");
    }

    private static GenerateValueType reject_legacy_effect_expression(string nodeName)
    {
        throw new NotSupportedException(
            $"LIR 不应再接收到遗留效应表达式 `{nodeName}`；请先完成 `CoreEffectToStandardLoweringRule` 降级。");
    }

    /// <summary>
    ///     直接发射 Core `Perform` 节点。
    ///     当前优化管线暂未把 `Perform` 统一改写为 `VmPerform`，
    ///     因此这里保底按运行时协议压入效应名与第一个参数，
    ///     再发射 `perform_effect`。
    ///     `Yielder::YieldBreak` 在语言设计上不可恢复，
    ///     因此发射后直接终止当前语句流，避免继续生成不可达指令。
    /// </summary>
    private static GenerateValueType emit_perform(Perform performNode, FunctionLoweringContext context)
    {
        emit_const(context.instructions, new GenerateOperand.Str(performNode.effectName), GenerateValueType.utf8);

        if (performNode.arguments.Length > 0)
        {
            lower_expression(performNode.arguments[0], context, null);
        }
        else
        {
            emit_none(context, GenerateValueType.any);
        }

        context.instructions.Add(new GenerateInstruction(NyarHeadCode.perform_effect));
        if (string.Equals(performNode.effectName, "Yielder::YieldBreak", StringComparison.Ordinal))
        {
            context.may_fall_through = false;
        }

        return GenerateValueType.@void;
    }

    private static GenerateValueType emit_choice_expression(
        Choice choice,
        FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var elseLabel = allocate_label_name("if_else", context);
        var endLabel = allocate_label_name("if_end", context);

        var conditionType = lower_expression(choice.condition, context, GenerateValueType.@bool);
        if (conditionType == GenerateValueType.@void)
        {
            throw new NotSupportedException("`choice` 条件无法降级为布尔值。");
        }

        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.jump_if_false,
            new GenerateOperand.Label(elseLabel)));

        var trueBranchContext = context.create_branch_context();
        var trueResultType = lower_expression(choice.then, trueBranchContext, expectedType);
        merge_branch_context(context, trueBranchContext);
        var trueTerminated = trueBranchContext.terminated;

        if (!trueTerminated)
        {
            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.jump,
                new GenerateOperand.Label(endLabel)));
        }

        add_label(context, elseLabel);

        var falseBranchContext = context.create_branch_context();
        var falseResultType = lower_expression(choice.elseBranch, falseBranchContext, expectedType);
        merge_branch_context(context, falseBranchContext);
        var falseTerminated = falseBranchContext.terminated;

        context.may_return |= trueBranchContext.may_return || falseBranchContext.may_return;
        context.may_break |= trueBranchContext.may_break || falseBranchContext.may_break;
        context.may_continue |= trueBranchContext.may_continue || falseBranchContext.may_continue;

        if (!trueTerminated || !falseTerminated)
        {
            add_label(context, endLabel);
        }

        if (trueTerminated && falseTerminated)
        {
            context.may_fall_through = false;
        }

        if (trueResultType == falseResultType)
        {
            return trueResultType;
        }

        if (expectedType is { } requiredType)
        {
            return requiredType;
        }

        if (!trueBranchContext.may_fall_through)
        {
            return falseResultType;
        }

        if (!falseBranchContext.may_fall_through)
        {
            return trueResultType;
        }

        return GenerateValueType.any;
    }

    private static void reject_legacy_effect_statement(string nodeName)
    {
        throw new NotSupportedException(
            $"LIR 不应再接收到遗留效应语句 `{nodeName}`；请先完成 `CoreEffectToStandardLoweringRule` 降级。");
    }

    /// <summary>
    ///     检查 Seq 是否为结构体字面量模式：NewObject 后跟若干个 SetField 节点
    /// </summary>
    private static bool is_struct_literal_sequence(AlgebraNode.Seq seq, EGraph<AlgebraNode> graph)
    {
        if (seq.children.Length < 1) return false;

        var firstNode = resolve_node(graph, seq.children[0]);
        if (firstNode is not AlgebraNode.NewObject) return false;

        for (var i = 1; i < seq.children.Length; i++)
        {
            var childNode = resolve_node(graph, seq.children[i]);
            if (childNode is not AlgebraNode.SetField) return false;
        }

        return true;
    }

    /// <summary>
    ///     降级结构体字面量：发射 new_object 一次，然后对每个 SetField 都先 dup 再发射字段名、字段值和 set_field。
    ///     栈布局：[obj, obj_dup, fieldName, value] → set_field 消费 3 项（fieldName, value, obj_dup）→ [obj]。
    ///     每个 set_field 消耗一个 dup'd 引用，newobj 的原始引用保留到最后供后续指令使用。
    ///     new_object 指令会携带字段类型注解（作为操作数追加在 type_name 和 field_count 之后），
    ///     供 WASM GC 后端构建正确的类型段。
    /// </summary>
    private static void lower_struct_literal(AlgebraNode.Seq seq, FunctionLoweringContext context)
    {
        if (seq.children.Length <= 1)
        {
            // 空结构体：仅发射 new_object
            lower_expression(seq.children[0], context, null);
            return;
        }

        // Phase 1: 扫描字段值表达式推断字段类型，不发射指令
        var fieldTypeNames = new List<string>(seq.children.Length - 1);
        for (var i = 1; i < seq.children.Length; i++)
        {
            var setField = (AlgebraNode.SetField)resolve_node(context.graph, seq.children[i])!;
            var valueType = infer_expression_type(setField.value, context);
            fieldTypeNames.Add(to_cg_type_name(valueType));
        }

        // Phase 2: 发射 new_object（含字段类型注解）
        var resolvedNode = resolve_node(context.graph, seq.children[0]);
        if (resolvedNode is AlgebraNode.NewObject newObj)
        {
            var emittedTypeName = try_build_runtime_tuple_type_name(newObj.type_name, fieldTypeNames) ?? newObj.type_name;
            var operands = new List<GenerateOperand>
            {
                new GenerateOperand.Str(emittedTypeName),
                new GenerateOperand.I32(newObj.field_count)
            };
            foreach (var ft in fieldTypeNames)
                operands.Add(new GenerateOperand.Str(ft));

            context.instructions.Add(new GenerateInstruction(NyarHeadCode.new_object, [.. operands]));
        }
        else
        {
            // 回退：旧的发射方式
            lower_expression(seq.children[0], context, null);
        }

        // Phase 3: 发射 set_field 序列（同旧逻辑）
        for (var i = 1; i < seq.children.Length; i++)
        {
            var setField = (AlgebraNode.SetField)resolve_node(context.graph, seq.children[i])!;

            // 每个 set_field 前都发射 dup，确保 newobj 的原始引用保留到最后
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.dup));

            // 发射字段名和字段值（不重新发射对象引用）
            // 栈顺序：[obj, obj_dup, fieldName, value]，与 emit_set_field 和 WASM 后端一致
            emit_const(context.instructions, new GenerateOperand.Str(setField.field_name), GenerateValueType.utf8);
            lower_expression(setField.value, context, null);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_field));
        }
    }

    private static string? try_build_runtime_tuple_type_name(string typeName, IReadOnlyList<string> fieldTypeNames)
    {
        if (!typeName.StartsWith("__tuple$arity", StringComparison.Ordinal))
        {
            return null;
        }

        var builder = new StringBuilder(typeName.Length + fieldTypeNames.Count * 16);
        builder.Append(typeName);
        foreach (var fieldTypeName in fieldTypeNames)
        {
            builder.Append("__");
            append_sanitized_type_name(builder, fieldTypeName);
        }

        return builder.ToString();
    }

    private static void append_sanitized_type_name(StringBuilder builder, string typeName)
    {
        foreach (var ch in typeName)
        {
            if ((ch >= 'a' && ch <= 'z') ||
                (ch >= 'A' && ch <= 'Z') ||
                (ch >= '0' && ch <= '9') ||
                ch == '_')
            {
                builder.Append(ch);
                continue;
            }

            builder.Append('_');
            builder.Append(((int)ch).ToString("X2"));
        }
    }

    /// <summary>
    ///     轻量推断表达式类型（不发射指令），用于结构体字面量的字段类型注解。
    ///     仅处理结构体字段值表达式中常见的节点类型，复杂/未知类型回退到 any。
    /// </summary>
    private static GenerateValueType infer_expression_type(Id id, FunctionLoweringContext context)
    {
        var node = resolve_node(context.graph, id);
        if (node is null) return GenerateValueType.any;

        return node switch
        {
            // 数值字面量
            Literal<long> c => c.value switch
            {
                >= int.MinValue and <= int.MaxValue => GenerateValueType.i32,
                _ => GenerateValueType.i64
            },
            AlgebraNode.Constant c => c.value switch
            {
                >= int.MinValue and <= int.MaxValue => GenerateValueType.i32,
                _ => GenerateValueType.i64
            },
            Literal<double> => GenerateValueType.f64,
            AlgebraNode.FloatConstant => GenerateValueType.f64,
            Literal<string> => GenerateValueType.utf8,
            AlgebraNode.StringConstant => GenerateValueType.utf8,
            Literal<bool> => GenerateValueType.@bool,
            AlgebraNode.BooleanConstant => GenerateValueType.@bool,
            Literal<object?> => GenerateValueType.@void,
            AlgebraNode.None => GenerateValueType.@void,

            // 符号（变量引用）
            Sym symbol => infer_var_ref_type(symbol.name, context),
            AlgebraNode.Symbol legacySymbol => infer_var_ref_type(legacySymbol.name, context),

            // 结构体/数组字面量 → object（引用类型）
            AlgebraNode.NewObject => GenerateValueType.@object,
            AlgebraNode.ArrayLiteral => GenerateValueType.@object,

            // 函数调用 → any（保守）
            Apply => GenerateValueType.any,
            PhysicalNode.Call => GenerateValueType.any,

            // 类型转换：优先采用目标类型，避免数组字面量等场景把显式 `as i32` 降成 any
            Cast cast => infer_cast_result_type(cast, context),
            AlgebraNode.Cast legacyCast => infer_legacy_cast_result_type(legacyCast, context),

            // 一元语义
            Neg => GenerateValueType.i32,
            Not => GenerateValueType.@bool,

            // 二元语义
            Add => reject_legacy_binary_operator("+", context),
            Sub => reject_legacy_binary_operator("-", context),
            Mul => reject_legacy_binary_operator("*", context),
            Div => reject_legacy_binary_operator("/", context),
            Rem => reject_legacy_binary_operator("%", context),
            And => GenerateValueType.@bool,
            Or => GenerateValueType.@bool,
            Cmp => GenerateValueType.@bool,
            
            // 选择表达式：取 then 分支的类型
            Choice choice => infer_expression_type(choice.then, context),

            // GetField → 引用类型
            AlgebraNode.GetField => GenerateValueType.@object,
            PhysicalNode.Access => GenerateValueType.@object,

            // 默认回退
            _ => GenerateValueType.any
        };
    }

    /// <summary>
    ///     推断变量引用的类型。
    /// </summary>
    private static GenerateValueType infer_var_ref_type(string name, FunctionLoweringContext context)
    {
        // 优先查找参数类型
        if (context.parameter_types.TryGetValue(name, out var paramType))
            return paramType;

        // 其次查找局部变量类型
        if (context.local_types.TryGetValue(name, out var localType))
            return localType;

        // 未知变量回退 any
        return GenerateValueType.any;
    }

    /// <summary>
    ///     推断显式类型转换表达式的结果类型。
    /// </summary>
    private static GenerateValueType infer_cast_result_type(Cast cast, FunctionLoweringContext context)
    {
        return resolve_cast_target_type(cast.target_type, context) ?? infer_expression_type(cast.value, context);
    }

    /// <summary>
    ///     推断遗留 Cast 节点的结果类型。
    /// </summary>
    private static GenerateValueType infer_legacy_cast_result_type(AlgebraNode.Cast cast, FunctionLoweringContext context)
    {
        return resolve_cast_target_type(cast.target_type, context) ?? infer_expression_type(cast.value, context);
    }

    /// <summary>
    ///     从类型引用节点解析 Cast 目标类型。
    /// </summary>
    private static GenerateValueType? resolve_cast_target_type(Id targetTypeId, FunctionLoweringContext context)
    {
        return resolve_node(context.graph, targetTypeId) is TypeRef typeRef
            ? normalize_integer_value_type(map_type_name_to_value_type(typeRef.name))
            : null;
    }

    /// <summary>
    ///     Seq 表达式降级：结构体字面量走专用路径；普通 Seq 取最后一个子表达式
    /// </summary>
    private static GenerateValueType lower_seq_expression(AlgebraNode.Seq seq, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        if (is_struct_literal_sequence(seq, context.graph))
        {
            lower_struct_literal(seq, context);
            return GenerateValueType.@object;
        }

        return seq.children.Length > 0
            ? lower_expression(seq.children[^1], context, expectedType)
            : GenerateValueType.@void;
    }

    private static void lower_choice_statement(Choice choice, FunctionLoweringContext context)
    {
        var elseLabel = allocate_label_name("choice_else", context);
        var endLabel = allocate_label_name("choice_end", context);

        var conditionType = lower_expression(choice.condition, context, GenerateValueType.@bool);
        if (conditionType == GenerateValueType.@void) throw new NotSupportedException("`choice` 条件无法降级为布尔值。");

        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.jump_if_false,
            new GenerateOperand.Label(elseLabel)));

        var thenContext = context.create_branch_context();
        lower_statement(choice.then, thenContext);
        merge_branch_context(context, thenContext);
        var thenTerminated = thenContext.terminated;
        if (!thenTerminated)
            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.jump,
                new GenerateOperand.Label(endLabel)));

        add_label(context, elseLabel);

        var elseContext = context.create_branch_context();
        lower_statement(choice.elseBranch, elseContext);
        merge_branch_context(context, elseContext);
        var elseTerminated = elseContext.terminated;
        context.may_return |= thenContext.may_return || elseContext.may_return;
        context.may_break |= thenContext.may_break || elseContext.may_break;
        context.may_continue |= thenContext.may_continue || elseContext.may_continue;

        if (thenTerminated && elseTerminated)
        {
            context.may_fall_through = false;
            return;
        }

        add_label(context, endLabel);
    }

    private static void lower_repeat_statement(AlgebraNode.Repeat repeat, FunctionLoweringContext context)
    {
        if (is_repeat_never_entered(repeat.count, context.graph)) return;

        var loopStartLabel = allocate_label_name("repeat_start", context);
        var loopEndLabel = allocate_label_name("repeat_end", context);
        var isInfiniteLoop = is_repeat_unbounded(repeat.count, context.graph);

        add_label(context, loopStartLabel);

        if (!isInfiniteLoop)
        {
            var conditionType = lower_expression(repeat.count, context, GenerateValueType.@bool);
            if (conditionType == GenerateValueType.@void) throw new NotSupportedException("`repeat` 条件无法降级为布尔值。");

            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.jump_if_false,
                new GenerateOperand.Label(loopEndLabel)));
        }

        var bodyContext = context.create_loop_context(loopEndLabel, loopStartLabel);
        lower_statement(repeat.body, bodyContext);
        merge_branch_context(context, bodyContext);
        context.may_return |= bodyContext.may_return;

        if (bodyContext.may_fall_through)
        {
            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.jump,
                new GenerateOperand.Label(loopStartLabel)));
        }

        if (!isInfiniteLoop || bodyContext.may_break)
        {
            add_label(context, loopEndLabel);
        }

        if (isInfiniteLoop && !bodyContext.may_break)
        {
            context.may_fall_through = false;
        }
    }

    private static void lower_break_statement(FunctionLoweringContext context)
    {
        if (string.IsNullOrWhiteSpace(context.break_label))
        {
            throw new NotSupportedException("`break` 当前只能在循环体内降级。");
        }

        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.jump,
            new GenerateOperand.Label(context.break_label)));
        context.may_fall_through = false;
        context.may_break = true;
    }

    private static void lower_continue_statement(FunctionLoweringContext context)
    {
        if (string.IsNullOrWhiteSpace(context.continue_label))
        {
            throw new NotSupportedException("`continue` 当前只能在循环体内降级。");
        }

        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.jump,
            new GenerateOperand.Label(context.continue_label)));
        context.may_fall_through = false;
        context.may_continue = true;
    }

    private static GenerateValueType emit_symbol(string name, FunctionLoweringContext context)
    {
        if (context.parameters.TryGetValue(name, out var parameterIndex))
        {
            var parameterType = context.parameter_types.GetValueOrDefault(name, GenerateValueType.i32);
            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.load_arg,
                new GenerateOperand.Param(parameterIndex, parameterType)));
            return parameterType;
        }

        if (context.locals.TryGetValue(name, out var localIndex))
        {
            var localType = context.local_types.GetValueOrDefault(name, GenerateValueType.i32);
            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.load_local,
                new GenerateOperand.Local(localIndex, localType)));
            return localType;
        }

        // 既不是参数也不是局部变量时，视为全局变量引用。
        // 模块路径（如 std、std.adaptor）在 build_expression 中被表示为 Symbol 节点，
        // 需要发射 load_global 指令加载对应的全局表。
        // 各后端负责将 load_global 映射为具体的加载指令：
        //   - CLR: ldsfld 从静态字段加载
        //   - JVM: getstatic 从静态字段加载
        //   - WASM: 从全局变量加载
        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.load_global,
            new GenerateOperand.Str(name)));
        return GenerateValueType.any;
    }

    private static GenerateValueType emit_none(FunctionLoweringContext context, GenerateValueType? expectedType)
    {
        var resolvedType = expectedType ?? GenerateValueType.@object;
        var normalizedType = normalize_integer_value_type(resolvedType);
        if (normalizedType is GenerateValueType.@object or GenerateValueType.any or GenerateValueType.function_ref or
            GenerateValueType.external_ref)
        {
            emit_default_value(context.instructions, normalizedType);
            return normalizedType;
        }

        // 整数/浮点/布尔/unit 类型：推入对应类型的默认值
        // unit 经 normalize_integer_value_type 映射为 i32，emit_default_value 会推入 I32(0)
        emit_default_value(context.instructions, normalizedType);
        return normalizedType;
    }

    private static GenerateValueType emit_float_constant(
        List<GenerateInstruction> instructions,
        double value,
        GenerateValueType? expectedType)
    {
        if (expectedType == GenerateValueType.f32)
        {
            emit_const(instructions, new GenerateOperand.F32((float)value), GenerateValueType.f32);
            return GenerateValueType.f32;
        }

        emit_const(instructions, new GenerateOperand.F64(value), GenerateValueType.f64);
        return GenerateValueType.f64;
    }

    private static GenerateValueType emit_neg(Neg neg, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var operandType = lower_expression(neg.operand, context, expectedType);
        if (operandType == GenerateValueType.f64)
        {
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.f64_neg));
            return GenerateValueType.f64;
        }

        if (operandType != GenerateValueType.@void)
        {
            var integerType = normalize_integer_value_type(operandType);
            context.instructions.Add(new GenerateInstruction(integerType == GenerateValueType.i64
                ? NyarHeadCode.i64_neg
                : NyarHeadCode.i32_neg));
            return integerType;
        }

        return operandType;
    }

    private static GenerateValueType emit_not_bool(Not notNode, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var operandType = lower_expression(notNode.operand, context, expectedType);
        var normalizedType = normalize_integer_value_type(operandType);
        emit_integer_constant(context.instructions, 0, normalizedType);
        context.instructions.Add(new GenerateInstruction(normalizedType == GenerateValueType.i64
            ? NyarHeadCode.i64_eq
            : NyarHeadCode.i32_eq));
        return GenerateValueType.@bool;
    }

    /// <summary>
    ///     发射布尔 And 指令：对两个布尔值执行按位与（布尔值以 i32 的 0/1 表示）。
    ///     MIR 在模式匹配守卫和范围模式中会生成 And 节点，LIR 需要将其降级为 i32_and。
    /// </summary>
    private static GenerateValueType emit_and_bool(And andNode, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var leftType = lower_expression(andNode.left, context, expectedType);
        var rightType = lower_expression(andNode.right, context, expectedType);
        var normalizedType = normalize_integer_value_type(leftType);
        context.instructions.Add(new GenerateInstruction(normalizedType == GenerateValueType.i64
            ? NyarHeadCode.i64_and
            : NyarHeadCode.i32_and));
        return GenerateValueType.@bool;
    }

    /// <summary>
    ///     发射布尔 Or 指令：对两个布尔值执行按位或（布尔值以 i32 的 0/1 表示）。
    ///     MIR 在模式匹配守卫中会生成 Or 节点，LIR 需要将其降级为 i32_or。
    /// </summary>
    private static GenerateValueType emit_or_bool(Or orNode, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var leftType = lower_expression(orNode.left, context, expectedType);
        var rightType = lower_expression(orNode.right, context, expectedType);
        var normalizedType = normalize_integer_value_type(leftType);
        context.instructions.Add(new GenerateInstruction(normalizedType == GenerateValueType.i64
            ? NyarHeadCode.i64_or
            : NyarHeadCode.i32_or));
        return GenerateValueType.@bool;
    }

    /// <summary>
    ///     发射比较指令：将 Cmp 节点降级为对应的 i32/i64/utf8 比较指令。
    ///     MIR 在模式匹配和条件判断中会生成 Cmp 节点。
    ///     注意：字符串类型仅支持 eq/ne，lt/le/gt/ge 对字符串无意义，
    ///     若出现则保持原有 i32 降级（语义错误应在更早阶段拦截）。
    /// </summary>
    private static GenerateValueType emit_cmp(Cmp cmpNode, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var leftType = lower_expression(cmpNode.left, context, expectedType);
        var rightType = lower_expression(cmpNode.right, context, expectedType);
        var normalizedType = normalize_integer_value_type(leftType);

        var headCode = cmpNode.op switch
        {
            CompareOp.eq => normalizedType switch
            {
                GenerateValueType.i64 => NyarHeadCode.i64_eq,
                GenerateValueType.utf8 => NyarHeadCode.utf8_eq,
                _ => NyarHeadCode.i32_eq
            },
            CompareOp.ne => normalizedType switch
            {
                GenerateValueType.i64 => NyarHeadCode.i64_ne,
                GenerateValueType.utf8 => NyarHeadCode.utf8_ne,
                _ => NyarHeadCode.i32_ne
            },
            CompareOp.lt => normalizedType == GenerateValueType.i64 ? NyarHeadCode.i64_lt_s : NyarHeadCode.i32_lt_s,
            CompareOp.le => normalizedType == GenerateValueType.i64 ? NyarHeadCode.i64_le_s : NyarHeadCode.i32_le_s,
            CompareOp.gt => normalizedType == GenerateValueType.i64 ? NyarHeadCode.i64_gt_s : NyarHeadCode.i32_gt_s,
            CompareOp.ge => normalizedType == GenerateValueType.i64 ? NyarHeadCode.i64_ge_s : NyarHeadCode.i32_ge_s,
            _ => NyarHeadCode.i32_eq
        };

        context.instructions.Add(new GenerateInstruction(headCode));
        return GenerateValueType.@bool;
    }

    private static GenerateValueType emit_state_update(StateUp update, FunctionLoweringContext context)
    {
        var keyNode = resolve_node(context.graph, update.key);
        if (keyNode is Sym symbol)
        {
            var valueType = lower_expression(update.value, context, null);

            if (context.parameters.TryGetValue(symbol.name, out var parameterIndex))
            {
                var parameterType = context.parameter_types[symbol.name];
                context.instructions.Add(new GenerateInstruction(
                    NyarHeadCode.store_arg,
                    new GenerateOperand.Param(parameterIndex, parameterType)));
                return GenerateValueType.@void;
            }

            if (context.locals.TryGetValue(symbol.name, out var localIndex))
            {
                var localType = context.local_types[symbol.name];
                insert_implicit_cast(valueType, localType, context);
                context.instructions.Add(new GenerateInstruction(
                    NyarHeadCode.store_local,
                    new GenerateOperand.Local(localIndex, localType)));
                return GenerateValueType.@void;
            }

            throw new NotSupportedException($"找不到符号 `{symbol.name}`，无法执行 `StateUpdate`。");
        }

        if (keyNode is GetOrdinalIdx getOrdinalIdx)
        {
            lower_expression(getOrdinalIdx.obj, context, null);
            lower_expression(getOrdinalIdx.index, context, null);
            lower_expression(update.value, context, null);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_ordinal_index));
            return GenerateValueType.@void;
        }

        if (keyNode is GetOffsetIdx getOffsetIdx)
        {
            lower_expression(getOffsetIdx.obj, context, null);
            lower_expression(getOffsetIdx.index, context, null);
            lower_expression(update.value, context, null);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_offset_index));
            return GenerateValueType.@void;
        }

        if (keyNode is AlgebraNode.GetIndex)
        {
            return reject_legacy_index_node(nameof(AlgebraNode.GetIndex));
        }

        if (keyNode is AlgebraNode.GetField getField)
        {
            lower_expression(getField.@object, context, null);
            emit_const(context.instructions, new GenerateOperand.Str(getField.field_name), GenerateValueType.utf8);
            lower_expression(update.value, context, null);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_field));
            return GenerateValueType.@void;
        }

        if (keyNode is PhysicalNode.Access { field_name: not null } access)
        {
            lower_expression(access.@object, context, null);
            emit_const(context.instructions, new GenerateOperand.Str(access.field_name), GenerateValueType.utf8);
            lower_expression(update.value, context, null);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_field));
            return GenerateValueType.@void;
        }

        throw new NotSupportedException("`StateUpdate` 当前仅支持符号键、索引键和字段键。");
    }

    /// <summary>
    ///     将 IKun 的 AlgebraNode.StateUpdate 降级为 LIR 指令。
    ///     与 Core 方言的 StateUp 结构相同（target + value），但类型不同。
    /// </summary>
    private static GenerateValueType emit_state_update_ikun(AlgebraNode.StateUpdate update, FunctionLoweringContext context)
    {
        var keyNode = resolve_node(context.graph, update.target);
        if (keyNode is Sym symbol)
        {
            return emit_state_update_ikun_to_symbol(symbol, update.value, context);
        }

        if (keyNode is AlgebraNode.Symbol legacySymbol)
        {
            return emit_state_update_ikun_to_symbol_name(legacySymbol.name, update.value, context);
        }

        if (keyNode is AlgebraNode.GetIndex)
        {
            return reject_legacy_index_node(nameof(AlgebraNode.GetIndex));
        }

        if (keyNode is AlgebraNode.GetField getField)
        {
            lower_expression(getField.@object, context, null);
            emit_const(context.instructions, new GenerateOperand.Str(getField.field_name), GenerateValueType.utf8);
            lower_expression(update.value, context, null);
            context.instructions.Add(new GenerateInstruction(NyarHeadCode.set_field));
            return GenerateValueType.@void;
        }

        throw new NotSupportedException($"`AlgebraNode.StateUpdate` 当前仅支持符号键、索引键和字段键，实际 key 类型为 `{keyNode.GetType().Name}`。");
    }

    /// <summary>
    ///     将 StateUpdate 到符号的操作降级为 LIR 指令。
    ///     如果符号尚未注册为局部变量，则自动注册（支持 var 声明的懒初始化）。
    /// </summary>
    private static GenerateValueType emit_state_update_ikun_to_symbol(Sym symbol, Id valueId, FunctionLoweringContext context)
    {
        return emit_state_update_ikun_to_symbol_name(symbol.name, valueId, context);
    }

    /// <summary>
    ///     将 StateUpdate 到具名符号的操作降级为 LIR 指令。
    ///     如果符号尚未注册为局部变量，则自动注册（支持 var 声明的懒初始化）。
    /// </summary>
    private static GenerateValueType emit_state_update_ikun_to_symbol_name(string name, Id valueId, FunctionLoweringContext context)
    {
        var valueType = lower_expression(valueId, context, null);

        if (context.parameters.TryGetValue(name, out var parameterIndex))
        {
            var parameterType = context.parameter_types[name];
            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.store_arg,
                new GenerateOperand.Param(parameterIndex, parameterType)));
            return GenerateValueType.@void;
        }

        if (context.locals.TryGetValue(name, out var localIndex))
        {
            var localType = context.local_types[name];
            insert_implicit_cast(valueType, localType, context);
            context.instructions.Add(new GenerateInstruction(
                NyarHeadCode.store_local,
                new GenerateOperand.Local(localIndex, localType)));
            return GenerateValueType.@void;
        }

        // 变量尚未注册：自动注册为局部变量（处理 var 声明懒初始化）
        var normalizedType = normalize_storage_type(valueType);
        var newLocalIndex = register_local(name, normalizedType, context);
        context.instructions.Add(new GenerateInstruction(
            NyarHeadCode.store_local,
            new GenerateOperand.Local(newLocalIndex, normalizedType)));
        return GenerateValueType.@void;
    }

    private static GenerateValueType emit_cast(Cast cast, FunctionLoweringContext context,
        GenerateValueType? expectedType)
    {
        var sourceType = normalize_integer_value_type(lower_expression(cast.value, context, null));
        var targetType = resolve_node(context.graph, cast.target_type) is TypeRef typeRef
            ? normalize_integer_value_type(map_type_name_to_value_type(typeRef.name))
            : normalize_integer_value_type(expectedType ?? sourceType);

        if (sourceType == targetType || sourceType == GenerateValueType.@void) return targetType;

        var opcode = try_get_cast_opcode(sourceType, targetType);
        if (opcode is null) return sourceType;

        context.instructions.Add(new GenerateInstruction(opcode.Value));
        return targetType;
    }

    private static void merge_branch_context(FunctionLoweringContext parent, FunctionLoweringContext branch)
    {
        var instructionOffset = parent.instructions.Count;
        foreach (var label in branch.labels)
            parent.labels.Add(new GenerateLabelMarker(label.name, label.instruction_index + instructionOffset));

        parent.instructions.AddRange(branch.instructions);
    }

    private static void add_label(FunctionLoweringContext context, string name)
    {
        context.labels.Add(new GenerateLabelMarker(name, context.instructions.Count));
    }

    private static string allocate_label_name(string prefix, FunctionLoweringContext context)
    {
        var labelId = context.shared_state.next_label_id++;
        return $"__{prefix}_{labelId}";
    }

    private static string allocate_temp_name(string prefix, FunctionLoweringContext context)
    {
        var tempId = context.shared_state.next_temp_id++;
        return $"__{prefix}_{tempId}";
    }

    #endregion

    #region 鎺ㄦ柇涓庤緟鍔?

    private static AlgebraNode? resolve_node(EGraph<AlgebraNode> graph, Id id)
    {
        var nodes = graph.get_class(id)?.nodes;
        if (nodes is null || nodes.Count == 0) return null;

        return nodes
            .OrderByDescending(get_node_preference)
            .ThenByDescending(get_node_label_length)
            .FirstOrDefault();
    }

    private static int get_node_preference(AlgebraNode node)
    {
        return node switch
        {
            Export exportNode => get_qualified_name_score(exportNode.name),
            Import importNode => get_qualified_name_score(importNode.name),
            Sym symbolNode => get_qualified_name_score(symbolNode.name),
            Mod moduleNode => moduleNode.children.Length,
            _ => 0
        };
    }

    private static int get_node_label_length(AlgebraNode node)
    {
        return node switch
        {
            Export exportNode => exportNode.name.Length,
            Import importNode => importNode.name.Length,
            Sym symbolNode => symbolNode.name.Length,
            Mod moduleNode => moduleNode.name.Length,
            _ => 0
        };
    }

    private static int get_qualified_name_score(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;

        var score = name.Length;
        score += name.Count(ch => ch is '.' or ':') * 16;
        return score;
    }

    private static bool is_repeat_unbounded(Id countId, EGraph<AlgebraNode> graph)
    {
        return resolve_node(graph, countId) switch
        {
            Literal<bool> { value: true } => true,
            Literal<long> { value: not 0 } => true,
            _ => false
        };
    }

    private static bool is_repeat_never_entered(Id countId, EGraph<AlgebraNode> graph)
    {
        return resolve_node(graph, countId) switch
        {
            Literal<bool> { value: false } => true,
            Literal<long> { value: 0 } => true,
            _ => false
        };
    }

    private static GenerateValueType infer_return_type(EGraph<AlgebraNode> graph, Id id)
    {
        return resolve_node(graph, id) switch
        {
            AlgebraNode.Return ret when resolve_node(graph, ret.value) is Literal<object?> => GenerateValueType.unit,
            AlgebraNode.Return ret => infer_return_type(graph, ret.value),
            AlgebraNode.Seq { children.Length: > 0 } seq => infer_return_type(graph, seq.children[^1]),
            AlgebraNode.StateUpdate => GenerateValueType.@void,
            AlgebraNode.None => GenerateValueType.@void,
            Literal<double> => GenerateValueType.f64,
            Literal<string> => GenerateValueType.utf8,
            Literal<bool> => GenerateValueType.@bool,
            Literal<object?> => GenerateValueType.unit,
            Literal<long> => GenerateValueType.i64,
            // 未知节点默认使用 any（引用类型），在 JVM 端映射为 java/lang/Object
            _ => GenerateValueType.any
        };
    }

    private static void ensure_return(FunctionLoweringContext context)
    {
        if (context.terminated) return;

        // unit 类型在 WASM 中映射为 void（无返回值），不需要生成默认返回值。
        // 如果为 unit 生成 i32.const 0，该值会被放在 block 内部导致栈不平衡，
        // 而末尾的 drop 指令在 block 外部，为时已晚。
        if (context.return_type != GenerateValueType.@void && context.return_type != GenerateValueType.unit)
        {
            // 如果最后一条指令是 pop，且函数返回非 void 类型，
            // 则移除 pop，使被 pop 的值成为返回值。
            // 这处理了 lower_statement 的 default 分支和结构体字面量路径
            // 在语句位置为表达式结果插入 pop 的情况：
            // 当该表达式恰好是函数体的最后一个值时，其结果应作为返回值而非被丢弃。
            if (context.instructions.Count > 0 &&
                context.instructions[^1].head_code == NyarHeadCode.pop)
            {
                context.instructions.RemoveAt(context.instructions.Count - 1);

                // 检查 pop 前面的值指令是否产生了与返回类型兼容的值。
                // 如果不兼容（例如 @const Null(@object) 对 unit 返回类型），
                // 则移除不兼容的值指令并发出正确的默认值。
                if (context.instructions.Count > 0 &&
                    context.instructions[^1].head_code == NyarHeadCode.@const)
                {
                    var constValue = context.instructions[^1].operands[0];
                    var constType = constValue switch
                    {
                        GenerateOperand.Null n => n.type,
                        GenerateOperand.I32 => GenerateValueType.i32,
                        GenerateOperand.I64 => GenerateValueType.i64,
                        GenerateOperand.F32 => GenerateValueType.f32,
                        GenerateOperand.F64 => GenerateValueType.f64,
                        GenerateOperand.Str => GenerateValueType.utf8,
                        GenerateOperand.Const c => c.type,
                        _ => GenerateValueType.any
                    };

                    if (!is_value_type_compatible(constType, context.return_type))
                    {
                        context.instructions.RemoveAt(context.instructions.Count - 1);
                        emit_default_value(context.instructions, context.return_type);
                    }
                }
            }
            else
            {
                emit_default_value(context.instructions, context.return_type);
            }
        }
        // unit 和 void 类型无需生成默认返回值，直接添加 return 指令即可

        context.instructions.Add(new GenerateInstruction(NyarHeadCode.@return));
        context.may_fall_through = false;
        context.may_return = true;
    }
    #endregion

    #region 效应降级
    #endregion

    #region 调试辅助
    #endregion
}
