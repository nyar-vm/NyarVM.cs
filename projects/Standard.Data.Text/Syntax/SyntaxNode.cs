namespace Std.Data.Text.Syntax;

/// <summary>
///     强类型 AST 节点的抽象基类
/// </summary>
public abstract class SyntaxNode
{
    /// <summary>
    ///     初始化语法节点
    protected SyntaxNode(GreenNode green, SyntaxTree tree, int offset)
    {
        _green = green;
        _tree = tree;
        _offset = offset;
    }

    /// <summary>
    ///     对应的绿树节点
    internal GreenNode _green { get; }

    /// <summary>
    ///     所属语法树
    /// </summary>
    internal SyntaxTree _tree { get; private set; }

    /// <summary>
    ///     相对于语法树根节点的偏移量
    internal int _offset { get; }

    /// <summary>
    ///     节点在源文本中的范围
    /// </summary>
    public TextSpan span => new(_offset, _green.width);

    /// <summary>
    ///     节点所属语言标识。当前默认跟随主语法根，供分析器进行语言分派。
    public string? language_id => _tree.primary_root?.language_id;

    /// <summary>
    ///     节点所属语言对象。分析器应优先面向该属性做语言分派。
    public Language? language => _tree.primary_root?.language;

    /// <summary>
    ///     将此节点绑定到指定的语法树
    protected void bind_to_tree(SyntaxTree tree, int offset)
    {
        _tree = tree;
    }

    /// <summary>
    ///     获取指定索引的子节点，并包装为强类型。
    ///     优先使用 NodeFactory 查表构造，若未注册则回退到反射。
    protected T child_node<T>(int index) where T : SyntaxNode
    {
        var childGreen = _green.get_child(index);
        var childOffset = compute_child_offset(index);

        var factory = NodeFactory.get(childGreen!.kind);
        if (factory is not null) return (T)factory(childGreen, _tree, childOffset);

        return (T)Activator.CreateInstance(typeof(T), childGreen, _tree, childOffset)!;
    }

    /// <summary>
    ///     获取指定索引的子标记
    /// </summary>
    protected SyntaxToken child_token(int index)
    {
        var childGreen = _green.get_child(index);
        var childOffset = compute_child_offset(index);
        var text = childGreen is GreenLeafNode leaf ? leaf.text ?? string.Empty : string.Empty;
        return new SyntaxToken(childGreen!.kind, default, text);
    }

    /// <summary>
    ///     计算指定索引子节点的偏移量
    private int compute_child_offset(int index)
    {
        var childOffset = _offset;
        for (var i = 0; i < index; i++)
        {
            var sibling = _green.get_child(i);
            if (sibling is not null) childOffset += sibling.width;
        }

        return childOffset;
    }

    /// <summary>
    ///     接受访问器访问，返回递归模式以控制遍历行为
    public abstract VisitRecursionMode accept(SyntaxVisitor visitor);

    /// <summary>
    ///     接受访问器访问的默认实现，返回继续遍历
    public virtual VisitRecursionMode accept_visitor(SyntaxVisitor visitor)
    {
        return accept(visitor);
    }
}