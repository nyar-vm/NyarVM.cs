namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     Markdown 文档根节点
/// </summary>
public sealed record MarkdownDocument : MarkdownNode
{
    public MarkdownDocument(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.document;


    /// <summary>
    ///     子节点列表
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}