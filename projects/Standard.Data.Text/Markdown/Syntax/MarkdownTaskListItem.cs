namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     任务列表项节点
/// </summary>
public sealed record MarkdownTaskListItem : MarkdownNode
{
    public MarkdownTaskListItem(bool isChecked, IReadOnlyList<MarkdownNode> children)
    {
        is_checked = isChecked;
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.task_list_item;


    /// <summary>
    ///     是否已勾选
    /// </summary>
    public bool is_checked { get; init; }


    /// <summary>
    ///     子节点
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}