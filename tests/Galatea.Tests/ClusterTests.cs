namespace Galatea.Tests;

/// <summary>
///     集群分布式训练集成测试 —— 验证 M7 里程碑：
///     数据并行训练、AllReduce 梯度同步、多节点一致性
/// </summary>
public class ClusterTests : GradientTestBase
{
    #region 梯度同步器

    [Fact]
    public void GradientSynchronizer_NormalizeAccumulated_FourWorkers_CorrectAverage()
    {
        var model = new TestLinearModel(5, 3);
        var inputs = RandomInput(8, 5);
        var labels = CreateIndexLabels(8, 3);

        for (var w = 0; w < 4; w++)
        {
            var workerInputs = inputs.SliceRows(w * 2, 2);
            var workerLabels = labels.SliceRows(w * 2, 2);

            var ctx = new AutogradContext();
            ctx.StartRecording();
            var logits = model.Forward(workerInputs, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, workerLabels, ctx);
            ctx.Backward(loss);
        }

        var paramsAccumulated = model.Parameters().ToList();
        var gradCopyBefore = CloneGrads(paramsAccumulated);

        GradientSynchronizer.NormalizeAccumulatedGradients(paramsAccumulated, 4);

        var gradCopyAfter = paramsAccumulated.Select(p => p.Grad!).ToArray();
        for (var pIdx = 0; pIdx < gradCopyBefore.Length; pIdx++)
        {
            var spanBefore = gradCopyBefore[pIdx].AsSpan();
            var spanAfter = gradCopyAfter[pIdx].AsSpan();
            for (var i = 0; i < spanBefore.Length; i++) Assert.Equal(spanBefore[i] / 4.0f, spanAfter[i], 1e-6f);
        }
    }

    [Fact]
    public void GradientSynchronizer_SingleWorker_NoChange()
    {
        var model = new TestLinearModel(4, 2);
        var ctx = new AutogradContext();
        ctx.StartRecording();
        var inputs = RandomInput(4, 4);
        var labels = CreateIndexLabels(4, 2);
        var logits = model.Forward(inputs, ctx);
        var (loss, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);
        ctx.Backward(loss);

        var paramsList = model.Parameters().ToList();
        var gradBefore = CloneGrads(paramsList);

        GradientSynchronizer.NormalizeAccumulatedGradients(paramsList, 1);

        for (var i = 0; i < gradBefore.Length; i++)
        {
            var error = MaxAbsoluteError(gradBefore[i], paramsList[i].Grad!);
            Assert.True(error < 1e-6f, $"单 worker 梯度不应改变，误差={error}");
        }
    }

    [Fact]
    public void GradientSynchronizer_AllReduceAverage_ProducesCorrectMean()
    {
        var model = new TestLinearModel(3, 2);

        var gradGroup1 = ComputeWorkerGradients(model, 0, 4);
        var gradGroup2 = ComputeWorkerGradients(model, 4, 4);
        var gradGroup3 = ComputeWorkerGradients(model, 8, 4);

        var targetParams = model.Parameters().ToList();
        GradientSynchronizer.AllReduceAverage(targetParams,
            new[] { gradGroup1, gradGroup2, gradGroup3 });

        foreach (var p in targetParams)
        {
            Assert.NotNull(p.Grad);
            Assert.True(p.Grad!.Shape.Length > 0);
        }
    }

    #endregion

    #region 分布式训练器

    [Fact]
    public void DistributedTrainer_TwoWorkers_ReducesLoss()
    {
        var (trainX, trainY) = GenerateSyntheticData(40, 10);
        var (evalX, evalY) = GenerateSyntheticData(16, 10);

        var model = new TestLinearModel(10, 10);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new DistributedTrainer(model, optimizer, numWorkers: 2);

        var (initialLoss, initialAcc) = trainer.Evaluate(evalX, evalY, batchSize: 4);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 4);

        var finalLoss = history.EvalLosses[^1];
        Assert.True(finalLoss < initialLoss,
            $"分布式训练应降低损失：初始={initialLoss:F4}，最终={finalLoss:F4}");
    }

    [Fact]
    public void DistributedTrainer_FourWorkers_ReducesLoss()
    {
        var (trainX, trainY) = GenerateSyntheticData(64, 4);
        var (evalX, evalY) = GenerateSyntheticData(16, 4);

        var model = new TestLinearModel(4, 4);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new DistributedTrainer(model, optimizer, numWorkers: 4);

        var (initialLoss, _) = trainer.Evaluate(evalX, evalY, batchSize: 4);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 4);

        Assert.True(history.EvalLosses[^1] < initialLoss,
            "4 节点分布式训练应降低损失");
    }

    [Fact]
    public void DistributedTrainer_History_RecordsAllEpochs()
    {
        var (trainX, trainY) = GenerateSyntheticData(24, 4);
        var (evalX, evalY) = GenerateSyntheticData(8, 4);

        var model = new TestLinearModel(4, 4);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new DistributedTrainer(model, optimizer, numWorkers: 2);

        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 3, batchSize: 3);

        Assert.Equal(3, history.TrainLosses.Count);
        Assert.Equal(3, history.EvalLosses.Count);
        Assert.Equal(3, history.EvalAccuracies.Count);

        var totalSamples = 24 * 3;
        Assert.Equal(totalSamples, history.ThroughSamples[^1]);
    }

    [Fact]
    public void DistributedTrainer_WorkerMetrics_PerStepRecorded()
    {
        var model = new TestLinearModel(4, 4);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new DistributedTrainer(model, optimizer, numWorkers: 2);

        var inputs = RandomInput(8, 4);
        var labels = CreateIndexLabels(8, 4);

        trainer.DistributedTrainStep(inputs, labels);

        Assert.Equal(2, trainer.WorkerMetrics.Count);
        Assert.Equal(0, trainer.WorkerMetrics[0].WorkerId);
        Assert.Equal(1, trainer.WorkerMetrics[1].WorkerId);
        Assert.Equal(4, trainer.WorkerMetrics[0].BatchSize);
        Assert.Equal(4, trainer.WorkerMetrics[1].BatchSize);
    }

    [Fact]
    public void DistributedTrainer_LastHistory_SetAfterFit()
    {
        var (trainX, trainY) = GenerateSyntheticData(24, 4);
        var (evalX, evalY) = GenerateSyntheticData(8, 4);

        var model = new TestLinearModel(4, 4);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new DistributedTrainer(model, optimizer, numWorkers: 3);

        _ = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 2, batchSize: 3);

        Assert.NotNull(trainer.LastHistory);
        Assert.Equal(2, trainer.LastHistory!.TrainLosses.Count);
        Assert.True(trainer.LastHistory.EvalAccuracies[^1] >= 0.0f);
    }

    [Fact]
    public void DistributedTrainer_SmallBatch_FallsBack()
    {
        var model = new TestLinearModel(4, 4);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new DistributedTrainer(model, optimizer, numWorkers: 4);

        var inputs = RandomInput(3, 4);
        var labels = CreateIndexLabels(3, 4);

        var loss = trainer.DistributedTrainStep(inputs, labels);

        Assert.True(loss > 0.0f, "小批次退化为单节点训练，应产生有效损失");
    }

    [Fact]
    public void DistributedTrainer_Accuracy_IncreasesOverEpochs()
    {
        var (trainX, trainY) = GenerateSyntheticData(50, 3);
        var (evalX, evalY) = GenerateSyntheticData(20, 3);

        var model = new TestLinearModel(3, 3);
        var optimizer = new Adam(learningRate: 0.02f);
        var trainer = new DistributedTrainer(model, optimizer, numWorkers: 2);

        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 8, batchSize: 5);

        var midAcc = history.EvalAccuracies[history.EvalAccuracies.Count / 2];
        var finalAcc = history.EvalAccuracies[^1];
        Assert.True(finalAcc >= midAcc * 0.5f,
            $"准确率应在训练中保持或提升：中期={midAcc:F3}，最终={finalAcc:F3}");
    }

    [Fact]
    public void DistributedTrainer_ComparableToSingleWorker()
    {
        var (trainX, trainY) = GenerateSyntheticData(40, 5);
        var (evalX, evalY) = GenerateSyntheticData(20, 5);

        var modelSingle = new TestLinearModel(5, 5);
        var optSingle = new Adam(learningRate: 0.01f);
        var singleTrainer = new Training.Trainer(modelSingle, optSingle);
        _ = singleTrainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 4);

        var modelDist = new TestLinearModel(5, 5);
        var optDist = new Adam(learningRate: 0.01f);
        var distTrainer = new DistributedTrainer(modelDist, optDist, numWorkers: 2);
        var distHistory = distTrainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 4);

        Assert.True(distHistory.EvalLosses[^1] > 0.0f,
            $"分布式训练应产生有效损失：最终={distHistory.EvalLosses[^1]:F4}");
    }

    #endregion

    #region InProcessTransport

    [Fact]
    public async Task InProcessTransport_SendAndReceive_RoundTripCorrect()
    {
        var transport = new InProcessTransport();
        var tensor = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 2, 2);

        await transport.SendAsync("node0", "node1", tensor);

        Assert.Equal(1, transport.PendingCount);

        var received = await transport.ReceiveAsync("node1");
        Assert.Equal(2, received.Shape[0]);
        Assert.Equal(2, received.Shape[1]);

        var error = MaxAbsoluteError(tensor, received);
        Assert.True(error < 1e-6f, $"张量传输应保持数值精度，误差={error}");
    }

    [Fact]
    public async Task InProcessTransport_ReceiveEmpty_Throws()
    {
        var transport = new InProcessTransport();
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.ReceiveAsync("unknown"));
    }

    [Fact]
    public async Task InProcessTransport_MultipleNodes_IndependentChannels()
    {
        var transport = new InProcessTransport();
        var t1 = ArrayND.Zeros(2, 2);
        var t2 = ArrayND.Zeros(3, 4);

        await transport.SendAsync("a", "worker1", t1);
        await transport.SendAsync("b", "worker2", t2);

        Assert.Equal(2, transport.PendingCount);

        var r1 = await transport.ReceiveAsync("worker1");
        var r2 = await transport.ReceiveAsync("worker2");

        Assert.Equal(t1.Shape[0], r1.Shape[0]);
        Assert.Equal(t2.Shape[0], r2.Shape[0]);
    }

    [Fact]
    public async Task InProcessTransport_Reset_ClearsAllBuffers()
    {
        var transport = new InProcessTransport();
        await transport.SendAsync("n0", "n1", ArrayND.Zeros(1, 1));
        await transport.SendAsync("n0", "n2", ArrayND.Zeros(1, 1));

        transport.Reset();

        Assert.Equal(0, transport.PendingCount);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     创建索引编码的标签（[batch, 1]）
    /// </summary>
    private static ArrayND CreateIndexLabels(int batch, int numClasses)
    {
        var data = new float[batch];
        var rng = new Random(123);
        for (var i = 0; i < batch; i++) data[i] = rng.Next(numClasses);
        return ArrayND.FromArray(data, batch, 1);
    }

    /// <summary>
    ///     克隆参数梯度数组
    /// </summary>
    private static ArrayND[] CloneGrads(IReadOnlyList<IParameter> parameters)
    {
        return parameters.Select(p => p.Grad?.Clone() ?? ArrayND.Zeros(p.Value.Shape)).ToArray();
    }

    /// <summary>
    ///     计算单 worker 的梯度（返回参数列表）
    /// </summary>
    private static IReadOnlyList<IParameter> ComputeWorkerGradients(
        TestLinearModel model, int dataOffset, int count)
    {
        var inputs = RandomInput(16, model.InputSize);
        var labels = CreateIndexLabels(16, model.OutputSize);

        var workerInputs = inputs.SliceRows(dataOffset, count);
        var workerLabels = labels.SliceRows(dataOffset, count);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var logits = model.Forward(workerInputs, ctx);
        var (loss, _) = Losses.SoftmaxCrossEntropy(logits, workerLabels, ctx);
        ctx.Backward(loss);

        return model.Parameters().ToList();
    }

    /// <summary>
    ///     生成合成训练数据
    /// </summary>
    private static (ArrayND Inputs, ArrayND Labels) GenerateSyntheticData(
        int numSamples, int numFeatures)
    {
        var rng = new Random(777);
        var inputsData = new float[numSamples * numFeatures];
        var labelsData = new float[numSamples];

        for (var i = 0; i < numSamples; i++)
        {
            labelsData[i] = i % numFeatures;
            var targetClass = (int)labelsData[i];
            for (var j = 0; j < numFeatures; j++)
                inputsData[i * numFeatures + j] = (float)(rng.NextDouble() - 0.5) * 0.2f
                                                  + (j == targetClass ? 1.0f : -0.5f);
        }

        return (ArrayND.FromArray(inputsData, numSamples, numFeatures),
            ArrayND.FromArray(labelsData, numSamples, 1));
    }

    #endregion
}

/// <summary>
///     用于测试的简单线性模型（Dense 单个输出）
/// </summary>
public sealed class TestLinearModel : Training.ITrainableModel
{
    private readonly Dense _dense;

    /// <summary>
    ///     创建测试线性模型
    /// </summary>
    public TestLinearModel(int inputSize, int outputSize)
    {
        InputSize = inputSize;
        OutputSize = outputSize;
        _dense = new Dense(inputSize, outputSize);
    }

    /// <summary>输入特征数</summary>
    public int InputSize { get; }

    /// <summary>输出类别数</summary>
    public int OutputSize { get; }

    /// <summary>
    ///     前向传播
    /// </summary>
    public ArrayND Forward(ArrayND input)
    {
        return _dense.Forward(input);
    }

    /// <summary>
    ///     前向传播（带自动微分）
    /// </summary>
    public ArrayND Forward(ArrayND input, AutogradContext ctx)
    {
        return _dense.Forward(input, ctx);
    }

    /// <summary>
    ///     获取参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _dense.Parameters();
    }
}