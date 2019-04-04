namespace Nyar.Analyzer.Semantic;

public sealed class ArrayType : IType
{
    public ArrayType(IType elementType)
    {
        element_type = elementType;
    }

    public IType element_type { get; internal set; }
    public string name => $"{element_type.name}[]";
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [element_type];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        return other is ArrayType a && element_type.is_assignable_from(a.element_type);
    }

    public bool is_assignable_to(IType other)
    {
        return other.is_assignable_from(this);
    }

    public bool is_subtype_of(IType other)
    {
        return equals(other);
    }

    public bool equals(IType? other)
    {
        return other is ArrayType a && element_type.equals(a.element_type);
    }
}