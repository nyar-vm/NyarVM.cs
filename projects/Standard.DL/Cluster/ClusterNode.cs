namespace Std.DL.Cluster;

/// <summary>集群节点</summary>
public sealed class ClusterNode
{
    /// <summary>节点标识</summary>
    public string Id { get; init; } = "";

    /// <summary>主机地址</summary>
    public string Host { get; init; } = "";

    /// <summary>端口号</summary>
    public int Port { get; init; } = 0;

    /// <summary>资源描述</summary>
    public ResourceRequest Resources { get; init; } = new();

    /// <summary>节点状态</summary>
    public NodeStatus Status { get; init; } = NodeStatus.Idle;
}