namespace Std.Data.Text.Syntax;

/// <summary>
///     语法树访问递归模式
/// </summary>
public enum VisitRecursionMode
{
    /// <summary>
    ///     继续遍历子节点
    /// </summary>
    @continue,

    /// <summary>
    ///     跳过当前节点的子节点，继续遍历兄弟节点
    /// </summary>
    skip,

    /// <summary>
    ///     停止遍历，不再访问任何后续节点
    /// </summary>
    stop
}