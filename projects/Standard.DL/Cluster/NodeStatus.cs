namespace Std.DL.Cluster;

/// <summary>节点状态</summary>
public enum NodeStatus
{
    /// <summary>空闲</summary>
    Idle,

    /// <summary>运行中</summary>
    Running,

    /// <summary>繁忙</summary>
    Busy,

    /// <summary>不可用</summary>
    Unavailable
}