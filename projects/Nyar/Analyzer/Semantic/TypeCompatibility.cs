namespace Nyar.Analyzer.Semantic;

internal static class TypeCompatibility
{
    public static bool row_assignable_from(RowType rowType, IType other)
    {
        return row_assignable_from(rowType.members, other, rowType.is_open);
    }

    public static bool row_assignable_from(IReadOnlyList<ISymbol> requiredMembers, IType other, bool isOpen)
    {
        var requiredMethods = get_row_methods(requiredMembers);
        var candidateMethods = get_row_methods(other.members);
        foreach (var requiredMethod in requiredMethods)
        {
            var candidate = candidateMethods.FirstOrDefault(member =>
                string.Equals(member.name, requiredMethod.name, StringComparison.Ordinal));
            if (candidate is null) return false;

            if (requiredMethod.type is null || candidate.type is null) return false;

            if (!requiredMethod.type.is_assignable_from(candidate.type)) return false;
        }

        if (isOpen) return true;

        return candidateMethods.Count == requiredMethods.Count;
    }

    public static bool have_equivalent_rows(IReadOnlyList<ISymbol> leftMembers, IReadOnlyList<ISymbol> rightMembers)
    {
        return row_assignable_from(leftMembers, new RowType(rightMembers, false), false)
               && row_assignable_from(rightMembers, new RowType(leftMembers, false), false);
    }

    private static IReadOnlyList<ISymbol> get_row_methods(IReadOnlyList<ISymbol> members)
    {
        return
        [
            .. members
                .Where(member => member.kind == SymbolKind.method)
                .OrderBy(member => member.name, StringComparer.Ordinal)
        ];
    }
}