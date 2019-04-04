namespace Nyar.Language.Valkyrie.TypeChecker;

public sealed class Scope
{
    private readonly List<Scope> _children = [];
    private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.Ordinal);

    public Scope(Scope? parent = null)
    {
        this.parent = parent;
        this.parent?._children.Add(this);
    }

    public Scope? parent { get; }

    public IReadOnlyList<Scope> children => _children;
    public IReadOnlyDictionary<string, Symbol> symbols => _symbols;

    public bool define(Symbol symbol)
    {
        if (!_symbols.TryAdd(symbol.name, symbol)) return false;

        return true;
    }

    /// <summary>PascalCase 别名，兼容 TypeChecker 的调用</summary>
    public bool Define(Symbol symbol)
    {
        return define(symbol);
    }

    public Symbol? resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol)) return symbol;

        return parent?.resolve(name);
    }

    /// <summary>PascalCase 别名</summary>
    public Symbol? Resolve(string name)
    {
        return resolve(name);
    }

    public Symbol? resolve_local(string name)
    {
        return _symbols.GetValueOrDefault(name);
    }

    /// <summary>PascalCase 别名</summary>
    public Symbol? ResolveLocal(string name)
    {
        return resolve_local(name);
    }

    public IEnumerable<Symbol> get_all_accessible_symbols()
    {
        var result = new List<Symbol>(_symbols.Values);

        if (parent is not null) result.AddRange(parent.get_all_accessible_symbols());

        return result;
    }

    /// <summary>PascalCase 别名</summary>
    public IEnumerable<Symbol> GetAllAccessibleSymbols()
    {
        return get_all_accessible_symbols();
    }
}