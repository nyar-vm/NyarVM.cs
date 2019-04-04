namespace Std.DL.Cluster;

/// <summary>拓扑节点物理地址</summary>
public sealed class TopologyAddress
{
    /// <summary>节点标识</summary>
    public string NodeId { get; init; } = "";

    /// <summary>主机地址</summary>
    public string Host { get; init; } = "";

    /// <summary>端口号</summary>
    public int Port { get; init; } = 0;
}