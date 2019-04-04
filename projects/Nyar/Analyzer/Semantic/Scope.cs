namespace Nyar.Analyzer.Semantic;

public sealed class Scope : IScope
{
    private readonly Dictionary<string, Scope> _child_scopes;
    private readonly Scope? _parent;
    private readonly Dictionary<string, ISymbol> _symbols;

    public Scope(string? name = null, Scope? parent = null)
    {
        this.name = name;
        _parent = parent;
        _symbols = new Dictionary<string, ISymbol>();
        _child_scopes = new Dictionary<string, Scope>();
    }

    public IScope? parent => _parent;
    public string? name { get; }

    public ISymbol? lookup(string name)
    {
        return _symbols.GetValueOrDefault(name);
    }

    public ISymbol? lookup_recursive(string name)
    {
        var symbol = lookup(name);
        if (symbol is not null) return symbol;

        return _parent?.lookup_recursive(name);
    }

    public IScope? get_child_scope(string name)
    {
        return _child_scopes.GetValueOrDefault(name);
    }

    public IEnumerable<ISymbol> get_members()
    {
        return _symbols.Values;
    }

    public IEnumerable<IScope> get_child_scopes()
    {
        return _child_scopes.Values;
    }

    public bool define(ISymbol symbol)
    {
        if (!_symbols.TryAdd(symbol.name, symbol)) return false;

        if (symbol is Symbol concreteSymbol) concreteSymbol.containing_scope = this;

        return true;
    }

    public Scope get_or_create_child_scope(string name)
    {
        if (_child_scopes.TryGetValue(name, out var existing)) return existing;

        var child = new Scope(name, this);
        _child_scopes[name] = child;
        return child;
    }

    internal void attach_child_scope(Scope child)
    {
        if (string.IsNullOrWhiteSpace(child.name)) return;

        _child_scopes[child.name] = child;
    }

    public bool undefine(string name)
    {
        return _symbols.Remove(name);
    }
}