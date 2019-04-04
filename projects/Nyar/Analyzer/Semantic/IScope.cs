namespace Nyar.Analyzer.Semantic;

public interface IScope
{
    IScope? parent { get; }
    string? name { get; }
    IEnumerable<ISymbol> get_members();
    IEnumerable<IScope> get_child_scopes();
    ISymbol? lookup(string name);
    ISymbol? lookup_recursive(string name);
    IScope? get_child_scope(string name);
}