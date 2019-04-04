namespace Std.Config.Node;

/// <summary>
///     数组类型的配置节点，包含有序元素列表�?///
/// </summary>
public sealed class ArrayConfigNode : ConfigNode
{
    private readonly List<ConfigNode> _items;

    /// <summary>
    ///     使用指定的元素列表初始化 <see cref="ArrayConfigNode" /> 的新实例�?    ///
    /// </summary>
    /// <param name="items">数组元素列表�?/param>
    public ArrayConfigNode(List<ConfigNode> items)
    {
        _items = items ?? [];
    }

    /// <summary>
    ///     获取配置节点的类型，始终�?<see cref="ConfigNode.NodeType.Array" />�?    ///
    /// </summary>
    public override NodeType type => NodeType.array;

    /// <summary>
    ///     枚举数组节点的所有元素�?    ///
    /// </summary>
    /// <returns>数组元素序列�?/returns>
    public override IEnumerable<ConfigNode> enumerate_array()
    {
        return _items;
    }
}