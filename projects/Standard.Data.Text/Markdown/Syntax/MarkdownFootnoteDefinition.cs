namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     脚注定义节点 [^1]: text
/// </summary>
public sealed record MarkdownFootnoteDefinition : MarkdownNode
{
    public MarkdownFootnoteDefinition(string label, IReadOnlyList<MarkdownNode> children)
    {
        this.label = label;
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.footnote_definition;


    /// <summary>
    ///     脚注标识
    /// </summary>
    public string label { get; init; } = string.Empty;


    /// <summary>
    ///     脚注内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}