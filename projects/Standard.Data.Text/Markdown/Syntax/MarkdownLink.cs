namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     链接节点
/// </summary>
public sealed record MarkdownLink : MarkdownNode
{
    public MarkdownLink(string url, string? title, IReadOnlyList<MarkdownNode> children)
    {
        this.url = url;
        this.title = title;
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.link;


    /// <summary>
    ///     链接地址
    /// </summary>
    public string url { get; init; } = string.Empty;


    /// <summary>
    ///     链接标题
    /// </summary>
    public string? title { get; init; }


    /// <summary>
    ///     显示内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}