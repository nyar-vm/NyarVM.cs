namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     列表项节点
/// </summary>
public sealed record MarkdownListItem : MarkdownNode
{
    public MarkdownListItem(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.list_item;


    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}