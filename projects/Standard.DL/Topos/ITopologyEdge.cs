namespace Std.DL.Topos;

/// <summary>拓扑边元数据接口</summary>
public interface ITopologyEdge
{
    /// <summary>源节点标识</summary>
    string SourceNodeId { get; }

    /// <summary>源端口名称</summary>
    string SourcePortName { get; }

    /// <summary>目标节点标识</summary>
    string TargetNodeId { get; }

    /// <summary>目标端口名称</summary>
    string TargetPortName { get; }
}