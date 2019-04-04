namespace Nyar.Analyzer.Semantic;

public interface ISymbolTable
{
    IScope global_scope { get; }
    ISymbol? resolve(string qualifiedName);
    IReadOnlyList<ISymbol> find_references(ISymbol symbol);
    IReadOnlyList<ISymbol> get_symbols_in_scope(IScope scope);
    void add_symbol(IScope scope, ISymbol symbol);
    void remove_symbol(ISymbol symbol);
}