namespace Std.DL.Topos;

/// <summary>拓扑节点元数据接口</summary>
public interface ITopologyNode
{
    /// <summary>节点标识</summary>
    string Id { get; }

    /// <summary>节点类型</summary>
    string Type { get; }

    /// <summary>节点属性</summary>
    Dictionary<string, object> Properties { get; }

    /// <summary>节点端口列表</summary>
    IReadOnlyList<Port> Ports { get; }
}