namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     强调节点（斜体）
/// </summary>
public sealed record MarkdownEmphasis : MarkdownNode
{
    public MarkdownEmphasis(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.emphasis;


    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}