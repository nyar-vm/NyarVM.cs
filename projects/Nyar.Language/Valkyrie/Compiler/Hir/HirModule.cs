using System.Collections;
using System.Reflection;
using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     高层中间表示模块�?
///     当前承载已解析的 `AST`、`SemanticModel` 以及 `trait/imply` 结构化语义�?
/// </summary>
public sealed partial class HirModule
{
    #region 构造与枚举

    public HirModule(
        string name,
        CompilationUnit syntax,
        IReadOnlyList<CompilationUnit> syntaxUnits,
        SemanticModel semantics,
        IReadOnlyList<HirFunction> functions,
        IReadOnlyList<HirLetDefs> lets,
        IReadOnlyList<HirTypeDef> types,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTypeAliasRef> typeAliases,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        IReadOnlyList<HirImplyDef> implys)
    {
        this.name = name;
        this.syntax = syntax;
        syntax_units = syntaxUnits;
        this.semantics = semantics;
        this.functions = functions;
        this.lets = lets;
        this.types = types;
        this.traits = traits;
        type_aliases = typeAliases;
        trait_aliases = traitAliases;
        this.implys = implys;
    }

    public string name { get; }

    public CompilationUnit syntax { get; }

    public IReadOnlyList<CompilationUnit> syntax_units { get; }

    public SemanticModel semantics { get; }

    /// <summary>
    ///     顶层自由函数�?
    /// </summary>
    public IReadOnlyList<HirFunction> functions { get; }

    /// <summary>
    ///     顶层 `let` 定义�?
    /// </summary>
    public IReadOnlyList<HirLetDefs> lets { get; }

    /// <summary>
    ///     类型定义及其固有方法�?
    /// </summary>
    public IReadOnlyList<HirTypeDef> types { get; }

    /// <summary>
    ///     `trait` 定义及其方法槽�?
    /// </summary>
    public IReadOnlyList<HirTraitDef> traits { get; }

    /// <summary>
    ///     `type X = ...` 形式的类型别名�?
    /// </summary>
    public IReadOnlyList<HirTypeAliasRef> type_aliases { get; }

    /// <summary>
    ///     `trait A = B + C` 形式�?trait 别名�?
    /// </summary>
    public IReadOnlyList<HirTraitAliasRef> trait_aliases { get; }

    /// <summary>
    ///     `imply` 块及其方法实现�?
    /// </summary>
    public IReadOnlyList<HirImplyDef> implys { get; }

    /// <summary>
    ///     按名称解析类型别名�?
    /// </summary>
    public HirTypeAliasRef? resolve_type_alias(SemanticNamePath aliasName, SemanticNameSpace? currentNamespace = null)
    {
        return type_aliases.FirstOrDefault(alias =>
            type_name_matches(alias.simple_name_path, alias.name_path, aliasName, currentNamespace));
    }

    /// <summary>
    ///     按名称解�?trait 别名�?
    /// </summary>
    public HirTraitAliasRef? resolve_trait_alias(SemanticNamePath aliasName, SemanticNameSpace? currentNamespace = null)
    {
        return trait_aliases.FirstOrDefault(alias =>
            type_name_matches(alias.simple_name_path, alias.name_path, aliasName, currentNamespace));
    }

    /// <summary>
    ///     按名称解析顶�?`let` 定义�?
    /// </summary>
    public HirLetDefs? resolve_let(SemanticNamePath letName, SemanticNameSpace? currentNamespace = null)
    {
        return lets.FirstOrDefault(let => let.matches_name(letName, currentNamespace));
    }

    /// <summary>
    ///     枚举所有带 `[data]` 形状的类型定义�?
    /// </summary>
    public IReadOnlyList<HirTypeDef> enumerate_data_types()
    {
        return [.. types.Where(type => type.is_data_type)];
    }

    /// <summary>
    ///     按类型名解析�?`[data]` 形状的类型定义�?
    /// </summary>
    public HirTypeDef? find_data_type(SemanticNamePath typeName, SemanticNameSpace? currentNamespace = null)
    {
        var resolved = resolve_type(typeName, currentNamespace);
        return resolved?.data_shape is null ? null : resolved;
    }

    /// <summary>
    ///     解析指定类型的可序列化字段视图�?
    /// </summary>
    public IReadOnlyList<HirDataField> enumerate_serializable_fields(SemanticNamePath typeName,
        SemanticNameSpace? currentNamespace = null)
    {
        return find_data_type(typeName, currentNamespace)?.data_shape?.enumerate_serializable_fields() ?? [];
    }

    /// <summary>
    ///     按输入字段名解析反序列化目标字段�?
    /// </summary>
    public HirDataField? find_deserializable_field(SemanticNamePath typeName, string fieldName,
        SemanticNameSpace? currentNamespace = null)
    {
        return find_data_type(typeName, currentNamespace)?.data_shape?.find_deserializable_field(fieldName);
    }

    /// <summary>
    ///     为后续阶段枚举所有可执行�?callable�?
    ///     这里的展开只是代码生成视图，不等同于源级声明已经被拍平�?
    /// </summary>
    public IReadOnlyList<HirCallable> enumerate_callables()
    {
        var callables = new List<HirCallable>(functions.Count);
        callables.AddRange(functions);

        foreach (var type in types) callables.AddRange(type.methods.Where(method => method.body is not null));

        foreach (var trait in traits) callables.AddRange(trait.methods.Where(method => method.body is not null));

        foreach (var imply in implys) callables.AddRange(imply.methods.Where(method => method.body is not null));

        return callables;
    }

    /// <summary>
    ///     为当�?`AOT` 代码生成枚举可直接发射的 callable�?
    ///     `trait` 默认方法仍保留在 `HIR` 语义层，�?`witness-capable` 后端接通后再进入运行时代码生成�?
    /// </summary>
    public IReadOnlyList<HirCallable> enumerate_aot_callables()
    {
        var callables = new List<HirCallable>(functions.Count);
        callables.AddRange(functions.Where(can_emit_aot_callable));

        foreach (var type in types)
            callables.AddRange(type.methods.Where(method => method.body is not null && can_emit_aot_callable(method)));

        foreach (var imply in implys)
            callables.AddRange(imply.methods.Where(method => method.body is not null && can_emit_aot_callable(method)));

        return callables;
    }

    /// <summary>
    ///     从逻辑入口出发，收集当前构建真正可达的 `AOT` callable�?
    ///     若未命中任何逻辑入口，则回退为全�?`AOT` callable�?
    /// </summary>
    public IReadOnlyList<HirCallable> enumerate_reachable_aot_callables(string? preferredLogicalEntry = null)
    {
        var allCallables = enumerate_aot_callables();
        var logicalEntries = allCallables
            .Where(callable => callable.is_logical_entry &&
                               logical_entry_matches(callable.name, preferredLogicalEntry))
            .ToArray();

        if (logicalEntries.Length == 0) return allCallables;

        var callableMap = allCallables
            .GroupBy(callable => callable.name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var reachableNames =
            new HashSet<string>(logicalEntries.Select(callable => callable.name), StringComparer.Ordinal);
        var pending = new Queue<HirCallable>(logicalEntries);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            collect_reachable_calls(current.body, current, callableMap, reachableNames, pending);
        }

        Console.Error.WriteLine(
            $"[DEBUG reachable] allCallables={allCallables.Count}, reachable={reachableNames.Count}");
        foreach (var name in reachableNames)
            Console.Error.WriteLine($"[DEBUG reachable] {name}");

        return
        [
            .. allCallables
                .Where(callable => reachableNames.Contains(callable.name))
        ];
    }

    /// <summary>
    ///     枚举从当前逻辑入口真正触达�?`[data]` 派生请求�?
    ///     这一层只表达“结构类�?<-> 结构化容器”的关系，不涉及具体容器实现或文本格式�?
    /// </summary>
    public IReadOnlyList<HirDataDeriveRequest> enumerate_reachable_data_derive_requests(
        string? preferredLogicalEntry = null)
    {
        var requests = new List<HirDataDeriveRequest>();

        foreach (var callable in enumerate_reachable_aot_callables(preferredLogicalEntry))
        {
            if (callable.body is null) continue;

            foreach (var callSite in enumerate_call_sites(callable.body))
                if (try_bind_data_derive_request(callable, callSite, out var request) &&
                    request is not null)
                    requests.Add(request);
        }

        return
        [
            .. requests.Distinct()
        ];
    }

    /// <summary>
    ///     判断 `callable` 是否已经满足 `AOT` 发射条件�?
    ///     当前仍包含开放类型参数的模板保留在语义层，不直接进入后端代码生成�?
    /// </summary>
    private bool can_emit_aot_callable(HirCallable callable)
    {
        if (is_external_import_declaration(callable)) return true;

        if (callable.body is null)
            // 无函数体的外部声明（�?`[wasm]`、`[import]`、`[js_builtin]`）需要进�?`MIR` 元数据，
            // 以便后端将其注册为导入符号；函数体本身不会被 lowering�?
            return is_external_import_declaration(callable);

        if (callable is HirFunction function &&
            function.syntax is FunctionDecl { type_parameters.Count: > 0 })
        {
            return false;
        }

        var allowedTypeParameters = callable is HirMethod method
            ? collect_callable_owned_type_parameters(method)
            : null;

        if (callable.parameters.Any(parameter =>
                contains_open_type_parameter(parameter.type, callable, allowedTypeParameters))) return false;

        if (contains_open_type_parameter(callable.return_type, callable, allowedTypeParameters)) return false;

        return true;
    }

    /// <summary>
    ///     判断无函数体�?callable 是否为外部导入声明�?
    ///     识别 [clr]、[jvm]、[wasm]、[import]（通过 semantics.external_import_links�?
    ///     以及 [js_builtin]（通过 surface_attributes，由 WASM 后端桥接�?env 导入）�?
    /// </summary>
    private static bool is_external_import_declaration(HirCallable callable)
    {
        if (callable.semantics.has_external_import) return true;

        return callable.surface_attributes.Any(attr =>
            string.Equals(attr.name, "js_builtin", StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlySet<string>? collect_callable_owned_type_parameters(HirMethod method)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        collect_owned_type_parameter_names(method.owner_type, names);
        if (method.contract_type is not null) collect_owned_type_parameter_names(method.contract_type, names);

        return names.Count > 0 ? names : null;
    }

    private void collect_owned_type_parameter_names(HirTypeRef type, ISet<string> names)
    {
        if (type.is_special) return;

        if (type.type_arguments is not null)
            foreach (var argument in type.type_arguments)
                collect_owned_type_parameter_names(argument, names);

        if (type.named_type_arguments is not null)
            foreach (var binding in type.named_type_arguments)
                collect_owned_type_parameter_names(binding.type, names);

        if (type.tuple_elements is not null)
            foreach (var element in type.tuple_elements)
                collect_owned_type_parameter_names(element.type, names);

        if (!is_known_concrete_type(type)) names.Add(type.name);
    }

    /// <summary>
    ///     判断 `callable` 是否匹配当前构建请求的逻辑入口�?
    ///     未指定时保留全部逻辑入口；指定时同时兼容简单名和限定名�?
    /// </summary>
    private static bool logical_entry_matches(string callableName, string? preferredLogicalEntry)
    {
        if (string.IsNullOrWhiteSpace(preferredLogicalEntry)) return true;

        return string.Equals(callableName, preferredLogicalEntry, StringComparison.Ordinal) ||
               callableName.EndsWith("." + preferredLogicalEntry, StringComparison.Ordinal);
    }

    private void collect_reachable_calls(
        object? node,
        HirCallable currentCallable,
        IReadOnlyDictionary<string, HirCallable> callableMap,
        ISet<string> reachableNames,
        Queue<HirCallable> pending)
    {
        foreach (var callSite in enumerate_call_sites(node))
            if (try_resolve_call(currentCallable, callSite, out var resolution) &&
                resolution is { dispatch: HirDispatchKind.@static, target_name: var targetName })
                enqueue_reachable_callable(targetName, callableMap, reachableNames, pending);

        foreach (var loopResolution in enumerate_loop_iteration_resolutions(node, currentCallable))
        {
            if (loopResolution.iterator_factory is
                { dispatch: HirDispatchKind.@static, target_name: var iteratorFactory })
                enqueue_reachable_callable(iteratorFactory, callableMap, reachableNames, pending);

            if (loopResolution.has_next_call is { dispatch: HirDispatchKind.@static, target_name: var hasNext })
                enqueue_reachable_callable(hasNext, callableMap, reachableNames, pending);

            if (loopResolution.next_call is { dispatch: HirDispatchKind.@static, target_name: var next })
                enqueue_reachable_callable(next, callableMap, reachableNames, pending);

            if (loopResolution.unwrap_call is { dispatch: HirDispatchKind.@static, target_name: var unwrap })
                enqueue_reachable_callable(unwrap, callableMap, reachableNames, pending);
        }
    }

    private static void enqueue_reachable_callable(
        string targetName,
        IReadOnlyDictionary<string, HirCallable> callableMap,
        ISet<string> reachableNames,
        Queue<HirCallable> pending)
    {
        if (callableMap.TryGetValue(targetName, out var targetCallable) &&
            reachableNames.Add(targetName))
            pending.Enqueue(targetCallable);
    }

    private static IEnumerable<AstNode> enumerate_call_sites(object? value)
    {
        if (value is null or string) yield break;

        if (value is TermCallExpression call) yield return call;

        if (value is TermDotExpression { call_body: not null } dotCall) yield return dotCall;

        if (value is IEnumerable enumerable and not AstNode)
        {
            foreach (var item in enumerable)
            foreach (var child in enumerate_call_sites(item))
                yield return child;

            yield break;
        }

        if (value is not AstNode) yield break;

        var properties = value.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            var childValue = property.GetValue(value);
            foreach (var child in enumerate_call_sites(childValue)) yield return child;
        }

        var fields = value.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (var field in fields)
        {
            var childValue = field.GetValue(value);
            foreach (var child in enumerate_call_sites(childValue)) yield return child;
        }
    }

    private IEnumerable<HirLoopIterationResolution> enumerate_loop_iteration_resolutions(
        object? value,
        HirCallable currentCallable)
    {
        if (value is null or string) yield break;

        if (value is LoopInStatement { iterable: AstNode iterable } loopInStatement &&
            try_resolve_loop_iteration(currentCallable, iterable, out var resolution) &&
            resolution is not null)
            yield return resolution;

        if (value is IEnumerable enumerable and not AstNode)
        {
            foreach (var item in enumerable)
            foreach (var child in enumerate_loop_iteration_resolutions(item, currentCallable))
                yield return child;

            yield break;
        }

        if (value is not AstNode) yield break;

        var properties = value.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            var childValue = property.GetValue(value);
            foreach (var child in enumerate_loop_iteration_resolutions(childValue, currentCallable)) yield return child;
        }

        var fields = value.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (var field in fields)
        {
            var childValue = field.GetValue(value);
            foreach (var child in enumerate_loop_iteration_resolutions(childValue, currentCallable)) yield return child;
        }
    }

    private bool try_bind_data_derive_request(
        HirCallable currentCallable,
        AstNode callSite,
        out HirDataDeriveRequest? request)
    {
        if (callSite is TermDotExpression
            {
                separator_kind: MemberAccessSeparatorKind.dot,
                callee.name: "serialize",
                call_body: not null
            } instanceSerialize &&
            get_method_argument_count(instanceSerialize) == 0 &&
            try_resolve_receiver_type(currentCallable, instanceSerialize.caller, out var receiverType))
        {
            var dataType = find_data_type(receiverType.name_path, currentCallable.namespace_path);
            if (dataType?.data_shape is not null)
            {
                request = new HirDataDeriveRequest(
                    HirDataDeriveOperationKind.serialize,
                    currentCallable.name,
                    dataType.namepath,
                    dataType.kind,
                    dataType.data_shape.container_kind);
                return true;
            }
        }

        if (try_get_deserialize_call_target(callSite, out var staticDeserialize) &&
            get_call_argument_count(callSite) > 0 &&
            try_get_qualified_term_name(staticDeserialize.caller, out var ownerTypeName))
        {
            var dataType = find_data_type(ValkyrieNamePath.parse(ownerTypeName), currentCallable.namespace_path);
            if (dataType?.data_shape is not null)
            {
                request = new HirDataDeriveRequest(
                    HirDataDeriveOperationKind.deserialize,
                    currentCallable.name,
                    dataType.namepath,
                    dataType.kind,
                    dataType.data_shape.container_kind);
                return true;
            }
        }

        request = null;
        return false;
    }

    private static bool try_get_deserialize_call_target(AstNode callSite, out TermDotExpression target)
    {
        switch (callSite)
        {
            case TermCallExpression
            {
                caller: TermDotExpression
                {
                    separator_kind: MemberAccessSeparatorKind.double_colon,
                    callee.name: "deserialize"
                } staticDeserialize
            }:
                target = staticDeserialize;
                return true;
            case TermDotExpression
            {
                separator_kind: MemberAccessSeparatorKind.double_colon,
                callee.name: "deserialize",
                call_body: not null
            } staticDeserialize:
                target = staticDeserialize;
                return true;
            default:
                target = null!;
                return false;
        }
    }

    /// <summary>
    ///     判断类型中是否仍包含未收敛的开放类型参数�?
    /// </summary>
    private bool contains_open_type_parameter(
        HirTypeRef type,
        HirCallable? currentCallable = null,
        IReadOnlySet<string>? allowedTypeParameters = null)
    {
        if (type.is_special) return false;

        if (type.type_arguments is not null &&
            type.type_arguments.Any(argument =>
                contains_open_type_parameter(argument, currentCallable, allowedTypeParameters)))
            return true;

        if (type.named_type_arguments is not null &&
            type.named_type_arguments.Any(binding =>
                contains_open_type_parameter(binding.type, currentCallable, allowedTypeParameters)))
            return true;

        if (type.tuple_elements is not null &&
            type.tuple_elements.Any(element =>
                contains_open_type_parameter(element.type, currentCallable, allowedTypeParameters)))
            return true;

        if (allowedTypeParameters is not null &&
            !string.IsNullOrWhiteSpace(type.name) &&
            allowedTypeParameters.Contains(type.name))
            return false;

        return !is_known_concrete_type(type, currentCallable);
    }

    /// <summary>
    ///     判断类型引用是否已经是当前模块可落地的具体类型�?
    /// </summary>
    private bool is_known_concrete_type(HirTypeRef type, HirCallable? currentCallable = null)
    {
        if (type.is_array_type || type.is_tuple_type) return true;

        return is_known_concrete_type_name(type.name_path, currentCallable);
    }

    /// <summary>
    ///     判断类型名是否已经是当前模块可落地的具体类型�?
    /// </summary>
    private bool is_known_concrete_type_name(SemanticNamePath typeName, HirCallable? currentCallable = null)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName.ToString());
        if (string.IsNullOrWhiteSpace(normalizedTypeName)) return false;

        var typeNamePath = ValkyrieNamePath.parse(normalizedTypeName);

        if (ValkyrieBuiltinTypeFacts.is_intrinsic_type_name(normalizedTypeName) ||
            ValkyrieTextTypeFacts.try_create_semantic_text_type(normalizedTypeName) is not null)
            return true;

        if (typeNamePath.is_tuple_type) return true;

        if (resolve_type(typeNamePath, currentCallable?.namespace_path, currentCallable) is not null) return true;

        return types.Any(type =>
            type.matches_name(typeNamePath));
    }

    /// <summary>
    ///     校验所有可调用体中的调用点，确保未解析调用会在前端阶段直接报错�?
    /// </summary>
    public void validate_call_sites()
    {
        foreach (var callable in enumerate_callables())
        {
            if (callable.body is null) continue;

            validate_node(callable, callable.body);
        }
    }

    private void validate_node(HirCallable currentCallable, AstNode node)
    {
        switch (node)
        {
            case FunctionBody body:
                foreach (var statement in body.statements) validate_node(currentCallable, statement);

                break;
            case LetDeclaration { initializer: AstNode initializer }:
                validate_node(currentCallable, initializer);
                break;
            case AssignmentStatement assignment:
                validate_node(currentCallable, assignment.target);
                validate_node(currentCallable, assignment.value);
                break;
            case IfStatement ifStatement:
                validate_node(currentCallable, ifStatement.condition);
                validate_node(currentCallable, ifStatement.then_block);
                if (ifStatement.else_block is not null) validate_node(currentCallable, ifStatement.else_block);

                break;
            case WhileStatement whileStatement:
                validate_node(currentCallable, whileStatement.condition);
                validate_node(currentCallable, whileStatement.body);
                break;
            case UntilStatement untilStatement:
                validate_node(currentCallable, untilStatement.condition);
                validate_node(currentCallable, untilStatement.body);
                break;
            case LoopStatement loopStatement:
                if (loopStatement.initializer is not null) validate_node(currentCallable, loopStatement.initializer);

                if (loopStatement.condition is not null) validate_node(currentCallable, loopStatement.condition);

                if (loopStatement.update is not null) validate_node(currentCallable, loopStatement.update);

                validate_node(currentCallable, loopStatement.body);
                break;
            case LoopInStatement loopInStatement:
                if (loopInStatement.iterable is not null) validate_node(currentCallable, loopInStatement.iterable);

                validate_node(currentCallable, loopInStatement.body);
                break;
            case ReturnStatement { value: not null } returnStatement:
                validate_node(currentCallable, returnStatement.value);
                break;
            case BreakStatement:
            case ContinueStatement:
                break;
            case MatchStatementNode matchStatement:
                validate_node(currentCallable, matchStatement.expression);
                break;
            case TryStatement tryStatement:
                validate_node(currentCallable, tryStatement.body);
                break;
            case RaiseStatement raiseStatement:
                validate_node(currentCallable, raiseStatement.value);
                break;
            case ResumeStatement { value: not null } resumeStatement:
                validate_node(currentCallable, resumeStatement.value);
                break;
            case TermCallExpression call:
                validate_call_expression(currentCallable, call);
                break;
            case TermDotExpression { call_body: not null } dotCall:
                validate_dot_call_expression(currentCallable, dotCall);
                break;
            case TermDotExpression dot:
                validate_node(currentCallable, dot.caller);
                break;
            case TermBinaryExpression binary:
                validate_node(currentCallable, binary.left);
                validate_node(currentCallable, binary.right);
                break;
            case TermLiteralTextNode:
                break;
            case TermUnaryExpression unary:
                validate_node(currentCallable, unary.operand);
                break;
            case TermAsExpression cast:
                validate_node(currentCallable, cast.operand);
                break;
            case TermOrdinalExpression ordinal:
                validate_node(currentCallable, ordinal.target);
                foreach (var index in ordinal.indices) validate_node(currentCallable, index);

                break;
            case TermOffsetExpression offset:
                validate_node(currentCallable, offset.target);
                foreach (var index in offset.indices) validate_node(currentCallable, index);

                break;
            case TermIndexExpression index:
                validate_node(currentCallable, index.target);
                validate_node(currentCallable, index.index);
                break;
        }
    }

    private void validate_call_expression(HirCallable currentCallable, TermCallExpression call)
    {
        validate_node(currentCallable, call.caller);
        validate_call_body(currentCallable, call.call_body);
        try_resolve_call(currentCallable, call, out _);
    }

    private void validate_dot_call_expression(HirCallable currentCallable, TermDotExpression dotCall)
    {
        validate_node(currentCallable, dotCall.caller);
        if (dotCall.call_body is not null) validate_call_body(currentCallable, dotCall.call_body);

        try_resolve_call(currentCallable, dotCall, out _);
    }

    private void validate_call_body(HirCallable currentCallable, CallBody callBody)
    {
        if (callBody.term_arguments is not null)
            foreach (var argument in callBody.term_arguments.items)
                validate_node(currentCallable, argument.value);

        if (callBody.function_body is not null) validate_node(currentCallable, callBody.function_body);
    }

    private void report_invalid_static_member_syntax(AstNode node, string ownerTypeName, string memberName)
    {
        semantics.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"不存在实例方�?`{memberName}`，如果要调用静态方法，请改�?`{ownerTypeName}::{memberName}(...)`�?,
            build_source_span(node),
            "VALK_STATIC_MEMBER_REQUIRES_COLON_COLON",
            semantics.file_path));
    }

    private static string get_member_access_separator_text(MemberAccessSeparatorKind separatorKind)
    {
        return separatorKind == MemberAccessSeparatorKind.double_colon ? "::" : ".";
    }

    #endregion

    #region 命名工具

    private static bool type_name_matches(
        SemanticNamePath simpleName,
        SemanticNamePath qualifiedName,
        SemanticNamePath query,
        SemanticNameSpace? currentNamespace)
    {
        return HirTypeRef.matches_name_path(simpleName, qualifiedName, query, currentNamespace);
    }

    private static SemanticNamePath qualify_member_name_path(SemanticNameSpace? currentNamespace, SemanticNamePath? ownerName,
        string memberName)
    {
        return HirTypeRef.qualify_name_path(currentNamespace, ownerName, memberName);
    }

    private static string qualify_member_name(SemanticNameSpace? currentNamespace, SemanticNamePath? ownerName, string memberName)
    {
        return qualify_member_name_path(currentNamespace, ownerName, memberName).ToString();
    }

    private static bool has_explicit_namespace(SemanticNamePath name)
    {
        return !name.@namespace.is_empty;
    }

    private static bool try_split_explicit_type_member_name(
        string qualifiedName,
        out SemanticNamePath ownerTypeName,
        out string memberName)
    {
        var parts = ValkyrieNamePath.parse(qualifiedName).parts;
        if (parts.Count < 2)
        {
            ownerTypeName = ValkyrieNamePath.parse(null);
            memberName = string.Empty;
            return false;
        }

        ownerTypeName = new ValkyrieNamePath([.. parts.Take(parts.Count - 1)]);
        memberName = parts[parts.Count - 1];
        return !string.IsNullOrWhiteSpace(memberName);
    }

    private HirTypeDef? resolve_type(SemanticNamePath typeName, SemanticNameSpace? currentNamespace, HirCallable? currentCallable)
    {
        return resolve_type(typeName, currentNamespace, currentCallable, new HashSet<SemanticNamePath>());
    }

    private HirTypeDef? resolve_type(
        SemanticNamePath typeName,
        SemanticNameSpace? currentNamespace,
        HirCallable? currentCallable,
        ISet<SemanticNamePath> visitedAliases)
    {
        var direct = types.FirstOrDefault(type =>
            type.matches_name(typeName, currentNamespace));
        if (direct is not null) return direct;

        var directAlias = resolve_type_alias_target(typeName, currentNamespace, currentCallable, visitedAliases);
        if (directAlias is not null) return directAlias;

        if (currentCallable is null || has_explicit_namespace(typeName)) return null;

        foreach (var importedName in enumerate_visible_imported_names(currentCallable, typeName.name))
        {
            var imported = types.FirstOrDefault(type =>
                type.namepath.semantically_equals(ValkyrieNamePath.parse(importedName)));
            if (imported is not null) return imported;

            var importedAlias = resolve_type_alias_target(ValkyrieNamePath.parse(importedName), currentNamespace,
                currentCallable,
                visitedAliases);
            if (importedAlias is not null) return importedAlias;
        }

        return null;
    }

    private HirTraitDef? resolve_trait(SemanticNamePath traitName, SemanticNameSpace? currentNamespace,
        HirCallable? currentCallable)
    {
        return resolve_single_trait(traitName, currentNamespace, currentCallable,
            new HashSet<SemanticNamePath>());
    }

    private HirTraitDef? resolve_single_trait(
        SemanticNamePath traitName,
        SemanticNameSpace? currentNamespace,
        HirCallable? currentCallable,
        ISet<SemanticNamePath> visitedAliases)
    {
        var targets = resolve_trait_targets(traitName, currentNamespace, currentCallable, visitedAliases);
        return targets.Count == 1 ? targets[0] : null;
    }

    private IReadOnlyList<HirTraitDef> resolve_trait_targets(SemanticNamePath traitName, SemanticNameSpace? currentNamespace)
    {
        return resolve_trait_targets(traitName, currentNamespace, null, new HashSet<SemanticNamePath>());
    }

    private IReadOnlyList<HirTraitDef> resolve_trait_targets(
        SemanticNamePath traitName,
        SemanticNameSpace? currentNamespace,
        HirCallable? currentCallable,
        ISet<SemanticNamePath> visitedAliases)
    {
        var direct = traits.FirstOrDefault(trait =>
            trait.matches_name(traitName, currentNamespace));
        if (direct is not null) return [direct];

        var directAlias = resolve_trait_alias_targets(traitName, currentNamespace, currentCallable, visitedAliases);
        if (directAlias.Count > 0) return directAlias;

        if (currentCallable is null || has_explicit_namespace(traitName)) return [];

        foreach (var importedName in enumerate_visible_imported_names(currentCallable, traitName.name))
        {
            var imported = traits.FirstOrDefault(trait =>
                trait.name_path.semantically_equals(ValkyrieNamePath.parse(importedName)));
            if (imported is not null) return [imported];

            var importedAlias = resolve_trait_alias_targets(ValkyrieNamePath.parse(importedName), currentNamespace,
                currentCallable,
                visitedAliases);
            if (importedAlias.Count > 0) return importedAlias;
        }

        return [];
    }

    private HirTypeDef? resolve_type_alias_target(
        SemanticNamePath typeName,
        SemanticNameSpace? currentNamespace,
        HirCallable? currentCallable,
        ISet<SemanticNamePath> visitedAliases)
    {
        var alias = resolve_type_alias(typeName, currentNamespace);
        if (alias is null || !visitedAliases.Add(alias.name_path)) return null;

        return resolve_type(alias.target_type.name_path, currentNamespace, currentCallable, visitedAliases);
    }

    private IReadOnlyList<HirTraitDef> resolve_trait_alias_targets(
        SemanticNamePath traitName,
        SemanticNameSpace? currentNamespace,
        HirCallable? currentCallable,
        ISet<SemanticNamePath> visitedAliases)
    {
        var alias = resolve_trait_alias(traitName, currentNamespace);
        if (alias is null || !visitedAliases.Add(alias.name_path)) return [];

        return
        [
            .. alias.target_traits
                .SelectMany(target =>
                    resolve_trait_targets(target.name_path, currentNamespace, currentCallable, visitedAliases))
                .GroupBy(trait => trait.name_path)
                .Select(group => group.First())
        ];
    }

    private IEnumerable<string> enumerate_visible_imported_names(HirCallable currentCallable, string simpleName)
    {
        var simpleNamePath = ValkyrieNamePath.parse(simpleName);
        if (string.IsNullOrWhiteSpace(simpleName) || has_explicit_namespace(simpleNamePath)) yield break;

        foreach (var importDecl in get_visible_imports(currentCallable))
        foreach (var candidate in enumerate_import_targets(importDecl, simpleName))
            yield return candidate;

        foreach (var candidate in enumerate_namespace_reexport_targets(currentCallable.namespace_path, simpleName,
                     new HashSet<SemanticNameSpace>()))
            yield return candidate;
    }

    private IReadOnlyList<ImportDecl> get_visible_imports(HirCallable currentCallable)
    {
        foreach (var unit in syntax_units)
        {
            if (!compilation_unit_contains_callable(unit, currentCallable)) continue;

            var imports = new List<ImportDecl>();
            collect_visible_imports(unit.declarations, null, currentCallable.namespace_path, imports);
            return imports;
        }

        return [];
    }

    private static void collect_visible_imports(
        IReadOnlyList<AstNode> declarations,
        SemanticNameSpace? currentNamespace,
        SemanticNameSpace? targetNamespace,
        ICollection<ImportDecl> imports)
    {
        var activeNamespace = currentNamespace;
        foreach (var declaration in declarations)
        {
            if (declaration is NamespaceDecl namespaceDecl)
            {
                var resolvedNamespace = combine_namespace(activeNamespace, namespaceDecl.name.name);
                if (namespaceDecl.declarations.Count == 0)
                    activeNamespace = resolvedNamespace;
                else
                    collect_visible_imports(namespaceDecl.declarations, resolvedNamespace, targetNamespace, imports);

                continue;
            }

            if (declaration is ImportDecl usingDecl &&
                namespace_matches(activeNamespace, targetNamespace))
                imports.Add(usingDecl);
        }
    }

    private IEnumerable<string> enumerate_namespace_reexport_targets(
        SemanticNameSpace? namespaceName,
        string simpleName,
        ISet<SemanticNameSpace> visitedNamespaces)
    {
        if (namespaceName is null or { is_empty: true } ||
            !visitedNamespaces.Add(namespaceName)) yield break;

        foreach (var importDecl in get_namespace_reexports(namespaceName))
        foreach (var candidate in enumerate_import_targets(importDecl, simpleName))
            yield return candidate;
    }

    private IReadOnlyList<ImportDecl> get_namespace_reexports(SemanticNameSpace namespaceName)
    {
        var imports = new List<ImportDecl>();

        foreach (var unit in syntax_units)
        {
            var activeNamespace = (SemanticNameSpace?)null;
            var isPrimaryEntry = false;

            foreach (var declaration in unit.declarations)
            {
                if (declaration is NamespaceDecl namespaceDecl)
                {
                    var resolvedNamespace = combine_namespace(activeNamespace, namespaceDecl.name.name);
                    if (namespaceDecl.declarations.Count == 0)
                    {
                        activeNamespace = resolvedNamespace;
                        if (namespaceDecl.is_primary && namespace_matches(resolvedNamespace, namespaceName))
                            isPrimaryEntry = true;
                    }

                    continue;
                }

                if (isPrimaryEntry &&
                    declaration is ImportDecl usingDecl &&
                    usingDecl.is_reexport &&
                    namespace_matches(activeNamespace, namespaceName))
                    imports.Add(usingDecl);
            }
        }

        return imports;
    }

    private IEnumerable<string> enumerate_import_targets(ImportDecl importDecl, string simpleName)
    {
        if (importDecl.selections.Count > 0)
        {
            if (importDecl.selections.Any(selection =>
                    string.Equals(selection.name, simpleName, StringComparison.Ordinal)))
                yield return qualify_import_target(importDecl.module_path, simpleName);

            yield break;
        }

        yield return qualify_import_target(importDecl.module_path, simpleName);

        foreach (var candidate in enumerate_namespace_reexport_targets(ValkyrieNameSpace.parse(importDecl.module_path),
                     simpleName,
                     new HashSet<SemanticNameSpace>()))
            yield return candidate;
    }

    private static string qualify_import_target(string modulePath, string simpleName)
    {
        return ValkyrieNameSpace.parse(modulePath).qualify(ValkyrieNamePath.parse(simpleName)).ToString();
    }

    private static bool compilation_unit_contains_callable(CompilationUnit unit, HirCallable callable)
    {
        return unit.declarations.Any(declaration => declaration_contains_callable(declaration, callable));
    }

    private static bool declaration_contains_callable(AstNode declaration, HirCallable callable)
    {
        switch (declaration)
        {
            case NamespaceDecl namespaceDecl:
                return namespaceDecl.declarations.Any(nested => declaration_contains_callable(nested, callable));
            case FunctionDecl functionDecl when callable is HirFunction function:
                return ReferenceEquals(functionDecl, function.syntax);
            case ClassDecl classDecl when callable is HirMethod method:
                return classDecl.body?.methods.Any(candidate => ReferenceEquals(candidate, method.syntax)) == true;
            case StructureDecl structureDecl when callable is HirMethod method:
                return structureDecl.body?.methods.Any(candidate => ReferenceEquals(candidate, method.syntax)) == true;
            case TraitDecl traitDecl when callable is HirMethod method:
                return traitDecl.body?.methods.Any(candidate => ReferenceEquals(candidate, method.syntax)) == true;
            case UniteDecl uniteDecl when callable is HirMethod method:
                return uniteDecl.methods.Any(candidate => ReferenceEquals(candidate, method.syntax));
            case DeclareImply implyDecl when callable is HirMethod method:
                return implyDecl.methods.Any(candidate => ReferenceEquals(candidate, method.syntax));
            default:
                return false;
        }
    }

    private static bool namespace_matches(SemanticNameSpace? lhs, SemanticNameSpace? rhs)
    {
        if (lhs is null or { is_empty: true }) return rhs is null or { is_empty: true };

        return lhs.semantically_equals(rhs);
    }

    private static SemanticNameSpace combine_namespace(SemanticNameSpace? currentNamespace, string declaredNamespace)
    {
        return (currentNamespace ?? ValkyrieNameSpace.parse(null)).combine(ValkyrieNameSpace.parse(declaredNamespace));
    }

    #endregion
}
