using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 语法节点抽象基类，扩展 Oak SyntaxNode 提供 Valkyrie 特定的导航能力
/// </summary>
public abstract class ValkyrieSyntaxNode : SyntaxNode
{
    /// <summary>
    ///     子节点数量（从构造函数参数 GreenNode 捕获，因 SyntaxNode.Green 为 internal 不可访问）
    /// </summary>
    private readonly int _child_count;

    /// <summary>
    ///     初始化 Valkyrie 语法节点
    /// </summary>
    protected ValkyrieSyntaxNode(GreenNode green, SyntaxTree tree, int offset)
        : base(green, tree, offset)
    {
        _child_count = green.child_count;
    }

    /// <summary>
    ///     子节点数量
    /// </summary>
    public int child_count => _child_count;

    /// <summary>
    ///     收集所有指定类型的子节点
    /// </summary>
    protected IReadOnlyList<T> collect_children<T>() where T : SyntaxNode
    {
        var result = new List<T>();
        for (var i = 0; i < _child_count; i++)
            try
            {
                var child = child_node<T>(i);
                if (child is T t) result.Add(t);
            }
            catch
            {
                // 子节点类型不匹配，跳过
            }

        return result;
    }

    /// <summary>
    ///     获取最后一枚子标记
    /// </summary>
    protected SyntaxToken last_token()
    {
        return child_token(_child_count - 1);
    }

    public override VisitRecursionMode accept(SyntaxVisitor visitor)
    {
        return visitor.visit_default(this);
    }
}