namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     索引表达式节点，如 <c>array[i]</c>
/// </summary>
public sealed record TermIndexExpression : TermNode
{
    /// <summary>
    ///     索引操作的目标表达式
    /// </summary>
    public TermNode target { get; init; } = new TermLiteralNamePathNode();

    /// <summary>
    ///     索引值表达式
    /// </summary>
    public TermNode index { get; init; } = new TermLiteralNumberNode();
}