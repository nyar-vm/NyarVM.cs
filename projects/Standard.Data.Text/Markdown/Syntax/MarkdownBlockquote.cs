namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     引用块节点
/// </summary>
public sealed record MarkdownBlockquote : MarkdownNode
{
    public MarkdownBlockquote(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.blockquote;


    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}