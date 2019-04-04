using Nyar.Analyzer.Semantic;
using Std.Data.Text.Syntax;
using Std.DataProcess.Serialize;

namespace Nyar.Language.Von.Semantic;

/// <summary>
///     为 VON/GON 提供最小语义桥，重点解决配置对象的键路径索引与跨文件存根导出。
///     这里消费的是 `Von` 自身的 literal 语义，而不是直接把 `SerdeValue` 当成最终 value model。
/// </summary>
public sealed class VonSemanticBridge : ISemanticAnalysisProvider, IReferenceProvider
{
    public IReadOnlyList<ReferenceEntry> collect_references(string filePath, object syntaxRoot, SemanticModel model)
    {
        return [];
    }

    public SemanticModel analyze(string filePath, object syntaxRoot)
    {
        return syntaxRoot switch
        {
            VonValue gonValue => build_semantic_model(gonValue.inner, filePath),
            SerdeValue serdeValue => build_semantic_model(serdeValue, filePath),
            _ => throw new ArgumentException("Von semantic analysis requires VonValue or SerdeValue.",
                nameof(syntaxRoot))
        };
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

    public SemanticModel build_semantic_model(SerdeValue rootValue, string filePath)
    {
        var globalScope = new Scope("global");
        var symbolTable = new SymbolTable(globalScope);
        var model = new SemanticModel(filePath, symbolTable);

        index_value(rootValue, globalScope, model, filePath, null);
        return model;
    }

    private void index_value(SerdeValue value, Scope scope, SemanticModel model, string filePath, string? prefix)
    {
        if (value.type != SerdeValueType.@object || value.fields is null) return;

        foreach (var (fieldName, fieldValue) in value.fields)
        {
            var qualifiedName = string.IsNullOrEmpty(prefix) ? fieldName : $"{prefix}.{fieldName}";
            var symbolType = global::Nyar.Language.Von.Semantic.VonLiteralSemantics.from_serde_literal(fieldValue);

            var symbol = new Symbol(
                qualifiedName,
                SymbolKind.property,
                SymbolAccessibility.@public,
                symbolType,
                scope,
                definitionSpan: default,
                definitionSourceSpan: new SourceSpan(filePath, 1, 1, 1, 1),
                filePath: filePath);

            scope.define(symbol);
            model.bind_symbol(qualifiedName.GetHashCode(), symbol);
            model.bind_type(qualifiedName.GetHashCode(), symbolType);

            if (fieldValue.type == SerdeValueType.@object)
            {
                var childScope = scope.get_or_create_child_scope(fieldName);
                index_value(fieldValue, childScope, model, filePath, qualifiedName);
            }
        }
    }

}
