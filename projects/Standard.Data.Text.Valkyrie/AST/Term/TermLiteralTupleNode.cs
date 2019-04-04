namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     元组字面量表达式，如 <c>(a, b)</c> 或 <c>(1, "hello", true)</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// let t = (1, 2);                            // 二元组
/// return (core, std);                        // 返回元组
/// let empty = ();                            // 空元组 / unit
/// </code>
public sealed record TermLiteralTupleNode : TermNode
{
    /// <summary>
    ///     元组元素列表
    /// </summary>
    public IReadOnlyList<TermNode> elements { get; init; } = [];
}