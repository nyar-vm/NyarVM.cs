namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     高亮/标记节点 ==text==
/// </summary>
public sealed record MarkdownHighlight : MarkdownNode
{
    public MarkdownHighlight(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.highlight;


    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}