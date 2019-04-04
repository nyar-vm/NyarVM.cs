namespace Std.Data.Text.Syntax;

/// <summary>
///     Concrete Syntax Tree 构建器。
///     栈分配（ref struct），用于高效构建 Green 树。
///     用法：BeginNode → AddLeaf/AddNode → EndNode 循环 → Build
/// </summary>
public ref struct CstBuilder
{
    private GreenNode[] _current_children;
    private int _child_count;
    private readonly StackEntry[] _stack;
    private int _stack_depth;

    /// <summary>
    ///     使用指定的最大子节点容量和最大嵌套深度创建构建器
    /// </summary>
    /// <param name="maxChildCapacity">最大子节点容量。</param>
    /// <param name="maxNestingDepth">最大嵌套深度。</param>
    public CstBuilder(int maxChildCapacity = 256, int maxNestingDepth = 64)
    {
        _current_children = new GreenNode[maxChildCapacity];
        _child_count = 0;
        _stack = new StackEntry[maxNestingDepth];
        _stack_depth = 0;
    }

    /// <summary>
    ///     开始构建一个内部节点
    /// </summary>
    /// <param name="kind">节点类型。</param>
    public void begin_node(NodeKind kind)
    {
        _stack[_stack_depth] = new StackEntry
        {
            kind = kind,
            saved_children = _current_children,
            saved_child_count = _child_count
        };

        _stack_depth++;
        _current_children = new GreenNode[_current_children.Length];
        _child_count = 0;
    }

    /// <summary>
    ///     BeginNode 的别名，兼容旧 API
    /// </summary>
    /// <param name="kind">节点类型。</param>
    public void start_node(NodeKind kind)
    {
        begin_node(kind);
    }

    /// <summary>
    ///     添加一个叶子节点
    /// </summary>
    /// <param name="kind">叶子节点类型。</param>
    /// <param name="width">源码中的字符宽度。</param>
    /// <param name="text">叶子节点文本内容。</param>
    public void add_leaf(NodeKind kind, int width, string? text = null)
    {
        if (_child_count >= _current_children.Length) throw new InvalidOperationException("子节点数量超过容量限制");

        _current_children[_child_count] = new GreenLeafNode(kind, width, text);
        _child_count++;
    }

    /// <summary>
    ///     AddLeaf 的别名（width = text?.Length ?? 0），兼容旧 API
    /// </summary>
    /// <param name="kind">叶子节点类型。</param>
    /// <param name="text">叶子节点文本内容。</param>
    public void add_token(NodeKind kind, string? text = null)
    {
        add_leaf(kind, text?.Length ?? 0, text);
    }

    /// <summary>
    ///     使用 TextSpan 的 Length 作为 width 的 AddLeaf 别名
    /// </summary>
    /// <param name="kind">叶子节点类型。</param>
    /// <param name="span">文本跨度（Length 作为 width，Offset 忽略）。</param>
    public void add_token(NodeKind kind, TextSpan span)
    {
        add_leaf(kind, span.length);
    }

    /// <summary>
    ///     添加一个已有的 Green 节点（叶子或内部节点）
    /// </summary>
    /// <param name="node">Green 节点。</param>
    public void add_node(GreenNode node)
    {
        if (_child_count >= _current_children.Length) throw new InvalidOperationException("子节点数量超过容量限制");

        _current_children[_child_count] = node;
        _child_count++;
    }

    /// <summary>
    ///     AddNode 的别名，兼容旧 API
    /// </summary>
    /// <param name="node">Green 节点。</param>
    public void add_child(GreenNode node)
    {
        add_node(node);
    }

    /// <summary>
    ///     结束当前节点的构建，将其作为子节点加入父节点
    /// </summary>
    public void end_node()
    {
        if (_stack_depth <= 0) throw new InvalidOperationException("没有正在构建的节点");

        _stack_depth--;

        var currentChildren = new GreenNode[_child_count];
        for (var i = 0; i < _child_count; i++) currentChildren[i] = _current_children[i];

        var node = new GreenInternalNode(
            _stack[_stack_depth].kind,
            currentChildren
        );

        _current_children = _stack[_stack_depth].saved_children;
        _child_count = _stack[_stack_depth].saved_child_count;

        add_node(node);
    }

    /// <summary>
    ///     构建并返回根节点。
    ///     如果只有一个根级子节点，直接返回它；
    ///     否则包装为一个 Module 内部节点
    /// </summary>
    /// <returns>Green 树根节点。</returns>
    public GreenNode build()
    {
        if (_stack_depth != 0) throw new InvalidOperationException($"还有 {_stack_depth} 个未结束的节点");

        if (_child_count == 1) return _current_children[0];

        var children = new GreenNode[_child_count];
        for (var i = 0; i < _child_count; i++) children[i] = _current_children[i];

        return new GreenInternalNode(0, children);
    }

    private struct StackEntry
    {
        public NodeKind kind;
        public GreenNode[] saved_children;
        public int saved_child_count;
    }
}