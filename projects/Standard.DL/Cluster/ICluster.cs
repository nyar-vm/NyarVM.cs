using Std.DL.Flux;
using Std.DL.Topos;

namespace Std.DL.Cluster;

/// <summary>集群外观接口</summary>
public interface ICluster
{
    /// <summary>提交工作流到集群</summary>
    Task<RunningWorkflow> SubmitAsync(ITopology topology, Dictionary<string, ArrayND> inputs,
        CancellationToken cancellationToken = default);

    /// <summary>等待工作流结果</summary>
    Task<WorkflowResult> WaitForResultAsync(string workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>取消工作流</summary>
    Task CancelAsync(string workflowId, CancellationToken cancellationToken = default);
}