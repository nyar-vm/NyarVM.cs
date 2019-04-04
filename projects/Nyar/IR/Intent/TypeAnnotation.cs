namespace Nyar.IR.Intent;

public sealed record TypeAnnotation
{
    public static readonly TypeAnnotation unknown = new(TypeKind.unknown);

    public TypeAnnotation(TypeKind kind, string? name = null)
    {
        this.kind = kind;
        this.name = name;
    }

    public TypeKind kind { get; init; }

    public string? name { get; init; }

    public static TypeAnnotation boolean => new(TypeKind.boolean, "bool");

    public static TypeAnnotation unit => new(TypeKind.unit, "unit");

    public static TypeAnnotation integer(int bits)
    {
        return new TypeAnnotation(TypeKind.integer, $"i{bits}");
    }

    public static TypeAnnotation @float(int bits)
    {
        return new TypeAnnotation(TypeKind.@float, $"f{bits}");
    }

    public static TypeAnnotation pointer(TypeAnnotation pointee)
    {
        return new TypeAnnotation(TypeKind.pointer, $"*{pointee}");
    }

    public static TypeAnnotation tuple(IReadOnlyList<TypeAnnotation> elements)
    {
        return new TypeAnnotation(TypeKind.tuple, $"({string.Join(", ", elements.Select(e => e.ToString()))})");
    }

    public static TypeAnnotation function(TypeAnnotation param, TypeAnnotation ret)
    {
        return new TypeAnnotation(TypeKind.function, $"({param}) -> {ret}");
    }

    public override string ToString()
    {
        return name ?? kind.ToString();
    }
}