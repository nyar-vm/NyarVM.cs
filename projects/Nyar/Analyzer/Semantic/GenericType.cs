namespace Nyar.Analyzer.Semantic;

public sealed class GenericType : IType
{
    public GenericType(string name, IReadOnlyList<IType> typeArguments)
    {
        this.name = name;
        type_arguments = typeArguments;
    }

    public string name { get; }
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments { get; internal set; }
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        if (other is not GenericType g || g.name != name || g.type_arguments.Count != type_arguments.Count)
            return false;

        for (var i = 0; i < type_arguments.Count; i++)
            if (!type_arguments[i].is_assignable_from(g.type_arguments[i]))
                return false;

        return true;
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
        if (other is not GenericType g || g.name != name || g.type_arguments.Count != type_arguments.Count)
            return false;

        for (var i = 0; i < type_arguments.Count; i++)
            if (!type_arguments[i].equals(g.type_arguments[i]))
                return false;

        return true;
    }
}