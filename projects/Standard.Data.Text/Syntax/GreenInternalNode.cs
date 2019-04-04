namespace Std.Data.Text.Syntax;

/// <summary>
///     Green 树内部节点（分支节点），包含一组子节点。
///     总 Width 为所有子节点 Width 之和。
/// </summary>
public sealed class GreenInternalNode : GreenNode
{
    private readonly GreenNode[] _children;

    /// <summary>
    ///     使用指定的 Kind 和子节点列表创建内部节点
    /// </summary>
    public GreenInternalNode(NodeKind kind, IReadOnlyList<GreenNode> children)
    {
        this.kind = kind;
        _children = new GreenNode[children.Count];
        for (var i = 0; i < children.Count; i++) _children[i] = children[i];

        width = _children.Sum(c => c.width);
    }

    /// <summary>
    ///     使用指定的 Kind 和子节点数组创建内部节点
    /// </summary>
    public GreenInternalNode(NodeKind kind, GreenNode[] children)
    {
        this.kind = kind;
        _children = children;
        width = _children.Sum(c => c.width);
    }

    /// <summary>
    ///     使用指定的 Kind、Width 和子节点列表创建内部节点
    /// </summary>
    public GreenInternalNode(NodeKind kind, int width, IReadOnlyList<GreenNode> children)
    {
        this.kind = kind;
        this.width = width;
        _children = new GreenNode[children.Count];
        for (var i = 0; i < children.Count; i++) _children[i] = children[i];
    }

    /// <summary>
    ///     节点类型标识
    /// </summary>
    public override NodeKind kind { get; }

    /// <summary>
    ///     节点在源码中的总字符跨度
    /// </summary>
    public override int width { get; }

    /// <summary>
    ///     子节点数量
    /// </summary>
    public override int child_count => _children.Length;

    /// <summary>
    ///     获取指定索引的子节点
    /// </summary>
    public override GreenNode? get_child(int index)
    {
        if (index < 0 || index >= _children.Length) return null;

        return _children[index];
    }

    /// <summary>
    ///     获取所有子节点（只读）
    /// </summary>
    public IReadOnlyList<GreenNode> get_children()
    {
        return _children;
    }
}