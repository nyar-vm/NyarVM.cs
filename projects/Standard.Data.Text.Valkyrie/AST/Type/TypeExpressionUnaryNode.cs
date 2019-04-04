namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     T?
///     +T
///     -T
/// </summary>
public sealed record TypeExpressionUnaryNode : TypeNode
{
    public TypeExpressionUnaryNode(TypeUnaryOperator op, TypeNode operand, bool isPrefix = false)
    {
        @operator = op;
        this.operand = operand;
        is_prefix = isPrefix;
    }

    public TypeUnaryOperator @operator { get; init; }

    public TypeNode operand { get; init; } = null!;

    public bool is_prefix { get; init; }
}