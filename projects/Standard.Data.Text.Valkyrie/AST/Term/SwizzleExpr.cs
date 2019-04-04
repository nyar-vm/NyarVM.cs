namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     Swizzle 表达式节点，如 <c>vector.xy</c>、<c>color.rgb</c>
/// </summary>
public sealed record SwizzleExpr : TermNode
{
    /// <summary>
    ///     Swizzle 操作的目标表达式
    /// </summary>
    public TermNode target { get; init; } = new TermLiteralNamePathNode();

    /// <summary>
    ///     Swizzle 分量字符串（如 "xy"、"rgb"）
    /// </summary>
    public string components { get; init; } = string.Empty;
}