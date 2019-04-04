namespace Nyar.Analyzer.Semantic;

public interface ISymbol
{
    string name { get; }
    SymbolKind kind { get; }
    SymbolAccessibility accessibility { get; }
    IType? type { get; }
    IScope? containing_scope { get; }
    bool is_static { get; }
    bool is_read_only { get; }
    bool is_abstract { get; }
    bool is_sealed { get; }
}