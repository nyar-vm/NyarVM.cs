namespace Std.DL.Topos;

/// <summary>蓝图 IR（节点 + 边 + 序列化）</summary>
public interface ITopology
{
    /// <summary>拓扑标识</summary>
    string Id { get; }

    /// <summary>拓扑名称</summary>
    string Name { get; }

    /// <summary>节点列表</summary>
    IReadOnlyList<ITopologyNode> Nodes { get; }

    /// <summary>边列表</summary>
    IReadOnlyList<ITopologyEdge> Edges { get; }

    /// <summary>序列化为字节</summary>
    byte[] Serialize();
}