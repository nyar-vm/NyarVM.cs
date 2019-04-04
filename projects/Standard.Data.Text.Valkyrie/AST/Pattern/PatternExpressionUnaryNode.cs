namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式 —— 匹配字面常量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// case ?x;
/// </code>
public sealed record PatternExpressionUnaryNode : PatternNode
{
    public PatternUnaryOperator @operator { get; init; }
    public PatternNode operand { get; init; }
}