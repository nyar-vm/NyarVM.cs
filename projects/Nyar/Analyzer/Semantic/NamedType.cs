namespace Nyar.Analyzer.Semantic;

public sealed class NamedType : IType
{
    public NamedType(string name, string kindTag, IType? baseType = null,
        IReadOnlyList<IType>? typeArguments = null, IReadOnlyList<ISymbol>? members = null)
    {
        this.name = name;
        kind_tag = kindTag;
        base_type = baseType;
        type_arguments = typeArguments ?? [];
        this.members = members ?? [];
    }

    public string kind_tag { get; }

    public bool is_tuple_type => string.Equals(kind_tag, "tuple", StringComparison.Ordinal);

    public bool is_union_type => string.Equals(kind_tag, "union", StringComparison.Ordinal);

    public bool is_intersection_type => string.Equals(kind_tag, "intersection", StringComparison.Ordinal);

    public bool supports_structural_row_typing => members.Count > 0
                                                  && kind_tag is "trait" or "class" or "struct" or "component" or "unite";

    public bool is_any_type => string.Equals(name, "any", StringComparison.Ordinal) &&
                               string.Equals(kind_tag, "any", StringComparison.Ordinal);

    public string name { get; }
    public IType? base_type { get; internal set; }
    public IReadOnlyList<IType> type_arguments { get; internal set; }
    public IReadOnlyList<ISymbol> members { get; internal set; }

    public bool is_assignable_from(IType other)
    {
        if (is_any_type) return true;

        // 跨表示兼容：同名 PrimitiveType
        if (other is PrimitiveType p && p.name == name) return true;

        if (is_union_type) return type_arguments.Any(member => member.is_assignable_from(other));

        if (is_intersection_type) return type_arguments.All(member => member.is_assignable_from(other));

        if (other is NamedType otherNamedType)
        {
            if (otherNamedType.is_union_type) return otherNamedType.type_arguments.All(is_assignable_from);

            if (otherNamedType.is_intersection_type) return otherNamedType.type_arguments.Any(is_assignable_from);
        }

        if (equals(other)) return true;

        if (other is NamedType n && n.name == name && n.type_arguments.Count == type_arguments.Count)
        {
            for (var i = 0; i < type_arguments.Count; i++)
                if (!type_arguments[i].is_assignable_from(n.type_arguments[i]))
                    return false;

            return true;
        }

        if (supports_structural_row_typing && TypeCompatibility.row_assignable_from(members, other, true))
            return true;

        return base_type?.is_assignable_from(other) ?? false;
    }

    public bool is_assignable_to(IType other)
    {
        return other.is_assignable_from(this) || (base_type?.is_assignable_to(other) ?? false);
    }

    public bool is_subtype_of(IType other)
    {
        if (other is NamedType namedType && namedType.is_any_type) return true;

        return equals(other)
               || (supports_structural_row_typing && TypeCompatibility.row_assignable_from(members, other, true))
               || (base_type?.is_subtype_of(other) ?? false);
    }

    public bool equals(IType? other)
    {
        // 跨表示兼容：同名 PrimitiveType
        if (other is PrimitiveType p && p.name == name && type_arguments.Count == 0) return true;

        if (other is not NamedType n || n.name != name || n.type_arguments.Count != type_arguments.Count) return false;

        for (var i = 0; i < type_arguments.Count; i++)
            if (!type_arguments[i].equals(n.type_arguments[i]))
                return false;

        return true;
    }
}