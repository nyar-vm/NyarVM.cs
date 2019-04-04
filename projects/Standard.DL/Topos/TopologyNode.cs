namespace Std.DL.Topos;

/// <summary>拓扑节点元数据</summary>
public sealed class TopologyNode : ITopologyNode
{
    /// <summary>节点标识</summary>
    public string Id { get; init; } = "";

    /// <summary>节点类型</summary>
    public string Type { get; init; } = "";

    /// <summary>节点属性</summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>节点端口列表</summary>
    public IReadOnlyList<Port> Ports { get; init; } = [];
}