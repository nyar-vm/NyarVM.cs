namespace Galatea.Tests;

/// <summary>
///     GPTModel / AutoregressiveGenerator / Warmup / LabelSmoothing / Xavier 集成测试
/// </summary>
public class ProductionGPTTests
{
    #region GPTModel

    [Fact]
    public void GPTModel_Forward_ProducesCorrectShape()
    {
        var model = new GPTModel(vocabSize: 16, dModel: 16, numHeads: 2, numLayers: 2, maxSeqLen: 32);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 1, 4, 16 }, logits.Shape);
    }

    [Fact]
    public void GPTModel_Parameters_CountReasonable()
    {
        var model = new GPTModel(vocabSize: 16, dModel: 16, numHeads: 2, numLayers: 2);

        var paramCount = ModelSummary.CountParameters(model.Parameters());

        Assert.True(paramCount > 0, "GPTModel 应有可训练参数");
        Assert.True(paramCount < 100000, $"参数量 {paramCount} 应在合理范围内");
    }

    [Fact]
    public void GPTModel_TrainingLossDecreases()
    {
        var vocabSize = 8;
        var dModel = 16;
        var model = new GPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: 2, numLayers: 1, maxSeqLen: 16);

        var rng = new Random(42);
        var inputIds = ArrayND.Zeros(4, 4);
        var spanIn = inputIds.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanIn[i] = rng.Next(0, vocabSize);

        var targetIds = ArrayND.Zeros(4, 1);
        var spanT = targetIds.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanT[i] = rng.Next(0, vocabSize);

        var optimizer = new Adam(learningRate: 0.01f);
        var allParams = model.Parameters().ToList();

        float initialLoss = 0;
        float finalLoss = 0;

        for (var epoch = 0; epoch < 40; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = model.ForwardForTraining(inputIds, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, targetIds, ctx);
            ctx.Backward(loss);

            if (epoch == 0) initialLoss = loss.AsSpan()[0];

            if (epoch == 39) finalLoss = loss.AsSpan()[0];

            GradientClipping.ClipGradNorm(allParams, maxNorm: 1.0f);
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        Assert.True(finalLoss < initialLoss,
            $"GPTModel 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4}");
    }

    #endregion

    #region AutoregressiveGenerator

    [Fact]
    public void AutoregressiveGenerator_Greedy_ProducesValidSequence()
    {
        var vocabSize = 8;
        var dModel = 16;
        var model = new GPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: 2, numLayers: 1, maxSeqLen: 16);

        var generator = model.CreateGenerator();
        var promptIds = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);

        var output = generator.GenerateGreedy(promptIds, maxNewTokens: 3);

        Assert.Equal(new[] { 1, 5 }, output.Shape);

        var spanOut = output.AsSpan();
        for (var i = 0; i < spanOut.Length; i++)
        {
            var tokenId = (int)spanOut[i];
            Assert.True(tokenId >= 0 && tokenId < vocabSize,
                $"生成的 token {tokenId} 应在 [0, {vocabSize}) 范围内");
        }
    }

    [Fact]
    public void AutoregressiveGenerator_Sampling_ProducesValidSequence()
    {
        var vocabSize = 8;
        var dModel = 16;
        var model = new GPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: 2, numLayers: 1, maxSeqLen: 16);

        var generator = model.CreateGenerator();
        var promptIds = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);

        var output = generator.Generate(promptIds, maxNewTokens: 3, temperature: 0.8f, topK: 4, seed: 42);

        Assert.Equal(new[] { 1, 5 }, output.Shape);

        var spanOut = output.AsSpan();
        for (var i = 0; i < spanOut.Length; i++)
        {
            var tokenId = (int)spanOut[i];
            Assert.True(tokenId >= 0 && tokenId < vocabSize,
                $"采样的 token {tokenId} 应在 [0, {vocabSize}) 范围内");
        }
    }

    #endregion

    #region Warmup Schedulers

    [Fact]
    public void LinearWarmup_RampsUpThenStays()
    {
        var scheduler = new LinearWarmup(peakLR: 0.01f, warmupSteps: 5);

        var lrs = new float[10];
        for (var i = 0; i < 10; i++)
        {
            lrs[i] = scheduler.LearningRate;
            scheduler.Step();
        }

        Assert.Equal(0.0f, lrs[0], 1e-6f);
        Assert.Equal(0.002f, lrs[1], 1e-5f);
        Assert.Equal(0.01f, lrs[5], 1e-5f);
        Assert.Equal(0.01f, lrs[9], 1e-5f);
    }

    [Fact]
    public void CosineDecayWithWarmup_FullSchedule()
    {
        var scheduler = new CosineDecayWithWarmup(peakLR: 0.01f, warmupSteps: 2, totalSteps: 10, etaMin: 0.0f);

        var lrs = new float[11];
        for (var i = 0; i <= 10; i++)
        {
            lrs[i] = scheduler.LearningRate;
            if (i < 10) scheduler.Step();
        }

        Assert.Equal(0.0f, lrs[0], 1e-6f);
        Assert.Equal(0.005f, lrs[1], 1e-4f);
        Assert.Equal(0.01f, lrs[2], 1e-4f);
        Assert.True(lrs[5] < lrs[2], "余弦衰减中期应低于峰值");
        Assert.Equal(0.0f, lrs[10], 1e-4f);
    }

    #endregion

    #region Label Smoothing

    [Fact]
    public void LabelSmoothing_ProducesValidLoss()
    {
        var logits = ArrayND.RandomNormal(4, 8);
        var targets = ArrayND.Zeros(4, 1);
        var spanT = targets.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanT[i] = i % 8;

        var (hardLoss, _) = Losses.SoftmaxCrossEntropyForward(logits, targets);
        var smoothLoss = Losses.LabelSmoothingCrossEntropyForward(logits, targets, smoothing: 0.1f);

        Assert.True(smoothLoss > 0, $"标签平滑损失 {smoothLoss:F4} 应为正数");
        Assert.True(MathF.Abs(smoothLoss - hardLoss) < 2.0f,
            $"标签平滑损失 {smoothLoss:F4} 与硬标签损失 {hardLoss:F4} 差异应合理");
    }

    [Fact]
    public void LabelSmoothing_Autograd_GradientsValid()
    {
        var logits = ArrayND.RandomNormal(2, 4);
        var targets = ArrayND.Zeros(2, 1);
        var spanT = targets.AsWriteSpan();
        spanT[0] = 0;
        spanT[1] = 2;

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var loss = Losses.LabelSmoothingCrossEntropy(logits, targets, ctx, smoothing: 0.1f);
        ctx.Backward(loss);

        Assert.NotNull(logits.Grad);
        var spanGrad = logits.Grad!.AsSpan();
        for (var i = 0; i < spanGrad.Length; i++) Assert.False(float.IsNaN(spanGrad[i]), "LabelSmoothing 梯度包含 NaN");
    }

    #endregion

    #region Xavier Init

    [Fact]
    public void XavierUniform_ValuesInCorrectRange()
    {
        var w = ArrayND.XavierUniform(64, 32, 64, 32);
        var limit = MathF.Sqrt(6.0f / (64 + 32));

        var span = w.AsSpan();
        for (var i = 0; i < span.Length; i++)
            Assert.True(MathF.Abs(span[i]) <= limit + 0.01f,
                $"XavierUniform 值 {span[i]} 超出范围 [-{limit:F4}, {limit:F4}]");
    }

    [Fact]
    public void XavierNormal_MeanNearZero()
    {
        var w = ArrayND.XavierNormal(64, 32, 64, 32);
        var span = w.AsSpan();

        var mean = 0.0f;
        for (var i = 0; i < span.Length; i++) mean += span[i];
        mean /= span.Length;

        Assert.True(MathF.Abs(mean) < 0.05f,
            $"XavierNormal 均值 {mean:F4} 应接近 0");
    }

    #endregion
}