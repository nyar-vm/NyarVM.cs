namespace Std.Data.Text.Syntax;

/// <summary>
///     轻量级值类型，持有绝对位置信息的红树节点
/// </summary>
public readonly struct RedNode
{
    /// <summary>
    ///     对应的绿树节点
    /// </summary>
    internal GreenNode _green { get; }

    /// <summary>
    ///     所属的语法树
    /// </summary>
    internal SyntaxTree _tree { get; }

    /// <summary>
    ///     节点在源文本中的起始位置
    /// </summary>
    internal int _start { get; }

    /// <summary>
    ///     节点类型
    /// </summary>
    public NodeKind kind => _green.kind;

    /// <summary>
    ///     节点在源文本中的跨度
    /// </summary>
    public TextSpan span => new(_start, _green.width);

    /// <summary>
    ///     子节点数量
    /// </summary>
    public int child_count => _green.child_count;

    /// <summary>
    ///     是否为叶子节点
    /// </summary>
    public bool is_leaf => _green.is_leaf;

    /// <summary>
    ///     构造红树节点
    /// </summary>
    internal RedNode(GreenNode green, SyntaxTree tree, int start)
    {
        _green = green;
        _tree = tree;
        _start = start;
    }

    /// <summary>
    ///     父节点。若语法树启用了父节点缓存，则查表获取；否则遍历查找。
    /// </summary>
    public RedNode? parent
    {
        get
        {
            if (_tree is null) return null;

            if (_tree.try_get_parent(this, out var parent)) return parent;

            var root = _tree.get_red_root();
            return find_parent(root, this);
        }
    }

    /// <summary>
    ///     获取指定索引的子节点
    /// </summary>
    public RedNode get_child(int index)
    {
        var childGreen = _green.get_child(index);
        var childStart = _start;
        for (var i = 0; i < index; i++)
        {
            var sibling = _green.get_child(i);
            if (sibling is not null) childStart += sibling.width;
        }

        return new RedNode(childGreen!, _tree, childStart);
    }

    /// <summary>
    ///     所有子节点
    /// </summary>
    public IEnumerable<RedNode> children
    {
        get
        {
            for (var i = 0; i < child_count; i++) yield return get_child(i);
        }
    }

    /// <summary>
    ///     所有后代节点（深度优先遍历）
    /// </summary>
    public IEnumerable<RedNode> descendants()
    {
        foreach (var child in children)
        {
            yield return child;
            foreach (var descendant in child.descendants()) yield return descendant;
        }
    }

    /// <summary>
    ///     所有祖先节点
    /// </summary>
    public IEnumerable<RedNode> ancestors()
    {
        var current = parent;
        while (current is not null)
        {
            yield return current.Value;
            current = current.Value.parent;
        }
    }

    /// <summary>
    ///     在以 root 为根的子树中查找 target 的父节点
    /// </summary>
    private static RedNode? find_parent(RedNode root, RedNode target)
    {
        for (var i = 0; i < root.child_count; i++)
        {
            var child = root.get_child(i);
            if (ReferenceEquals(child._green, target._green) && child._start == target._start) return root;

            var found = find_parent(child, target);
            if (found is not null) return found;
        }

        return null;
    }
}