namespace Nyar.Language.Valkyrie.TypeChecker;

public sealed class Symbol
{
    public Symbol(string name, ValkyrieType type, SymbolKind kind,
        bool isMutable = false, bool isExported = false)
    {
        this.name = name;
        this.type = type;
        this.kind = kind;
        is_mutable = isMutable;
        is_exported = isExported;
    }

    public string name { get; }
    public ValkyrieType type { get; }
    public SymbolKind kind { get; }
    public bool is_mutable { get; }
    public bool is_exported { get; }
}