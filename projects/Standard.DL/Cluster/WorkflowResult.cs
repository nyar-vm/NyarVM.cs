using Std.DL.Flux;

namespace Std.DL.Cluster;

/// <summary>工作流结果</summary>
public sealed class WorkflowResult
{
    /// <summary>工作流标识</summary>
    public string WorkflowId { get; init; } = "";

    /// <summary>输出张量</summary>
    public Dictionary<string, ArrayND> Outputs { get; init; } = new();

    /// <summary>是否成功</summary>
    public bool Success { get; init; } = true;

    /// <summary>错误消息</summary>
    public string? ErrorMessage { get; init; }
}