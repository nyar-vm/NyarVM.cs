using System.Text;
using Nyar.Analyzer.Semantic;

namespace Std.Data.Text.Awsl.Semantic;

/// <summary>
///     AWSL 语义桥接器 —— 将 AWSL Widget 解析结果转换为 Nyar 语义模型
/// </summary>
public sealed class AwslSemanticBridge : ISemanticAnalysisProvider, IReferenceProvider
{
    #region 公共 API

    public SemanticModel build_semantic_model(AwslParseResult parseResult, string filePath)
    {
        var globalScope = new Scope("global");
        var symbolTable = new SymbolTable(globalScope);
        var model = new SemanticModel(filePath, symbolTable);

        index_widget_declarations(parseResult, globalScope, filePath, model);
        validate_widget_semantics(parseResult, globalScope, filePath, model);

        return model;
    }

    public SemanticModel analyze(string filePath, object syntaxRoot)
    {
        if (syntaxRoot is not AwslParseResult parseResult)
            throw new ArgumentException("AWSL semantic analysis requires AwslParseResult syntax.", nameof(syntaxRoot));

        return build_semantic_model(parseResult, filePath);
    }

    public IReadOnlyList<SymbolStub> build_stubs(string filePath, object syntaxRoot)
    {
        var model = analyze(filePath, syntaxRoot);
        var stubs = new List<SymbolStub>();

        foreach (var symbol in model.get_all_declared_symbols())
        {
            if (symbol is not Symbol concreteSymbol) continue;

            stubs.Add(new SymbolStub(
                symbol.name,
                symbol.kind,
                symbol.accessibility,
                concreteSymbol.definition_span,
                concreteSymbol.definition_source_span,
                symbol.type?.name,
                filePath));
        }

        return stubs;
    }

    public IReadOnlyList<ReferenceEntry> collect_references(string filePath, object syntaxRoot, SemanticModel model)
    {
        return [];
    }

    public IType convert_widget_property_type(string typeName)
    {
        ensure_no_legacy_type_alias(typeName);
        return typeName switch
        {
            "f64" => new PrimitiveType("f64"),
            "f32" => new PrimitiveType("f32"),
            "i32" => new PrimitiveType("i32"),
            "i64" => new PrimitiveType("i64"),
            "bool" => new PrimitiveType("bool"),
            "utf8" => new PrimitiveType("utf8"),
            "list" => new GenericType("list", [UnknownType.instance]),
            "map" => new GenericType("map", [UnknownType.instance, UnknownType.instance]),
            "auto" => AutoType.instance,
            "" => UnknownType.instance,
            _ => new NamedType(typeName, "widget_prop")
        };
    }

    public IType infer_value_kind_type(AwslValueKind kind)
    {
        return kind switch
        {
            AwslValueKind.boolean => new PrimitiveType("bool"),
            AwslValueKind.number => new PrimitiveType("f64"),
            AwslValueKind.@string => new PrimitiveType("utf8"),
            AwslValueKind.array => new GenericType("list", [UnknownType.instance]),
            AwslValueKind.@object => new GenericType("map", [UnknownType.instance, UnknownType.instance]),
            AwslValueKind.identifier => UnknownType.instance,
            AwslValueKind.expression => UnknownType.instance,
            _ => UnknownType.instance
        };
    }

    /// <summary>
    ///     `AWSL` 语义层禁止继续接受历史遗留的 JS 风格类型别名。
    /// </summary>
    private static void ensure_no_legacy_type_alias(string typeName)
    {
        if (typeName is not "number"
            and not "boolean"
            and not "string"
            and not "str"
            and not "int"
            and not "long"
            and not "float"
            and not "double"
            and not "array"
            and not "object")
        {
            return;
        }

        throw new InvalidOperationException(
            $"AWSL 语义层禁止继续使用历史遗留类型别名 `{typeName}`，请改用 `f64`、`bool`、`utf8`、`i32`、`i64`、`f32`、`list` 或 `map`。");
    }

    #endregion

    #region 声明索引

    private void index_widget_declarations(AwslParseResult parseResult, Scope globalScope, string filePath,
        SemanticModel model)
    {
        var widgetScope = globalScope.get_or_create_child_scope(parseResult.name);

        var widgetType = new NamedType(parseResult.name, "widget");
        var widgetSymbol = new Symbol(
            parseResult.name,
            SymbolKind.@class,
            SymbolAccessibility.@public,
            widgetType,
            globalScope,
            definitionSourceSpan: new SourceSpan(1, 1, 1, 1) with { file_path = filePath },
            filePath: filePath);
        globalScope.define(widgetSymbol);
        model.bind_symbol(parseResult.GetHashCode(), widgetSymbol);
        model.bind_type(parseResult.GetHashCode(), widgetType);

        foreach (var prop in parseResult.properties) index_property(prop, widgetScope, filePath, model);

        foreach (var method in parseResult.methods) index_method(method, widgetScope, filePath, model);
    }

    private void index_property(AwslProperty prop, Scope scope, string filePath, SemanticModel model)
    {
        var propType = convert_widget_property_type(prop.type_name);

        if (prop.default_value_kind != AwslValueKind.none && propType is AutoType or UnknownType)
            propType = infer_value_kind_type(prop.default_value_kind);

        var symbol = new Symbol(
            prop.name,
            SymbolKind.property,
            prop.is_readonly ? SymbolAccessibility.@private : SymbolAccessibility.@public,
            propType,
            scope,
            isReadOnly: prop.is_readonly,
            definitionSourceSpan: new SourceSpan(1, 1, 1, 1) with { file_path = filePath },
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(prop.GetHashCode(), symbol);
        model.bind_type(prop.GetHashCode(), propType);
    }

    private void index_method(AwslMethod method, Scope scope, string filePath, SemanticModel model)
    {
        var returnType = new PrimitiveType("void");
        var paramTypes = new List<IType>();

        if (!string.IsNullOrWhiteSpace(method.parameters))
        {
            var paramNames = method.parameters.Split(',');
            foreach (var _ in paramNames) paramTypes.Add(UnknownType.instance);
        }

        var funcType = new FunctionType(paramTypes, returnType);
        var symbol = new Symbol(
            method.name,
            SymbolKind.method,
            SymbolAccessibility.@public,
            funcType,
            scope,
            definitionSourceSpan: new SourceSpan(1, 1, 1, 1) with { file_path = filePath },
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(method.GetHashCode(), symbol);
        model.bind_type(method.GetHashCode(), funcType);
    }

    #endregion

    #region 语义验证

    private void validate_widget_semantics(AwslParseResult parseResult, IScope globalScope, string filePath,
        SemanticModel model)
    {
        var widgetScope = globalScope.get_child_scope(parseResult.name);
        if (widgetScope is null) return;

        validate_template_nodes(parseResult.template_nodes, widgetScope, filePath, model);

        validate_property_defaults(parseResult.properties, widgetScope, filePath, model);
    }

    private void validate_template_nodes(IReadOnlyList<AwslTemplateNode> nodes, IScope scope, string filePath,
        SemanticModel model)
    {
        foreach (var node in nodes) validate_template_node(node, scope, filePath, model);
    }

    private void validate_template_node(AwslTemplateNode node, IScope scope, string filePath, SemanticModel model)
    {
        switch (node)
        {
            case AwslInterpolationNode interpolation:
                validate_interpolation_expression(interpolation.expression, scope, filePath, model);
                break;

            case AwslElementNode element:
                validate_element_attributes(element, scope, filePath, model);
                validate_template_nodes(element.children, scope, filePath, model);
                break;

            case AwslIfNode ifNode:
                validate_interpolation_expression(ifNode.condition, scope, filePath, model);
                validate_template_nodes(ifNode.children, scope, filePath, model);
                validate_template_nodes(ifNode.else_children, scope, filePath, model);
                break;

            case AwslForNode forNode:
                if (scope is Scope concreteScope)
                {
                    var forScope = concreteScope.get_or_create_child_scope("for_" + forNode.iterator);
                    var iteratorSymbol = new Symbol(
                        forNode.iterator,
                        SymbolKind.variable,
                        SymbolAccessibility.@private,
                        UnknownType.instance,
                        forScope);
                    forScope.define(iteratorSymbol);

                    var iterableSymbol = scope.lookup_recursive(forNode.iterable);
                    if (iterableSymbol is null)
                        model.add_diagnostic(new SemanticDiagnostic(
                            DiagnosticSeverity.warning,
                            $"未定义的可迭代对象 '{forNode.iterable}'",
                            default,
                            "AW0501",
                            filePath));

                    validate_template_nodes(forNode.children, forScope, filePath, model);
                }

                break;
        }
    }

    private void validate_interpolation_expression(string expression, IScope scope, string filePath,
        SemanticModel model)
    {
        if (string.IsNullOrWhiteSpace(expression)) return;

        var identifiers = extract_identifiers(expression);
        foreach (var ident in identifiers)
        {
            var resolved = scope.lookup_recursive(ident);
            if (resolved is null)
                model.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.warning,
                    $"模板中未定义的标识符 '{ident}'",
                    default,
                    "AW0530",
                    filePath));
        }
    }

    private void validate_element_attributes(AwslElementNode element, IScope scope, string filePath,
        SemanticModel model)
    {
        foreach (var attr in element.attributes)
            if (attr.Value.StartsWith('{') && attr.Value.EndsWith('}'))
            {
                var expr = attr.Value[1..^1].Trim();
                validate_interpolation_expression(expr, scope, filePath, model);
            }
    }

    private void validate_property_defaults(IReadOnlyList<AwslProperty> properties, IScope scope, string filePath,
        SemanticModel model)
    {
        foreach (var prop in properties)
        {
            if (prop.default_value is null || prop.default_value_kind == AwslValueKind.none) continue;

            if (prop.default_value_kind == AwslValueKind.identifier)
            {
                var resolved = scope.lookup_recursive(prop.default_value);
                if (resolved is null)
                    model.add_diagnostic(new SemanticDiagnostic(
                        DiagnosticSeverity.warning,
                        $"属性 '{prop.name}' 的默认值引用了未定义的标识符 '{prop.default_value}'",
                        default,
                        "AW0531",
                        filePath));
            }

            var declaredType = convert_widget_property_type(prop.type_name);
            var inferredType = infer_value_kind_type(prop.default_value_kind);

            if (declaredType is not AutoType and not UnknownType
                && inferredType is not UnknownType
                && !declaredType.is_assignable_from(inferredType))
                model.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.warning,
                    $"属性 '{prop.name}' 默认值类型不匹配：期望 {declaredType.name}，实际 {inferredType.name}",
                    default,
                    "AW0401",
                    filePath));
        }
    }

    #endregion

    #region 标识符提取

    private static List<string> extract_identifiers(string expression)
    {
        var identifiers = new List<string>();
        var buffer = new StringBuilder();
        var inString = false;
        var stringChar = '\0';

        for (var i = 0; i < expression.Length; i++)
        {
            var c = expression[i];

            if (inString)
            {
                if (c == stringChar && (i == 0 || expression[i - 1] != '\\')) inString = false;

                continue;
            }

            if (c is '\'' or '"' or '`')
            {
                inString = true;
                stringChar = c;
                if (buffer.Length > 0) process_buffer(buffer, identifiers);

                continue;
            }

            if (char.IsLetterOrDigit(c) || c == '_')
            {
                buffer.Append(c);
            }
            else
            {
                if (buffer.Length > 0) process_buffer(buffer, identifiers);
            }
        }

        if (buffer.Length > 0) process_buffer(buffer, identifiers);

        return identifiers;
    }

    private static void process_buffer(StringBuilder buffer, List<string> identifiers)
    {
        var token = buffer.ToString();
        buffer.Clear();

        if (token.Length == 0) return;

        if (!char.IsLetter(token[0]) && token[0] != '_') return;

        var keywords = new HashSet<string>
        {
            "true", "false", "null", "undefined",
            "if", "else", "for", "while", "return",
            "let", "const", "var", "function",
            "new", "this", "super", "class",
            "import", "export", "from", "as",
            "typeof", "instanceof", "in", "of"
        };

        if (!keywords.Contains(token)) identifiers.Add(token);
    }

    #endregion
}
