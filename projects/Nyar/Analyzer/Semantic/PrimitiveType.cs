namespace Nyar.Analyzer.Semantic;

public sealed class PrimitiveType : IType
{
    public PrimitiveType(string name, IType? baseType = null)
    {
        this.name = name;
        base_type = baseType;
    }

    public string name { get; }
    public IType? base_type { get; internal set; }
    public IReadOnlyList<IType> type_arguments => [];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        return equals(other);
    }

    public bool is_assignable_to(IType other)
    {
        return equals(other) || (base_type?.is_assignable_to(other) ?? false);
    }

    public bool is_subtype_of(IType other)
    {
        return equals(other) || (base_type?.is_subtype_of(other) ?? false);
    }

    public bool equals(IType? other)
    {
        if (other is PrimitiveType p && p.name == name) return true;

        // 跨表示兼容：同名 PrimitiveType 与 NamedType 视为相等
        if (other is NamedType n && n.name == name) return true;

        return false;
    }
}