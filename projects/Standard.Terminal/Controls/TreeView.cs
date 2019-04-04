namespace Std.Terminal.Controls;

/// <summary>
///     树视图控件，支持层级展开/折叠和键盘导航
/// </summary>
/// <typeparam name="T">节点数据类型</typeparam>
public sealed class TreeView<T> : View
{
    private readonly List<FlatTreeNode> _flat_nodes = [];
    private TreeNode<T>? _root;
    private int _scroll_offset;

    /// <summary>
    ///     创建树视图
    /// </summary>
    public TreeView()
    {
        width = 30;
        height = 15;
        tab_stop = true;
    }

    /// <summary>
    ///     使用指定数据创建树视图
    /// </summary>
    /// <param name="root">根节点</param>
    public TreeView(TreeNode<T> root) : this()
    {
        _root = root;
        rebuild_flat_list();
    }

    /// <summary>
    ///     根节点
    /// </summary>
    public TreeNode<T>? root
    {
        get => _root;
        set
        {
            _root = value;
            rebuild_flat_list();
        }
    }

    /// <summary>
    ///     当前选中节点
    /// </summary>
    public TreeNode<T>? selected_node { get; set; }

    /// <summary>
    ///     选中变化事件
    /// </summary>
    public event Action<TreeView<T>, TreeNode<T>>? OnSelected;

    /// <summary>
    ///     展开/折叠事件
    /// </summary>
    public event Action<TreeView<T>, TreeNode<T>, bool>? OnExpandChanged;

    private void rebuild_flat_list()
    {
        _flat_nodes.Clear();
        if (_root == null) return;

        flatten_node(_root, 0);
    }

    private void flatten_node(TreeNode<T> node, int depth)
    {
        _flat_nodes.Add(new FlatTreeNode(node, depth, node.children.Count > 0));
        if (node.is_expanded)
            for (var i = 0; i < node.children.Count; i++)
                flatten_node(node.children[i], depth + 1);
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, ctx.default_background);

        var innerWidth = width - 2;
        var innerHeight = height - 2;

        _scroll_offset = System.Math.Clamp(_scroll_offset, 0, System.Math.Max(0, _flat_nodes.Count - innerHeight));

        for (var i = 0; i < innerHeight && _scroll_offset + i < _flat_nodes.Count; i++)
        {
            var flatIndex = _scroll_offset + i;
            var flatNode = _flat_nodes[flatIndex];
            var isSelected = flatNode.node == selected_node;
            var y = 1 + i;

            var bg = isSelected ? new RgbColor(50, 50, 100) : ctx.default_background;
            var fg = isSelected ? RgbColor.White : new RgbColor(200, 200, 200);

            var indent = new string(' ', flatNode.depth * 3);
            var expandSymbol = flatNode.has_children
                ? flatNode.node.is_expanded ? "▼ " : "▶ "
                : "  ";

            var displayText = indent + expandSymbol + flatNode.node.text;
            if (displayText.Length > innerWidth) displayText = displayText[..innerWidth];

            ctx.fill_rect(1, y, innerWidth, 1, bg);
            ctx.draw_text(1, y, displayText, fg, bg);
        }

        if (_flat_nodes.Count > innerHeight) draw_scroll_indicator(ctx, innerHeight);
    }

    private void draw_scroll_indicator(RenderContext ctx, int innerHeight)
    {
        if (_flat_nodes.Count == 0) return;

        var scrollBarHeight = System.Math.Max(1, (int)((float)innerHeight / _flat_nodes.Count * innerHeight));
        var maxScroll = _flat_nodes.Count - innerHeight;
        var scrollBarPos =
            maxScroll > 0 ? (int)((float)_scroll_offset / maxScroll * (innerHeight - scrollBarHeight)) : 0;

        for (var i = 0; i < innerHeight; i++)
        {
            var ch = i >= scrollBarPos && i < scrollBarPos + scrollBarHeight ? '█' : '░';
            var fg = i >= scrollBarPos && i < scrollBarPos + scrollBarHeight ? RgbColor.White : RgbColor.Gray;
            ctx.draw_text(width - 1, 1 + i, ch.ToString(), fg, ctx.default_background);
        }
    }

    /// <summary>
    ///     查找展开后的扁平索引
    /// </summary>
    private int find_flat_index(TreeNode<T> node)
    {
        return _flat_nodes.FindIndex(f => f.node == node);
    }

    /// <summary>
    ///     切换展开/折叠
    /// </summary>
    /// <param name="node">目标节点</param>
    public void toggle_expand(TreeNode<T> node)
    {
        if (node.children.Count == 0) return;

        node.is_expanded = !node.is_expanded;
        rebuild_flat_list();
        selected_node = node;
        OnExpandChanged?.Invoke(this, node, node.is_expanded);
    }

    internal void move_selection_up()
    {
        if (_flat_nodes.Count == 0) return;

        var currentIndex = selected_node != null ? find_flat_index(selected_node) : -1;
        var newIndex = currentIndex <= 0 ? 0 : currentIndex - 1;
        select_node_by_flat_index(newIndex);
    }

    internal void move_selection_down()
    {
        if (_flat_nodes.Count == 0) return;

        var currentIndex = selected_node != null ? find_flat_index(selected_node) : -1;
        var newIndex = currentIndex >= _flat_nodes.Count - 1 ? _flat_nodes.Count - 1 : currentIndex + 1;
        select_node_by_flat_index(newIndex);
    }

    private void select_node_by_flat_index(int flatIndex)
    {
        if (flatIndex < 0 || flatIndex >= _flat_nodes.Count) return;

        selected_node = _flat_nodes[flatIndex].node;
        var visibleHeight = height - 2;
        if (flatIndex < _scroll_offset)
            _scroll_offset = flatIndex;
        else if (flatIndex >= _scroll_offset + visibleHeight) _scroll_offset = flatIndex - visibleHeight + 1;

        OnSelected?.Invoke(this, selected_node);
    }

    internal void handle_expand_collapse()
    {
        if (selected_node != null) toggle_expand(selected_node);
    }

    /// <summary>
    ///     处理键盘输入事件
    /// </summary>
    /// <param name="key">键盘输入信息</param>
    /// <returns>是否已处理该按键</returns>
    public override bool OnKeyDown(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                move_selection_up();
                return true;
            case ConsoleKey.DownArrow:
                move_selection_down();
                return true;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                handle_expand_collapse();
                return true;
            case ConsoleKey.RightArrow:
                if (selected_node is { is_expanded: false, children.Count: > 0 })
                    toggle_expand(selected_node);

                return true;
            case ConsoleKey.LeftArrow:
                if (selected_node is { is_expanded: true }) toggle_expand(selected_node);

                return true;
            default:
                return false;
        }
    }

    private sealed class FlatTreeNode
    {
        public FlatTreeNode(TreeNode<T> node, int depth, bool hasChildren)
        {
            this.node = node;
            this.depth = depth;
            has_children = hasChildren;
        }

        public TreeNode<T> node { get; }
        public int depth { get; }
        public bool has_children { get; }
    }
}