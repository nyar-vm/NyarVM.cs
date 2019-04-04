namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     数组字面量表达式，如 <c>[a, b, c]</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// let numbers = [1, 2, 3, 4, 5];            // Elements = [LiteralExpr(1), ..., LiteralExpr(5)]
/// let mixed = [42, "hello", true];          // Elements 可以是任意表达式
/// let empty = [];                           // Elements = []
/// </code>
public sealed record TermLiteralArrayNode : TermNode
{
    /// <summary>
    ///     数组元素列表
    /// </summary>
    public IReadOnlyList<TermNode> elements { get; init; } = [];
}