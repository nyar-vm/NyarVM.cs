namespace Nyar.Analyzer.Semantic;

public sealed class RowType : IType
{
    public RowType(IReadOnlyList<ISymbol> members, bool isOpen = true, string? name = null)
    {
        this.members = members;
        is_open = isOpen;
        this.name = string.IsNullOrWhiteSpace(name)
            ? isOpen ? "{ .. }" : "{ }"
            : name;
    }

    public bool is_open { get; }
    public string name { get; internal set; }
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [];
    public IReadOnlyList<ISymbol> members { get; internal set; }

    public bool is_assignable_from(IType other)
    {
        return TypeCompatibility.row_assignable_from(this, other);
    }

    public bool is_assignable_to(IType other)
    {
        return other.is_assignable_from(this);
    }

    public bool is_subtype_of(IType other)
    {
        return equals(other) || TypeCompatibility.row_assignable_from(this, other);
    }

    public bool equals(IType? other)
    {
        if (other is not RowType rowType || rowType.is_open != is_open) return false;

        return TypeCompatibility.have_equivalent_rows(members, rowType.members);
    }
}