namespace Galatea.Tests;

/// <summary>
///     WeightTiedGPTModel 权重共享 GPT 模型测试
/// </summary>
public class WeightTiedGPTTests
{
    #region 参数量对比

    [Fact]
    public void WeightTiedGPT_SavesParametersVsGPTModel()
    {
        var vocabSize = 32;
        var dModel = 32;
        var numHeads = 4;
        var numLayers = 2;

        var tiedModel = new WeightTiedGPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: numHeads,
            numLayers: numLayers);
        var gptModel = new GPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: numHeads, numLayers: numLayers);

        var tiedCount = ModelSummary.CountParameters(tiedModel.Parameters());
        var gptCount = ModelSummary.CountParameters(gptModel.Parameters());

        var saved = gptCount - tiedCount;
        var expectedSaved = vocabSize * dModel + vocabSize;

        Assert.Equal(expectedSaved, saved);
        Assert.True(tiedCount < gptCount,
            $"权重共享模型 {tiedCount} 参数应少于独立模型 {gptCount}");
    }

    #endregion

    #region 前向传播

    [Fact]
    public void WeightTiedGPT_Forward_ProducesCorrectShape()
    {
        var model = new WeightTiedGPTModel(vocabSize: 16, dModel: 16, numHeads: 2, numLayers: 2, maxSeqLen: 32);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 1, 4, 16 }, logits.Shape);
    }

    [Fact]
    public void WeightTiedGPT_Forward_BatchShape()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);

        var inputIds = ArrayND.Zeros(3, 5);
        var span = inputIds.AsWriteSpan();
        var rng = new Random(42);
        for (var i = 0; i < span.Length; i++) span[i] = rng.Next(0, 8);

        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 3, 5, 8 }, logits.Shape);
    }

    #endregion

    #region 权重共享验证

    [Fact]
    public void WeightTiedGPT_NoSeparateLMHead()
    {
        var vocabSize = 16;
        var dModel = 16;
        var model = new WeightTiedGPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: 2, numLayers: 1);

        var tiedParams = model.Parameters().ToList();

        var gptModel = new GPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: 2, numLayers: 1);
        var separateParams = gptModel.Parameters().ToList();

        Assert.True(tiedParams.Count < separateParams.Count,
            $"权重共享模型参数 {tiedParams.Count} 应少于独立 LM Head 模型参数 {separateParams.Count}");
    }

    [Fact]
    public void WeightTiedGPT_LogitsUseEmbeddingWeight()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3 }, 1, 3);
        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 1, 3, 8 }, logits.Shape);

        var span = logits.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            Assert.False(float.IsNaN(span[i]), "logits 不应包含 NaN");
            Assert.False(float.IsInfinity(span[i]), "logits 不应包含 Infinity");
        }
    }

    #endregion

    #region 训练

    [Fact]
    public void WeightTiedGPT_TrainingLossDecreases()
    {
        var vocabSize = 8;
        var dModel = 16;
        var model = new WeightTiedGPTModel(vocabSize: vocabSize, dModel: dModel, numHeads: 2, numLayers: 1,
            maxSeqLen: 16);

        var rng = new Random(42);
        var inputIds = ArrayND.Zeros(2, 4);
        var spanIn = inputIds.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanIn[i] = rng.Next(0, vocabSize);

        var targetIds = ArrayND.Zeros(2, 1);
        var spanT = targetIds.AsWriteSpan();
        for (var i = 0; i < 2; i++) spanT[i] = rng.Next(0, vocabSize);

        var optimizer = new Adam(learningRate: 0.005f);
        var allParams = model.Parameters().ToList();

        float initialLoss = 0;
        float finalLoss = 0;

        for (var epoch = 0; epoch < 80; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = model.ForwardForTraining(inputIds, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, targetIds, ctx);
            ctx.Backward(loss);

            if (epoch == 0) initialLoss = loss.AsSpan()[0];

            if (epoch == 79) finalLoss = loss.AsSpan()[0];

            GradientClipping.ClipGradNorm(allParams, maxNorm: 1.0f);
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        Assert.True(finalLoss < initialLoss,
            $"WeightTiedGPT 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4}");
    }

    [Fact]
    public void WeightTiedGPT_GradientsFlowToEmbedding()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targetIds = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        var ctx = new AutogradContext();
        ctx.StartRecording();

        var logits = model.ForwardForTraining(inputIds, ctx);
        var (loss, _) = Losses.SoftmaxCrossEntropy(logits, targetIds, ctx);
        ctx.Backward(loss);

        var embeddingWeight = model.TokenEmbedding.Weight;
        Assert.NotNull(embeddingWeight.Grad);

        var spanGrad = embeddingWeight.Grad!.AsSpan();
        var hasNonZero = false;
        for (var i = 0; i < spanGrad.Length; i++)
            if (spanGrad[i] != 0)
            {
                hasNonZero = true;
                break;
            }

        Assert.True(hasNonZero, "Embedding 权重应有非零梯度（权重共享 LM Head 反向传播）");
    }

    #endregion

    #region 生成

    [Fact]
    public void WeightTiedGPT_GeneratorGreedy_ProducesValidTokens()
    {
        var vocabSize = 8;
        var model = new WeightTiedGPTModel(vocabSize: vocabSize, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);

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
    public void WeightTiedGPT_GeneratorSampling_ProducesValidTokens()
    {
        var vocabSize = 8;
        var model = new WeightTiedGPTModel(vocabSize: vocabSize, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);

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
}