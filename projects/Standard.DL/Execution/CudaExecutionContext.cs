using Std.DL.Flux;

namespace Std.DL.Execution;

/// <summary>
///     CUDA 执行上下文 —— GPU 后端原型（Mock 实现）
///     模拟 CUDA 内核启动、显存管理和异步执行
///     实际计算回退到 CPU，但保留了 CUDA API 设计
/// </summary>
[Obsolete("请使用 OnnxRuntimeGpuContext 替代")]
public sealed class CudaExecutionContext : IExecutionContext
{
    private readonly List<KernelLaunchRecord> _kernelHistory = [];
    private readonly Dictionary<string, object> _operatorCache = new();

    /// <summary>
    ///     内核启动历史记录（用于分析和优化）
    /// </summary>
    public IReadOnlyList<KernelLaunchRecord> KernelHistory => _kernelHistory;

    /// <summary>
    ///     执行设备
    /// </summary>
    public ExecutionDevice Device => ExecutionDevice.Cuda;

    /// <summary>
    ///     异步执行编译后的计算图（GPU 后端）
    /// </summary>
    /// <param name="graph">编译后的计算图</param>
    /// <param name="inputs">命名输入张量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public Task<ExecutionResult> ExecuteAsync(
        CompiledGraph graph,
        Dictionary<string, ArrayND> inputs,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var intermediateTensors = new Dictionary<string, GpuTensor>();
        var totalOps = 0;

        try
        {
            foreach (var op in graph.Operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var inputTensors = ResolveGpuInputs(op.InputNames, inputs, intermediateTensors);

                RecordKernelLaunch(op.OpType, op.Name);

                var output = ExecuteOperationOnGpu(op, inputTensors);
                intermediateTensors[op.Name] = output;
                totalOps++;
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ExecutionResult
            {
                Success = false,
                ErrorMessage = $"CUDA 执行失败：{ex.Message}"
            });
        }

        var outputs = new Dictionary<string, ArrayND>();
        foreach (var outputName in graph.OutputNames)
            if (intermediateTensors.TryGetValue(outputName, out var gpuTensor))
            {
                var cpuArray = new float[gpuTensor.Size];
                gpuTensor.CopyTo(cpuArray);
                outputs[outputName] = ArrayND.FromArray(cpuArray, gpuTensor.Shape);
            }

        var metrics = new ExecutionMetrics
        {
            Duration = DateTime.UtcNow - startTime,
            OperationsCount = totalOps,
            MemoryUsageBytes = GpuMemoryTracker.AllocatedBytes,
            GpuUtilization = GpuMemoryTracker.MemoryUtilization,
            TotalFlops = _kernelHistory.Sum(k => k.EstimatedFlops),
            GpuOverheadMs = EstimateGpuOverhead(totalOps)
        };

        return Task.FromResult(new ExecutionResult
        {
            Success = true,
            Outputs = outputs,
            Metrics = metrics
        });
    }

    /// <summary>
    ///     在 GPU 上分配张量
    /// </summary>
    public Task<ArrayND> AllocateTensorAsync(int[] shape, CancellationToken cancellationToken = default)
    {
        var gpuTensor = new GpuTensor(shape);
        GpuMemoryTracker.Register(gpuTensor);

        var cpuData = new float[gpuTensor.Size];
        return Task.FromResult(ArrayND.FromArray(cpuData, shape));
    }

    /// <summary>
    ///     释放 GPU 张量
    /// </summary>
    public Task FreeTensorAsync(ArrayND arrayNd, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     重置执行状态
    /// </summary>
    public void Reset()
    {
        _operatorCache.Clear();
        _kernelHistory.Clear();
        GpuMemoryTracker.Reset();
    }

    /// <summary>
    ///     在 GPU 上执行单个操作
    /// </summary>
    private GpuTensor ExecuteOperationOnGpu(GraphOperation op, GpuTensor[] inputs)
    {
        var result = op.OpType switch
        {
            "Dense" => ExecuteDenseOnGpu(op, inputs),
            "Conv2D" => ExecuteConv2DOnGpu(op, inputs),
            "ReLU" => ExecuteActivationOnGpu(inputs[0], "ReLU"),
            "Sigmoid" => ExecuteActivationOnGpu(inputs[0], "Sigmoid"),
            "Tanh" => ExecuteActivationOnGpu(inputs[0], "Tanh"),
            "Softmax" => ExecuteActivationOnGpu(inputs[0], "Softmax"),
            "GELU" => ExecuteActivationOnGpu(inputs[0], "GELU"),
            "SiLU" => ExecuteActivationOnGpu(inputs[0], "SiLU"),
            "MaxPool2D" => ExecuteMaxPoolOnGpu(op, inputs),
            "Flatten" => ExecuteFlattenOnGpu(op, inputs),
            "Dropout" => ExecuteDropoutOnGpu(op, inputs),
            "BatchNorm" => ExecuteBatchNormOnGpu(op, inputs),
            _ => throw new NotSupportedException($"GPU 不支持的操作类型：{op.OpType}")
        };

        GpuMemoryTracker.Register(result);
        return result;
    }

    /// <summary>
    ///     执行 Dense 的 GPU 内核
    /// </summary>
    private GpuTensor ExecuteDenseOnGpu(GraphOperation op, GpuTensor[] inputs)
    {
        var dense = GetOrCreateOperator(op, () =>
        {
            var fanIn = Convert.ToInt32(op.Config["fanIn"]);
            var fanOut = Convert.ToInt32(op.Config["fanOut"]);
            var d = new Dense(fanIn, fanOut);
            if (op.Parameters.Length >= 2)
            {
                op.Parameters[0].AsSpan().CopyTo(d.Weight.AsWriteSpan());
                op.Parameters[1].AsSpan().CopyTo(d.Bias.AsWriteSpan());
            }

            return d;
        });

        var cpuOutput = dense.forward(inputs[0].CpuData);
        return new GpuTensor(cpuOutput);
    }

    /// <summary>
    ///     执行 Conv2D 的 GPU 内核
    /// </summary>
    private GpuTensor ExecuteConv2DOnGpu(GraphOperation op, GpuTensor[] inputs)
    {
        var conv = GetOrCreateOperator(op, () =>
        {
            var inCh = Convert.ToInt32(op.Config["inChannels"]);
            var outCh = Convert.ToInt32(op.Config["outChannels"]);
            var kernel = Convert.ToInt32(op.Config["kernelSize"]);
            var c = new Conv2D(inCh, outCh, kernel);
            if (op.Parameters.Length >= 2)
            {
                op.Parameters[0].AsSpan().CopyTo(c.Weight.AsWriteSpan());
                op.Parameters[1].AsSpan().CopyTo(c.Bias.AsWriteSpan());
            }

            return c;
        });

        var inH = Convert.ToInt32(op.Config["inH"]);
        var inW = Convert.ToInt32(op.Config["inW"]);
        var (output, _, _) = conv.Forward(inputs[0].CpuData, inH, inW);
        return new GpuTensor(output);
    }

    /// <summary>
    ///     执行激活函数的 GPU 内核
    /// </summary>
    private static GpuTensor ExecuteActivationOnGpu(GpuTensor input, string activationType)
    {
        var cpuOutput = activationType switch
        {
            "ReLU" => Activations.ReLUForward(input.CpuData),
            "Sigmoid" => Activations.SigmoidForward(input.CpuData),
            "Tanh" => Activations.TanhForward(input.CpuData),
            "Softmax" => Activations.SoftmaxForward(input.CpuData),
            "GELU" => Activations.GELUForward(input.CpuData),
            "SiLU" => Activations.SiLUForward(input.CpuData),
            _ => throw new NotSupportedException($"GPU 不支持的激活函数：{activationType}")
        };

        return new GpuTensor(cpuOutput);
    }

    /// <summary>
    ///     执行 MaxPool2D 的 GPU 内核
    /// </summary>
    private GpuTensor ExecuteMaxPoolOnGpu(GraphOperation op, GpuTensor[] inputs)
    {
        var channels = Convert.ToInt32(op.Config["channels"]);
        var inH = Convert.ToInt32(op.Config["inH"]);
        var inW = Convert.ToInt32(op.Config["inW"]);
        var pool = GetOrCreateOperator(op, () => new MaxPool2D());
        var (output, _, _) = pool.Forward(inputs[0].CpuData, channels, inH, inW);
        return new GpuTensor(output);
    }

    /// <summary>
    ///     执行 Flatten 的 GPU 内核
    /// </summary>
    private static GpuTensor ExecuteFlattenOnGpu(GraphOperation op, GpuTensor[] inputs)
    {
        var channels = Convert.ToInt32(op.Config["channels"]);
        var h = Convert.ToInt32(op.Config["h"]);
        var w = Convert.ToInt32(op.Config["w"]);
        return new GpuTensor(Flatten.Forward(inputs[0].CpuData, channels, h, w));
    }

    /// <summary>
    ///     执行 Dropout 的 GPU 内核
    /// </summary>
    private GpuTensor ExecuteDropoutOnGpu(GraphOperation op, GpuTensor[] inputs)
    {
        var rate = Convert.ToSingle(op.Config["rate"]);
        var dropout = GetOrCreateOperator(op, () => new Dropout(rate));
        return new GpuTensor(dropout.Forward(inputs[0].CpuData));
    }

    /// <summary>
    ///     执行 BatchNorm 的 GPU 内核
    /// </summary>
    private GpuTensor ExecuteBatchNormOnGpu(GraphOperation op, GpuTensor[] inputs)
    {
        var bn = GetOrCreateOperator(op, () =>
        {
            var numFeatures = Convert.ToInt32(op.Config["numFeatures"]);
            var b = new BatchNorm(numFeatures);
            if (op.Parameters.Length >= 2)
            {
                op.Parameters[0].AsSpan().CopyTo(b.Gamma.AsWriteSpan());
                op.Parameters[1].AsSpan().CopyTo(b.Beta.AsWriteSpan());
            }

            return b;
        });

        return new GpuTensor(bn.Forward(inputs[0].CpuData));
    }

    /// <summary>
    ///     解析 GPU 操作的输入
    /// </summary>
    private static GpuTensor[] ResolveGpuInputs(
        string[] inputNames,
        Dictionary<string, ArrayND> externalInputs,
        Dictionary<string, GpuTensor> intermediates)
    {
        var result = new GpuTensor[inputNames.Length];
        for (var i = 0; i < inputNames.Length; i++)
            if (intermediates.TryGetValue(inputNames[i], out var intermediate))
            {
                result[i] = intermediate;
            }
            else if (externalInputs.TryGetValue(inputNames[i], out var external))
            {
                result[i] = new GpuTensor(external);
                GpuMemoryTracker.Register(result[i]);
            }
            else
            {
                throw new InvalidOperationException($"GPU 找不到输入：{inputNames[i]}");
            }

        return result;
    }

    /// <summary>
    ///     记录 CUDA 内核启动
    /// </summary>
    private void RecordKernelLaunch(string opType, string opName)
    {
        _kernelHistory.Add(new KernelLaunchRecord
        {
            OpType = opType,
            OpName = opName,
            Timestamp = DateTime.UtcNow,
            EstimatedFlops = EstimateFlops(opType)
        });
    }

    /// <summary>
    ///     估算操作的 FLOPs（基于典型深度学习算子的计算量）
    /// </summary>
    private static long EstimateFlops(string opType)
    {
        return opType switch
        {
            "Conv2D" => 5_000_000,
            "Dense" => 500_000,
            "ReLU" => 50_000,
            "Sigmoid" => 80_000,
            "Tanh" => 100_000,
            "GELU" => 120_000,
            "SiLU" => 90_000,
            "Softmax" => 200_000,
            "MaxPool2D" => 100_000,
            "Flatten" => 1_000,
            "Dropout" => 30_000,
            "BatchNorm" => 300_000,
            _ => 10_000
        };
    }

    /// <summary>
    ///     估算 GPU 内核启动开销（微秒级）
    /// </summary>
    private static double EstimateGpuOverhead(int operationCount)
    {
        return operationCount * 0.005;
    }

    /// <summary>
    ///     获取或缓存操作符实例
    /// </summary>
    private T GetOrCreateOperator<T>(GraphOperation op, Func<T> factory) where T : class
    {
        if (_operatorCache.TryGetValue(op.Name, out var cached)) return (T)cached;

        var instance = factory();
        _operatorCache[op.Name] = instance;
        return instance;
    }
}

/// <summary>
///     CUDA 内核启动记录 —— 用于性能分析和优化
/// </summary>
public sealed class KernelLaunchRecord
{
    /// <summary>操作类型</summary>
    public string OpType { get; init; } = string.Empty;

    /// <summary>操作名称</summary>
    public string OpName { get; init; } = string.Empty;

    /// <summary>启动时间戳</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>估算 FLOPs</summary>
    public long EstimatedFlops { get; init; }
}