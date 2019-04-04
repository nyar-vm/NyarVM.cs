namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     序数索引表达式，如 <c>list[-1]</c>、<c>array[1]</c>、<c>matrix[1, 2, 3]</c>
/// </summary>
/// <para>语义：</para>
/// <list type="bullet">
///     <item>
///         <description><c>a[1]</c> 表示第一个元素，序号从 1 开始。</description>
///     </item>
///     <item>
///         <description>多维序数索引可写作 <c>matrix[i, j, k] </c>。</description>
///     </item>
/// </list>
public sealed record TermOrdinalExpression : TermNode
{
    /// <summary>
    ///     被索引的目标对象
    /// </summary>
    public ValkyrieNode target { get; init; } = new IdentifierNode();

    /// <summary>
    ///     序数索引表达式
    /// </summary>
    public IReadOnlyList<ValkyrieNode> indices { get; init; } = [];
}