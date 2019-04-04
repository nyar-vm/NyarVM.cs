namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     列表节点
/// </summary>
public sealed record MarkdownList : MarkdownNode
{
    public MarkdownList(bool isOrdered, IReadOnlyList<MarkdownNode> items)
    {
        is_ordered = isOrdered;
        this.items = items;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.list;


    /// <summary>
    ///     是否为有序列表
    /// </summary>
    public bool is_ordered { get; init; }


    /// <summary>
    ///     列表项（可为 MarkdownListItem 或 MarkdownTaskListItem）
    /// </summary>
    public IReadOnlyList<MarkdownNode> items { get; init; } = [];
}