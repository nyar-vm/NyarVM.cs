namespace Std.Data.Text.Syntax;

/// <summary>
///     语法树变更事件，描述编辑前后两棵树的差异
/// </summary>
public readonly struct TreeChangeEvent
{
    /// <summary>
    ///     编辑前的旧语法树
    /// </summary>
    public SyntaxTree old_tree { get; }

    /// <summary>
    ///     编辑后的新语法树
    /// </summary>
    public SyntaxTree new_tree { get; }

    /// <summary>
    ///     受影响的源文本范围
    /// </summary>
    public TextSpan changed_span { get; }

    /// <summary>
    ///     被替换的绿树节点列表
    /// </summary>
    public IReadOnlyList<GreenNode> replaced_nodes { get; }

    /// <summary>
    ///     触发此次变更的编辑操作
    /// </summary>
    public Edit source_edit { get; }

    public TreeChangeEvent(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        TextSpan changedSpan,
        IReadOnlyList<GreenNode> replacedNodes,
        Edit sourceEdit)
    {
        old_tree = oldTree;
        new_tree = newTree;
        changed_span = changedSpan;
        replaced_nodes = replacedNodes;
        source_edit = sourceEdit;
    }

    public override string ToString()
    {
        return $"TreeChange: {changed_span}, {replaced_nodes.Count} nodes replaced";
    }
}