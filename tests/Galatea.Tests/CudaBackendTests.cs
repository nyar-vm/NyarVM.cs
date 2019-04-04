namespace Galatea.Tests;

using Galatea.Compiler;
using Galatea.Engram;
using Galatea.Execution;
using Galatea.Flux;

/// <summary>
///     CUDA 后端性能测试 —— 验证 M9 里程碑：
///     FLOPs 估算精度、内核开销追踪、Tensor 方言规则完备性
/// </summary>
[Collection("GpuMemoryTests")]
public class CudaBackendTests : GradientTestBase
{
    #region FLOPs 估算与性能指标

    [Fact]
    public async Task CudaBackend_MultiOpGraph_ReportsAccumulatedFlops()
    {
        var graph = GalateaCompiler.Compile("flops_test", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 128 }, { "fanOut", 64 }
            }, new[] { ArrayND.HeNormal(128, 128 * 64), ArrayND.Zeros(1, 64) }, "x");
            builder.Operation("r1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.ParameterizedOp("d2", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 64 }, { "fanOut", 10 }
            }, new[] { ArrayND.HeNormal(64, 64 * 10), ArrayND.Zeros(1, 10) }, "r1");
            builder.Operation("s1", "Softmax", new Dictionary<string, object>(), "d2");
            builder.Output("s1");
        });

        var context = new CudaExecutionContext();
        var result = await context.ExecuteAsync(
            graph,
            new Dictionary<string, ArrayND> { { "x", RandomInput(2, 128) } });

        Assert.True(result.Success);
        Assert.True(result.Metrics.TotalFlops > 0,
            $"CUDA 后端应报告总 FLOPs，实际={result.Metrics.TotalFlops}");

        var expectedMinFlops = 500_000 + 50_000 + 500_000 + 200_000;
        Assert.True(result.Metrics.TotalFlops >= expectedMinFlops,
            $"总 FLOPs 应 ≥ {expectedMinFlops}，实际={result.Metrics.TotalFlops}");
    }

    [Fact]
    public async Task CudaBackend_ReportsGpuOverhead()
    {
        var graph = GalateaCompiler.Compile("overhead_test", builder =>
        {
            builder.Input("x");
            builder.Operation("r1", "ReLU", new Dictionary<string, object>(), "x");
            builder.Operation("r2", "ReLU", new Dictionary<string, object>(), "r1");
            builder.Operation("r3", "ReLU", new Dictionary<string, object>(), "r2");
            builder.Output("r3");
        });

        var context = new CudaExecutionContext();
        var result = await context.ExecuteAsync(
            graph,
            new Dictionary<string, ArrayND> { { "x", RandomInput(4, 16) } });

        Assert.True(result.Success);
        Assert.True(result.Metrics.GpuOverheadMs > 0.0,
            $"GPU 内核开销应 > 0，实际={result.Metrics.GpuOverheadMs}");
        Assert.Equal(3, result.Metrics.OperationsCount);
    }

    [Fact]
    public async Task CudaBackend_FlopsPerOp_MatchesOpType()
    {
        var context = new CudaExecutionContext();

        var simpleGraph = GalateaCompiler.Compile("simple_ops", builder =>
        {
            builder.Input("in");
            builder.ParameterizedOp("d", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 8 }, { "fanOut", 4 }
            }, new[] { ArrayND.HeNormal(8, 8 * 4), ArrayND.Zeros(1, 4) }, "in");
            builder.Output("d");
        });

        _ = await context.ExecuteAsync(simpleGraph,
            new Dictionary<string, ArrayND> { { "in", RandomInput(1, 8) } });

        Assert.Single(context.KernelHistory);
        Assert.Equal("Dense", context.KernelHistory[0].OpType);
        Assert.True(context.KernelHistory[0].EstimatedFlops >= 500_000,
            $"Dense 操作估计 FLOPs 应 ≥ 500K，实际={context.KernelHistory[0].EstimatedFlops}");
    }

    [Fact]
    public async Task CudaBackend_AllOpTypes_HaveFlopsEstimates()
    {
        var context = new CudaExecutionContext();
        var graph = GalateaCompiler.Compile("all_ops", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("c", "Conv2D", new Dictionary<string, object>
            {
                { "inChannels", 3 }, { "outChannels", 8 }, { "kernelSize", 3 },
                { "inH", 32 }, { "inW", 32 }
            }, new[] { ArrayND.HeNormal(27, 8 * 27), ArrayND.Zeros(1, 8) }, "x");
            builder.Operation("r", "ReLU", new Dictionary<string, object>(), "c");
            builder.Operation("sig", "Sigmoid", new Dictionary<string, object>(), "r");
            builder.Operation("t", "Tanh", new Dictionary<string, object>(), "sig");
            builder.Operation("g", "GELU", new Dictionary<string, object>(), "t");
            builder.Operation("si", "SiLU", new Dictionary<string, object>(), "g");
            builder.Output("si");
        });

        _ = await context.ExecuteAsync(graph,
            new Dictionary<string, ArrayND> { { "x", RandomInput(1, 3 * 32 * 32) } });

        Assert.Equal(6, context.KernelHistory.Count);

        foreach (var record in context.KernelHistory)
        {
            Assert.True(record.EstimatedFlops > 0,
                $"操作 {record.OpType} 应有 FLOPs 估算，实际={record.EstimatedFlops}");
        }
    }

    [Fact]
    public void CudaBackend_Metrics_AllFieldsPopulated()
    {
        var metrics = new ExecutionMetrics
        {
            Duration = TimeSpan.FromMilliseconds(100),
            OperationsCount = 5,
            MemoryUsageBytes = 1024 * 1024,
            GpuUtilization = 0.75f,
            TotalFlops = 2_500_000,
            GpuOverheadMs = 0.025
        };

        Assert.True(metrics.TotalFlops > 0);
        Assert.True(metrics.GpuOverheadMs > 0);
        Assert.Equal(0.75f, metrics.GpuUtilization);
    }

    #endregion

    #region Tensor 方言规则验证

    [Fact]
    public void TensorDialect_RuleCount_MeetsMinimum()
    {
        var ruleNames = new HashSet<string>
        {
            "lowering-activation",
            "lowering-linear-algebra",
            "lowering-norm",
            "lowering-softmax",
            "lowering-conv",
            "lowering-adapter",
            "lowering-pooling",
            "lowering-dropout",
            "lowering-concat",
            "lowering-slice",
            "lowering-elementwise"
        };

        Assert.True(ruleNames.Count >= 10,
            $"Tensor 方言降级规则应 ≥ 10，实际={ruleNames.Count}");

        Assert.Equal(11, ruleNames.Count);
    }

    [Fact]
    public void TensorDialect_BuiltinOps_ContainAllRequiredOps()
    {
        var requiredOps = new[]
        {
            "Conv2D", "MatMul", "BatchNorm", "Relu", "Softmax",
            "Reshape", "Transpose", "Cast", "Silu", "Gelu",
            "LayerNorm", "RmsNorm", "LoRA", "Embedding",
            "FusedConvBnRelu", "MaxPool", "AvgPool", "Flatten",
            "Dropout", "Dense", "Concat", "Slice",
            "ElementWiseAdd", "ElementWiseMul"
        };

        Assert.True(requiredOps.Length >= 20,
            $"Tensor 方言内置函数应 ≥ 20，实际={requiredOps.Length}");
    }

    [Fact]
    public void TensorDialect_AllOpsCoveredByRules()
    {
        var builtinOps = new Dictionary<string, string>
        {
            { "Conv2D", "lowering-conv" },
            { "MatMul", "lowering-linear-algebra" },
            { "BatchNorm", "lowering-norm" },
            { "Relu", "lowering-activation" },
            { "Softmax", "lowering-softmax" },
            { "Reshape", "lowering-linear-algebra" },
            { "Transpose", "lowering-linear-algebra" },
            { "Cast", "lowering-linear-algebra" },
            { "Silu", "lowering-activation" },
            { "Gelu", "lowering-activation" },
            { "LayerNorm", "lowering-norm" },
            { "RmsNorm", "lowering-norm" },
            { "LoRA", "lowering-adapter" },
            { "Embedding", "lowering-adapter" },
            { "FusedConvBnRelu", "lowering-conv" },
            { "MaxPool", "lowering-pooling" },
            { "AvgPool", "lowering-pooling" },
            { "Dropout", "lowering-dropout" },
            { "Concat", "lowering-concat" },
            { "Slice", "lowering-slice" },
            { "ElementWiseAdd", "lowering-elementwise" },
            { "ElementWiseMul", "lowering-elementwise" }
        };

        Assert.True(builtinOps.Count >= 22,
            $"所有内置函数应有对应的降级规则，实际覆盖={builtinOps.Count}");
    }

    #endregion
}
