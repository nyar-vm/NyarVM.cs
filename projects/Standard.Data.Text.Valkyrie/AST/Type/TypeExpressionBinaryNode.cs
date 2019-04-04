namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     T | U
///     T &amp; U
///     T -> U
/// </summary>
public record TypeExpressionBinaryNode : TypeNode
{
    public TypeExpressionBinaryNode()
    {
    }

    public TypeExpressionBinaryNode(TypeBinaryOperator op, TypeNode lhs, TypeNode rhs)
    {
        @operator = op;
        this.lhs = lhs;
        this.rhs = rhs;
    }

    public TypeBinaryOperator @operator { get; init; }

    public TypeNode lhs { get; init; } = null!;

    public TypeNode rhs { get; init; } = null!;
}