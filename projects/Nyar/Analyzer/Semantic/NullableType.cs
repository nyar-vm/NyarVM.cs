namespace Nyar.Analyzer.Semantic;

public sealed class NullableType : IType
{
    public NullableType(IType innerType)
    {
        inner_type = innerType;
    }

    public IType inner_type { get; internal set; }
    public string name => $"{inner_type.name}?";
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [inner_type];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        if (other is NullableType n) return inner_type.is_assignable_from(n.inner_type);

        return inner_type.is_assignable_from(other);
    }

    public bool is_assignable_to(IType other)
    {
        return other.is_assignable_from(this);
    }

    public bool is_subtype_of(IType other)
    {
        return equals(other) || inner_type.is_subtype_of(other);
    }

    public bool equals(IType? other)
    {
        return other is NullableType n && inner_type.equals(n.inner_type);
    }
}