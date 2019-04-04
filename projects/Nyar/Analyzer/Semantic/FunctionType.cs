namespace Nyar.Analyzer.Semantic;

public sealed class FunctionType : IType
{
    public FunctionType(IReadOnlyList<IType> parameterTypes, IType returnType)
    {
        parameter_types = parameterTypes;
        return_type = returnType;
    }

    public IReadOnlyList<IType> parameter_types { get; internal set; }
    public IType return_type { get; internal set; }
    public string name => $"({string.Join(", ", parameter_types)}) -> {return_type.name}";

    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments => [];
    public IReadOnlyList<ISymbol> members => [];

    public bool is_assignable_from(IType other)
    {
        return equals(other);
    }

    public bool is_assignable_to(IType other)
    {
        return equals(other);
    }

    public bool is_subtype_of(IType other)
    {
        return equals(other);
    }

    public bool equals(IType? other)
    {
        return other is FunctionType f && f.name == name;
    }
}