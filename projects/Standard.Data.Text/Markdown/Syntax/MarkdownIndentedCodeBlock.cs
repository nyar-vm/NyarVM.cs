namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     缩进代码块节点（4 空格缩进）
/// </summary>
public sealed record MarkdownIndentedCodeBlock : MarkdownNode
{
    public MarkdownIndentedCodeBlock(string content)
    {
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.indented_code_block;


    /// <summary>
    ///     代码内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}