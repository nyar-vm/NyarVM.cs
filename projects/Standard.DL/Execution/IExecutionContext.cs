using Std.DL.Flux;

namespace Std.DL.Execution;

/// <summary>设备后端执行抽象</summary>
public interface IExecutionContext
{
    /// <summary>执行设备</summary>
    ExecutionDevice Device { get; }

    /// <summary>执行编译后的计算图</summary>
    Task<ExecutionResult> ExecuteAsync(CompiledGraph graph, Dictionary<string, ArrayND> inputs,
        CancellationToken cancellationToken = default);

    /// <summary>分配张量</summary>
    Task<ArrayND> AllocateTensorAsync(int[] shape, CancellationToken cancellationToken = default);

    /// <summary>释放张量</summary>
    Task FreeTensorAsync(ArrayND arrayNd, CancellationToken cancellationToken = default);
}