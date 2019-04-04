namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     标题节点
/// </summary>
public sealed record MarkdownHeading : MarkdownNode
{
    public MarkdownHeading(int level, IReadOnlyList<MarkdownNode> children)
    {
        this.level = level;
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.heading;


    /// <summary>
    ///     标题级别（1-6）
    /// </summary>
    public int level { get; init; }


    /// <summary>
    ///     标题内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}