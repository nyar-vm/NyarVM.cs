namespace Std.Config.Node;

/// <summary>
///     对象类型的配置节点，包含命名字段的字典�?///
/// </summary>
public sealed class ObjectConfigNode : ConfigNode
{
    private readonly Dictionary<string, ConfigNode> _fields;

    /// <summary>
    ///     使用指定的字段字典初始化 <see cref="ObjectConfigNode" /> 的新实例�?    ///
    /// </summary>
    /// <param name="fields">字段名与配置节点的字典�?/param>
    public ObjectConfigNode(Dictionary<string, ConfigNode> fields)
    {
        _fields = fields ?? new Dictionary<string, ConfigNode>();
    }

    /// <summary>
    ///     获取配置节点的类型，始终�?<see cref="ConfigNode.NodeType.Object" />�?    ///
    /// </summary>
    public override NodeType type => NodeType.@object;

    /// <summary>
    ///     获取对象节点中指定名称的字段�?    ///
    /// </summary>
    /// <param name="name">
    ///     字段名称�?/param>
    ///     <returns>字段对应的配置节点，不存在则返回 null�?/returns>
    public override ConfigNode? get_field(string name)
    {
        return _fields.GetValueOrDefault(name);
    }

    /// <summary>
    ///     枚举对象节点的所有字段�?    ///
    /// </summary>
    /// <returns>字段名与节点的键值对序列�?/returns>
    public override IEnumerable<KeyValuePair<string, ConfigNode>> enumerate_fields()
    {
        return _fields;
    }
}