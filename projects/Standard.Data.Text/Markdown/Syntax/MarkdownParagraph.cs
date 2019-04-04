namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     段落节点
/// </summary>
public sealed record MarkdownParagraph : MarkdownNode
{
    public MarkdownParagraph(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.paragraph;


    /// <summary>
    ///     段落内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}