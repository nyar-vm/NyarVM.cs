using Std.DL.Flux;
using Std.DL.Topos;

namespace Std.DL.Cluster;

/// <summary>默认集群实现</summary>
public sealed class Cluster : ICluster
{
    /// <summary>提交工作流到集群</summary>
    public Task<RunningWorkflow> SubmitAsync(ITopology topology, Dictionary<string, ArrayND> inputs,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RunningWorkflow { Id = Guid.NewGuid().ToString() });
    }

    /// <summary>等待工作流结果</summary>
    public Task<WorkflowResult> WaitForResultAsync(string workflowId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WorkflowResult { WorkflowId = workflowId, Success = true });
    }

    /// <summary>取消工作流</summary>
    public Task CancelAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}