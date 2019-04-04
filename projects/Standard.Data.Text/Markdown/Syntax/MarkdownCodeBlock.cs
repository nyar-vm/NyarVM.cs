namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     代码块节点
/// </summary>
public sealed record MarkdownCodeBlock : MarkdownNode
{
    public MarkdownCodeBlock(string? language, string content)
    {
        this.language = language;
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.code_block;


    /// <summary>
    ///     语言标识符
    /// </summary>
    public string? language { get; init; }


    /// <summary>
    ///     代码内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}