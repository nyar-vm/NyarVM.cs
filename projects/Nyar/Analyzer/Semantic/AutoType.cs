namespace Nyar.Analyzer.Semantic;

public sealed class AutoType : IType
{
    public static AutoType instance { get; } = new();

    public string name => "auto";
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        return false;
    }

    public bool is_assignable_to(IType other)
    {
        return true;
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