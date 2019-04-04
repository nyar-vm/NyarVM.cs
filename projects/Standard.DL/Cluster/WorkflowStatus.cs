namespace Std.DL.Cluster;

/// <summary>工作流状态</summary>
public enum WorkflowStatus
{
    /// <summary>等待中</summary>
    Pending,

    /// <summary>运行中</summary>
    Running,

    /// <summary>已完成</summary>
    Completed,

    /// <summary>失败</summary>
    Failed,

    /// <summary>已取消</summary>
    Cancelled
}