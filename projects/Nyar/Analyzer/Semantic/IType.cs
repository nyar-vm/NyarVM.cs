namespace Nyar.Analyzer.Semantic;

public interface IType
{
    string name { get; }
    IType? base_type { get; }
    IReadOnlyList<IType> type_arguments { get; }
    IReadOnlyList<ISymbol> members { get; }
    bool is_assignable_from(IType other);
    bool is_assignable_to(IType other);
    bool is_subtype_of(IType other);
    bool equals(IType? other);
}