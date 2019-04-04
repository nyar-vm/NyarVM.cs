using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     尝试解析调用目标�?
///     当前仅解析自由函数、类型固有方法和 `imply` 中的固有实现，暂不处�?`trait` 默认方法中的 `witness` 分派�?
/// </summary>
public sealed partial class HirModule
{
    #region 调用解析

    public bool try_resolve_call(HirCallable currentCallable, AstNode callee, out HirCallResolution? resolution)
    {
        var callTarget = callee is TermCallExpression call ? call.caller : callee;
        var argumentCount = get_call_argument_count(callee);

        if (try_resolve_function_call(currentCallable, callee, callTarget, argumentCount, out resolution)) return true;

        if (try_resolve_method_call(currentCallable, callTarget, out resolution)) return true;

        resolution = default;
        return false;
    }

    public bool try_resolve_loop_iteration(HirCallable currentCallable, AstNode iterable,
        out HirLoopIterationResolution? resolution)
    {
        resolution = default;

        if (iterable is not TermNode iterableTerm ||
            !try_resolve_receiver_type(currentCallable, iterableTerm, out var iterableType))
            return false;

        var iteratorType = iterableType;
        HirCallResolution? iteratorFactory = null;
        if (try_resolve_method_call_on_type(currentCallable, iterableType, "into_iterator", 0,
                out var iterResolution) &&
            iterResolution is not null)
        {
            iteratorFactory = iterResolution;
            if (!try_resolve_callable_return_type(iterResolution.target_name, out iteratorType))
                return false;
        }

        if (!try_resolve_method_call_on_type(currentCallable, iteratorType, "has_next", 0,
                out var hasNextResolution) ||
            hasNextResolution is null)
            return false;

        if (!try_resolve_method_call_on_type(currentCallable, iteratorType, "next", 0,
                out var nextResolution) ||
            nextResolution is null)
            return false;

        HirCallResolution? unwrapResolution = null;
        HirTypeRef? boundItemType = null;
        if (iteratorFactory is not null &&
            try_resolve_associated_type_binding(currentCallable, iterableType, currentCallable.namespace_path,
                ValkyrieNamePath.parse("IntoIterator"), "Item",
                out var intoIteratorItemType))
            boundItemType = intoIteratorItemType;

        if (try_resolve_associated_type_binding(currentCallable, iteratorType, currentCallable.namespace_path,
                ValkyrieNamePath.parse("Iterator"), "Item",
                out var iteratorItemType))
        {
            if (boundItemType is not null &&
                !type_refs_compatible(boundItemType, iteratorItemType, currentCallable.namespace_path))
            {
                report_loop_item_constraint_mismatch(iterable, "IntoIterator::Item", boundItemType.name,
                    "Iterator::Item", iteratorItemType.name);
                return false;
            }

            boundItemType ??= iteratorItemType;
        }

        HirTypeRef inferredItemType;
        if (try_resolve_callable_return_type(nextResolution.target_name, out var nextReturnType))
        {
            try_resolve_method_call_on_type(currentCallable, nextReturnType, "unwrap", 0,
                out unwrapResolution);
            inferredItemType = unwrapResolution is not null &&
                               try_resolve_specialized_callable_return_type(unwrapResolution.target_name,
                                   nextReturnType,
                                   out var unwrappedType)
                ? unwrappedType
                : nextReturnType;
        }
        else
        {
            inferredItemType = HirTypeRef.unknown();
        }

        HirTypeRef itemType;
        if (boundItemType is not null)
        {
            if (!inferredItemType.is_unknown_type &&
                !type_refs_compatible(boundItemType, inferredItemType, currentCallable.namespace_path))
            {
                report_loop_item_constraint_mismatch(iterable, "Item", boundItemType.name, "next()/unwrap()",
                    inferredItemType.name);
                return false;
            }

            itemType = boundItemType;
        }
        else
        {
            itemType = inferredItemType;
        }

        resolution = new HirLoopIterationResolution(iteratorFactory, hasNextResolution, nextResolution,
            unwrapResolution,
            itemType);
        return true;
    }

    /// <summary>
    ///     尝试解析表达式的静态类型�?
    ///     当前只覆盖参数、局部变量、字面量以及可直接识别的对象构造结果�?
    /// </summary>
    public bool try_resolve_expression_type(HirCallable currentCallable, AstNode expression, out HirTypeRef type)
    {
        if (expression is IdentifierNode { name: { Length: > 0 } identifierName })
        {
            var parameter = currentCallable.parameters.FirstOrDefault(candidate =>
                string.Equals(candidate.name, identifierName, StringComparison.Ordinal));
            if (parameter is not null)
            {
                type = parameter.type;
                return true;
            }

            if (try_resolve_local_variable_type(currentCallable, identifierName, out var localType))
            {
                type = localType;
                return true;
            }

            if (semantics.resolve_symbol(identifierName) is { type: not null } symbolType &&
                !string.IsNullOrWhiteSpace(symbolType.type.name))
            {
                type = HirTypeRef.from_type(symbolType.type);
                return true;
            }
        }

        if (expression is TermCallExpression callExpression &&
            try_resolve_call_expression_type(currentCallable, callExpression, out type))
            return true;

        if (expression is TermDotExpression { call_body: not null } dotCallExpression &&
            try_resolve_call_expression_type(currentCallable, dotCallExpression, out type))
            return true;

        if (expression is TermNode term && try_resolve_receiver_type(currentCallable, term, out type)) return true;

        type = HirTypeRef.unknown();
        return false;
    }

    /// <summary>
    ///     判断源类型是否可安全视为目标类型�?
    /// </summary>
    public bool is_type_compatible(HirTypeRef sourceType, SemanticNamePath targetTypeName, SemanticNameSpace? currentNamespace)
    {
        return is_type_compatible(sourceType.name_path, targetTypeName, currentNamespace);
    }

    private bool try_resolve_function_call(
        HirCallable currentCallable,
        AstNode callSite,
        AstNode callee,
        int argumentCount,
        out HirCallResolution? resolution)
    {
        switch (callee)
        {
            case IdentifierNode identifier when !string.IsNullOrWhiteSpace(identifier.name):
                return try_resolve_function_name(currentCallable, identifier.name, get_call_arguments(callSite),
                    out resolution);
            case QualifiedPathNode path when !string.IsNullOrWhiteSpace(path.full_name):
                if (try_resolve_explicit_type_method_call(currentCallable, callSite, path.full_name,
                        get_call_arguments(callSite), argumentCount, out resolution))
                    return true;

                return try_resolve_function_name(currentCallable, path.full_name, get_call_arguments(callSite),
                    out resolution);
            case TermLiteralNamePathNode termPath when !string.IsNullOrWhiteSpace(termPath.path.full_name):
                if (try_resolve_explicit_type_method_call(currentCallable, callSite, termPath.path.full_name,
                        get_call_arguments(callSite), argumentCount, out resolution))
                    return true;

                return try_resolve_function_name(currentCallable, termPath.path.full_name, get_call_arguments(callSite),
                    out resolution);
            case TermDotExpression { separator_kind: MemberAccessSeparatorKind.double_colon } dot
                when try_get_qualified_term_name(dot, out var staticQualifiedName):
                if (try_resolve_explicit_type_method_call(currentCallable, callSite, staticQualifiedName,
                        get_call_arguments(callSite), argumentCount, out resolution))
                    return true;

                return try_resolve_function_name(currentCallable, staticQualifiedName, get_call_arguments(callSite),
                    out resolution);
            case TermDotExpression dot
                when try_get_qualified_term_name(dot, out var qualifiedName):
                return try_resolve_function_name(currentCallable, qualifiedName, get_call_arguments(callSite),
                    out resolution);
            default:
                resolution = default;
                return false;
        }
    }

    private bool try_resolve_explicit_type_method_call(
        HirCallable currentCallable,
        AstNode callSite,
        string qualifiedName,
        IReadOnlyList<TermArgumentItem> callArguments,
        int argumentCount,
        out HirCallResolution? resolution)
    {
        if (!try_split_explicit_type_member_name(qualifiedName, out var ownerTypeName, out var memberName))
        {
            resolution = default;
            return false;
        }

        var ownerType = resolve_type(ownerTypeName, currentCallable.namespace_path);
        if (ownerType is null)
        {
            var ownerTrait = resolve_trait(ownerTypeName, currentCallable.namespace_path);
            if (ownerTrait is null)
            {
                resolution = default;
                return false;
            }

            return try_resolve_explicit_trait_method_call(currentCallable, callSite, ownerTrait, memberName,
                callArguments, out resolution);
        }

        var method = select_best_method_overload(
            currentCallable,
            ownerType.methods.Where(candidate =>
                candidate.matches_member_name(memberName) &&
                candidate.matches_explicit_arity(argumentCount)),
            callArguments,
            0);
        if (method is null)
        {
            resolution = default;
            return false;
        }

        resolution = method.to_call_resolution(HirDispatchKind.@static, false);
        return true;
    }

    private bool try_resolve_explicit_trait_method_call(
        HirCallable currentCallable,
        AstNode callSite,
        HirTraitDef trait,
        string memberName,
        IReadOnlyList<TermArgumentItem> callArguments,
        out HirCallResolution? resolution)
    {
        if (callArguments.Count == 0 ||
            !try_resolve_receiver_type(currentCallable, callArguments[0].value, out var receiverType))
        {
            resolution = default;
            return false;
        }

        var receiverTypeName = receiverType.name;

        var contractMethod = select_best_method_overload(
            currentCallable,
            resolve_trait_contract_methods(trait, currentCallable.namespace_path)
                .Where(candidate =>
                    candidate.matches_member_name(memberName) &&
                    candidate.matches_explicit_arity(callArguments.Count)),
            callArguments,
            0);
        if (contractMethod is null)
        {
            resolution = default;
            return false;
        }

        report_unimplemented_qualified_trait_warning(currentCallable, callSite, trait, receiverTypeName, memberName,
            currentCallable.namespace_path);

        var candidates = resolve_explicit_trait_dispatch_candidates(
            ValkyrieNamePath.parse(receiverTypeName),
            currentCallable.namespace_path,
            trait,
            contractMethod);
        if (candidates.Count == 1)
        {
            var candidate = candidates[0];
            resolution = candidate.target_method.to_call_resolution(
                HirDispatchKind.@static,
                false,
                candidate.trait_name);
            return true;
        }

        if (candidates.Count > 1)
        {
            report_dispatch_ambiguity(callSite, receiverTypeName, memberName,
                candidates.Select(candidate => candidate.trait_name));
            resolution = default;
            return false;
        }

        resolution = default;
        return false;
    }

    private bool try_resolve_method_call(HirCallable currentCallable, AstNode callee,
        out HirCallResolution? resolution)
    {
        if (callee is not TermDotExpression dot)
        {
            resolution = default;
            return false;
        }

        if (dot.separator_kind != MemberAccessSeparatorKind.dot)
        {
            resolution = default;
            return false;
        }

        if (try_resolve_self_method_call(currentCallable, dot, out resolution)) return true;

        if (try_resolve_receiver_method_call(currentCallable, dot, out resolution)) return true;

        if (try_resolve_static_type_method_call(currentCallable, dot, out resolution)) return true;

        resolution = default;
        return false;
    }

    private bool try_resolve_function_name(
        HirCallable currentCallable,
        string name,
        IReadOnlyList<TermArgumentItem> callArguments,
        out HirCallResolution? resolution)
    {
        var qualifiedName = qualify_member_name(currentCallable.namespace_path, null, name);
        var isQualifiedLookup = !ValkyrieNamePath.parse(name).@namespace.is_empty;
        var candidates = functions.Where(candidate =>
            callable_name_matches(candidate.name, qualifiedName) ||
            (isQualifiedLookup && callable_name_matches(candidate.name, name)) ||
            matches_simple_name(candidate.name, name));

        if (!isQualifiedLookup)
        {
            var importedNames = enumerate_visible_imported_names(currentCallable, name)
                .ToHashSet(StringComparer.Ordinal);
            if (importedNames.Count > 0)
                candidates = candidates.Concat(functions.Where(candidate =>
                    importedNames.Contains(candidate.name)));
        }

        var function = select_best_function_overload(currentCallable, candidates, callArguments);
        if (function is null)
        {
            resolution = default;
            return false;
        }

        resolution = function.to_call_resolution();
        return true;
    }

    private bool try_resolve_self_method_call(HirCallable currentCallable, TermDotExpression dot,
        out HirCallResolution? resolution)
    {
        if (currentCallable is not HirMethod currentMethod)
        {
            resolution = default;
            return false;
        }

        if (!try_get_simple_term_name(dot.caller, out var callerName) ||
            !string.Equals(callerName, "self", StringComparison.Ordinal))
        {
            resolution = default;
            return false;
        }

        if (try_resolve_inherent_method(currentMethod.owner_type.name_path, currentMethod.namespace_path,
                dot.callee.name,
                get_method_argument_count(dot), out var inherentMethod) &&
            inherentMethod is not null)
        {
            resolution = inherentMethod.to_call_resolution(HirDispatchKind.@static, true);
            return true;
        }

        if (currentMethod.kind == HirMethodKind.imply &&
            try_resolve_imply_method(currentMethod.owner_type.name_path, currentMethod.contract_type?.name_path,
                currentMethod.namespace_path, dot.callee.name, get_method_argument_count(dot), out var implyMethod) &&
            implyMethod is not null)
        {
            resolution = implyMethod.to_call_resolution(HirDispatchKind.@static, true);
            return true;
        }

        if (currentMethod.kind == HirMethodKind.trait &&
            try_resolve_trait_method(currentMethod.owner_type.name_path, currentMethod.namespace_path, dot.callee.name,
                get_method_argument_count(dot), out var traitMethod) &&
            traitMethod is not null &&
            traitMethod.slot_index is { } slotIndex)
        {
            resolution = traitMethod.to_call_resolution(
                HirDispatchKind.witness,
                true,
                currentMethod.owner_type.name_path,
                dot.callee.name);
            return true;
        }

        resolution = default;
        return false;
    }

    private bool try_resolve_receiver_method_call(HirCallable currentCallable, TermDotExpression dot,
        out HirCallResolution? resolution)
    {
        if (!try_resolve_receiver_type(currentCallable, dot.caller, out var receiverType))
        {
            resolution = default;
            return false;
        }

        var receiverTypeName = receiverType.name_path;

        if (try_resolve_inherent_method(receiverTypeName, currentCallable.namespace_path, dot.callee.name,
                get_method_argument_count(dot), out var inherentMethod) &&
            inherentMethod is not null)
        {
            resolution = inherentMethod.to_call_resolution(HirDispatchKind.@static, true);
            return true;
        }

        // 褰撳�?trait 绯荤粺灏氭湭瀹屾暣钀藉湴鏃讹紝鍍?`imply i32 { infix ... }`
        // 杩欐牱鐨勬棤 contract 杩愮畻绗﹀疄鐜板氨鏄寮忕殑闈欐€佸垎娲炬潵婧愩�?
        if (try_resolve_imply_method(receiverTypeName, null, currentCallable.namespace_path, dot.callee.name,
                get_method_argument_count(dot), out var implyMethod) &&
            implyMethod is not null)
        {
            resolution = implyMethod.to_call_resolution(HirDispatchKind.@static, true);
            return true;
        }

        var traitCandidates = resolve_trait_dispatch_candidates(
            receiverTypeName,
            currentCallable.namespace_path,
            dot.callee.name,
            get_method_argument_count(dot));
        if (traitCandidates.Count == 1)
        {
            var candidate = traitCandidates[0];
            resolution = candidate.target_method.to_call_resolution(
                candidate.dispatch,
                true,
                candidate.trait_name);
            return true;
        }

        if (traitCandidates.Count > 1)
        {
            report_dispatch_ambiguity(dot, receiverTypeName.ToString(), dot.callee.name,
                traitCandidates.Select(candidate => candidate.trait_name));
            resolution = default;
            return false;
        }

        if (try_resolve_extension_micro(receiverTypeName, currentCallable.namespace_path, dot.callee.name,
                get_method_argument_count(dot), out var extensionFunction) &&
            extensionFunction is not null)
        {
            resolution = new HirCallResolution(HirDispatchKind.@static, extensionFunction.name, true, null, null, null);
            return true;
        }

        resolution = default;
        return false;
    }

    private bool try_resolve_method_call_on_type(HirCallable currentCallable, HirTypeRef receiverType,
        string methodName,
        int argumentCount, out HirCallResolution? resolution)
    {
        var receiverTypeName = receiverType.name_path;
        var namespaceName = currentCallable.namespace_path;

        if (try_resolve_inherent_method(receiverTypeName, namespaceName, methodName, argumentCount,
                out var inherentMethod) &&
            inherentMethod is not null)
        {
            resolution = inherentMethod.to_call_resolution(HirDispatchKind.@static, true);
            return true;
        }

        if (try_resolve_imply_method(receiverTypeName, null, namespaceName, methodName, argumentCount,
                out var implyMethod) &&
            implyMethod is not null)
        {
            resolution = implyMethod.to_call_resolution(HirDispatchKind.@static, true);
            return true;
        }

        var traitCandidates =
            resolve_trait_dispatch_candidates(receiverTypeName, namespaceName, methodName, argumentCount);
        if (traitCandidates.Count == 1)
        {
            var candidate = traitCandidates[0];
            resolution = candidate.target_method.to_call_resolution(
                candidate.dispatch,
                true,
                candidate.trait_name);
            return true;
        }

        if (traitCandidates.Count > 1)
        {
            resolution = default;
            return false;
        }

        if (try_resolve_extension_micro(receiverTypeName, namespaceName, methodName, argumentCount,
                out var extensionFunction) &&
            extensionFunction is not null)
        {
            resolution = new HirCallResolution(HirDispatchKind.@static, extensionFunction.name, true, null, null, null);
            return true;
        }

        if (try_resolve_generic_bound_method(currentCallable, receiverTypeName, methodName, argumentCount,
                out resolution)) return true;

        resolution = default;
        return false;
    }

    private bool try_resolve_associated_type_binding(HirCallable currentCallable, HirTypeRef ownerType,
        SemanticNameSpace? currentNamespace, SemanticNamePath contractTypeName,
        string associatedTypeName, out HirTypeRef type)
    {
        if (ownerType.named_type_arguments is not null)
        {
            var namedArgument = ownerType.named_type_arguments.FirstOrDefault(candidate =>
                string.Equals(candidate.slot_name, associatedTypeName, StringComparison.Ordinal));
            if (namedArgument is not null)
            {
                type = namedArgument.type;
                return true;
            }
        }

        if (try_find_current_callable_trait_constraint_type(currentCallable, ownerType.name, contractTypeName,
                currentNamespace,
                out var constrainedType))
        {
            var namedArgument = constrainedType.named_type_arguments?.FirstOrDefault(candidate =>
                string.Equals(candidate.slot_name, associatedTypeName, StringComparison.Ordinal));
            if (namedArgument != null)
            {
                type = namedArgument.type;
                return true;
            }
        }

        var contract = resolve_trait(contractTypeName, currentNamespace);
        if (contract is not null &&
            contract.associated_type_names.All(name =>
                !string.Equals(name, associatedTypeName, StringComparison.Ordinal)))
        {
            type = HirTypeRef.unknown();
            return false;
        }

        foreach (var imply in implys)
        {
            if (!type_name_matches(
                    imply.target_type.name_path,
                    qualify_member_name_path(ValkyrieNameSpace.parse(imply.namespace_name), null, imply.target_type.name),
                    ownerType.name_path,
                    currentNamespace))
                continue;

            if (imply.contract_type is null ||
                !type_name_matches(
                    imply.contract_type.name_path,
                    qualify_member_name_path(ValkyrieNameSpace.parse(imply.namespace_name), null, imply.contract_type.name),
                    contractTypeName,
                    currentNamespace))
                continue;

            var binding = imply.type_bindings.FirstOrDefault(candidate =>
                string.Equals(candidate.associated_type_name, associatedTypeName, StringComparison.Ordinal) &&
                (candidate.trait_name is null ||
                 type_name_matches(
                     candidate.trait_name,
                     candidate.trait_name,
                     contractTypeName,
                     currentNamespace)));

            if (binding is null) continue;

            type = binding.concrete_type;
            return true;
        }

        type = HirTypeRef.unknown();
        return false;
    }

    private bool type_refs_compatible(HirTypeRef left, HirTypeRef right, SemanticNameSpace? currentNamespace)
    {
        return is_type_compatible(left.name_path, right.name_path, currentNamespace) ||
               is_type_compatible(right.name_path, left.name_path, currentNamespace);
    }

    private bool try_resolve_static_type_method_call(HirCallable currentCallable, TermDotExpression dot,
        out HirCallResolution? resolution)
    {
        if (!try_get_qualified_term_name(dot.caller, out var callerName) || string.IsNullOrWhiteSpace(callerName))
        {
            resolution = default;
            return false;
        }

        var callerTypeName = ValkyrieNamePath.parse(callerName);
        var ownerTypeName = is_self_type_name(callerTypeName) && currentCallable is HirMethod currentMethod
            ? currentMethod.owner_type.name_path
            : callerTypeName;

        if (has_static_type_member(ownerTypeName, currentCallable.namespace_path, dot.callee.name,
                get_method_argument_count(dot)))
        {
            report_invalid_static_member_syntax(dot, ownerTypeName.ToString(), dot.callee.name);
            resolution = default;
            return false;
        }

        resolution = default;
        return false;
    }

    #endregion

    #region 类型推断与局部解�?

    private static bool try_get_simple_term_name(TermNode term, out string name)
    {
        switch (term)
        {
            case TermLiteralNamePathNode { path.name: { Length: > 0 } pathName }:
                name = pathName;
                return true;
            default:
                name = string.Empty;
                return false;
        }
    }

    private static bool try_get_qualified_term_name(TermNode term, out string name)
    {
        switch (term)
        {
            case TermLiteralNamePathNode { path.full_name: { Length: > 0 } pathName }:
                name = pathName;
                return true;
            case TermDotExpression dot when try_get_qualified_term_name(dot.caller, out var callerName):
                name = $"{callerName}{get_member_access_separator_text(dot.separator_kind)}{dot.callee.full_name}";
                return true;
            default:
                name = string.Empty;
                return false;
        }
    }

    private static bool is_self_value_name(string? name)
    {
        return string.Equals(name, "self", StringComparison.Ordinal);
    }

    private static bool is_self_type_name(SemanticNamePath name)
    {
        return name.@namespace.is_empty && string.Equals(name.name, "Self", StringComparison.Ordinal);
    }

    private bool try_resolve_receiver_type(HirCallable currentCallable, AstNode receiver, out HirTypeRef type)
    {
        var semanticType = semantics.get_type_info(receiver.GetHashCode());
        if (semanticType is not null && !string.IsNullOrWhiteSpace(semanticType.name) && semanticType.name != "?")
        {
            type = HirTypeRef.from_type(semanticType);
            return true;
        }

        switch (receiver)
        {
            case TermCallExpression callExpression
                when try_resolve_call_expression_type(currentCallable, callExpression, out var callType):
                type = callType;
                return true;
            case TermDotExpression { call_body: not null } dotCallExpression
                when try_resolve_call_expression_type(currentCallable, dotCallExpression, out var dotCallType):
                type = dotCallType;
                return true;
            case TermDotExpression { call_body: null } dotExpression
                when try_resolve_receiver_type(currentCallable, dotExpression.caller, out var dotReceiverType):
                if (try_resolve_tuple_ordinal_member_type(dotReceiverType, dotExpression.callee.name,
                        out var tupleMemberType))
                {
                    type = tupleMemberType;
                    return true;
                }

                if (try_resolve_field_member_type(dotReceiverType, currentCallable.namespace_path,
                        dotExpression.callee.name,
                        out var fieldMemberType))
                {
                    type = fieldMemberType;
                    return true;
                }

                type = HirTypeRef.unknown();
                return false;
            case IdentifierNode { name: { Length: > 0 } receiverName }:
                if (is_self_value_name(receiverName) &&
                    currentCallable is HirMethod currentMethodByIdentifier)
                {
                    type = currentMethodByIdentifier.owner_type;
                    return true;
                }

                if (is_self_type_name(ValkyrieNamePath.parse(receiverName)) &&
                    currentCallable is HirMethod ownerMethodByIdentifier)
                {
                    type = ownerMethodByIdentifier.owner_type;
                    return true;
                }

                var identifierParameter = currentCallable.parameters.FirstOrDefault(candidate =>
                    string.Equals(candidate.name, receiverName, StringComparison.Ordinal));
                if (identifierParameter is not null)
                {
                    type = identifierParameter.type;
                    return true;
                }

                if (try_resolve_local_variable_type(currentCallable, receiverName, out type)) return true;

                if (semantics.resolve_symbol(receiverName) is { type: not null } identifierSymbolType &&
                    !string.IsNullOrWhiteSpace(identifierSymbolType.type.name))
                {
                    type = HirTypeRef.from_type(identifierSymbolType.type);
                    return true;
                }

                type = HirTypeRef.unknown();
                return false;
            case TermLiteralNamePathNode { path.name: { Length: > 0 } receiverName }:
                if (is_self_value_name(receiverName) &&
                    currentCallable is HirMethod currentMethod)
                {
                    type = currentMethod.owner_type;
                    return true;
                }

                if (is_self_type_name(ValkyrieNamePath.parse(receiverName)) &&
                    currentCallable is HirMethod ownerMethod)
                {
                    type = ownerMethod.owner_type;
                    return true;
                }

                var parameter = currentCallable.parameters.FirstOrDefault(candidate =>
                    string.Equals(candidate.name, receiverName, StringComparison.Ordinal));
                if (parameter is not null)
                {
                    type = parameter.type;
                    return true;
                }

                if (try_resolve_local_variable_type(currentCallable, receiverName, out type)) return true;

                if (semantics.resolve_symbol(receiverName) is { type: not null } symbolType &&
                    !string.IsNullOrWhiteSpace(symbolType.type.name))
                {
                    type = HirTypeRef.from_type(symbolType.type);
                    return true;
                }

                type = HirTypeRef.unknown();
                return false;
            case TermLiteralObjectNode objectLiteral
                when try_resolve_constructor_name_path(objectLiteral.constructor, out var constructorNamePath):
                type = HirTypeRef.from_name_path(constructorNamePath);
                return true;
            case TermLiteralNumberNode:
                type = HirTypeRef.i32();
                return true;
            case TermLiteralTextNode textLiteral:
                var literalTypeName = textLiteral.literal_kind switch
                {
                    TextLiteralKind.literal_char => ValkyrieTextTypeFacts.char_name,
                    _ => ValkyrieTextTypeFacts.utf8_name
                };
                type = HirTypeRef.from_name_path(ValkyrieNamePath.parse(literalTypeName));
                return true;
            case TermLiteralBooleanNode:
                type = HirTypeRef.@bool();
                return true;
            case LiteralNullNode:
                type = HirTypeRef.@null();
                return true;
            default:
                type = HirTypeRef.unknown();
                return false;
        }
    }

    private bool try_resolve_call_expression_type(HirCallable currentCallable, AstNode callExpression,
        out HirTypeRef type)
    {
        if (try_resolve_call(currentCallable, callExpression, out var resolution) &&
            resolution is not null)
        {
            if (callExpression is TermDotExpression receiverCall &&
                try_resolve_receiver_type(currentCallable, receiverCall.caller, out var receiverType) &&
                try_resolve_specialized_callable_return_type(resolution.target_name, receiverType, out type))
                return true;

            if (try_resolve_callable_return_type(resolution.target_name, out type)) return true;
        }

        if (callExpression is TermDotExpression { call_body: not null } dotCall &&
            try_infer_operator_call_type(currentCallable, dotCall, out type))
            return true;

        type = HirTypeRef.unknown();
        return false;
    }

    private static bool try_resolve_tuple_ordinal_member_type(HirTypeRef receiverType, string memberName,
        out HirTypeRef type)
    {
        if (!receiverType.is_tuple_type ||
            receiverType.tuple_elements is null ||
            !int.TryParse(memberName, out var ordinal) ||
            ordinal <= 0 ||
            ordinal > receiverType.tuple_elements.Count)
        {
            type = HirTypeRef.unknown();
            return false;
        }

        type = receiverType.tuple_elements[ordinal - 1].type;
        return true;
    }

    private bool try_resolve_field_member_type(
        HirTypeRef receiverType,
        SemanticNameSpace? currentNamespace,
        string memberName,
        out HirTypeRef type)
    {
        var resolvedType = resolve_type(receiverType.name_path, currentNamespace, null);
        var typeSymbol = try_resolve_type_symbol(receiverType.name, resolvedType, currentNamespace);
        var semanticField = typeSymbol?.type?.members.FirstOrDefault(candidate =>
            candidate.kind == SymbolKind.property &&
            string.Equals(candidate.name, memberName, StringComparison.Ordinal));
        if (semanticField?.type is not null && !string.IsNullOrWhiteSpace(semanticField.type.name))
        {
            type = HirTypeRef.from_type(semanticField.type);
            return true;
        }

        var dataField = resolvedType?.data_shape?.fields.FirstOrDefault(candidate =>
                            candidate.matches_member_name(memberName)) ??
                        find_data_type(receiverType.name_path, currentNamespace)?.data_shape?.fields
                            .FirstOrDefault(candidate =>
                                candidate.matches_member_name(memberName));
        if (dataField is not null)
        {
            type = dataField.type;
            return true;
        }

        type = HirTypeRef.unknown();
        return false;
    }

    private ISymbol? try_resolve_type_symbol(string receiverTypeName, HirTypeDef? resolvedType,
        SemanticNameSpace? currentNamespace)
    {
        var candidateNames = new List<string>();
        append_type_symbol_candidate(candidateNames, receiverTypeName);
        append_type_symbol_candidate(candidateNames, strip_generic_suffix(receiverTypeName));
        append_type_symbol_candidate(candidateNames, resolvedType?.name);
        append_type_symbol_candidate(candidateNames, resolvedType?.namepath.ToString());

        if (currentNamespace is not null && !currentNamespace.is_empty)
        {
            append_type_symbol_candidate(candidateNames,
                qualify_member_name_path(currentNamespace, null, receiverTypeName).ToString());
            append_type_symbol_candidate(candidateNames,
                qualify_member_name_path(currentNamespace, null, strip_generic_suffix(receiverTypeName))
                    .ToString());
        }

        foreach (var candidateName in candidateNames)
        {
            var symbol = semantics.resolve_symbol(candidateName);
            if (symbol?.type is not null) return symbol;
        }

        return null;
    }

    private static void append_type_symbol_candidate(ICollection<string> candidates, string? candidateName)
    {
        if (string.IsNullOrWhiteSpace(candidateName)) return;

        if (!candidates.Contains(candidateName)) candidates.Add(candidateName);

        var scopeQualifiedName = candidateName.Replace(".", "::", StringComparison.Ordinal);
        if (!candidates.Contains(scopeQualifiedName)) candidates.Add(scopeQualifiedName);
    }

    private static string strip_generic_suffix(string typeName)
    {
        var genericStart = typeName.IndexOf('<');
        return genericStart >= 0 ? typeName[..genericStart] : typeName;
    }

    /// <summary>
    ///     为已经解糖成成员调用的运算符补全返回类型推断�?
    ///     这样链式表达式也能继续参与后续调用解析�?
    /// </summary>
    private bool try_infer_operator_call_type(
        HirCallable currentCallable,
        TermDotExpression dotCall,
        out HirTypeRef type)
    {
        if (!try_resolve_receiver_type(currentCallable, dotCall.caller, out var receiverType))
        {
            type = HirTypeRef.unknown();
            return false;
        }

        var argumentTypeNames = new List<string>();
        foreach (var argument in dotCall.call_body?.term_arguments?.items ?? [])
        {
            if (!try_resolve_expression_type(currentCallable, argument.value, out var argumentType))
            {
                type = HirTypeRef.unknown();
                return false;
            }

            argumentTypeNames.Add(argumentType.name);
        }

        if (!ValkyrieOperatorTypeFacts.try_infer_operator_result_type_name(
                receiverType.name,
                dotCall.callee.name,
                argumentTypeNames,
                out var resultTypeName))
        {
            type = HirTypeRef.unknown();
            return false;
        }

        type = HirTypeRef.from_name_path(ValkyrieNamePath.parse(resultTypeName));
        return true;
    }

    private bool try_resolve_local_variable_type(HirCallable currentCallable, string variableName, out HirTypeRef type)
    {
        if (currentCallable.body is null)
        {
            type = HirTypeRef.unknown();
            return false;
        }

        foreach (var statement in currentCallable.body.statements)
        {
            if (statement is not LetDeclaration declaration ||
                !string.Equals(declaration.name?.name, variableName, StringComparison.Ordinal))
                continue;

            if (declaration.var_type is not null)
            {
                type = get_constraint_type_ref(declaration.var_type);
                return true;
            }

            if (declaration.initializer is TermLiteralObjectNode objectLiteral &&
                try_resolve_constructor_name_path(objectLiteral.constructor, out var constructorNamePath))
            {
                type = HirTypeRef.from_name_path(constructorNamePath);
                return true;
            }

            if (declaration.initializer is TermNode initializer &&
                try_resolve_receiver_type(currentCallable, initializer, out type))
                return true;
        }

        type = HirTypeRef.unknown();
        return false;
    }

    private static bool try_resolve_constructor_name_path(AstNode constructor, out SemanticNamePath namePath)
    {
        switch (constructor)
        {
            case IdentifierNode { name: { Length: > 0 } identifierName }:
                namePath = ValkyrieNamePath.parse(identifierName);
                return true;
            case QualifiedPathNode { full_name: { Length: > 0 } qualifiedName }:
                namePath = ValkyrieNamePath.parse(qualifiedName);
                return true;
            case TypeLiteralNamePathNode { path.full_name: { Length: > 0 } typePathName }:
                namePath = ValkyrieNamePath.parse(typePathName);
                return true;
            case TermLiteralNamePathNode { path.full_name: { Length: > 0 } literalName }:
                namePath = ValkyrieNamePath.parse(literalName);
                return true;
            default:
                namePath = ValkyrieNamePath.parse(null);
                return false;
        }
    }

    private bool try_resolve_callable_return_type(string targetName, out HirTypeRef type)
    {
        var callable = enumerate_callables().FirstOrDefault(candidate =>
            string.Equals(candidate.name, targetName, StringComparison.Ordinal));
        if (callable is not null)
        {
            type = callable.return_type;
            return true;
        }

        type = HirTypeRef.unknown();
        return false;
    }

    private bool try_resolve_specialized_callable_return_type(string targetName, HirTypeRef receiverType,
        out HirTypeRef type)
    {
        var callable = enumerate_callables().FirstOrDefault(candidate =>
            string.Equals(candidate.name, targetName, StringComparison.Ordinal));
        if (callable is not HirMethod method) return try_resolve_callable_return_type(targetName, out type);

        type = specialize_method_return_type(method, receiverType);
        return true;
    }

    private HirTypeRef specialize_method_return_type(HirMethod method, HirTypeRef receiverType)
    {
        var bindings = new Dictionary<string, HirTypeRef>(StringComparer.Ordinal);
        collect_type_parameter_bindings(method.owner_type, receiverType, bindings);
        if (bindings.Count == 0) return method.return_type;

        return substitute_type_parameters(method.return_type, bindings);
    }

    private void collect_type_parameter_bindings(HirTypeRef formalType, HirTypeRef actualType,
        Dictionary<string, HirTypeRef> bindings)
    {
        if (formalType.is_special || actualType.is_special) return;

        if ((formalType.type_arguments is null || formalType.type_arguments.Count == 0) &&
            (formalType.named_type_arguments is null || formalType.named_type_arguments.Count == 0) &&
            (formalType.tuple_elements is null || formalType.tuple_elements.Count == 0) &&
            !is_known_concrete_type_name(formalType.name_path))
        {
            bindings.TryAdd(formalType.name, actualType);
            return;
        }

        if (formalType.type_arguments is not null &&
            actualType.type_arguments is not null &&
            formalType.type_arguments.Count == actualType.type_arguments.Count)
            for (var i = 0; i < formalType.type_arguments.Count; i++)
                collect_type_parameter_bindings(formalType.type_arguments[i], actualType.type_arguments[i], bindings);

        if (formalType.named_type_arguments is not null &&
            actualType.named_type_arguments is not null)
            foreach (var formalBinding in formalType.named_type_arguments)
            {
                var actualBinding = actualType.named_type_arguments.FirstOrDefault(candidate =>
                    string.Equals(candidate.slot_name, formalBinding.slot_name, StringComparison.Ordinal));
                if (actualBinding is not null)
                    collect_type_parameter_bindings(formalBinding.type, actualBinding.type, bindings);
            }

        if (formalType.tuple_elements is null || actualType.tuple_elements is null ||
            formalType.tuple_elements.Count != actualType.tuple_elements.Count)
            return;

        for (var i = 0; i < formalType.tuple_elements.Count; i++)
            collect_type_parameter_bindings(formalType.tuple_elements[i].type, actualType.tuple_elements[i].type,
                bindings);
    }

    private static HirTypeRef substitute_type_parameters(HirTypeRef type,
        IReadOnlyDictionary<string, HirTypeRef> bindings)
    {
        if (type.is_special) return type;

        if ((type.type_arguments is null || type.type_arguments.Count == 0) &&
            (type.named_type_arguments is null || type.named_type_arguments.Count == 0) &&
            (type.tuple_elements is null || type.tuple_elements.Count == 0) &&
            bindings.TryGetValue(type.name, out var boundType))
            return boundType;

        IReadOnlyList<HirTypeRef>? positionalTypeArguments = null;
        if (type.type_arguments is not null && type.type_arguments.Count > 0)
            positionalTypeArguments =
            [
                .. type.type_arguments
                    .Select(argument => substitute_type_parameters(argument, bindings))
            ];

        IReadOnlyList<HirTypeArgumentBinding>? namedTypeArguments = null;
        if (type.named_type_arguments is not null && type.named_type_arguments.Count > 0)
            namedTypeArguments =
            [
                .. type.named_type_arguments
                    .Select(binding => new HirTypeArgumentBinding(
                        binding.slot_name,
                        substitute_type_parameters(binding.type, bindings)))
            ];

        IReadOnlyList<HirTupleElementRef>? tupleElements = null;
        if (type.tuple_elements is not null && type.tuple_elements.Count > 0)
            tupleElements =
            [
                .. type.tuple_elements
                    .Select(element => new HirTupleElementRef(
                        element.label,
                        substitute_type_parameters(element.type, bindings)))
            ];

        return new HirTypeRef(type.name, type.special_kind, positionalTypeArguments, namedTypeArguments, tupleElements);
    }

    #endregion


    #region 分派查找

    private bool try_resolve_inherent_method(SemanticNamePath ownerTypeName, SemanticNameSpace? currentNamespace,
        string memberName,
        int argumentCount, out HirMethod? method)
    {
        method = resolve_type_hierarchy(ownerTypeName, currentNamespace)
            .SelectMany(type => type.methods)
            .FirstOrDefault(candidate =>
                candidate.matches_member_name(memberName) &&
                candidate.matches_instance_arity(argumentCount))!;
        return method is not null;
    }

    private bool try_resolve_trait_method(SemanticNamePath ownerTypeName, SemanticNameSpace? currentNamespace, string memberName,
        int argumentCount, out HirMethod? method)
    {
        method = resolve_trait_hierarchy(ownerTypeName, currentNamespace)
            .SelectMany(trait => trait.methods)
            .FirstOrDefault(candidate =>
                candidate.matches_member_name(memberName) &&
                candidate.matches_instance_arity(argumentCount))!;
        return method is not null;
    }

    private bool try_resolve_imply_method(
        SemanticNamePath ownerTypeName,
        SemanticNamePath? contractTypeName,
        SemanticNameSpace? currentNamespace,
        string memberName,
        int argumentCount,
        out HirMethod? method)
    {
        method = implys
            .Where(candidate =>
                candidate.matches_target_type(ownerTypeName, currentNamespace) &&
                (contractTypeName is null || candidate.matches_contract_type(contractTypeName, currentNamespace)))
            .SelectMany(candidate => candidate.methods)
            .FirstOrDefault(candidate =>
                candidate.matches_member_name(memberName) &&
                candidate.matches_instance_arity(argumentCount))!;
        return method is not null;
    }

    private bool try_resolve_extension_micro(
        SemanticNamePath receiverTypeName,
        SemanticNameSpace? currentNamespace,
        string memberName,
        int argumentCount,
        out HirFunction? function)
    {
        var candidates = functions
            .Where(candidate =>
                matches_simple_name(candidate.name, memberName) &&
                candidate.parameters.Count == argumentCount + 1 &&
                is_type_compatible(receiverTypeName, candidate.parameters[0].type.name_path, currentNamespace))
            .ToArray();
        function = candidates.Length == 1 ? candidates[0] : null;
        return function is not null;
    }

    private List<HirResolvedTraitCandidate> resolve_trait_dispatch_candidates(
        SemanticNamePath receiverTypeName,
        SemanticNameSpace? currentNamespace,
        string memberName,
        int argumentCount)
    {
        var candidates = new List<HirResolvedTraitCandidate>();
        var seenTraits = new HashSet<SemanticNamePath>();

        foreach (var trait in resolve_satisfied_traits(receiverTypeName, currentNamespace))
        {
            if (!seenTraits.Add(trait.name_path)) continue;

            if (try_resolve_trait_dispatch_target(receiverTypeName, currentNamespace, trait, memberName, argumentCount,
                    out var candidate) &&
                candidate is not null)
                candidates.Add(candidate);
        }

        return candidates;
    }

    private List<HirResolvedTraitCandidate> resolve_explicit_trait_dispatch_candidates(
        SemanticNamePath receiverTypeName,
        SemanticNameSpace? currentNamespace,
        HirTraitDef requiredTrait,
        HirMethod requiredMethod)
    {
        if (try_resolve_inherent_method(receiverTypeName, currentNamespace, requiredMethod.member_name,
                requiredMethod.parameters.Count - 1, out var inherentMethod) &&
            inherentMethod is not null &&
            inherentMethod.semantically_matches_signature(requiredMethod))
            return
            [
                new HirResolvedTraitCandidate(requiredTrait.name_path, inherentMethod, HirDispatchKind.@static)
            ];

        var candidates = new List<HirResolvedTraitCandidate>();
        var seenTargets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in resolve_explicit_imply_shape_candidates(receiverTypeName, currentNamespace,
                     requiredMethod))
            if (seenTargets.Add(candidate.target_method.name))
                candidates.Add(candidate);

        return candidates;
    }

    private IEnumerable<HirResolvedTraitCandidate> resolve_explicit_imply_shape_candidates(
        SemanticNamePath receiverTypeName,
        SemanticNameSpace? currentNamespace,
        HirMethod requiredMethod)
    {
        foreach (var imply in implys.Where(candidate =>
                     candidate.source_kind == HirImplySourceKind.explicit_declaration &&
                     candidate.matches_target_type(receiverTypeName, currentNamespace)))
        {
            var method = imply.methods.FirstOrDefault(candidate =>
                candidate.matches_member_name(requiredMethod.member_name) &&
                candidate.semantically_matches_signature(requiredMethod));
            if (method is null) continue;

            var providerName = imply.contract_type is null
                ? receiverTypeName
                : resolve_trait(imply.contract_type.name_path,
                      string.IsNullOrWhiteSpace(imply.namespace_name)
                          ? currentNamespace
                          : ValkyrieNameSpace.parse(imply.namespace_name), null)?.name_path
                  ?? imply.contract_type.name_path;
            yield return new HirResolvedTraitCandidate(providerName, method, HirDispatchKind.@static);
        }
    }

    private IEnumerable<HirTraitDef> resolve_satisfied_traits(SemanticNamePath receiverTypeName,
        SemanticNameSpace? currentNamespace)
    {
        var explicitTraits = implys
            .Where(candidate =>
                candidate.contract_type is not null &&
                candidate.matches_target_type(receiverTypeName, currentNamespace))
            .Select(candidate =>
                resolve_trait(candidate.contract_type!.name_path,
                    string.IsNullOrWhiteSpace(candidate.namespace_name)
                        ? currentNamespace
                        : ValkyrieNameSpace.parse(candidate.namespace_name), null))
            .Where(trait => trait is not null)
            .SelectMany(trait => resolve_trait_hierarchy(trait!.name_path, currentNamespace));

        var structuralTraits = traits
            .Where(trait => can_satisfy_trait(receiverTypeName, currentNamespace, trait))
            .SelectMany(trait => resolve_trait_hierarchy(trait.name_path, currentNamespace));

        return explicitTraits
            .Concat(structuralTraits)
            .GroupBy(trait => trait.name_path)
            .Select(group => group.First());
    }

    private bool can_satisfy_trait(SemanticNamePath receiverTypeName, SemanticNameSpace? currentNamespace, HirTraitDef trait)
    {
        var traitMethods = resolve_trait_contract_methods(trait, currentNamespace);
        foreach (var traitMethod in traitMethods)
        {
            if (try_resolve_inherent_method(receiverTypeName, currentNamespace, traitMethod.member_name,
                    traitMethod.parameters.Count - 1, out var inherentMethod) &&
                inherentMethod is not null &&
                inherentMethod.semantically_matches_signature(traitMethod))
                continue;

            if (try_resolve_imply_method(receiverTypeName, trait.name_path, currentNamespace,
                    traitMethod.member_name, traitMethod.parameters.Count - 1, out var implyMethod) &&
                implyMethod is not null &&
                implyMethod.semantically_matches_signature(traitMethod))
                continue;

            if (traitMethod.body is not null) continue;

            return false;
        }

        return true;
    }

    private bool try_resolve_trait_dispatch_target(
        SemanticNamePath receiverTypeName,
        SemanticNameSpace? currentNamespace,
        HirTraitDef trait,
        string memberName,
        int argumentCount,
        out HirResolvedTraitCandidate? candidate)
    {
        if (try_resolve_imply_method(receiverTypeName, trait.name_path, currentNamespace,
                memberName,
                argumentCount, out var implyMethod) &&
            implyMethod is not null)
        {
            candidate = new HirResolvedTraitCandidate(trait.name_path, implyMethod, HirDispatchKind.@static);
            return true;
        }

        if (try_resolve_trait_method(trait.name_path, currentNamespace, memberName,
                argumentCount,
                out var traitMethod) &&
            traitMethod is not null &&
            traitMethod.body is not null)
        {
            candidate = new HirResolvedTraitCandidate(trait.name_path, traitMethod, HirDispatchKind.@static);
            return true;
        }

        candidate = null;
        return false;
    }

    private IReadOnlyList<HirMethod> resolve_trait_contract_methods(HirTraitDef trait, SemanticNameSpace? currentNamespace)
    {
        return trait.enumerate_contract_methods(baseTraitName =>
            resolve_trait_targets(baseTraitName, currentNamespace));
    }

    private IEnumerable<HirTypeDef> resolve_type_hierarchy(SemanticNamePath ownerTypeName, SemanticNameSpace? currentNamespace)
    {
        var resolvedType = resolve_type(ownerTypeName, currentNamespace);
        return resolvedType?.resolve_hierarchy(baseTypeName => resolve_type(baseTypeName, currentNamespace)) ?? [];
    }

    private IEnumerable<HirTraitDef> resolve_trait_hierarchy(SemanticNamePath traitName, SemanticNameSpace? currentNamespace)
    {
        var results = new List<HirTraitDef>();
        var visited = new HashSet<SemanticNamePath>();
        foreach (var trait in resolve_trait_targets(traitName, currentNamespace))
            trait.collect_hierarchy(baseTraitName => resolve_trait_targets(baseTraitName, currentNamespace), results,
                visited);

        return results;
    }

    private HirTypeDef? resolve_type(SemanticNamePath typeName, SemanticNameSpace? currentNamespace)
    {
        return resolve_type(typeName, currentNamespace, null);
    }

    private HirTraitDef? resolve_trait(SemanticNamePath traitName, SemanticNameSpace? currentNamespace)
    {
        return resolve_trait(traitName, currentNamespace, null);
    }

    private bool is_type_compatible(SemanticNamePath sourceTypeName, SemanticNamePath targetTypeName,
        SemanticNameSpace? currentNamespace)
    {
        var sourceCanonicalName = sourceTypeName.ToString();
        var targetCanonicalName = targetTypeName.ToString();

        var sourceIsTextLike = ValkyrieTextTypeFacts.is_text_like_name(sourceCanonicalName)
                               || ValkyrieTextTypeFacts.is_owned_text_class_name(sourceCanonicalName);
        var targetIsTextLike = ValkyrieTextTypeFacts.is_text_like_name(targetCanonicalName)
                               || ValkyrieTextTypeFacts.is_owned_text_class_name(targetCanonicalName);
        if (sourceIsTextLike && targetIsTextLike) return true;

        if (type_name_matches(targetTypeName, targetTypeName, sourceTypeName, currentNamespace)) return true;

        return resolve_type_hierarchy(sourceTypeName, currentNamespace)
            .Skip(1)
            .Any(baseType =>
                type_name_matches(
                    new([baseType.name]),
                    baseType.namepath,
                    targetTypeName,
                    currentNamespace));
    }

    private bool has_static_type_member(
        SemanticNamePath ownerTypeName,
        SemanticNameSpace? currentNamespace,
        string memberName,
        int argumentCount)
    {
        var ownerType = resolve_type(ownerTypeName, currentNamespace);
        if (ownerType is not null &&
            ownerType.methods.Any(candidate =>
                candidate.matches_member_name(memberName) &&
                candidate.matches_explicit_arity(argumentCount)))
            return true;

        return implys
            .Where(candidate =>
                candidate.matches_target_type(ownerTypeName, currentNamespace) &&
                candidate.contract_type is null)
            .SelectMany(candidate => candidate.methods)
            .Any(candidate =>
                candidate.matches_member_name(memberName) &&
                candidate.matches_explicit_arity(argumentCount));
    }

    private HirFunction? select_best_function_overload(
        HirCallable currentCallable,
        IEnumerable<HirFunction> candidates,
        IReadOnlyList<TermArgumentItem> callArguments)
    {
        return candidates
            .Select(candidate => new
            {
                candidate,
                score = score_parameter_match(currentCallable, candidate.parameters, callArguments, 0)
            })
            .Where(item => item.score >= 0)
            .OrderByDescending(item => item.score)
            .Select(item => item.candidate)
            .FirstOrDefault();
    }

    private HirMethod? select_best_method_overload(
        HirCallable currentCallable,
        IEnumerable<HirMethod> candidates,
        IReadOnlyList<TermArgumentItem> callArguments,
        int parameterOffset)
    {
        return candidates
            .Select(candidate => new
            {
                candidate,
                score = score_parameter_match(currentCallable, candidate.parameters, callArguments, parameterOffset)
            })
            .Where(item => item.score >= 0)
            .OrderByDescending(item => item.score)
            .Select(item => item.candidate)
            .FirstOrDefault();
    }

    private int score_parameter_match(
        HirCallable currentCallable,
        IReadOnlyList<HirSymbolRef> parameters,
        IReadOnlyList<TermArgumentItem> callArguments,
        int parameterOffset)
    {
        if (parameters.Count != callArguments.Count + parameterOffset) return -1;

        var score = 0;
        for (var index = 0; index < callArguments.Count; index++)
        {
            if (!try_resolve_expression_type(currentCallable, callArguments[index].value, out var argumentType))
                continue;

            var parameterType = parameters[index + parameterOffset].type;
            if (argumentType.semantically_equals(parameterType))
            {
                score += 2;
                continue;
            }

            if (is_type_compatible(argumentType, parameterType.name_path, currentCallable.namespace_path))
            {
                score += 1;
                continue;
            }

            return -1;
        }

        return score;
    }

    private static int get_method_argument_count(TermDotExpression dot)
    {
        return dot.call_body?.term_arguments?.items.Count ?? 0;
    }

    private static int get_call_argument_count(AstNode node)
    {
        return node switch
        {
            TermCallExpression call => call.call_body.term_arguments?.items.Count ?? 0,
            TermDotExpression dot => get_method_argument_count(dot),
            _ => 0
        };
    }

    private static IReadOnlyList<TermArgumentItem> get_call_arguments(AstNode node)
    {
        return node switch
        {
            TermCallExpression call => call.call_body.term_arguments?.items ?? [],
            TermDotExpression dot => dot.call_body?.term_arguments?.items ?? [],
            _ => []
        };
    }

    private static bool matches_simple_name(string qualifiedName, string memberName)
    {
        var baseName = extract_callable_base_name(qualifiedName);
        return string.Equals(baseName, memberName, StringComparison.Ordinal) ||
               baseName.EndsWith("." + memberName, StringComparison.Ordinal);
    }

    private static bool callable_name_matches(string candidateName, string expectedName)
    {
        return string.Equals(extract_callable_base_name(candidateName), expectedName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断是否存在同名自由函数，供 `MIR` lowering 阶段识别命名空间限定的静态调用�?
    /// </summary>
    public bool has_function_name(string expectedName, int argumentCount)
    {
        return functions.Any(candidate =>
            candidate.parameters.Count == argumentCount &&
            callable_name_matches(candidate.name, expectedName));
    }

    private static string extract_callable_base_name(string callableName)
    {
        var overloadSuffixIndex = callableName.IndexOf("__ovl_", StringComparison.Ordinal);
        return overloadSuffixIndex >= 0 ? callableName[..overloadSuffixIndex] : callableName;
    }

    private void report_unimplemented_qualified_trait_warning(
        HirCallable currentCallable,
        AstNode node,
        HirTraitDef trait,
        string receiverTypeName,
        string memberName,
        SemanticNameSpace? currentNamespace)
    {
        if (has_explicit_trait_implementation(trait, currentNamespace) ||
            current_callable_has_trait_bound(currentCallable, receiverTypeName, trait, currentNamespace) ||
            type_name_matches(
                new([trait.name]),
                trait.name_path,
                ValkyrieNamePath.parse(receiverTypeName),
                currentNamespace))
            return;

        semantics.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.warning,
            $"限定 trait 调用 `{trait.name}::{memberName}` 当前仅作为结构约束使用；trait `{trait.name}` 尚无显式实现，也不是由参�?trait bound 直接限定得到�?,
            build_source_span(node),
            "VALK_TRAIT_SHAPE_ONLY",
            semantics.file_path));
    }

    private bool has_explicit_trait_implementation(HirTraitDef trait, SemanticNameSpace? currentNamespace)
    {
        return implys.Any(candidate =>
            candidate is { source_kind: HirImplySourceKind.explicit_declaration, contract_type: not null } &&
            candidate.matches_contract_type(trait.name_path, currentNamespace));
    }

    private bool current_callable_has_trait_bound(
        HirCallable currentCallable,
        string receiverTypeName,
        HirTraitDef trait,
        SemanticNameSpace? currentNamespace)
    {
        return try_find_current_callable_trait_constraint_type(
            currentCallable,
            receiverTypeName,
            trait.name_path,
            currentNamespace,
            out _);
    }

    private static IReadOnlyList<GenericConstraint> get_current_callable_generic_constraints(
        HirCallable currentCallable)
    {
        return currentCallable switch
        {
            HirFunction function => ((FunctionDecl)function.syntax).generic_constraints,
            _ => []
        };
    }

    private bool try_resolve_generic_bound_method(
        HirCallable currentCallable,
        string receiverTypeName,
        string methodName,
        int argumentCount,
        out HirCallResolution? resolution)
    {
        var matches = new List<(HirTraitDef trait, HirMethod method)>();
        foreach (var constraintType in enumerate_current_callable_trait_constraints(currentCallable, receiverTypeName))
        {
            var trait = resolve_trait(constraintType.name_path, currentCallable.namespace_path);
            if (trait is null) continue;

            var method = resolve_trait_contract_methods(trait, currentCallable.namespace_path)
                .FirstOrDefault(candidate =>
                    candidate.matches_member_name(methodName) &&
                    candidate.matches_instance_arity(argumentCount));
            if (method is null) continue;

            matches.Add((trait, method));
        }

        if (matches.Count == 1)
        {
            var match = matches[0];
            resolution = match.method.to_call_resolution(
                HirDispatchKind.witness,
                true,
                match.trait.name);
            return true;
        }

        resolution = default;
        return false;
    }

    private bool try_find_current_callable_trait_constraint_type(
        HirCallable currentCallable,
        string receiverTypeName,
        SemanticNamePath contractTypeName,
        SemanticNameSpace? currentNamespace,
        out HirTypeRef type)
    {
        foreach (var constraintType in enumerate_current_callable_trait_constraints(currentCallable, receiverTypeName))
        {
            if (!type_name_matches(
                    contractTypeName,
                    contractTypeName,
                    constraintType.name_path,
                    currentNamespace))
                continue;

            type = constraintType;
            return true;
        }

        type = HirTypeRef.unknown();
        return false;
    }

    private IEnumerable<HirTypeRef> enumerate_current_callable_trait_constraints(
        HirCallable currentCallable,
        string receiverTypeName)
    {
        foreach (var constraint in get_current_callable_generic_constraints(currentCallable))
        {
            if (!string.Equals(constraint.parameter_name, receiverTypeName, StringComparison.Ordinal)) continue;

            foreach (var constraintType in constraint.constraint_types)
                yield return get_constraint_type_ref(constraintType);
        }
    }

    private static HirTypeRef get_constraint_type_ref(TypeNode? typeNode, string fallback = "unknown")
    {
        return typeNode switch
        {
            TypeLiteralNamePathNode literal => get_constraint_named_type_ref(literal),
            null => HirTypeRef.from_name_path(ValkyrieNamePath.parse(fallback)),
            _ => HirTypeRef.from_name_path(ValkyrieNamePath.parse(get_type_name(typeNode, fallback)))
        };
    }

    private static HirTypeRef get_constraint_named_type_ref(TypeLiteralNamePathNode literal)
    {
        var typeRef = HirTypeRef.from_name_path(ValkyrieNamePath.parse(literal.path.full_name));
        if (literal.type_arguments is null || literal.type_arguments.items.Count == 0) return typeRef;

        var positionalTypeArguments = literal.type_arguments.items
            .Where(item => item.slot is null)
            .Select(item => get_constraint_type_ref(item.argument))
            .ToArray();
        if (positionalTypeArguments.Length > 0) typeRef = typeRef.with_type_arguments(positionalTypeArguments);

        var namedTypeArguments = literal.type_arguments.items
            .Where(item => item.slot is not null)
            .Select(item => new HirTypeArgumentBinding(
                item.slot!.name,
                get_constraint_type_ref(item.argument)))
            .ToArray();
        if (namedTypeArguments.Length > 0) typeRef = typeRef.with_named_type_arguments(namedTypeArguments);

        return typeRef;
    }

    private void report_dispatch_ambiguity(AstNode node, string receiverTypeName, string memberName,
        IEnumerable<SemanticNamePath> traitNames)
    {
        var orderedTraits = traitNames
            .Select(name => name.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (orderedTraits.Length < 2) return;

        semantics.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"类型 `{receiverTypeName}` �?`{memberName}` 调用�?trait `{string.Join("`, `", orderedTraits)}` 之间存在歧义�?,
            build_source_span(node),
            "VALK_TRAIT_AMBIGUOUS",
            semantics.file_path));
    }

    private void report_loop_item_constraint_mismatch(AstNode node, string leftSource, string leftTypeName,
        string rightSource, string rightTypeName)
    {
        semantics.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"`loop ... in ...` �?`Item` 约束不一致：{leftSource} = `{leftTypeName}`，但 {rightSource} = `{rightTypeName}`�?,
            build_source_span(node),
            "VALK_LOOP_ITEM_MISMATCH",
            semantics.file_path));
    }

    private static SourceSpan build_source_span(AstNode node)
    {
        return new SourceSpan(string.Empty, 0, node.span.start, 0, node.span.end);
    }

    private static string get_type_name(TypeNode? typeNode, string fallback = "unknown")
    {
        return typeNode switch
        {
            TypeLiteralNamePathNode literal =>
                ValkyrieTextTypeFacts.normalize_display_type_name(literal.path.full_name),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.product } product => get_product_type_name(product,
                fallback),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.or } union => join_type_names(union,
                TypeBinaryOperator.or, " | ", fallback),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.and } intersection => join_type_names(intersection,
                TypeBinaryOperator.and, " & ", fallback),
            TypeExpressionUnaryNode { @operator: TypeUnaryOperator.nullable } nullable =>
                $"{get_type_name(nullable.operand, fallback)}?",
            null => fallback,
            _ => typeNode.ToString() ?? fallback
        };
    }

    private static string get_product_type_name(TypeExpressionBinaryNode productNode, string fallback)
    {
        var items = flatten_type_binary(productNode, TypeBinaryOperator.product);
        if (items.Count == 0) return fallback;

        if (items[0] is TypeLiteralNamePathNode constructor)
            return ValkyrieTextTypeFacts.normalize_display_type_name(constructor.path.full_name);

        return get_type_name(items[0], fallback);
    }

    private static string join_type_names(TypeExpressionBinaryNode node, TypeBinaryOperator op, string separator,
        string fallback)
    {
        var items = flatten_type_binary(node, op)
            .Select(item => get_type_name(item, fallback))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        return items.Length > 0 ? string.Join(separator, items) : fallback;
    }

    private static IReadOnlyList<TypeNode> flatten_type_binary(
        TypeNode node,
        TypeBinaryOperator op)
    {
        var items = new List<TypeNode>();
        collect_type_binary(node, op, items);
        return items;
    }

    private static void collect_type_binary(
        TypeNode node,
        TypeBinaryOperator op,
        ICollection<TypeNode> items)
    {
        if (node is TypeExpressionBinaryNode binary && binary.@operator == op)
        {
            collect_type_binary(binary.lhs, op, items);
            collect_type_binary(binary.rhs, op, items);
            return;
        }

        items.Add(node);
    }

    #endregion
}
