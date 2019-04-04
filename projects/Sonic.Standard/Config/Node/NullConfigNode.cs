namespace Std.Config.Node;

/// <summary>
///     空类型的配置节点，表示缺失或空值。全局单例�?///
/// </summary>
public sealed class NullConfigNode : ConfigNode
{
    private NullConfigNode()
    {
    }

    /// <summary>
    ///     获取空节点的全局单例实例�?    ///
    /// </summary>
    public static NullConfigNode instance { get; } = new();

    /// <summary>
    ///     获取配置节点的类型，始终�?<see cref="ConfigNode.NodeType.Null" />�?    ///
    /// </summary>
    public override NodeType type => NodeType.@null;
}