namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     脚注引用节点 [^1]
/// </summary>
public sealed record MarkdownFootnote : MarkdownNode
{
    public MarkdownFootnote(string label)
    {
        this.label = label;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.footnote;


    /// <summary>
    ///     脚注标识
    /// </summary>
    public string label { get; init; } = string.Empty;
}