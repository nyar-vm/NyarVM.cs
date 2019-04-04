namespace Std.Data.Text.Syntax;

/// <summary>
///     语法树，支持增量编辑和可选的父节点缓存
/// </summary>
public class SyntaxTree
{
    private readonly List<SyntaxRoot> _all_roots = [];

    private readonly Dictionary<(GreenNode, int), RedNode>? _parent_cache;

    /// <summary>
    ///     构造语法树
    /// </summary>
    public SyntaxTree(ISource source, GreenNode root, bool enableParentCache = false)
    {
        this.source = source;
        this.root = root;
        enable_parent_cache = enableParentCache;

        if (enableParentCache)
        {
            _parent_cache = new Dictionary<(GreenNode, int), RedNode>();
            build_parent_cache(new RedNode(root, this, 0), null);
        }
    }

    /// <summary>
    ///     源文本
    /// </summary>
    public ISource source { get; }

    /// <summary>
    ///     绿树根节点
    /// </summary>
    public GreenNode root { get; }

    /// <summary>
    ///     是否启用父节点缓存
    /// </summary>
    public bool enable_parent_cache { get; }

    /// <summary>
    ///     所有语法根
    /// </summary>
    public IReadOnlyList<SyntaxRoot> all_roots => _all_roots;

    /// <summary>
    ///     主语法根
    /// </summary>
    public SyntaxRoot? primary_root => _all_roots.Count > 0 ? _all_roots[0] : null;

    /// <summary>
    ///     构建父节点缓存
    /// </summary>
    private void build_parent_cache(RedNode node, RedNode? parent)
    {
        if (parent is not null) _parent_cache![(node._green, node._start)] = parent.Value;

        for (var i = 0; i < node.child_count; i++) build_parent_cache(node.get_child(i), node);
    }

    /// <summary>
    ///     尝试从缓存中获取父节点
    /// </summary>
    internal bool try_get_parent(RedNode node, out RedNode? parent)
    {
        if (_parent_cache is not null)
        {
            if (_parent_cache.TryGetValue((node._green, node._start), out var p))
            {
                parent = p;
                return true;
            }

            parent = null;
            return false;
        }

        parent = null;
        return false;
    }

    /// <summary>
    ///     获取红树根节点
    /// </summary>
    public RedNode get_red_root()
    {
        return new RedNode(root, this, 0);
    }

    /// <summary>
    ///     应用编辑并尝试增量重解析
    /// </summary>
    public SyntaxTree edit(Edit edit, IncrementalParserRepo parsers)
    {
        var affectedNode = find_deepest_node(root, edit.old_span, 0);
        if (affectedNode is null) return this;

        var oldRoot = root;
        var newRoot = try_incremental_reparse(root, affectedNode, edit, parsers, 0);
        var newSource = apply_edit(edit);
        var newTree = new SyntaxTree(newSource, newRoot, enable_parent_cache);

        if (!ReferenceEquals(oldRoot, newRoot))
        {
            var replaced = collect_replaced_nodes(oldRoot, newRoot);
            var changeEvent = new TreeChangeEvent(this, newTree, edit.old_span, replaced, edit);
            newTree.OnChanged(changeEvent);
        }

        return newTree;
    }

    /// <summary>
    ///     语法树变更事件
    /// </summary>
    public event Action<TreeChangeEvent>? Changed;

    /// <summary>
    ///     触发变更事件
    /// </summary>
    private void OnChanged(TreeChangeEvent e)
    {
        Changed?.Invoke(e);
    }

    /// <summary>
    ///     收集被替换的绿树节点
    /// </summary>
    private static List<GreenNode> collect_replaced_nodes(GreenNode oldRoot, GreenNode newRoot)
    {
        var replaced = new List<GreenNode>();
        collect_diff(oldRoot, newRoot, replaced);
        return replaced;
    }

    /// <summary>
    ///     递归比较两棵绿树，收集差异节点
    /// </summary>
    private static void collect_diff(GreenNode oldNode, GreenNode newNode, List<GreenNode> replaced)
    {
        if (!ReferenceEquals(oldNode, newNode)) replaced.Add(oldNode);

        var minCount = System.Math.Min(oldNode.child_count, newNode.child_count);
        for (var i = 0; i < minCount; i++)
        {
            var oldChild = oldNode.get_child(i);
            var newChild = newNode.get_child(i);
            if (oldChild is not null && newChild is not null && !ReferenceEquals(oldChild, newChild))
                collect_diff(oldChild, newChild, replaced);
        }
    }

    /// <summary>
    ///     根据语言标识获取语法根
    /// </summary>
    public SyntaxRoot? get_root(string languageId)
    {
        foreach (var root in _all_roots)
            if (root.language_id == languageId)
                return root;

        return null;
    }

    /// <summary>
    ///     添加语法根
    /// </summary>
    internal void add_root(SyntaxRoot root)
    {
        _all_roots.Add(root);
    }

    /// <summary>
    ///     查找与指定跨度重叠的最深节点
    /// </summary>
    private GreenNode? find_deepest_node(GreenNode node, TextSpan span, int offset)
    {
        var nodeSpan = new TextSpan(offset, node.width);
        if (!nodeSpan.overlaps_with(span) && !nodeSpan.contains(span.start)) return null;

        var childOffset = offset;
        for (var i = 0; i < node.child_count; i++)
        {
            var child = node.get_child(i);
            if (child is not null)
            {
                var deeper = find_deepest_node(child, span, childOffset);
                if (deeper is not null) return deeper;

                childOffset += child.width;
            }
        }

        return node;
    }

    /// <summary>
    ///     尝试对受影响节点进行增量重解析，失败则向上冒泡至父节点
    /// </summary>
    private GreenNode try_incremental_reparse(GreenNode root, GreenNode affected, Edit edit,
        IncrementalParserRepo parsers, int offset)
    {
        var parser = parsers.get(affected.kind);
        if (parser is not null)
        {
            var newSource = apply_edit(edit);
            var newSpan = new TextSpan(edit.old_span.start, edit.old_span.length);
            var result = parser(newSource, newSpan, null, out var changed);
            if (result is not null && changed) return replace_node(root, affected, result);
        }

        var parent = find_parent_green(root, affected);
        if (parent is not null)
        {
            var parentOffset = find_node_offset(root, parent, 0);
            return try_incremental_reparse(root, parent, edit, parsers, parentOffset);
        }

        return root;
    }

    /// <summary>
    ///     在绿树中将目标节点替换为新节点，复用未变更的子节点
    /// </summary>
    private static GreenNode replace_node(GreenNode root, GreenNode target, GreenNode replacement)
    {
        if (ReferenceEquals(root, target)) return replacement;

        if (root.child_count == 0) return root;

        var anyChanged = false;
        var children = new GreenNode[root.child_count];

        for (var i = 0; i < root.child_count; i++)
        {
            var child = root.get_child(i);

            if (child is not null)
            {
                var newChild = replace_node(child, target, replacement);
                children[i] = newChild;

                if (!ReferenceEquals(child, newChild)) anyChanged = true;
            }
        }

        if (!anyChanged) return root;

        return new GreenInternalNode(root.kind, children);
    }

    /// <summary>
    ///     在绿树中查找目标节点的父节点
    /// </summary>
    private static GreenNode? find_parent_green(GreenNode root, GreenNode target)
    {
        for (var i = 0; i < root.child_count; i++)
        {
            var child = root.get_child(i);
            if (ReferenceEquals(child, target)) return root;

            if (child is not null)
            {
                var found = find_parent_green(child, target);
                if (found is not null) return found;
            }
        }

        return null;
    }

    /// <summary>
    ///     在绿树中查找目标节点的偏移量
    /// </summary>
    private static int find_node_offset(GreenNode root, GreenNode target, int offset)
    {
        if (ReferenceEquals(root, target)) return offset;

        var childOffset = offset;
        for (var i = 0; i < root.child_count; i++)
        {
            var child = root.get_child(i);
            if (child is not null)
            {
                var found = find_node_offset(child, target, childOffset);
                if (found >= 0) return found;

                childOffset += child.width;
            }
        }

        return -1;
    }

    /// <summary>
    ///     应用编辑到源文本，生成新的 StringSource
    /// </summary>
    private StringSource apply_edit(Edit edit)
    {
        var original = source.substring(new Range(0, source.length));
        var before = original[..edit.old_span.start];
        var after = original[edit.old_span.end..];
        return new StringSource(before + edit.new_text + after);
    }
}