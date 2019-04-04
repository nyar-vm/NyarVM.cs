namespace Galatea.Tests;

/// <summary>
///     LR Scheduler / ModelSerializer / 端到端训练集成测试
/// </summary>
public class TrainingPipelineTests : GradientTestBase
{
    #region ModelSerializer

    [Fact]
    public void ModelSerializer_SaveLoad_RoundTrips()
    {
        var dense = new Dense(4, 3);
        var path = Path.GetTempFileName();

        try
        {
            var serialized = ModelSerializer.Serialize(dense.Parameters());
            File.WriteAllBytes(path, serialized);

            var loaded = File.ReadAllBytes(path);
            var tensors = ModelSerializer.Deserialize(loaded);
            var dense2 = new Dense(4, 3);
            ModelSerializer.LoadInto(dense2.Parameters().ToList(), tensors);

            var span1 = dense.Weight.AsSpan();
            var span2 = dense2.Weight.AsSpan();
            for (var i = 0; i < span1.Length; i++) Assert.Equal(span1[i], span2[i], 1e-6f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region LRScheduler

    [Fact]
    public void CosineAnnealingLR_DecaysCorrectly()
    {
        var optim = new SGD(0.01f);
        var lr = new CosineAnnealingLR(optim, totalSteps: 10, etaMin: 0.001f);

        Assert.True(lr.LearningRate > 0.009f, $"初始 LR {lr.LearningRate} 应接近 0.01");

        for (var i = 0; i < 5; i++) lr.Step();

        Assert.True(lr.LearningRate < 0.008f, $"中程 LR {lr.LearningRate} 应 < 0.008");

        for (var i = 0; i < 5; i++) lr.Step();

        Assert.True(Math.Abs(lr.LearningRate - 0.001f) < 0.001f,
            $"末尾 LR {lr.LearningRate} 应接近 0.001");
    }

    [Fact]
    public void StepLR_DecaysEveryNSteps()
    {
        var lr = new StepLR(initialLR: 1.0f, stepSize: 3, gamma: 0.5f);

        Assert.Equal(1.0f, lr.LearningRate);

        for (var i = 0; i < 3; i++) lr.Step();

        Assert.Equal(0.5f, lr.LearningRate, 1e-6f);

        for (var i = 0; i < 3; i++) lr.Step();

        Assert.Equal(0.25f, lr.LearningRate, 1e-6f);
    }

    [Fact]
    public void ReduceLROnPlateau_ReducesWhenStagnant()
    {
        var lr = new ReduceLROnPlateau(initialLR: 0.1f, factor: 0.5f, patience: 3);

        for (var i = 0; i < 4; i++) lr.UpdateMetric(0.5f);

        Assert.True(lr.LearningRate < 0.1f, $"耐心耗尽后 LR {lr.LearningRate} 应 < 0.1");
    }

    [Fact]
    public void ReduceLROnPlateau_ResetsOnImprovement()
    {
        var lr = new ReduceLROnPlateau(initialLR: 0.1f, factor: 0.1f, patience: 4);

        lr.UpdateMetric(0.5f);
        lr.UpdateMetric(0.4f);

        for (var i = 0; i < 3; i++) lr.UpdateMetric(0.5f);

        Assert.Equal(0.1f, lr.LearningRate, 1e-6f);
    }

    #endregion

    #region End-to-End Training

    [Fact]
    public void EndToEnd_BinaryClassification_Converges()
    {
        var numSamples = 200;
        var inputDim = 8;
        var numClasses = 2;

        var (inputs, labels) = GenerateClassificationData(numSamples, inputDim, numClasses);
        var dataset = new InMemoryDataset(inputs, labels, numClasses);

        var model = new Dense(inputDim, numClasses);
        var optimizer = new SGD(learningRate: 0.05f);

        var initialLoss = EvaluateLoss(model, dataset);

        for (var epoch = 0; epoch < 30; epoch++)
        {
            var loader = new Galatea.Data.DataLoader(dataset,
                new DataLoaderOptions { BatchSize = 32, Shuffle = true });

            if (epoch == 0)
            {
                TrainEpoch(model, optimizer, loader);
            }
            else
            {
                var batches = loader.NextEpoch();
                TrainEpoch(model, optimizer, batches);
            }
        }

        var finalLoss = EvaluateLoss(model, dataset);

        Assert.True(finalLoss < initialLoss * 0.8f,
            $"训练 30 epoch 后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 80%");
    }

    [Fact]
    public void EndToEnd_AdamW_WithCosineAnnealing_Converges()
    {
        var numSamples = 200;
        var inputDim = 8;
        var numClasses = 2;

        var (inputs, labels) = GenerateClassificationData(numSamples, inputDim, numClasses);
        var dataset = new InMemoryDataset(inputs, labels, numClasses);

        var model = new Dense(inputDim, numClasses);
        var optimizer = new AdamW(learningRate: 0.01f, weightDecay: 0.001f);
        var scheduler = new CosineAnnealingLR(optimizer, totalSteps: 30, etaMin: 0.0001f);

        var initialLoss = EvaluateLoss(model, dataset);

        var loader = new Galatea.Data.DataLoader(dataset,
            new DataLoaderOptions { BatchSize = 32, Shuffle = true });

        TrainEpoch(model, optimizer, loader);
        scheduler.Step();

        for (var epoch = 1; epoch < 30; epoch++)
        {
            var batches = loader.NextEpoch();
            TrainEpoch(model, optimizer, batches);
            scheduler.Step();
        }

        var finalLoss = EvaluateLoss(model, dataset);

        Assert.True(finalLoss < initialLoss * 0.5f,
            $"AdamW + Cosine Annealing 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 50%");
    }

    [Fact]
    public void EndToEnd_MultiClassMLP_Converges()
    {
        var numSamples = 300;
        var inputDim = 12;
        var hiddenDim = 16;
        var numClasses = 3;

        var (inputs, labels) = GenerateClassificationData(numSamples, inputDim, numClasses, 0.4f);
        var dataset = new InMemoryDataset(inputs, labels, numClasses);

        var dense1 = new Dense(inputDim, hiddenDim);
        var dense2 = new Dense(hiddenDim, numClasses);
        var optimizer = new Adam(learningRate: 0.02f);

        var initialLoss = EvaluateLossMLP(dense1, dense2, dataset);

        var loader = new Galatea.Data.DataLoader(dataset,
            new DataLoaderOptions { BatchSize = 32, Shuffle = true });

        TrainEpochMLP(dense1, dense2, optimizer, loader);

        for (var epoch = 1; epoch < 40; epoch++)
        {
            var batches = loader.NextEpoch();
            TrainEpochMLP(dense1, dense2, optimizer, batches);
        }

        var finalLoss = EvaluateLossMLP(dense1, dense2, dataset);

        Assert.True(finalLoss < initialLoss * 0.5f,
            $"多分类 MLP 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 50%");
    }

    #endregion

    #region Helpers

    private static (ArrayND Inputs, ArrayND Labels) GenerateClassificationData(
        int numSamples, int inputDim, int numClasses, float clusterSpread = 0.3f)
    {
        var rng = Random.Shared;
        var inputs = ArrayND.Zeros(numSamples, inputDim);
        var labels = ArrayND.Zeros(numSamples, 1);
        var spanIn = inputs.AsWriteSpan();
        var spanLb = labels.AsWriteSpan();

        for (var i = 0; i < numSamples; i++)
        {
            var label = i % numClasses;
            spanLb[i] = label;

            for (var d = 0; d < inputDim; d++)
            {
                var baseVal = label switch
                {
                    0 => 2.0f + d * 0.1f,
                    1 => -2.0f - d * 0.1f,
                    _ => (label - 1.0f) * 2.0f
                };
                spanIn[i * inputDim + d] = baseVal + clusterSpread * BoxMullerSimple(rng);
            }
        }

        return (inputs, labels);
    }

    private static float EvaluateLoss(Dense model, InMemoryDataset dataset)
    {
        var totalLoss = 0.0f;
        var opt = new DataLoaderOptions { BatchSize = 64, Shuffle = false };
        var loader = new Galatea.Data.DataLoader(dataset, opt);

        foreach (var batch in loader.Batches)
        {
            var logits = model.Forward(batch.Inputs);
            var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, batch.Labels);
            totalLoss += lossVal * batch.Inputs.Shape[0];
        }

        return totalLoss / dataset.Count;
    }

    private static float EvaluateLossMLP(Dense d1, Dense d2, InMemoryDataset dataset)
    {
        var totalLoss = 0.0f;
        var opt = new DataLoaderOptions { BatchSize = 64, Shuffle = false };
        var loader = new Galatea.Data.DataLoader(dataset, opt);

        foreach (var batch in loader.Batches)
        {
            var h = d1.Forward(batch.Inputs);
            h = Activations.ReLUForward(h);
            var logits = d2.Forward(h);
            var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, batch.Labels);
            totalLoss += lossVal * batch.Inputs.Shape[0];
        }

        return totalLoss / dataset.Count;
    }

    private static void TrainEpoch(Dense model, SGD optimizer, Galatea.Data.DataLoader loader)
    {
        TrainEpochInner(model, optimizer, loader.Batches);
    }

    private static void TrainEpoch(Dense model, SGD optimizer, IReadOnlyList<DataBatch> batches)
    {
        TrainEpochInner(model, optimizer, batches);
    }

    private static void TrainEpoch(Dense model, AdamW optimizer, Galatea.Data.DataLoader loader)
    {
        TrainEpochInner(model, optimizer, loader.Batches);
    }

    private static void TrainEpoch(Dense model, AdamW optimizer, IReadOnlyList<DataBatch> batches)
    {
        TrainEpochInner(model, optimizer, batches);
    }

    private static void TrainEpochInner(Dense model, IOptimizer optimizer, IReadOnlyList<DataBatch> batches)
    {
        foreach (var batch in batches)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = model.Forward(batch.Inputs, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, batch.Labels, ctx);
            ctx.Backward(loss);

            optimizer.Step(model.Parameters());

            optimizer.ZeroGrad(model.Parameters());
        }
    }

    private static void TrainEpochMLP(Dense d1, Dense d2, Adam optimizer, Galatea.Data.DataLoader loader)
    {
        TrainEpochMLPInner(d1, d2, optimizer, loader.Batches);
    }

    private static void TrainEpochMLP(Dense d1, Dense d2, Adam optimizer, IReadOnlyList<DataBatch> batches)
    {
        TrainEpochMLPInner(d1, d2, optimizer, batches);
    }

    private static void TrainEpochMLPInner(Dense d1, Dense d2, IOptimizer optimizer, IReadOnlyList<DataBatch> batches)
    {
        foreach (var batch in batches)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var h = d1.Forward(batch.Inputs, ctx);
            h = Activations.ReLUForward(h);
            var logits = d2.Forward(h, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, batch.Labels, ctx);
            ctx.Backward(loss);

            var allParams = d1.Parameters().Concat(d2.Parameters());
            optimizer.Step(allParams);

            optimizer.ZeroGrad(allParams);
        }
    }

    private static float BoxMullerSimple(Random rng)
    {
        var u1 = 1.0f - rng.NextSingle();
        var u2 = 1.0f - rng.NextSingle();
        return MathF.Sqrt(-2.0f * MathF.Log(MathF.Max(u1, 1e-10f))) * MathF.Cos(2.0f * MathF.PI * u2);
    }

    #endregion
}