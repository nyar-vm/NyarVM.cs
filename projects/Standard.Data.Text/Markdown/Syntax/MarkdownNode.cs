namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     Markdown AST 节点基类
/// </summary>
public abstract record MarkdownNode
{
    /// <summary>
    ///     节点类型
    /// </summary>
    public abstract MarkdownNodeType node_type { get; }
}