namespace Std.DL.Topos;

/// <summary>拓扑边元数据</summary>
public sealed class TopologyEdge : ITopologyEdge
{
    /// <summary>源节点标识</summary>
    public string SourceNodeId { get; init; } = "";

    /// <summary>源端口名称</summary>
    public string SourcePortName { get; init; } = "";

    /// <summary>目标节点标识</summary>
    public string TargetNodeId { get; init; } = "";

    /// <summary>目标端口名称</summary>
    public string TargetPortName { get; init; } = "";
}