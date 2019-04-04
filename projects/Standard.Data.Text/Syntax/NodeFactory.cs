namespace Std.Data.Text.Syntax;

/// <summary>
///     语法节点工厂，维护 NodeKind 到构造委托的映射，避免反射调用
/// </summary>
public static class NodeFactory
{
    private static readonly Dictionary<int, Func<GreenNode, SyntaxTree, int, SyntaxNode>> _factories = new();

    /// <summary>
    ///     注册指定节点类型的构造委托
    /// </summary>
    public static void register(NodeKind kind, Func<GreenNode, SyntaxTree, int, SyntaxNode> factory)
    {
        _factories[kind.value] = factory;
    }

    /// <summary>
    ///     尝试获取指定节点类型的构造委托
    /// </summary>
    public static Func<GreenNode, SyntaxTree, int, SyntaxNode>? get(NodeKind kind)
    {
        return _factories.GetValueOrDefault(kind.value);
    }

    /// <summary>
    ///     使用注册的构造委托创建语法节点
    /// </summary>
    public static SyntaxNode? create(NodeKind kind, GreenNode green, SyntaxTree tree, int offset)
    {
        var factory = get(kind);
        return factory?.Invoke(green, tree, offset);
    }

    /// <summary>
    ///     判断指定节点类型是否已注册
    /// </summary>
    public static bool is_registered(NodeKind kind)
    {
        return _factories.ContainsKey(kind.value);
    }

    /// <summary>
    ///     清空所有注册
    /// </summary>
    public static void clear()
    {
        _factories.Clear();
    }
}