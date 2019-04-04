namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     删除线节点
/// </summary>
public sealed record MarkdownStrikethrough : MarkdownNode
{
    public MarkdownStrikethrough(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.strikethrough;


    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}