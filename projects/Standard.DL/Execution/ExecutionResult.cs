using Std.DL.Flux;

namespace Std.DL.Execution;

/// <summary>执行结果</summary>
public sealed class ExecutionResult
{
    /// <summary>输出张量</summary>
    public Dictionary<string, ArrayND> Outputs { get; init; } = new();

    /// <summary>执行指标</summary>
    public ExecutionMetrics Metrics { get; init; } = new();

    /// <summary>是否成功</summary>
    public bool Success { get; init; } = true;

    /// <summary>错误消息</summary>
    public string? ErrorMessage { get; init; }
}