using Nyar.Analyzer.Semantic;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Highlight;

public class SemanticHighlighter
{
    private readonly Dictionary<SymbolKind, HighlightKind> _symbol_kind_map;

    public SemanticHighlighter()
    {
        _symbol_kind_map = new Dictionary<SymbolKind, HighlightKind>
        {
            { SymbolKind.@class, HighlightKind.type_identifier },
            { SymbolKind.@interface, HighlightKind.type_identifier },
            { SymbolKind.@enum, HighlightKind.type_identifier },
            { SymbolKind.type_alias, HighlightKind.type_identifier },
            { SymbolKind.type_parameter, HighlightKind.type_identifier },
            { SymbolKind.function, HighlightKind.function_identifier },
            { SymbolKind.method, HighlightKind.function_identifier },
            { SymbolKind.constructor, HighlightKind.function_identifier },
            { SymbolKind.property, HighlightKind.property },
            { SymbolKind.field, HighlightKind.field },
            { SymbolKind.variable, HighlightKind.variable },
            { SymbolKind.parameter, HighlightKind.parameter },
            { SymbolKind.constant, HighlightKind.constant },
            { SymbolKind.@namespace, HighlightKind.@namespace },
            { SymbolKind.module, HighlightKind.module },
            { SymbolKind.import, HighlightKind.identifier },
            { SymbolKind.export, HighlightKind.identifier },
            { SymbolKind.@event, HighlightKind.property },
            { SymbolKind.@delegate, HighlightKind.type_identifier },
            { SymbolKind.@operator, HighlightKind.@operator },
            { SymbolKind.destructor, HighlightKind.function_identifier }
        };
    }

    public void map_symbol_kind(SymbolKind symbolKind, HighlightKind highlightKind)
    {
        _symbol_kind_map[symbolKind] = highlightKind;
    }

    public IReadOnlyList<HighlightToken> highlight(SemanticModel model,
        IReadOnlyList<(ISymbol Symbol, TextSpan Span)> symbolSpans)
    {
        var results = new List<HighlightToken>(symbolSpans.Count);

        foreach (var (symbol, span) in symbolSpans)
            if (_symbol_kind_map.TryGetValue(symbol.kind, out var highlightKind))
            {
                var modifier = get_modifier(symbol);
                results.Add(new HighlightToken(highlightKind, span, modifier));
            }

        return results;
    }

    private static string? get_modifier(ISymbol symbol)
    {
        if (symbol.is_static) return "static";

        if (symbol.is_read_only) return "readonly";

        if (symbol.is_abstract) return "abstract";

        return null;
    }
}