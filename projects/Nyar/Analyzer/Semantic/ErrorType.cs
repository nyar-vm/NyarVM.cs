namespace Nyar.Analyzer.Semantic;

public sealed class ErrorType : IType
{
    public static ErrorType instance { get; } = new();

    public string name => "<error>";
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        return true;
    }

    public bool is_assignable_to(IType other)
    {
        return true;
    }

    public bool is_subtype_of(IType other)
    {
        return true;
    }

    public bool equals(IType? other)
    {
        return ReferenceEquals(this, other);
    }
}