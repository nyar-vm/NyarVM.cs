namespace Galatea.Tests;

using Galatea.Compiler;
using Galatea.Execution;
using Galatea.Models;
using Galatea.Training;

/// <summary>
///     ResNet-8 + CUDA 后端集成测试 —— 验证 M6 里程碑
/// </summary>
[Collection("GpuMemoryTests")]
public class ResNetCudaTests : GradientTestBase
{
    #region ResNet-8 前向传播

    [Fact]
    public void ResNet8_Forward_ProducesCorrectShape()
    {
        var model = new ResNet8(numClasses: 10);
        var input = RandomInput(2, 3 * 32 * 32);

        var output = model.Forward(input);

        Assert.Equal(2, output.Shape[0]);
        Assert.Equal(10, output.Shape[1]);
    }

    [Fact]
    public void ResNet8_Forward_DeterministicForSameInput()
    {
        var model = new ResNet8(numClasses: 10);
        var input = RandomInput(1, 3 * 32 * 32);

        var output1 = model.Forward(input);
        var output2 = model.Forward(input);

        var error = MaxAbsoluteError(output1, output2);
        Assert.True(error < 1e-6f, $"相同输入应产生相同输出，误差={error}");
    }

    [Fact]
    public void ResNet8_Forward_ChangesWithDifferentInput()
    {
        var model = new ResNet8(numClasses: 5);
        var input1 = RandomInput(1, 3 * 32 * 32);
        var input2 = AddNoise(input1, scale: 0.1f);

        var output1 = model.Forward(input1);
        var output2 = model.Forward(input2);

        var error = MaxAbsoluteError(output1, output2);
        Assert.True(error > 1e-5f, $"不同输入应产生不同输出，误差={error}");
    }

    /// <summary>
    ///     对张量添加小幅随机噪声（无固定种子）
    /// </summary>
    private static ArrayND AddNoise(ArrayND input, float scale)
    {
        var rng = new Random();
        var result = input.Clone();
        var span = result.AsWriteSpan();
        for (var i = 0; i < span.Length; i++)
        {
            span[i] += ((float)rng.NextDouble() * 2.0f - 1.0f) * scale;
        }
        return result;
    }

    [Fact]
    public void ResNet8_ParameterCount_MatchesArchitecture()
    {
        var model = new ResNet8(numClasses: 10);
        var parameters = model.Parameters().ToList();

        Assert.True(parameters.Count > 10, $"ResNet-8 应有多个参数层，实际={parameters.Count}");

        foreach (var p in parameters)
        {
            Assert.True(p.Value.Shape.Length > 0, "每个参数应有有效形状");
        }
    }

    #endregion

    #region ResNet-8 训练

    [Fact]
    public void ResNet8_Training_ReducesLoss()
    {
        var (trainX, trainY) = GenerateSyntheticCifarData(32, 10);
        var (evalX, evalY) = GenerateSyntheticCifarData(16, 10);

        var model = new ResNet8(numClasses: 10);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new Trainer(model, optimizer);

        var (initialLoss, _) = trainer.Evaluate(evalX, evalY, batchSize: 8);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 3, batchSize: 8);

        var finalLoss = history.EvalLosses[^1];
        Assert.True(finalLoss < initialLoss,
            $"ResNet-8 训练后评估损失应下降：初始={initialLoss:F4}，最终={finalLoss:F4}");
    }

    #endregion

    #region CUDA 后端执行

    [Fact]
    public async Task CudaContext_ExecuteSimpleGraph_ProducesCorrectOutput()
    {
        var graph = GalateaCompiler.Compile("cuda_test", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 4 }, { "fanOut", 3 }
            }, new[] { ArrayND.HeNormal(4, 4 * 3), ArrayND.Zeros(1, 3) }, "x");
            builder.Operation("a1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.Output("a1");
        });

        var input = RandomInput(2, 4);

        var cpuContext = new CpuExecutionContext();
        var cpuResult = await cpuContext.ExecuteAsync(graph, new Dictionary<string, ArrayND> { { "x", input } });

        var cudaContext = new CudaExecutionContext();
        var cudaResult = await cudaContext.ExecuteAsync(graph, new Dictionary<string, ArrayND> { { "x", input } });

        Assert.True(cudaResult.Success);
        Assert.True(cpuResult.Success);

        var error = MaxAbsoluteError(cpuResult.Outputs["a1"], cudaResult.Outputs["a1"]);
        Assert.True(error < GradientTolerance,
            $"CUDA 和 CPU 执行结果应一致，误差={error}");
    }

    [Fact]
    public async Task CudaContext_Metrics_AreRecorded()
    {
        var graph = GalateaCompiler.Compile("cuda_metrics", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 8 }, { "fanOut", 5 }
            }, new[] { ArrayND.HeNormal(8, 8 * 5), ArrayND.Zeros(1, 5) }, "x");
            builder.Operation("a1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.Output("a1");
        });

        var cudaContext = new CudaExecutionContext();
        var result = await cudaContext.ExecuteAsync(
            graph,
            new Dictionary<string, ArrayND> { { "x", RandomInput(4, 8) } });

        Assert.True(result.Success);
        Assert.Equal(ExecutionDevice.Cuda, cudaContext.Device);
        Assert.True(result.Metrics.Duration.Ticks > 0, "执行时长应 > 0");
        Assert.Equal(2, result.Metrics.OperationsCount);
        Assert.True(result.Metrics.GpuUtilization >= 0.0f, "GPU 利用率应 ≥ 0");
    }

    [Fact]
    public async Task CudaContext_KernelHistory_RecordsAllOps()
    {
        var graph = GalateaCompiler.Compile("kernel_hist", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 10 }, { "fanOut", 5 }
            }, new[] { ArrayND.HeNormal(10, 10 * 5), ArrayND.Zeros(1, 5) }, "x");
            builder.Operation("r1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.Operation("s1", "Sigmoid", new Dictionary<string, object>(), "r1");
            builder.Output("s1");
        });

        var cudaContext = new CudaExecutionContext();
        _ = await cudaContext.ExecuteAsync(
            graph,
            new Dictionary<string, ArrayND> { { "x", RandomInput(2, 10) } });

        Assert.Equal(3, cudaContext.KernelHistory.Count);

        Assert.Equal("Dense", cudaContext.KernelHistory[0].OpType);
        Assert.Equal("ReLU", cudaContext.KernelHistory[1].OpType);
        Assert.Equal("Sigmoid", cudaContext.KernelHistory[2].OpType);
    }

    [Fact]
    public async Task CudaContext_MemoryTracking_RecordsPeak()
    {
        GpuMemoryTracker.Reset();

        var context = new CudaExecutionContext();
        // Dense 5 输出 [batch,5]
        // ReLU 5 输入/输出 [batch,5]
        // 所以有外部输入 [batch,10] + dense权重 [10,50] + dense偏置 [1,5] + 中间结果 [batch,5] 等

        Assert.Equal(0, GpuMemoryTracker.AllocatedBytes);
        Assert.Equal(0, GpuMemoryTracker.ActiveTensorCount);
    }

    [Fact]
    public void CudaContext_Reset_ClearsState()
    {
        var context = new CudaExecutionContext();
        context.Reset();

        Assert.Empty(context.KernelHistory);
        Assert.Equal(0, GpuMemoryTracker.AllocatedBytes);
        Assert.Equal(0, GpuMemoryTracker.ActiveTensorCount);
    }

    #endregion

    #region GPU 显存管理

    [Fact]
    public void GpuTensor_AllocatesMemory_RecordsInTracker()
    {
        GpuMemoryTracker.Reset();

        var tensor = new GpuTensor(new[] { 2, 3, 4 });
        Assert.Equal(2 * 3 * 4 * sizeof(float), tensor.MemoryBytes);

        GpuMemoryTracker.Register(tensor);
        Assert.Equal(tensor.MemoryBytes, GpuMemoryTracker.AllocatedBytes);
        Assert.Equal(1, GpuMemoryTracker.ActiveTensorCount);

        GpuMemoryTracker.Unregister(tensor);
        Assert.Equal(0, GpuMemoryTracker.AllocatedBytes);
        Assert.Equal(0, GpuMemoryTracker.ActiveTensorCount);
    }

    [Fact]
    public void GpuMemoryTracker_PeakBytes_Recorded()
    {
        GpuMemoryTracker.Reset();

        var t1 = new GpuTensor(new[] { 1000, 1000 });
        GpuMemoryTracker.Register(t1);
        var peak1 = GpuMemoryTracker.PeakBytes;

        var t2 = new GpuTensor(new[] { 2000, 2000 });
        GpuMemoryTracker.Register(t2);
        var peak2 = GpuMemoryTracker.PeakBytes;

        Assert.True(peak2 > peak1, "峰值应在分配更多张量后更新");
    }

    [Fact]
    public void GpuMemoryTracker_Utilization_BetweenZeroAndOne()
    {
        GpuMemoryTracker.Reset();

        var utilization = GpuMemoryTracker.MemoryUtilization;
        Assert.True(utilization >= 0.0f && utilization <= 1.0f,
            $"显存利用率应在 [0,1] 之间，当前={utilization}");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     生成合成 CIFAR 风格数据（3 通道 32×32 图像 → 分类标签）
    /// </summary>
    private static (ArrayND Inputs, ArrayND Labels) GenerateSyntheticCifarData(int numSamples, int numClasses)
    {
        var rng = new Random(42);
        var inputsData = new float[numSamples * 3 * 32 * 32];
        var labelsData = new float[numSamples];

        for (var i = 0; i < numSamples; i++)
        {
            labelsData[i] = i % numClasses;
            var label = (int)labelsData[i];
            for (var j = 0; j < 3 * 32 * 32; j++)
            {
                inputsData[i * 3 * 32 * 32 + j] = (float)(rng.NextDouble() - 0.5) * 0.1f
                    + (j % 3 * 32 * 32 / 10 == label ? 0.5f : 0.0f);
            }
        }

        return (ArrayND.FromArray(inputsData, numSamples, 3 * 32 * 32),
                ArrayND.FromArray(labelsData, numSamples, 1));
    }

    #endregion
}
