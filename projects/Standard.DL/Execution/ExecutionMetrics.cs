namespace Std.DL.Execution;

/// <summary>执行指标数据</summary>
public sealed class ExecutionMetrics
{
    /// <summary>执行时长</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>内存使用量（字节）</summary>
    public long MemoryUsageBytes { get; init; }

    /// <summary>操作数量</summary>
    public int OperationsCount { get; init; }

    /// <summary>GPU 利用率</summary>
    public float GpuUtilization { get; init; }

    /// <summary>总 FLOPs 估算</summary>
    public long TotalFlops { get; init; }

    /// <summary>GPU 内核启动开销（毫秒）</summary>
    public double GpuOverheadMs { get; init; }
}