namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     偏移索引表达式，如 <c>array⁅0⁆</c>、<c>matrix⁅0, 1, 2⁆</c>
/// </summary>
/// <para>语义：</para>
/// <list type="bullet">
///     <item>
///         <description><c>a⁅0⁆</c> 表示偏移为 0 的第一个元素。</description>
///     </item>
///     <item>
///         <description><c>a::[0]</c> 是 <c>a⁅0⁆</c> 的等价别名。</description>
///     </item>
///     <item>
///         <description>多维偏移索引可写作 <c>matrix⁅i, j, k⁆</c>。</description>
///     </item>
/// </list>
public sealed record TermOffsetExpression : TermNode
{
    /// <summary>
    ///     被索引的目标对象
    /// </summary>
    public ValkyrieNode target { get; init; } = new IdentifierNode();

    /// <summary>
    ///     偏移索引表达式
    /// </summary>
    public IReadOnlyList<ValkyrieNode> indices { get; init; } = [];
}