namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     引用式链接定义节点 [id]: url "title"
/// </summary>
public sealed record MarkdownReferenceLinkDefinition : MarkdownNode
{
    public MarkdownReferenceLinkDefinition(string label, string url, string? title = null)
    {
        this.label = label;
        this.url = url;
        this.title = title;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.reference_link_definition;


    /// <summary>
    ///     链接标识
    /// </summary>
    public string label { get; init; } = string.Empty;


    /// <summary>
    ///     链接地址
    /// </summary>
    public string url { get; init; } = string.Empty;


    /// <summary>
    ///     链接标题
    /// </summary>
    public string? title { get; init; }
}