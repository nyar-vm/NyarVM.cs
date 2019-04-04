namespace Nyar.Analyzer.Semantic;

public sealed class UnknownType : IType
{
    public static UnknownType instance { get; } = new();

    public string name => "?";
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        return false;
    }

    public bool is_assignable_to(IType other)
    {
        return false;
    }

    public bool is_subtype_of(IType other)
    {
        return false;
    }

    public bool equals(IType? other)
    {
        return ReferenceEquals(this, other);
    }
}