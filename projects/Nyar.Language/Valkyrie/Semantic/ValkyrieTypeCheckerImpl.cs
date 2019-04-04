using Nyar.Analyzer.Semantic;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.ECS;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using TypeChecker_TypeChecker = Nyar.Language.Valkyrie.TypeChecker.TypeChecker;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     Valkyrie 类型检查桥接实现。
/// </summary>
public sealed class ValkyrieTypeCheckerImpl : Analyzer.Semantic.TypeChecker
{
    private readonly Dictionary<int, IType> _expression_types;
    private readonly TypeChecker_TypeChecker _inner;
    private ValkyrieTypeInference? _type_inference;

    public ValkyrieTypeCheckerImpl()
    {
        _inner = new TypeChecker_TypeChecker();
        bridge = new ValkyrieSemanticBridge();
        _expression_types = new Dictionary<int, IType>();
    }

    public ValkyrieSemanticBridge bridge { get; }

    public SemanticModel check_compilation_unit(CompilationUnit compilationUnit, string? filePath = null)
    {
        _type_inference = new ValkyrieTypeInference(compilationUnit.declarations.OfType<FunctionDecl>(), bridge);
        var checkedResult = _inner.check(compilationUnit, filePath);
        return bridge.build_semantic_model(checkedResult, compilationUnit, filePath ?? string.Empty);
    }

    public override IType infer_type(ISymbol symbol)
    {
        if (symbol.type is null) return UnknownType.instance;

        return symbol.type;
    }

    public override IType infer_type_of_expression(object node)
    {
        if (node is not AstNode expression) return UnknownType.instance;

        if (_expression_types.TryGetValue(expression.GetHashCode(), out var cached)) return cached;

        var result = infer_type_core(expression);
        _expression_types[expression.GetHashCode()] = result;
        return result;
    }

    public override bool check_type(IType expected, IType actual, out SemanticDiagnostic? diagnostic)
    {
        if (expected.is_assignable_from(actual))
        {
            diagnostic = null;
            return true;
        }

        diagnostic = type_mismatch(expected, actual, new TextSpan(0, 0));
        return false;
    }

    public override IReadOnlyList<SemanticDiagnostic> check_all_types(SemanticModel model)
    {
        return [];
    }

    private IType infer_type_core(AstNode node)
    {
        return node switch
        {
            TermLiteralNumberNode literal => infer_literal_type(literal),
            TermLiteralTextNode literal => infer_literal_type(literal),
            TermLiteralBooleanNode literal => infer_literal_type(literal),
            LiteralNullNode literal => infer_literal_type(literal),
            IdentifierNode identifier => infer_identifier_type(identifier),
            TermLiteralNamePathNode symbol => infer_qualified_path_type(symbol.path),
            TermBinaryExpression binary => infer_binary_type(binary),
            TermUnaryExpression unary => infer_type_of_expression(unary.operand),
            TermAsExpression cast => bridge.convert_type_annotation(cast.target_type),
            TermCallExpression call => infer_call_type(call),
            TermOrdinalExpression ordinal => infer_index_type(ordinal.target),
            TermOffsetExpression offset => infer_index_type(offset.target),
            TermDotExpression dot => infer_member_access_type(dot),
            QualifiedPathNode or QueryExpr => UnknownType.instance,
            LetDeclaration { var_type: not null } letDecl => bridge.convert_type_annotation(letDecl.var_type),
            LetDeclaration { initializer: not null } letDecl => infer_type_of_expression(letDecl.initializer),
            ComponentDeclaration component => new NamedType(component.name?.name ?? "", "component"),
            SystemDeclaration system => new NamedType(system.name?.name ?? "", "system"),
            FunctionDecl micro => build_function_type(micro),
            _ => UnknownType.instance
        };
    }

    private static IType infer_literal_type(TermNode literal)
    {
        return ValkyrieBuiltinTypeFacts.create_literal_type(literal);
    }

    private IType infer_identifier_type(IdentifierNode identifier)
    {
        if (_type_inference is not null &&
            !string.IsNullOrWhiteSpace(identifier.name) &&
            _type_inference.try_get_function_type(identifier.name, out var functionType))
            return functionType;

        return ValkyrieBuiltinTypeFacts.try_create_identifier_type(identifier.name) ?? UnknownType.instance;
    }

    private IType infer_qualified_path_type(QualifiedPathNode path)
    {
        if (path.segments.Count == 0) return UnknownType.instance;

        if (_type_inference is not null)
        {
            var fullName = path.full_name;
            if (!string.IsNullOrWhiteSpace(fullName) &&
                _type_inference.try_get_function_type(fullName, out var fullFunctionType))
                return fullFunctionType;
        }

        return infer_identifier_type(path.segments[^1]);
    }

    private IType infer_binary_type(TermBinaryExpression binary)
    {
        var leftType = infer_type_of_expression(binary.left);
        var rightType = infer_type_of_expression(binary.right);

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
            TermBinaryOperator.power => infer_arithmetic_result_type(leftType, rightType),
            TermBinaryOperator.addition or
                TermBinaryOperator.subtraction or
                TermBinaryOperator.multiplication or
                TermBinaryOperator.division or
                TermBinaryOperator.modulus => infer_arithmetic_result_type(leftType, rightType),
            _ => UnknownType.instance
        };
    }

    private static IType infer_arithmetic_result_type(IType left, IType right)
    {
        return ValkyrieBuiltinTypeFacts.infer_arithmetic_result_type(left, right, requireBothI32: true);
    }

    private IType infer_call_type(TermCallExpression call)
    {
        var calleeType = infer_type_of_expression(call.caller);
        if (calleeType is FunctionType functionType) return functionType.return_type;

        return UnknownType.instance;
    }

    private IType infer_member_access_type(TermDotExpression dot)
    {
        var targetType = infer_type_of_expression(dot.caller);
        if (targetType is NamedType namedType)
            foreach (var member in namedType.members)
                if (member.name == dot.callee.full_name
                    || (dot.callee.segments.Count > 0 && member.name == dot.callee.segments[^1].name))
                    return member.type ?? UnknownType.instance;

        if (targetType is not UnknownType) return UnknownType.instance;

        if (_type_inference is not null)
        {
            var fullName = dot.callee.full_name;
            if (!string.IsNullOrWhiteSpace(fullName) &&
                _type_inference.try_get_function_type(fullName, out var fullFunctionType))
                return fullFunctionType;

            if (dot.callee.segments.Count > 0 &&
                _type_inference.try_get_function_type(dot.callee.segments[^1].name, out var terminalFunctionType))
                return terminalFunctionType;
        }

        return UnknownType.instance;
    }

    private IType infer_index_type(ValkyrieNode target)
    {
        var targetType = infer_type_of_expression(target);
        if (targetType is ArrayType arrayType) return arrayType.element_type;

        if (targetType is GenericType { type_arguments.Count: > 0 } genericType) return genericType.type_arguments[0];

        return UnknownType.instance;
    }

    private IType build_function_type(FunctionDecl micro)
    {
        if (_type_inference is not null) return _type_inference.build_function_type(micro);

        var parameterTypes = new List<IType>(micro.parameters.Count);
        foreach (var parameter in micro.parameters)
            parameterTypes.AddRange(parameter.items.Select(item =>
                bridge.convert_type_annotation(item.bound_type ?? null!)));

        var returnType = micro.return_type is not null && !is_auto_return_type(micro.return_type)
            ? bridge.convert_type_annotation(micro.return_type)
            : UnknownType.instance;

        return new FunctionType(parameterTypes, returnType);
    }

    private static bool is_auto_return_type(TypeNode? typeNode)
    {
        return ValkyrieBuiltinTypeFacts.is_auto_return_type(typeNode);
    }
}
