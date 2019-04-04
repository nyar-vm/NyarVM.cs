namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     二元运算表达式
/// </summary>
/// <para>支持所有二元运算符：</para>
/// <code>
/// a + b     // Operator = Addition
/// x &gt; 0  // Operator = GreaterThan
/// a &amp;&amp; b  // Operator = LogicalAnd
/// </code>
public sealed record TermBinaryExpression : TermNode
{
    public TermBinaryExpression()
    {
    }

    public TermBinaryExpression(TermBinaryOperator binary, ValkyrieNode left, ValkyrieNode right)
    {
        @operator = binary;
        this.left = left;
        this.right = right;
    }

    /// <summary>
    ///     运算符
    /// </summary>
    public TermBinaryOperator @operator { get; init; }

    /// <summary>
    ///     左操作数
    /// </summary>
    public ValkyrieNode left { get; init; } = new IdentifierNode();


    /// <summary>
    ///     右操作数
    /// </summary>
    public ValkyrieNode right { get; init; } = new IdentifierNode();
}