namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式 —— 匹配字面常量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// case a | b;
/// </code>
public sealed record PatternExpressionBinaryNode : PatternNode
{
    public PatternBinaryOperator @operator { get; init; }
    public PatternNode lhs { get; init; }
    public PatternNode rhs { get; init; }
}