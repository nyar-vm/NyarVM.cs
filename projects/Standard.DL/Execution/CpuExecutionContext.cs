using Std.DL.Flux;

namespace Std.DL.Execution;

/// <summary>
///     CPU 执行上下文 —— 逐操作执行编译后的计算图
/// </summary>
public sealed class CpuExecutionContext : IExecutionContext
{
    private readonly Dictionary<string, object> _operatorCache = new();

    /// <summary>
    ///     执行设备
    /// </summary>
    public ExecutionDevice Device => ExecutionDevice.Cpu;

    /// <summary>
    ///     异步执行编译后的计算图
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
        var intermediateResults = new Dictionary<string, ArrayND>();
        var totalOps = 0;
        var totalMemoryBytes = 0L;

        try
        {
            foreach (var op in graph.Operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = ExecuteOperation(op, inputs, intermediateResults);
                intermediateResults[op.Name] = result;
                totalOps++;
                totalMemoryBytes += result.Size * sizeof(float);
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ExecutionResult
            {
                Success = false,
                ErrorMessage = $"执行失败：{ex.Message}"
            });
        }

        var outputs = new Dictionary<string, ArrayND>();
        foreach (var outputName in graph.OutputNames)
            if (intermediateResults.TryGetValue(outputName, out var output))
                outputs[outputName] = output;

        var metrics = new ExecutionMetrics
        {
            Duration = DateTime.UtcNow - startTime,
            OperationsCount = totalOps,
            MemoryUsageBytes = totalMemoryBytes,
            GpuUtilization = 0.0f
        };

        return Task.FromResult(new ExecutionResult
        {
            Success = true,
            Outputs = outputs,
            Metrics = metrics
        });
    }

    /// <summary>
    ///     分配张量
    /// </summary>
    public Task<ArrayND> AllocateTensorAsync(int[] shape, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ArrayND.Zeros(shape));
    }

    /// <summary>
    ///     释放张量
    /// </summary>
    public Task FreeTensorAsync(ArrayND arrayNd, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     执行单个操作
    /// </summary>
    private ArrayND ExecuteOperation(
        GraphOperation op,
        Dictionary<string, ArrayND> externalInputs,
        Dictionary<string, ArrayND> intermediates)
    {
        var inputs = ResolveInputs(op.InputNames, externalInputs, intermediates);

        return op.OpType switch
        {
            "Dense" => ExecuteDense(op, inputs),
            "Conv2D" => ExecuteConv2D(op, inputs),
            "ReLU" => Activations.ReLUForward(inputs[0]),
            "Sigmoid" => Activations.SigmoidForward(inputs[0]),
            "Tanh" => Activations.TanhForward(inputs[0]),
            "Softmax" => Activations.SoftmaxForward(inputs[0]),
            "GELU" => Activations.GELUForward(inputs[0]),
            "SiLU" => Activations.SiLUForward(inputs[0]),
            "MaxPool2D" => ExecuteMaxPool2D(op, inputs),
            "Flatten" => ExecuteFlatten(op, inputs),
            "Dropout" => ExecuteDropout(op, inputs),
            "BatchNorm" => ExecuteBatchNorm(op, inputs),
            "ElementWiseAdd" => inputs[0] + inputs[1],
            "MatMul" => ArrayND.MatMul(inputs[0], inputs[1]),
            _ => throw new NotSupportedException($"不支持的操作类型：{op.OpType}")
        };
    }

    /// <summary>
    ///     解析操作输入
    /// </summary>
    private static ArrayND[] ResolveInputs(
        string[] inputNames,
        Dictionary<string, ArrayND> externalInputs,
        Dictionary<string, ArrayND> intermediates)
    {
        var result = new ArrayND[inputNames.Length];
        for (var i = 0; i < inputNames.Length; i++)
            if (intermediates.TryGetValue(inputNames[i], out var intermediate))
                result[i] = intermediate;
            else if (externalInputs.TryGetValue(inputNames[i], out var external))
                result[i] = external;
            else
                throw new InvalidOperationException($"找不到输入：{inputNames[i]}");

        return result;
    }

    /// <summary>
    ///     执行 Dense 操作
    /// </summary>
    private ArrayND ExecuteDense(GraphOperation op, ArrayND[] inputs)
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

        return dense.forward(inputs[0]);
    }

    /// <summary>
    ///     执行 Conv2D 操作
    /// </summary>
    private ArrayND ExecuteConv2D(GraphOperation op, ArrayND[] inputs)
    {
        var conv = GetOrCreateOperator(op, () =>
        {
            var inCh = Convert.ToInt32(op.Config["inChannels"]);
            var outCh = Convert.ToInt32(op.Config["outChannels"]);
            var kernel = Convert.ToInt32(op.Config["kernelSize"]);
            var conv2D = new Conv2D(inCh, outCh, kernel);
            if (op.Parameters.Length >= 2)
            {
                op.Parameters[0].AsSpan().CopyTo(conv2D.Weight.AsWriteSpan());
                op.Parameters[1].AsSpan().CopyTo(conv2D.Bias.AsWriteSpan());
            }

            return conv2D;
        });

        var inH = Convert.ToInt32(op.Config["inH"]);
        var inW = Convert.ToInt32(op.Config["inW"]);
        var (output, _, _) = conv.Forward(inputs[0], inH, inW);
        return output;
    }

    /// <summary>
    ///     执行 MaxPool2D 操作
    /// </summary>
    private ArrayND ExecuteMaxPool2D(GraphOperation op, ArrayND[] inputs)
    {
        var channels = Convert.ToInt32(op.Config["channels"]);
        var inH = Convert.ToInt32(op.Config["inH"]);
        var inW = Convert.ToInt32(op.Config["inW"]);
        var pool = GetOrCreateOperator(op, () => new MaxPool2D());
        var (output, _, _) = pool.Forward(inputs[0], channels, inH, inW);
        return output;
    }

    /// <summary>
    ///     执行 Flatten 操作
    /// </summary>
    private static ArrayND ExecuteFlatten(GraphOperation op, ArrayND[] inputs)
    {
        var channels = Convert.ToInt32(op.Config["channels"]);
        var h = Convert.ToInt32(op.Config["h"]);
        var w = Convert.ToInt32(op.Config["w"]);
        return Flatten.Forward(inputs[0], channels, h, w);
    }

    /// <summary>
    ///     执行 Dropout 操作
    /// </summary>
    private ArrayND ExecuteDropout(GraphOperation op, ArrayND[] inputs)
    {
        var rate = Convert.ToSingle(op.Config["rate"]);
        var dropout = GetOrCreateOperator(op, () => new Dropout(rate));
        return dropout.Forward(inputs[0]);
    }

    /// <summary>
    ///     执行 BatchNorm 操作
    /// </summary>
    private ArrayND ExecuteBatchNorm(GraphOperation op, ArrayND[] inputs)
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

        return bn.Forward(inputs[0]);
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

    /// <summary>
    ///     执行 LayerNorm 操作
    /// </summary>
    private ArrayND ExecuteLayerNorm(GraphOperation op, ArrayND[] inputs)
    {
        var normalizedShape = Convert.ToInt32(op.Config["normalizedShape"]);
        var ln = GetOrCreateOperator(op, () => new LayerNorm(normalizedShape));
        return ln.Forward(inputs[0]);
    }

    /// <summary>
    ///     执行 Embedding 操作
    /// </summary>
    private ArrayND ExecuteEmbedding(GraphOperation op, ArrayND[] inputs)
    {
        var numEmbeddings = Convert.ToInt32(op.Config["numEmbeddings"]);
        var embeddingDim = Convert.ToInt32(op.Config["embeddingDim"]);
        var emb = GetOrCreateOperator(op, () => new Embedding(numEmbeddings, embeddingDim));
        return emb.Forward(inputs[0]);
    }

    /// <summary>
    ///     执行多头注意力
    /// </summary>
    private ArrayND ExecuteMultiHeadAttention(GraphOperation op, ArrayND[] inputs)
    {
        var dModel = Convert.ToInt32(op.Config["dModel"]);
        var numHeads = Convert.ToInt32(op.Config["numHeads"]);
        var mha = GetOrCreateOperator(op, () => new MultiHeadAttention(dModel, numHeads));
        return mha.Forward(inputs[0]);
    }

    /// <summary>
    ///     执行 Concat
    /// </summary>
    private ArrayND ExecuteConcat(GraphOperation op, ArrayND[] inputs)
    {
        var axis = Convert.ToInt32(op.Config["axis"]);
        return ArrayND.Concat(axis, inputs);
    }

    /// <summary>
    ///     执行 Slice
    /// </summary>
    private ArrayND ExecuteSlice(GraphOperation op, ArrayND[] inputs)
    {
        var axis = Convert.ToInt32(op.Config["axis"]);
        var start = Convert.ToInt32(op.Config["start"]);
        var count = Convert.ToInt32(op.Config["count"]);
        return inputs[0].Slice(axis, start, count);
    }

    /// <summary>
    ///     执行 Transpose
    /// </summary>
    private static ArrayND ExecuteTranspose(GraphOperation op, ArrayND[] inputs)
    {
        var dim0 = Convert.ToInt32(op.Config["dim0"]);
        var dim1 = Convert.ToInt32(op.Config["dim1"]);
        return inputs[0].Transpose(dim0, dim1);
    }
}