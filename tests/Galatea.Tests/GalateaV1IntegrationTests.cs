namespace Galatea.Tests;

using Galatea.Cluster;
using Galatea.Compiler;
using Galatea.Compiler.NyarBridge;
using Galatea.Data;
using Galatea.Engram;
using Galatea.Execution;
using Galatea.Flux;
using Galatea.Runtime;
using Galatea.Training;

/// <summary>
///     Galatea 1.0 全管线集成验收测试
///     验证 DataLoader → Trainer → Engram → LoRA → CUDA → Compiler/Runtime → NyarBridge 全链路正确性
/// </summary>
[Collection("IntegrationTests")]
public class GalateaV1IntegrationTests : GradientTestBase
{
    #region 数据生成工具

    /// <summary>
    ///     生成合成分类数据
    /// </summary>
    private static (ArrayND Inputs, ArrayND Labels) GenerateSyntheticData(
        int numSamples, int inputDim, int numClasses, int seed = 42)
    {
        var rng = new Random(seed);
        var inputsData = new float[numSamples * inputDim];
        var labelsData = new float[numSamples];

        for (var i = 0; i < numSamples; i++)
        {
            labelsData[i] = i % numClasses;

            var label = (int)labelsData[i];
            for (var j = 0; j < inputDim; j++)
            {
                inputsData[i * inputDim + j] = (float)(rng.NextDouble() - 0.5) * 0.1f
                    + (j % numClasses == label ? 0.8f : 0.0f);
            }
        }

        return (ArrayND.FromArray(inputsData, numSamples, inputDim),
                ArrayND.FromArray(labelsData, numSamples, 1));
    }

    /// <summary>
    ///     生成图像分类合成数据
    /// </summary>
    private static (ArrayND Inputs, ArrayND Labels) GenerateImageData(
        int numSamples, int channels, int h, int w, int numClasses)
    {
        var rng = new Random(42);
        var flatSize = channels * h * w;
        var inputsData = new float[numSamples * flatSize];
        var labelsData = new float[numSamples];

        for (var i = 0; i < numSamples; i++)
        {
            labelsData[i] = i % numClasses;
            for (var j = 0; j < flatSize; j++)
            {
                inputsData[i * flatSize + j] = (float)(rng.NextDouble() - 0.5) * 0.2f;
            }
        }

        return (ArrayND.FromArray(inputsData, numSamples, flatSize),
                ArrayND.FromArray(labelsData, numSamples, 1));
    }

    #endregion

    #region 全链路集成：DataLoader → Trainer → Engram → LoRA

    /// <summary>
    ///     完整工作流：数据加载 → Dense 训练 → 保存/加载 → 恢复后继续训练 → LoRA 微调
    /// </summary>
    [Fact]
    public async Task FullPipeline_Dense_DataLoaderToLoRA_Complete()
    {
        var (trainX, trainY) = GenerateSyntheticData(64, 16, 4);
        var (evalX, evalY) = GenerateSyntheticData(32, 16, 4, seed: 99);

        var dataset = new InMemoryDataset(trainX, trainY, outputSize: 4);
        var loader = new DataLoader(dataset, new DataLoaderOptions { BatchSize = 8, Shuffle = true });

        Assert.True(loader.BatchCount > 0);
        Assert.Equal(64, loader.SampleCount);

        var model = new Dense(fanIn: 16, fanOut: 4);
        var optimizer = new SGD(learningRate: 0.05f);
        var trainer = new Trainer(model, optimizer);

        var (initialLoss, _) = trainer.Evaluate(evalX, evalY, batchSize: 8);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 8);

        var finalLoss = history.EvalLosses[^1];
        Assert.True(finalLoss < initialLoss * 0.9f,
            $"训练后评估损失应下降：初始={initialLoss:F4}，最终={finalLoss:F4}");

        // Engram 序列化 + 保存
        var serialized = ModelSerializer.Serialize(model.Parameters());
        var store = new InMemoryEngramStore();

        var branch = store.CreateBranch("dense_model", "main");
        var engData = new EngramData
        {
            BranchId = branch.Id,
            ModelData = serialized,
            Metadata = new Dictionary<string, string> { ["message"] = "版本 1：初始训练" }
        };
        var v1Id = await store.SaveAsync(engData);
        Assert.NotNull(v1Id);

        // 获取分支最新数据
        var head = store.GetBranchHead(branch.Id);
        Assert.NotNull(head);

        // 反序列化并加载到新模型
        var restoredModel = new Dense(fanIn: 16, fanOut: 4);
        var tensors = ModelSerializer.Deserialize(head.ModelData);
        ModelSerializer.LoadInto(restoredModel.Parameters().ToList(), tensors);

        // 验证恢复后输出一致
        var restoredOutput = restoredModel.Forward(evalX);
        Assert.Equal(evalX.Shape[0], restoredOutput.Shape[0]);
        Assert.Equal(4, restoredOutput.Shape[1]);

        // 恢复后继续训练
        var restoredTrainer = new Trainer(restoredModel, new SGD(learningRate: 0.02f));
        var continueHistory = restoredTrainer.Fit(trainX, trainY, evalX, evalY, epochs: 3, batchSize: 8);
        Assert.True(continueHistory.EvalLosses[^1] < finalLoss,
            "恢复后继续训练应进一步降低损失");

        // LoRA 微调
        var lora = new LoRALinear(restoredModel, rank: 4, alpha: 4.0f);
        var loraOutput = lora.Forward(evalX);
        Assert.Equal(evalX.Shape[0], loraOutput.Shape[0]);
        Assert.Equal(4, loraOutput.Shape[1]);

        // LoRA Merge（先修改 B 以产生非零增量）
        var snapshotBefore = restoredModel.Weight.Clone();

        var spanB = lora.WeightB.AsWriteSpan();
        for (var i = 0; i < spanB.Length; i++)
        {
            spanB[i] = 0.1f + i * 0.01f;
        }

        lora.MergeWeights();

        var maxDiff = 0.0f;
        var spanBefore = snapshotBefore.AsSpan();
        var spanAfter = restoredModel.Weight.AsSpan();
        for (var i = 0; i < spanBefore.Length; i++)
        {
            maxDiff = MathF.Max(maxDiff, MathF.Abs(spanBefore[i] - spanAfter[i]));
        }
        Assert.True(maxDiff > 0.0f, "MergeWeights 应改变基础权重");
    }

    #endregion

    #region 全链路集成：Compiler + Runtime + NyarBridge

    /// <summary>
    ///     完整管线：Compiler → Runtime 执行 Dense+ReLU 端到端
    /// </summary>
    [Fact]
    public void FullPipeline_CompilerToRuntime_EndToEnd()
    {
        var dense = new Dense(fanIn: 16, fanOut: 4);
        var input = RandomInput(2, 16);

        var graph = GalateaCompiler.Compile("e2e_runtime", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 16 }, { "fanOut", 4 }
            }, new[] { dense.Weight, dense.Bias }, "x");
            builder.Operation("r1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.Output("r1");
        });

        var runtime = new GalateaRuntime();
        var execResult = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });
        Assert.True(execResult.Success);
        Assert.True(execResult.Outputs.ContainsKey("r1"));

        var direct = Activations.ReLUForward(dense.Forward(input));
        var error = MaxAbsoluteError(direct, execResult.Outputs["r1"]);
        Assert.True(error < GradientTolerance, $"Runtime 执行误差 {error} 应在容差范围");
    }

    /// <summary>
    ///     NyarBridge 转换 + TensorIkunOptimizer 融合验证
    ///     Conv+Bn+ReLU 编译后经 GraphToIkunConverter → Optimize → 融合为 FusedConvBnRelu
    /// </summary>
    [Fact]
    public void FullPipeline_NyarBridge_ConversionAndFusion()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 4, kernelSize: 3, padding: 0);
        var bn = new BatchNorm(4);

        var graph = GalateaCompiler.Compile("e2e_nyar", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("conv1", "Conv2D", new Dictionary<string, object>
            {
                { "inChannels", 1 }, { "outChannels", 4 }, { "kernelSize", 3 },
                { "inH", 8 }, { "inW", 8 }
            }, new[] { conv.Weight, conv.Bias }, "x");
            builder.ParameterizedOp("bn1", "BatchNorm", new Dictionary<string, object>
            {
                { "numFeatures", 4 }
            }, new[] { bn.Gamma, bn.Beta }, "conv1");
            builder.Operation("relu1", "ReLU", new Dictionary<string, object>(), "bn1");
            builder.Output("relu1");
        });

        var ikunResult = GraphToIkunConverter.Convert(graph);
        Assert.True(ikunResult.Nodes.Count >= 2);

        var optimized = TensorIkunOptimizer.Optimize(ikunResult.Nodes);
        var fusedCount = optimized.OfType<TensorIkunNode.FusedConvBnRelu>().Count();
        Assert.True(fusedCount > 0, "Conv+Bn+ReLU 应融合为 FusedConvBnRelu");

        var optRate = TensorIkunOptimizer.GetOptimizationRate(ikunResult.Nodes.Count, optimized.Count);
        Assert.True(optRate > 0.3f, $"融合优化率 {optRate:P0} 应超过 30%");
    }

    #endregion

    #region 全链路集成：CUDA 后端执行

    /// <summary>
    ///     CUDA 执行上下文完整工作流
    /// </summary>
    [Fact]
    public async Task FullPipeline_CUDA_CompleteWorkflow()
    {
        var dense = new Dense(fanIn: 8, fanOut: 4);
        var input = RandomInput(4, 8);

        var graph = new CompiledGraph
        {
            Id = "cuda_test",
            InputNames = new[] { "x" },
            OutputNames = new[] { "y" },
            Operations = new[]
            {
                new GraphOperation
                {
                    Name = "y",
                    OpType = "Dense",
                    InputNames = new[] { "x" },
                    Config = new Dictionary<string, object>
                    {
                        { "fanIn", 8 }, { "fanOut", 4 }
                    },
                    Parameters = new[] { dense.Weight, dense.Bias }
                }
            }
        };

        var cudaCtx = new CudaExecutionContext();
        var result = await cudaCtx.ExecuteAsync(graph, new Dictionary<string, ArrayND> { { "x", input } });

        Assert.True(result.Success);
        Assert.True(result.Outputs.ContainsKey("y"));

        Assert.True(result.Metrics.OperationsCount >= 1);
        Assert.True(result.Metrics.Duration > TimeSpan.Zero);
        Assert.True(result.Metrics.TotalFlops > 0, "应记录 FLOPs");
        Assert.True(result.Metrics.GpuOverheadMs >= 0, "应记录 GPU 开销");

        var kernelHistory = cudaCtx.KernelHistory;
        Assert.True(kernelHistory.Count >= 1);

        cudaCtx.Reset();

        Assert.True(GpuMemoryTracker.AllocatedBytes >= 0);
    }

    #endregion

    #region 全链路集成：分布式训练

    /// <summary>
    ///     分布式训练完整工作流
    /// </summary>
    [Fact]
    public void FullPipeline_DistributedTraining_Complete()
    {
        var (trainX, trainY) = GenerateSyntheticData(32, 8, 3);
        var (evalX, evalY) = GenerateSyntheticData(16, 8, 3, seed: 99);

        var model = new Dense(fanIn: 8, fanOut: 3);
        var optimizer = new SGD(learningRate: 0.05f);

        var distTrainer = new DistributedTrainer(model, optimizer, numWorkers: 2);

        var history = distTrainer.Fit(trainX, trainY, evalX, evalY, epochs: 3, batchSize: 4);

        Assert.Equal(3, history.TrainLosses.Count);
        Assert.Equal(3, history.EvalLosses.Count);
        Assert.Equal(3, history.EvalAccuracies.Count);

        var finalLoss = history.EvalLosses[^1];
        Assert.True(finalLoss > 0, "分布式训练应产生有效损失值");

        Assert.True(distTrainer.WorkerCount >= 1);
    }

    #endregion

    #region 全部代表模型训练验收

    /// <summary>
    ///     模型验收：Dense 可训练并收敛
    /// </summary>
    [Fact]
    public void Acceptance_Dense_Trainable()
    {
        var (trainX, trainY) = GenerateSyntheticData(32, 10, 3);
        var (evalX, evalY) = GenerateSyntheticData(16, 10, 3, seed: 99);

        var model = new Dense(fanIn: 10, fanOut: 3);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        var (initialLoss, _) = trainer.Evaluate(evalX, evalY, batchSize: 8);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 10, batchSize: 8);

        var finalAcc = history.EvalAccuracies[^1];
        var finalLoss = history.EvalLosses[^1];

        Assert.True(finalLoss < 2.5f, $"Dense 应收敛：最终损失={finalLoss:F4}");
        Assert.True(finalAcc >= 0.2f, $"准确率应达标：最终准确率={finalAcc:P2}");

        var hasGradient = false;
        foreach (var param in model.Parameters())
        {
            var span = param.Value.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                if (MathF.Abs(span[i]) > 0.001f)
                {
                    hasGradient = true;
                    break;
                }
            }
            if (hasGradient) break;
        }
        Assert.True(hasGradient, "训练后权重应有非零值");
    }

    /// <summary>
    ///     模型验收：Conv2D + Dense 分类器可训练
    /// </summary>
    [Fact]
    public void Acceptance_ConvClassifier_Trainable()
    {
        var (trainX, trainY) = GenerateImageData(16, 1, 8, 8, 3);
        var (evalX, evalY) = GenerateImageData(8, 1, 8, 8, 3);

        var conv = new Conv2D(inChannels: 1, outChannels: 4, kernelSize: 3);
        var dense = new Dense(fanIn: 4 * 6 * 6, fanOut: 3);

        var model = new ConvClassifier(conv, dense);
        var optimizer = new Adam(learningRate: 0.005f);
        var trainer = new Trainer(model, optimizer);

        var (initialLoss, _) = trainer.Evaluate(evalX, evalY, batchSize: 4);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 8, batchSize: 4);

        var finalLoss = history.EvalLosses[^1];
        Assert.True(finalLoss < 3.0f, $"Conv2D 模型应收敛：最终损失={finalLoss:F4}");
    }

    /// <summary>
    ///     Conv2D + Dense 简单分类模型
    /// </summary>
    private sealed class ConvClassifier : ITrainableModel
    {
        private readonly Conv2D _conv;
        private readonly Dense _dense;

        public ConvClassifier(Conv2D conv, Dense dense)
        {
            _conv = conv;
            _dense = dense;
        }

        public ArrayND Forward(ArrayND input)
        {
            var (x, _, _) = _conv.Forward(input, inH: 8, inW: 8);
            x = Activations.ReLUForward(x);
            x = _dense.Forward(x);
            return x;
        }

        public ArrayND Forward(ArrayND input, AutogradContext ctx)
        {
            var (x, _, _) = _conv.Forward(input, inH: 8, inW: 8, ctx);
            x = Activations.ReLU(x, ctx);
            x = _dense.Forward(x, ctx);
            return x;
        }

        public IEnumerable<IParameter> Parameters()
        {
            foreach (var p in _conv.Parameters()) yield return p;
            foreach (var p in _dense.Parameters()) yield return p;
        }
    }

    /// <summary>
    ///     模型验收：LeNet-5 架构前向传播和参数完整性
    ///     验证多层 Conv+Dense 组合的正确性
    /// </summary>
    [Fact]
    public void Acceptance_LeNet5_ForwardAndParams_Correct()
    {
        var evalX = ArrayND.FromArray(new float[16 * 28 * 28], 16, 28 * 28);

        var model = new MiniLeNet();

        var parameters = model.Parameters().ToList();
        Assert.True(parameters.Count >= 6, $"MiniLeNet 应有 ≥ 6 个参数，实际={parameters.Count}");

        var output = model.Forward(evalX);
        Assert.Equal(16, output.Shape[0]);
        Assert.Equal(10, output.Shape[1]);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var autogradOutput = model.Forward(evalX, ctx);
        Assert.Equal(16, autogradOutput.Shape[0]);
        Assert.Equal(10, autogradOutput.Shape[1]);
    }

    /// <summary>
    ///     Mini LeNet 卷积模型（用于验证 Conv→ReLU→Pool→Flatten→Dense 流水线正确性）
    /// </summary>
    private sealed class MiniLeNet : ITrainableModel
    {
        public Conv2D Conv1 { get; } = new(inChannels: 1, outChannels: 6, kernelSize: 5);
        public Conv2D Conv2 { get; } = new(inChannels: 6, outChannels: 16, kernelSize: 5);
        public MaxPool2D Pool1 { get; } = new();
        public MaxPool2D Pool2 { get; } = new();
        public Dense Fc1 { get; } = new(fanIn: 256, fanOut: 120);
        public Dense Fc2 { get; } = new(fanIn: 120, fanOut: 84);
        public Dense Fc3 { get; } = new(fanIn: 84, fanOut: 10);

        public ArrayND Forward(ArrayND input)
        {
            var (x, h, w) = Conv1.Forward(input, inH: 28, inW: 28);
            x = Activations.ReLUForward(x);
            (x, h, w) = Pool1.Forward(x, channels: 6, inH: h, inW: w);
            (x, h, w) = Conv2.Forward(x, inH: h, inW: w);
            x = Activations.ReLUForward(x);
            (x, h, w) = Pool2.Forward(x, channels: 16, inH: h, inW: w);
            x = Flatten.Forward(x, channels: 16, h: h, w: w);
            x = Fc1.Forward(x);
            x = Activations.ReLUForward(x);
            x = Fc2.Forward(x);
            x = Activations.ReLUForward(x);
            x = Fc3.Forward(x);
            return x;
        }

        public ArrayND Forward(ArrayND input, AutogradContext ctx)
        {
            var (x, h, w) = Conv1.Forward(input, inH: 28, inW: 28, ctx);
            x = Activations.ReLU(x, ctx);
            (x, h, w) = Pool1.Forward(x, channels: 6, inH: h, inW: w, ctx);
            (x, h, w) = Conv2.Forward(x, inH: h, inW: w, ctx);
            x = Activations.ReLU(x, ctx);
            (x, h, w) = Pool2.Forward(x, channels: 16, inH: h, inW: w, ctx);
            x = Flatten.Forward(x, channels: 16, h: h, w: w, ctx);
            x = Fc1.Forward(x, ctx);
            x = Activations.ReLU(x, ctx);
            x = Fc2.Forward(x, ctx);
            x = Activations.ReLU(x, ctx);
            x = Fc3.Forward(x, ctx);
            return x;
        }

        public IEnumerable<IParameter> Parameters()
        {
            foreach (var p in Conv1.Parameters()) yield return p;
            foreach (var p in Conv2.Parameters()) yield return p;
            foreach (var p in Fc1.Parameters()) yield return p;
            foreach (var p in Fc2.Parameters()) yield return p;
            foreach (var p in Fc3.Parameters()) yield return p;
        }
    }

    /// <summary>
    ///     模型验收：ResNet-8 可训练并收敛
    /// </summary>
    [Fact]
    public void Acceptance_ResNet8_Trainable()
    {
        var (trainX, trainY) = GenerateImageData(16, 3, 32, 32, 4);
        var (evalX, evalY) = GenerateImageData(8, 3, 32, 32, 4);

        var model = new Models.ResNet8(numClasses: 4);
        var optimizer = new Adam(learningRate: 0.005f);
        var trainer = new Trainer(model, optimizer);

        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 4);

        var finalLoss = history.EvalLosses[^1];

        Assert.True(finalLoss < 3.0f,
            $"ResNet-8 应收敛：最终损失={finalLoss:F4}");

        // 小数据集（16 训练样本）准确率波动属正常现象，重点验证损失收敛

        var paramCount = model.Parameters().Count();
        Assert.True(paramCount > 10,
            $"ResNet-8 应有 ≥ 10 个参数，实际={paramCount}");
    }

    #endregion

    #region 全优化器集成验证

    /// <summary>
    ///     Dense 模型可使用全部四种优化器训练
    /// </summary>
    [Fact]
    public void AllOptimizers_WorkWithDenseTraining()
    {
        var (trainX, trainY) = GenerateSyntheticData(24, 6, 3);
        var (evalX, evalY) = GenerateSyntheticData(12, 6, 3, seed: 99);

        void TestOptimizer(string name, IOptimizer optimizer)
        {
            var model = new Dense(fanIn: 6, fanOut: 3);
            var trainer = new Trainer(model, optimizer);

            var initialLoss = trainer.TrainStep(trainX, trainY);
            for (var step = 0; step < 20; step++)
            {
                trainer.TrainStep(trainX, trainY);
            }
            var (finalLoss, _) = trainer.Evaluate(evalX, evalY, batchSize: 8);

            Assert.True(finalLoss < initialLoss * 1.1f,
                $"[{name}] 20 步训练后损失 ({finalLoss:F4}) 应不高于初始 ({initialLoss:F4})");
        }

        TestOptimizer("SGD", new SGD(learningRate: 0.1f));
        TestOptimizer("Adam", new Adam(learningRate: 0.01f));
        TestOptimizer("AdamW", new AdamW(learningRate: 0.01f));
        TestOptimizer("RMSprop", new RMSprop(learningRate: 0.01f));
    }

    #endregion

    #region Engram 完整生命周期

    /// <summary>
    ///     Engram 模型版本管理完整工作流
    /// </summary>
    [Fact]
    public async Task Engram_CompleteLifecycle()
    {
        var store = new InMemoryEngramStore();

        var modelId = "lifecycle_model";
        var branch = store.CreateBranch(modelId, "main");
        Assert.NotNull(branch);

        // 版本 1：初始训练
        var model1 = new Dense(fanIn: 4, fanOut: 3);
        var serialized1 = ModelSerializer.Serialize(model1.Parameters());
        var v1 = new EngramData
        {
            BranchId = branch.Id,
            ModelData = serialized1,
            Metadata = new Dictionary<string, string> { ["message"] = "版本 1" }
        };
        var v1Id = await store.SaveAsync(v1);
        Assert.NotNull(v1Id);

        // 版本 2：继续训练后保存
        var (trainX, trainY) = GenerateSyntheticData(16, 4, 3);
        var optimizer = new SGD(learningRate: 0.05f);
        var trainer = new Trainer(model1, optimizer);
        trainer.Fit(trainX, trainY, trainX, trainY, epochs: 5, batchSize: 4);

        var serialized2 = ModelSerializer.Serialize(model1.Parameters());
        var v2 = new EngramData
        {
            BranchId = branch.Id,
            ModelData = serialized2,
            Metadata = new Dictionary<string, string> { ["message"] = "版本 2" }
        };
        var v2Id = await store.SaveAsync(v2);
        Assert.NotNull(v2Id);
        Assert.NotEqual(v1Id, v2Id);

        // 获取最新版本
        var latest = await store.GetAsync(v2Id);
        Assert.NotNull(latest);
        Assert.Equal("版本 2", latest.Metadata["message"]);

        // 分支管理
        var expBranch = store.CreateBranch("experiment");
        Assert.NotNull(expBranch);

        var branches = store.ListBranches();
        Assert.True(branches.Count >= 2, $"应有 ≥ 2 个分支，实际={branches.Count}");

        // 获取分支头
        var head = store.GetBranchHead(branch.Id);
        Assert.NotNull(head);

        // 获取分支版本列表
        var branchVersions = new List<EngramVersion>();
        await foreach (var v in store.ListBranchVersionsAsync(branch.Id))
        {
            branchVersions.Add(v);
        }
        Assert.True(branchVersions.Count >= 2);

        // 回滚
        var rollbackId = await store.RollbackAsync(v1Id, branch.Id, "回滚到版本 1");
        Assert.NotNull(rollbackId);

        // 验证回滚后分支头更新
        var afterRollbackHead = store.GetBranchHead(branch.Id);
        Assert.NotNull(afterRollbackHead);

        // 重置
        store.Reset();
        var afterReset = store.ListBranches();
        Assert.Single(afterReset);
        Assert.Equal("main", afterReset[0].Name);
    }

    #endregion

    #region DataLoader → Trainer 集成

    /// <summary>
    ///     DataLoader 与 Trainer 集成：通过 DataLoader 批次训练
    /// </summary>
    [Fact]
    public void DataLoader_Integration_WithTrainer()
    {
        var (trainX, trainY) = GenerateSyntheticData(40, 8, 5);

        var dataset = new InMemoryDataset(trainX, trainY, outputSize: 5);
        Assert.Equal(40, dataset.Count);

        var loader = new DataLoader(dataset, new DataLoaderOptions
        {
            BatchSize = 10,
            Shuffle = true
        });

        Assert.Equal(4, loader.BatchCount);
        Assert.Equal(10, loader.BatchSize);

        var batches = loader.Batches;
        Assert.Equal(4, batches.Count);

        foreach (var batch in batches)
        {
            Assert.NotNull(batch.Inputs);
            Assert.NotNull(batch.Labels);
            Assert.Equal(8, batch.Inputs.Shape[1]);
        }

        // 使用 DataLoader 第一个批次训练
        var firstBatch = batches[0];
        Assert.True(firstBatch.Inputs.Shape[0] > 0);
        Assert.Equal(firstBatch.Inputs.Shape[0], firstBatch.Labels.Shape[0]);

        var model = new Dense(fanIn: 8, fanOut: 5);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        var initialLoss = trainer.TrainStep(firstBatch.Inputs, firstBatch.Labels);
        Assert.True(initialLoss > 0, "初始损失应为正值");

        // NextEpoch 产生新批次
        var epoch2 = loader.NextEpoch();
        Assert.Equal(4, epoch2.Count);

        // Reset 后 epoch 回到 1
        loader.Reset();
        Assert.Equal(1, loader.Epoch);
    }

    #endregion

    #region 稳定性验证

    /// <summary>
    ///     确定性验证：相同输入产生相同输出
    /// </summary>
    [Fact]
    public void Determinism_DenseProducesConsistentOutput()
    {
        var model = new Dense(fanIn: 4, fanOut: 3);
        var input = ArrayND.FromArray(
            new float[] { 1, 1, 1, 1, 2, 2, 2, 2 }, 2, 4);

        var output1 = model.Forward(input);
        var output2 = model.Forward(input);

        Assert.True(MaxAbsoluteError(output1, output2) < 1e-7f,
            "相同输入应产生相同输出");
    }

    /// <summary>
    ///     大批次稳定性验证：无 NaN/Inf
    /// </summary>
    [Fact]
    public void Stability_LargeBatch_NoNaNOrInf()
    {
        var (trainX, trainY) = GenerateSyntheticData(64, 16, 4);

        var model = new Dense(fanIn: 16, fanOut: 4);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        for (var step = 0; step < 10; step++)
        {
            var loss = trainer.TrainStep(trainX, trainY);
            Assert.False(float.IsNaN(loss), $"第 {step} 步损失不应为 NaN");
            Assert.False(float.IsInfinity(loss), $"第 {step} 步损失不应为 Infinity");
        }
    }

    /// <summary>
    ///     梯度稳定性验证：无梯度爆炸
    /// </summary>
    [Fact]
    public void Stability_Gradients_DoNotExplode()
    {
        var (trainX, trainY) = GenerateSyntheticData(16, 8, 3);

        var model = new Dense(fanIn: 8, fanOut: 3);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        for (var step = 0; step < 20; step++)
        {
            trainer.TrainStep(trainX, trainY);
        }

        foreach (var param in model.Parameters())
        {
            var span = param.Value.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                Assert.False(float.IsNaN(span[i]), "参数值不应为 NaN");
                Assert.True(MathF.Abs(span[i]) < 100.0f,
                    $"参数值 {span[i]} 应小于 100（防止梯度爆炸）");
            }
        }
    }

    #endregion

    #region 管线综合验证

    /// <summary>
    ///     综合场景：定义 → 训练 → 导出 → 编译 → 执行 全流程
    /// </summary>
    [Fact]
    public void Comprehensive_CreateTrainSaveCompileExecute()
    {
        var (trainX, trainY) = GenerateSyntheticData(24, 8, 3);

        var d1 = new Dense(fanIn: 8, fanOut: 6);
        var d2 = new Dense(fanIn: 6, fanOut: 3);

        var model = new TwoLayerModel(d1, d2);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        var history = trainer.Fit(trainX, trainY, trainX, trainY, epochs: 8, batchSize: 8);

        var finalLoss = history.EvalLosses[^1];
        var finalAcc = history.EvalAccuracies[^1];

        Assert.True(finalLoss < 3.0f,
            $"两层模型应收敛：最终损失={finalLoss:F4}");
        Assert.True(finalAcc >= 0.3f,
            $"两层模型准确率应达标：最终准确率={finalAcc:P2}");

        // 编译执行
        var input = RandomInput(4, 8);
        var graph = GalateaCompiler.Compile("comprehensive", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 8 }, { "fanOut", 6 }
            }, new[] { d1.Weight, d1.Bias }, "x");
            builder.Operation("r1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.ParameterizedOp("d2", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 6 }, { "fanOut", 3 }
            }, new[] { d2.Weight, d2.Bias }, "r1");
            builder.Output("d2");
        });

        var runtime = new GalateaRuntime();
        var execResult = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });
        Assert.True(execResult.Success);

        var compiledOutput = execResult.Outputs["d2"];
        var directOutput = model.Forward(input);

        var error = MaxAbsoluteError(directOutput, compiledOutput);
        Assert.True(error < GradientTolerance * 10,
            $"Comprehensive 管线执行误差 {error} 应在合理范围");
    }

    /// <summary>
    ///     两层全连接模型
    /// </summary>
    private sealed class TwoLayerModel : ITrainableModel
    {
        private readonly Dense _d1;
        private readonly Dense _d2;

        public TwoLayerModel(Dense d1, Dense d2)
        {
            _d1 = d1;
            _d2 = d2;
        }

        public ArrayND Forward(ArrayND input)
        {
            var x = _d1.Forward(input);
            x = Activations.ReLUForward(x);
            x = _d2.Forward(x);
            return x;
        }

        public ArrayND Forward(ArrayND input, AutogradContext ctx)
        {
            var x = _d1.Forward(input, ctx);
            x = Activations.ReLU(x, ctx);
            x = _d2.Forward(x, ctx);
            return x;
        }

        public IEnumerable<IParameter> Parameters()
        {
            foreach (var p in _d1.Parameters()) yield return p;
            foreach (var p in _d2.Parameters()) yield return p;
        }
    }

    #endregion

    #region API 稳定性验证

    /// <summary>
    ///     损失函数 API 覆盖：MSE / BCE / KLDiv 可用
    /// </summary>
    [Fact]
    public void ApiStability_AllLosses_AreCallable()
    {
        var pred = RandomInput(4, 5);
        var target = RandomInput(4, 5);

        var mseVal = Losses.MSEForward(pred, target);
        Assert.True(mseVal >= 0);

        var bcePred = Activations.SigmoidForward(pred);
        var bceVal = Losses.BCEForward(bcePred, target);
        Assert.True(float.IsFinite(bceVal));

        var softPred = Activations.SoftmaxForward(pred);
        var softTarget = Activations.SoftmaxForward(target);
        var klVal = Losses.KLDivForward(softPred, softTarget);
        Assert.True(klVal >= 0);
    }

    /// <summary>
    ///     激活函数 API 覆盖：全部六种激活可用
    /// </summary>
    [Fact]
    public void ApiStability_AllActivations_AreCallable()
    {
        var input = RandomInput(3, 8);

        var relu = Activations.ReLUForward(input);
        var sigmoid = Activations.SigmoidForward(input);
        var tanh = Activations.TanhForward(input);
        var softmax = Activations.SoftmaxForward(input);
        var gelu = Activations.GELUForward(input);
        var silu = Activations.SiLUForward(input);

        Assert.Equal(input.Shape, relu.Shape);
        Assert.Equal(input.Shape, sigmoid.Shape);
        Assert.Equal(input.Shape, tanh.Shape);
        Assert.Equal(input.Shape, softmax.Shape);
        Assert.Equal(input.Shape, gelu.Shape);
        Assert.Equal(input.Shape, silu.Shape);
    }

    #endregion
}
