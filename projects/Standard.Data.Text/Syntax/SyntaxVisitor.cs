namespace Std.Data.Text.Syntax;

/// <summary>
///     语法树访问器抽象基类，支持短路遍历控制
/// </summary>
public abstract class SyntaxVisitor
{
    /// <summary>
    ///     访问语法节点的默认行为，返回递归模式
    /// </summary>
    public virtual VisitRecursionMode visit_default(SyntaxNode node)
    {
        return VisitRecursionMode.@continue;
    }

    /// <summary>
    ///     从指定节点开始深度优先遍历，返回最终的递归模式
    /// </summary>
    public VisitRecursionMode visit(SyntaxNode node)
    {
        var mode = node.accept(this);

        if (mode == VisitRecursionMode.stop) return VisitRecursionMode.stop;

        if (mode == VisitRecursionMode.skip) return VisitRecursionMode.@continue;

        return visit_children(node);
    }

    /// <summary>
    ///     遍历节点的所有子节点，根据访问结果控制递归行为
    /// </summary>
    protected VisitRecursionMode visit_children(SyntaxNode node)
    {
        var greenNode = node._green;
        var childCount = greenNode.child_count;
        var offset = node.span.start;

        var childOffset = offset;

        for (var i = 0; i < childCount; i++)
        {
            var childGreen = greenNode.get_child(i);

            if (childGreen is null) continue;

            var childNode = NodeFactory.create(childGreen.kind, childGreen, node._tree, childOffset);

            if (childNode is null)
            {
                childOffset += childGreen.width;
                continue;
            }

            var result = visit(childNode);

            if (result == VisitRecursionMode.stop) return VisitRecursionMode.stop;

            childOffset += childGreen.width;
        }

        return VisitRecursionMode.@continue;
    }
}