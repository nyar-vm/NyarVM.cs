namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     let f: T -> U;
///     let f: (T) -> U;
///     let f: micro(T) -> U;
/// </summary>
public record TypeMicroNode : TypeNode
{
    public TypeMicroNode()
    {
    }

    public TypeMicroNode(TypeNode parameterType, TypeNode returnType)
    {
        parameter_type = parameterType;
        return_type = returnType;
    }

    public TypeNode parameter_type { get; init; } = null!;

    public TypeNode return_type { get; init; } = null!;
}