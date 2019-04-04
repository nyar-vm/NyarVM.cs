namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     粗体节点
/// </summary>
public sealed record MarkdownStrong : MarkdownNode
{
    public MarkdownStrong(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.strong;


    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}