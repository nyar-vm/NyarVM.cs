namespace Std.DL.Cluster;

/// <summary>运行中工作流句柄</summary>
public sealed class RunningWorkflow
{
    /// <summary>工作流标识</summary>
    public string Id { get; init; } = "";

    /// <summary>提交时间</summary>
    public DateTime SubmittedAt { get; init; } = DateTime.UtcNow;

    /// <summary>工作流状态</summary>
    public WorkflowStatus Status { get; init; } = WorkflowStatus.Pending;
}