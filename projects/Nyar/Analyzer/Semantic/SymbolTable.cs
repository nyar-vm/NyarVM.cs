namespace Nyar.Analyzer.Semantic;

public sealed class SymbolTable : ISymbolTable
{
    private readonly Scope _global_scope;
    private readonly Dictionary<string, List<ISymbol>> _references;

    public SymbolTable()
    {
        _global_scope = new Scope("global");
        _references = new Dictionary<string, List<ISymbol>>();
    }

    public SymbolTable(Scope globalScope)
    {
        _global_scope = globalScope;
        _references = new Dictionary<string, List<ISymbol>>();
    }

    public IScope global_scope => _global_scope;

    public ISymbol? resolve(string qualifiedName)
    {
        var parts = qualifiedName.Split("::");
        IScope current = _global_scope;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            var child = current.get_child_scope(parts[i]);
            if (child is null) return null;

            current = child;
        }

        var name = parts[^1];
        var result = current.lookup_recursive(name);
        if (result is not null) return result;

        return deep_lookup(_global_scope, name);
    }

    public IReadOnlyList<ISymbol> find_references(ISymbol symbol)
    {
        if (_references.TryGetValue(symbol.name, out var refs)) return refs;

        return [];
    }

    public IReadOnlyList<ISymbol> get_symbols_in_scope(IScope scope)
    {
        var result = new List<ISymbol>();

        collect_symbols_in_scope(scope, result);

        return result;
    }

    public void add_symbol(IScope scope, ISymbol symbol)
    {
        if (scope is Scope concreteScope) concreteScope.define(symbol);
    }

    public void remove_symbol(ISymbol symbol)
    {
        if (symbol.containing_scope is Scope concreteScope) concreteScope.undefine(symbol.name);

        if (_references.TryGetValue(symbol.name, out var refs)) refs.RemoveAll(r => ReferenceEquals(r, symbol));
    }

    private static ISymbol? deep_lookup(IScope scope, string name)
    {
        var symbol = scope.lookup(name);
        if (symbol is not null) return symbol;

        foreach (var child in scope.get_child_scopes())
        {
            var found = deep_lookup(child, name);
            if (found is not null) return found;
        }

        return null;
    }

    public void add_reference(ISymbol symbol, ISymbol reference)
    {
        if (!_references.TryGetValue(symbol.name, out var refs))
        {
            refs = [];
            _references[symbol.name] = refs;
        }

        refs.Add(reference);
    }

    internal IEnumerable<KeyValuePair<string, IReadOnlyList<ISymbol>>> enumerate_reference_buckets()
    {
        foreach (var pair in _references) yield return new KeyValuePair<string, IReadOnlyList<ISymbol>>(pair.Key, pair.Value);
    }

    internal void restore_reference_bucket(string symbolName, IEnumerable<ISymbol> references)
    {
        _references[symbolName] = [.. references];
    }

    private static void collect_symbols_in_scope(IScope scope, List<ISymbol> result)
    {
        result.AddRange(scope.get_members());

        if (scope.parent is not null) collect_symbols_in_scope(scope.parent, result);
    }
}