using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.TypeSystem;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     Valkyrie 轻量类型推断器。
///     目前负责两类推断：
///     1. `micro` 未标注返回类型时，根据显式 `return` 或尾表达式推断返回类型。
///     2. `let` 未标注类型时，根据初始化表达式推断局部变量类型。
/// </summary>
public sealed class ValkyrieTypeInference
{
    private readonly ValkyrieSemanticBridge _bridge;
    private readonly Dictionary<string, IType> _function_type_cache = new(StringComparer.Ordinal);
    private readonly HashSet<string> _function_type_in_progress = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionDecl> _functions;

    public ValkyrieTypeInference(IEnumerable<FunctionDecl> functions, ValkyrieSemanticBridge bridge)
    {
        _bridge = bridge;
        _functions = functions
            .Where(function => !string.IsNullOrWhiteSpace(function.name?.name))
            .GroupBy(function => function.name!.name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    public bool try_get_function_type(string name, out IType type)
    {
        if (_function_type_cache.TryGetValue(name, out type!)) return true;

        if (_function_type_in_progress.Contains(name))
        {
            type = UnknownType.instance;
            return false;
        }

        if (!_functions.TryGetValue(name, out var function))
        {
            type = UnknownType.instance;
            return false;
        }

        type = build_function_type(function);
        _function_type_cache[name] = type;
        return true;
    }

    public IType build_function_type(FunctionDecl micro)
    {
        var functionName = micro.name?.name;
        if (functionName is { Length: > 0 } namedFunction &&
            _function_type_cache.TryGetValue(namedFunction, out var cached))
            return cached;

        var parameterTypes = micro.parameters
            .SelectMany(parameter => parameter.items)
            .Select(parameter => _bridge.convert_type_annotation(parameter.bound_type ?? create_any_type()))
            .ToArray();

        if (functionName is { Length: > 0 } recursiveFunction && _function_type_in_progress.Contains(recursiveFunction))
            return _function_type_cache.TryGetValue(recursiveFunction, out var provisional)
                ? provisional
                : new FunctionType(parameterTypes, get_declared_return_type_or_unknown(micro));

        var provisionalType = new FunctionType(parameterTypes, get_declared_return_type_or_unknown(micro));
        if (functionName is { Length: > 0 } currentFunction)
        {
            _function_type_in_progress.Add(currentFunction);
            _function_type_cache[currentFunction] = provisionalType;
        }

        try
        {
            IType returnType;
            if (micro.return_type is not null && !is_auto_return_type(micro.return_type))
                returnType = _bridge.convert_type_annotation(micro.return_type);
            else
                returnType = infer_function_return_type(micro);

            var functionType = new FunctionType(parameterTypes, returnType);
            if (functionName is { Length: > 0 } finalizedFunction)
                _function_type_cache[finalizedFunction] = functionType;

            return functionType;
        }
        finally
        {
            if (functionName is { Length: > 0 } completedFunction) _function_type_in_progress.Remove(completedFunction);
        }
    }

    private IType get_declared_return_type_or_unknown(FunctionDecl micro)
    {
        return micro.return_type is not null && !is_auto_return_type(micro.return_type)
            ? _bridge.convert_type_annotation(micro.return_type)
            : UnknownType.instance;
    }

    public IType infer_function_return_type(FunctionDecl micro)
    {
        if (micro.body is null) return ValkyrieBuiltinTypeFacts.unit_type;

        var scope = create_parameter_scope(micro);
        if (try_infer_generator_return_type(micro.body, scope, micro.name?.name, out var generatorReturnType))
        {
            return generatorReturnType;
        }

        return infer_block_return_type(micro.body, scope, micro.name?.name);
    }

    public IType infer_statement_type(AstNode node, IDictionary<string, IType>? scope = null,
        string? currentFunctionName = null)
    {
        scope ??= new Dictionary<string, IType>(StringComparer.Ordinal);

        return node switch
        {
            TermNode term => infer_expression_type(term, scope, currentFunctionName),
            ReturnStatement ret => ret.value is null
                ? ValkyrieBuiltinTypeFacts.unit_type
                : infer_expression_type(ret.value, scope, currentFunctionName),
            YieldStatement yieldStatement => yieldStatement.value is null
                ? ValkyrieBuiltinTypeFacts.unit_type
                : infer_expression_type(yieldStatement.value, scope, currentFunctionName),
            BreakStatement => ValkyrieBuiltinTypeFacts.unit_type,
            ContinueStatement => ValkyrieBuiltinTypeFacts.unit_type,
            LetDeclaration letDecl => infer_let_type(letDecl, scope, currentFunctionName),
            IfStatement ifStatement => infer_if_type(ifStatement, scope, currentFunctionName),
            _ => ValkyrieBuiltinTypeFacts.unit_type
        };
    }

    public IType infer_expression_type(AstNode node, IDictionary<string, IType>? scope = null,
        string? currentFunctionName = null)
    {
        scope ??= new Dictionary<string, IType>(StringComparer.Ordinal);

        return node switch
        {
            IdentifierNode identifier => infer_identifier_type(identifier.name, scope, currentFunctionName),
            TermLiteralNamePathNode symbol => infer_qualified_path_type(symbol.path, scope, currentFunctionName),
            QualifiedPathNode path => infer_qualified_path_type(path, scope, currentFunctionName),
            TermLiteralNumberNode literal => infer_literal_type(literal),
            TermLiteralTextNode literal => infer_literal_type(literal),
            TermLiteralBooleanNode literal => infer_literal_type(literal),
            TermLiteralTupleNode tuple => infer_tuple_literal_type(tuple, scope, currentFunctionName),
            LiteralNullNode literal => infer_literal_type(literal),
            TermBinaryExpression binary => infer_binary_type(binary, scope, currentFunctionName),
            TermUnaryExpression unary => infer_unary_type(unary, scope, currentFunctionName),
            TermDotExpression dot => infer_dot_type(dot, scope, currentFunctionName),
            TermCallExpression call => infer_call_type(call, scope, currentFunctionName),
            TermAsExpression cast => _bridge.convert_type_annotation(cast.target_type),
            LetDeclaration letDecl => infer_let_type(letDecl, scope, currentFunctionName),
            _ => UnknownType.instance
        };
    }

    private Dictionary<string, IType> create_parameter_scope(FunctionDecl micro)
    {
        var scope = new Dictionary<string, IType>(StringComparer.Ordinal);
        foreach (var parameter in micro.parameters)
        foreach (var item in parameter.items)
            if (!string.IsNullOrWhiteSpace(item.name?.name))
                scope[item.name!.name] = _bridge.convert_type_annotation(item.bound_type ?? create_any_type());

        return scope;
    }

    private IType infer_block_return_type(BlockStmt body, IDictionary<string, IType> inheritedScope,
        string? currentFunctionName)
    {
        var scope = new Dictionary<string, IType>(inheritedScope, StringComparer.Ordinal);
        var explicitReturns = new List<IType>();

        foreach (var statement in body.statements)
            switch (statement)
            {
                case LetDeclaration letDecl:
                    var letType = infer_let_type(letDecl, scope, currentFunctionName);
                    if (!string.IsNullOrWhiteSpace(letDecl.name?.name)) scope[letDecl.name!.name] = letType;
                    break;
                case ReturnStatement ret:
                {
                    var inferred = ret.value is null
                        ? ValkyrieBuiltinTypeFacts.unit_type
                        : infer_expression_type(ret.value, scope, currentFunctionName);
                    explicitReturns.Add(inferred);
                }
                    break;
            }

        if (explicitReturns.Count > 0) return merge_candidate_types(explicitReturns);

        if (body.statements.Count == 0) return ValkyrieBuiltinTypeFacts.unit_type;

        return infer_statement_type(body.statements[^1], scope, currentFunctionName);
    }

    private IType infer_let_type(LetDeclaration letDecl, IDictionary<string, IType> scope, string? currentFunctionName)
    {
        if (letDecl.var_type is not null) return _bridge.convert_type_annotation(letDecl.var_type);

        if (letDecl.initializer is not null)
            return infer_expression_type(letDecl.initializer, scope, currentFunctionName);

        return UnknownType.instance;
    }

    private IType infer_if_type(IfStatement ifStatement, IDictionary<string, IType> scope, string? currentFunctionName)
    {
        var thenType = infer_block_return_type(ifStatement.then_block, scope, currentFunctionName);
        if (ifStatement.else_block is not null)
        {
            var elseType = infer_statement_type(ifStatement.else_block, scope, currentFunctionName);
            return merge_candidate_types([thenType, elseType]);
        }

        return ValkyrieBuiltinTypeFacts.unit_type;
    }

    private IType infer_identifier_type(string name, IDictionary<string, IType> scope, string? currentFunctionName)
    {
        if (scope.TryGetValue(name, out var scopedType)) return scopedType;

        if (try_get_visible_function_type(name, currentFunctionName, out var functionType)) return functionType;

        return ValkyrieBuiltinTypeFacts.try_create_identifier_type(name) ?? UnknownType.instance;
    }

    private static bool is_auto_return_type(TypeNode? typeNode)
    {
        return ValkyrieBuiltinTypeFacts.is_auto_return_type(typeNode);
    }

    private static IType infer_literal_type(TermNode literal)
    {
        return ValkyrieBuiltinTypeFacts.create_literal_type(literal);
    }

    private IType infer_tuple_literal_type(TermLiteralTupleNode tuple, IDictionary<string, IType> scope,
        string? currentFunctionName)
    {
        var elementTypes = tuple.elements
            .Select(element => infer_expression_type(element, scope, currentFunctionName))
            .ToArray();
        var tupleName = $"({string.Join(", ", elementTypes.Select(type => type.name))})";
        return new NamedType(tupleName, "tuple", typeArguments: elementTypes);
    }

    private IType infer_binary_type(TermBinaryExpression binary, IDictionary<string, IType> scope,
        string? currentFunctionName)
    {
        var leftType = infer_expression_type(binary.left, scope, currentFunctionName);
        var rightType = infer_expression_type(binary.right, scope, currentFunctionName);

        return binary.@operator switch
        {
            TermBinaryOperator.equal or
                TermBinaryOperator.not_equal or
                TermBinaryOperator.less_than or
                TermBinaryOperator.greater_than or
                TermBinaryOperator.less_than_or_equal or
                TermBinaryOperator.greater_than_or_equal => ValkyrieBuiltinTypeFacts.bool_type,
            TermBinaryOperator.logical_and or TermBinaryOperator.logical_or => ValkyrieBuiltinTypeFacts.bool_type,
            TermBinaryOperator.addition when ValkyrieOperatorTypeFacts.try_infer_text_binary_result_type(leftType, rightType) is { } textType =>
                textType,
            TermBinaryOperator.power =>
                infer_arithmetic_result_type(leftType, rightType),
            TermBinaryOperator.addition or
                TermBinaryOperator.subtraction or
                TermBinaryOperator.multiplication or
                TermBinaryOperator.division or
                TermBinaryOperator.modulus => infer_arithmetic_result_type(leftType, rightType),
            _ => UnknownType.instance
        };
    }

    private IType infer_unary_type(TermUnaryExpression unary, IDictionary<string, IType> scope,
        string? currentFunctionName)
    {
        return unary.@operator switch
        {
            TermUnaryOperator.logical_not => ValkyrieBuiltinTypeFacts.bool_type,
            _ => infer_expression_type(unary.operand, scope, currentFunctionName)
        };
    }

    private IType infer_call_type(TermCallExpression call, IDictionary<string, IType> scope,
        string? currentFunctionName)
    {
        if (get_callable_name(call.caller) == "ExitCode") return new NamedType("ExitCode", "ExitCode");

        var calleeType = infer_expression_type(call.caller, scope, currentFunctionName);
        return calleeType is FunctionType functionType ? functionType.return_type : UnknownType.instance;
    }

    private IType infer_dot_type(TermDotExpression dot, IDictionary<string, IType> scope, string? currentFunctionName)
    {
        var memberType = infer_dot_reference_type(dot, scope, currentFunctionName);
        if (dot.call_body is not null)
            return memberType is FunctionType functionType ? functionType.return_type : UnknownType.instance;

        return memberType;
    }

    private IType infer_dot_reference_type(TermDotExpression dot, IDictionary<string, IType> scope,
        string? currentFunctionName)
    {
        var targetType = infer_expression_type(dot.caller, scope, currentFunctionName);

        // 处理运算符方法调用（由 binary/unary 表达式脱糖产生）
        if (dot.call_body is not null && dot.callee.segments.Count > 0 && targetType is not UnknownType)
        {
            var operatorName = dot.callee.segments[0].name;
            var argTypes = dot.call_body.term_arguments?.items
                .Select(item => infer_expression_type(item.value, scope, currentFunctionName))
                .ToArray() ?? [];
            if (try_infer_operator_return_type(targetType, operatorName, argTypes, out var resultType))
                return new FunctionType(argTypes, resultType);
        }

        if (targetType is NamedType namedType)
        {
            if (namedType.is_tuple_type)
            {
                var ordinalName = dot.callee.segments.Count > 0 ? dot.callee.segments[^1].name : dot.callee.full_name;
                if (try_get_tuple_ordinal_element_type(namedType, ordinalName, out var tupleElementType))
                {
                    return tupleElementType;
                }

                return UnknownType.instance;
            }

            var fullName = dot.callee.full_name;
            var terminalName = dot.callee.segments.Count > 0 ? dot.callee.segments[^1].name : null;
            foreach (var member in namedType.members)
                if (string.Equals(member.name, fullName, StringComparison.Ordinal) ||
                    (!string.IsNullOrWhiteSpace(terminalName) &&
                     string.Equals(member.name, terminalName, StringComparison.Ordinal)))
                    return member.type ?? UnknownType.instance;
        }

        if (targetType is not UnknownType) return UnknownType.instance;

        return resolve_callable_path_type(dot.callee, currentFunctionName);
    }

    private static bool try_get_tuple_ordinal_element_type(NamedType tupleType, string? memberName, out IType elementType)
    {
        if (!int.TryParse(memberName, out var ordinal) ||
            ordinal <= 0 ||
            ordinal > tupleType.type_arguments.Count)
        {
            elementType = UnknownType.instance;
            return false;
        }

        elementType = tupleType.type_arguments[ordinal - 1];
        return true;
    }

    /// <summary>
    ///     尝试推断运算符方法调用的返回类型（由 binary/unary 表达式脱糖后的 dot 调用）。
    /// </summary>
    private static bool try_infer_operator_return_type(IType targetType, string operatorName,
        IReadOnlyList<IType> argTypes, out IType resultType)
    {
        switch (operatorName)
        {
            // 二元算术运算符
            case "infix +":
            case "infix -":
            case "infix *":
            case "infix /":
            case "infix %":
                if (argTypes.Count >= 1)
                {
                    resultType = infer_arithmetic_result_type(targetType, argTypes[0]);
                    return true;
                }

                break;

            // 幂运算
            case "infix ^":
                if (argTypes.Count >= 1)
                {
                    resultType = infer_arithmetic_result_type(targetType, argTypes[0]);
                    return true;
                }

                break;

            // 比较运算符
            case "infix ==":
            case "infix !=":
            case "infix <":
            case "infix >":
            case "infix <=":
            case "infix >=":
                resultType = ValkyrieBuiltinTypeFacts.bool_type;
                return true;

            // 逻辑运算符
            case "infix &&":
            case "infix ||":
                resultType = ValkyrieBuiltinTypeFacts.bool_type;
                return true;

            // 位运算（返回左操作数类型）
            case "bit_and":
            case "bit_or":
            case "bit_xor":
            case "bit_shift_left":
            case "bit_shift_right":
                resultType = targetType;
                return true;

            // 一元逻辑非
            case "prefix !":
                resultType = ValkyrieBuiltinTypeFacts.bool_type;
                return true;

            // 一元取负 / 位非（返回操作数类型）
            case "prefix -":
            case "bit_not":
                resultType = targetType;
                return true;
        }

        resultType = UnknownType.instance;
        return false;
    }

    private IType infer_qualified_path_type(QualifiedPathNode path, IDictionary<string, IType> scope,
        string? currentFunctionName)
    {
        if (path.segments.Count == 0) return UnknownType.instance;

        var fullName = path.full_name;
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            if (scope.TryGetValue(fullName, out var scopedType)) return scopedType;

            if (try_get_visible_function_type(fullName, currentFunctionName, out var fullFunctionType))
                return fullFunctionType;
        }

        var terminalName = path.segments[^1].name;
        return infer_identifier_type(terminalName, scope, currentFunctionName);
    }

    private static string? get_callable_name(TermNode caller)
    {
        return caller switch
        {
            TermLiteralNamePathNode { path.segments.Count: > 0 } symbol => symbol.path.segments[^1].name,
            TermDotExpression { callee.segments.Count: > 0 } dot => dot.callee.segments[^1].name,
            _ => null
        };
    }

    private IType resolve_callable_path_type(QualifiedPathNode path, string? currentFunctionName)
    {
        var fullName = path.full_name;
        if (!string.IsNullOrWhiteSpace(fullName) &&
            try_get_visible_function_type(fullName, currentFunctionName, out var fullFunctionType))
            return fullFunctionType;

        if (path.segments.Count == 0) return UnknownType.instance;

        var terminalName = path.segments[^1].name;
        return try_get_visible_function_type(terminalName, currentFunctionName, out var terminalFunctionType)
            ? terminalFunctionType
            : UnknownType.instance;
    }

    private bool try_get_visible_function_type(string name, string? currentFunctionName, out IType type)
    {
        foreach (var candidateName in enumerate_visible_function_names(name, currentFunctionName))
            if (try_get_function_type(candidateName, out type))
                return true;

        type = UnknownType.instance;
        return false;
    }

    private IEnumerable<string> enumerate_visible_function_names(string name, string? currentFunctionName)
    {
        if (string.IsNullOrWhiteSpace(name)) yield break;

        var normalizedName = normalize_function_name(name);
        yield return normalizedName;

        var terminalName = extract_terminal_function_name(normalizedName);
        var currentNamespace = extract_function_namespace(currentFunctionName);
        if (!string.IsNullOrWhiteSpace(currentNamespace) &&
            !normalizedName.Contains('.', StringComparison.Ordinal))
            yield return $"{currentNamespace}.{terminalName}";

        if (!string.Equals(terminalName, normalizedName, StringComparison.Ordinal)) yield return terminalName;
    }

    private static string normalize_function_name(string name)
    {
        return name.Replace("::", ".", StringComparison.Ordinal);
    }

    private static string extract_terminal_function_name(string name)
    {
        var lastSeparator = name.LastIndexOf('.');
        return lastSeparator >= 0 ? name[(lastSeparator + 1)..] : name;
    }

    private static string? extract_function_namespace(string? functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName)) return null;

        var normalizedName = normalize_function_name(functionName);
        var lastSeparator = normalizedName.LastIndexOf('.');
        return lastSeparator > 0 ? normalizedName[..lastSeparator] : null;
    }

    private static TypeNode create_any_type()
    {
        return ValkyrieBuiltinTypeFacts.create_any_type_node();
    }

    private static IType infer_arithmetic_result_type(IType left, IType right)
    {
        return ValkyrieBuiltinTypeFacts.infer_arithmetic_result_type(left, right);
    }

    private static IType merge_candidate_types(IReadOnlyList<IType> candidateTypes)
    {
        if (candidateTypes.Count == 0) return ValkyrieBuiltinTypeFacts.unit_type;

        var firstKnown = candidateTypes.FirstOrDefault(type => type is not UnknownType) ?? candidateTypes[0];
        foreach (var candidate in candidateTypes)
        {
            if (candidate is UnknownType) continue;

            if (!candidate.equals(firstKnown)) return UnknownType.instance;
        }

        return firstKnown;
    }


    private static IType create_generator_type(IType itemType)
    {
        return new NamedType("Generator", "Generator", typeArguments: [itemType]);
    }

    private bool try_infer_generator_return_type(
        FunctionBody body,
        IDictionary<string, IType> inheritedScope,
        string? currentFunctionName,
        out IType generatorReturnType)
    {
        var yieldedTypes = new List<IType>();
        collect_yield_types(body, new Dictionary<string, IType>(inheritedScope, StringComparer.Ordinal), currentFunctionName, yieldedTypes);
        if (yieldedTypes.Count == 0)
        {
            generatorReturnType = UnknownType.instance;
            return false;
        }

        generatorReturnType = create_generator_type(merge_candidate_types(yieldedTypes));
        return true;
    }

    private void collect_yield_types(
        FunctionBody body,
        Dictionary<string, IType> scope,
        string? currentFunctionName,
        ICollection<IType> yieldedTypes)
    {
        foreach (var statement in body.statements)
        {
            switch (statement)
            {
                case LetDeclaration letDecl:
                    var letType = infer_let_type(letDecl, scope, currentFunctionName);
                    if (!string.IsNullOrWhiteSpace(letDecl.name?.name))
                    {
                        scope[letDecl.name!.name] = letType;
                    }

                    break;
                case YieldStatement yieldStatement:
                    yieldedTypes.Add(yieldStatement.value is null
                        ? ValkyrieBuiltinTypeFacts.unit_type
                        : infer_expression_type(yieldStatement.value, scope, currentFunctionName));
                    break;
                case IfStatement ifStatement:
                    collect_yield_types(ifStatement.then_block,
                        new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                        currentFunctionName,
                        yieldedTypes);
                    if (ifStatement.else_block is FunctionBody elseBody)
                    {
                        collect_yield_types(elseBody,
                            new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                            currentFunctionName,
                            yieldedTypes);
                    }
                    else if (ifStatement.else_block is IfStatement elseIfStatement)
                    {
                        collect_yield_types(new FunctionBody([elseIfStatement]),
                            new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                            currentFunctionName,
                            yieldedTypes);
                    }

                    break;
                case WhileStatement whileStatement:
                    collect_yield_types(whileStatement.body,
                        new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                        currentFunctionName,
                        yieldedTypes);
                    break;
                case UntilStatement untilStatement:
                    collect_yield_types(untilStatement.body,
                        new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                        currentFunctionName,
                        yieldedTypes);
                    break;
                case LoopStatement loopStatement:
                    collect_yield_types(loopStatement.body,
                        new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                        currentFunctionName,
                        yieldedTypes);
                    break;
                case LoopInStatement loopInStatement:
                    collect_yield_types(loopInStatement.body,
                        new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                        currentFunctionName,
                        yieldedTypes);
                    break;
                case FunctionBody nestedBody:
                    collect_yield_types(nestedBody,
                        new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                        currentFunctionName,
                        yieldedTypes);
                    break;
            }
        }
    }
}
