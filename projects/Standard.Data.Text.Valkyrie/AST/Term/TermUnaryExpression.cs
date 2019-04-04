namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     一元运算表达式（前缀或后缀）
/// </summary>
/// <para>支持以下一元运算符：</para>
/// <code>
/// -x              // Operator = Negate,     IsPrefix = true
/// !flag           // Operator = LogicalNot, IsPrefix = true
/// i++             // Operator = Increment,  IsPrefix = false
/// </code>
public sealed record TermUnaryExpression : TermNode
{
    public TermUnaryExpression(TermNode operand)
    {
        this.operand = operand;
    }

    public TermUnaryExpression(TermUnaryOperator unary, TermNode operand)
    {
        @operator = unary;
        this.operand = operand;
    }

    /// <summary>
    ///     一元运算符
    /// </summary>
    public TermUnaryOperator @operator { get; init; }

    /// <summary>
    ///     是否前缀运算符（true 为前缀，false 为后缀）
    /// </summary>
    public bool is_prefix { get; init; } = true;

    /// <summary>
    ///     操作数
    /// </summary>
    public TermNode operand { get; init; }
}