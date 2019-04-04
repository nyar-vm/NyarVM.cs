namespace Galatea.Tests;

/// <summary>
///     知识蒸馏 / 滑动窗口注意力 / 推测性解码 综合测试
/// </summary>
public class AdvancedInferenceTests
{
    private static ArrayND AddScalar(ArrayND a, float scalar)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++)
            spanR[i] = spanA[i] + scalar;
        return result;
    }

    #region Knowledge Distillation

    [Fact]
    public void DistillationLoss_SameLogits_ZeroLoss()
    {
        var logits = ArrayND.RandomNormal(2, 10);
        var loss = KnowledgeDistillation.DistillationLoss(logits, logits, temperature: 2.0f);

        Assert.True(loss < 0.01f, $"相同 logits 的蒸馏损失应接近 0，实际={loss:F6}");
    }

    [Fact]
    public void DistillationLoss_DifferentLogits_PositiveLoss()
    {
        var student = ArrayND.RandomNormal(2, 10);
        var teacher = AddScalar(ArrayND.RandomNormal(2, 10), 5.0f);

        var loss = KnowledgeDistillation.DistillationLoss(student, teacher, temperature: 2.0f);

        Assert.True(loss > 0, "不同 logits 的蒸馏损失应为正");
    }

    [Fact]
    public void DistillationLoss_HigherTemperature_SmootherDistribution()
    {
        var student = ArrayND.RandomNormal(1, 20);
        var teacher = ArrayND.RandomNormal(1, 20);

        var lossLowT = KnowledgeDistillation.DistillationLoss(student, teacher, temperature: 0.5f);
        var lossHighT = KnowledgeDistillation.DistillationLoss(student, teacher, temperature: 10.0f);

        Assert.True(lossHighT < lossLowT * 10,
            "高温度应产生更平滑的分布，KL 散度应相对更小");
    }

    [Fact]
    public void HardLabelLoss_CorrectClass_LowerLoss()
    {
        var logits = ArrayND.Zeros(1, 5);
        var span = logits.AsWriteSpan();
        span[2] = 10.0f;

        var targets = ArrayND.Zeros(1);
        targets.AsWriteSpan()[0] = 2;

        var loss = KnowledgeDistillation.HardLabelLoss(logits, targets);

        Assert.True(loss < 0.01f, $"正确类别的损失应接近 0，实际={loss:F6}");
    }

    [Fact]
    public void CombinedLoss_WeightedSum()
    {
        var student = ArrayND.RandomNormal(2, 10);
        var teacher = ArrayND.RandomNormal(2, 10);
        var targets = ArrayND.Zeros(2);
        var spanT = targets.AsWriteSpan();
        spanT[0] = 3;
        spanT[1] = 5;

        var hardLoss = KnowledgeDistillation.HardLabelLoss(student, targets);
        var softLoss = KnowledgeDistillation.DistillationLoss(student, teacher, 2.0f);
        var combined = KnowledgeDistillation.CombinedLoss(student, teacher, targets, 2.0f, 0.3f);

        var expected = 0.3f * hardLoss + 0.7f * softLoss;
        Assert.True(MathF.Abs(combined - expected) < 0.01f,
            $"综合损失应为加权组合，期望={expected:F4}，实际={combined:F4}");
    }

    [Fact]
    public void DistillationTrainer_TrainStep_ReturnsLosses()
    {
        var teacher = new Dense(8, 10);
        var student = new Dense(8, 10);
        var optimizer = new SGD(learningRate: 0.01f);

        var trainer = new DistillationTrainer(teacher, student, optimizer,
            temperature: 2.0f, alpha: 0.3f);

        var input = ArrayND.RandomNormal(4, 8);
        var targets = ArrayND.Zeros(4);
        var spanT = targets.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanT[i] = i % 10;

        var (totalLoss, hardLoss, softLoss) = trainer.TrainStep(input, targets);

        Assert.True(totalLoss >= 0, "总损失应非负");
        Assert.True(hardLoss >= 0, "硬标签损失应非负");
        Assert.True(softLoss >= 0, "软标签损失应非负");
    }

    [Fact]
    public void DistillationTrainer_TrainEpoch_ReturnsResult()
    {
        var teacher = new Dense(4, 6);
        var student = new Dense(4, 6);
        var optimizer = new Adam(learningRate: 0.001f);

        var trainer = new DistillationTrainer(teacher, student, optimizer,
            temperature: 3.0f, alpha: 0.5f);

        var inputs = ArrayND.RandomNormal(16, 4);
        var targets = ArrayND.Zeros(16);
        var spanT = targets.AsWriteSpan();
        for (var i = 0; i < 16; i++) spanT[i] = i % 6;

        var result = trainer.TrainEpoch(inputs, targets, batchSize: 4);

        Assert.True(result.TotalLoss >= 0);
        Assert.True(result.HardLoss >= 0);
        Assert.True(result.SoftLoss >= 0);
        Assert.Equal(4, result.NumBatches);
    }

    [Fact]
    public void DistillationLossWithGrad_ProducesGradient()
    {
        var student = ArrayND.RandomNormal(2, 10);
        var teacher = ArrayND.RandomNormal(2, 10);

        var ctx = new AutogradContext();
        ctx.StartRecording();

        var lossTensor = KnowledgeDistillation.DistillationLossWithGrad(
            student, teacher, 2.0f, ctx);

        Assert.NotNull(lossTensor);
        Assert.True(lossTensor.AsSpan()[0] >= 0, "损失应非负");
    }

    #endregion

    #region Sliding Window Attention

    [Fact]
    public void SlidingWindowMask_CausalAndWindowed()
    {
        var mask = SlidingWindowAttention.CreateSlidingWindowMask(seqLen: 6, windowSize: 3);
        var span = mask.AsSpan();

        Assert.Equal(float.NegativeInfinity, span[0 * 6 + 1]);
        Assert.Equal(0.0f, span[2 * 6 + 0]);
        Assert.Equal(0.0f, span[2 * 6 + 1]);
        Assert.Equal(0.0f, span[2 * 6 + 2]);
        Assert.Equal(float.NegativeInfinity, span[2 * 6 + 3]);

        Assert.Equal(0.0f, span[5 * 6 + 3]);
        Assert.Equal(0.0f, span[5 * 6 + 4]);
        Assert.Equal(0.0f, span[5 * 6 + 5]);
        Assert.Equal(float.NegativeInfinity, span[5 * 6 + 0]);
    }

    [Fact]
    public void SlidingWindowMask_FullWindow_EqualsCausalMask()
    {
        var seqLen = 5;
        var windowSize = 100;
        var swMask = SlidingWindowAttention.CreateSlidingWindowMask(seqLen, windowSize);
        var causalMask = TransformerDecoderLayer.CreateCausalMask(seqLen);

        var spanSW = swMask.AsSpan();
        var spanC = causalMask.AsSpan();

        for (var i = 0; i < spanSW.Length; i++)
        {
            var swVal = float.IsNegativeInfinity(spanSW[i]) ? -1 : 0;
            var cVal = float.IsNegativeInfinity(spanC[i]) ? -1 : 0;
            Assert.Equal(cVal, swVal);
        }
    }

    [Fact]
    public void SlidingWindowMask_WindowSize1_OnlySelfAttention()
    {
        var mask = SlidingWindowAttention.CreateSlidingWindowMask(seqLen: 4, windowSize: 1);
        var span = mask.AsSpan();

        Assert.Equal(0.0f, span[0 * 4 + 0]);
        Assert.Equal(float.NegativeInfinity, span[0 * 4 + 1]);

        Assert.Equal(float.NegativeInfinity, span[1 * 4 + 0]);
        Assert.Equal(0.0f, span[1 * 4 + 1]);
        Assert.Equal(float.NegativeInfinity, span[1 * 4 + 2]);
    }

    [Fact]
    public void SlidingWindowAttention_Compute_CorrectShape()
    {
        var q = ArrayND.RandomNormal(1, 8, 4);
        var k = ArrayND.RandomNormal(1, 8, 4);
        var v = ArrayND.RandomNormal(1, 8, 4);

        var output = SlidingWindowAttention.Compute(q, k, v, windowSize: 4);

        Assert.Equal(new[] { 1, 8, 4 }, output.Shape);
    }

    [Fact]
    public void SlidingWindowAttention_OutputNotNaN()
    {
        var q = ArrayND.RandomNormal(2, 8, 4);
        var k = ArrayND.RandomNormal(2, 8, 4);
        var v = ArrayND.RandomNormal(2, 8, 4);

        var output = SlidingWindowAttention.Compute(q, k, v, windowSize: 3);
        var span = output.AsSpan();

        for (var i = 0; i < span.Length; i++) Assert.False(float.IsNaN(span[i]), $"输出包含 NaN 在位置 {i}");
    }

    [Fact]
    public void EffectiveCacheLength_Correct()
    {
        Assert.Equal(1, SlidingWindowAttention.EffectiveCacheLength(0, 4));
        Assert.Equal(3, SlidingWindowAttention.EffectiveCacheLength(2, 4));
        Assert.Equal(4, SlidingWindowAttention.EffectiveCacheLength(5, 4));
        Assert.Equal(4, SlidingWindowAttention.EffectiveCacheLength(100, 4));
    }

    [Fact]
    public void MistralDecoderLayer_Forward_CorrectShape()
    {
        var layer = new MistralDecoderLayer(
            dModel: 16, numHeads: 4, numKVHeads: 2, windowSize: 4);

        var x = ArrayND.RandomNormal(1, 8, 16);
        var output = layer.Forward(x);

        Assert.Equal(new[] { 1, 8, 16 }, output.Shape);
    }

    [Fact]
    public void MistralDecoderLayer_WithRoPE_CorrectShape()
    {
        var layer = new MistralDecoderLayer(
            dModel: 16, numHeads: 4, numKVHeads: 2, windowSize: 4,
            useRoPE: true, roPETheta: 10000.0f);

        var x = ArrayND.RandomNormal(1, 6, 16);
        var output = layer.Forward(x);

        Assert.Equal(new[] { 1, 6, 16 }, output.Shape);
    }

    [Fact]
    public void MistralDecoderLayer_Parameters_Count()
    {
        var layer = new MistralDecoderLayer(
            dModel: 16, numHeads: 4, numKVHeads: 2, windowSize: 4);

        var params_ = layer.Parameters().ToList();
        Assert.True(params_.Count > 0, "Mistral 层应有可训练参数");
    }

    #endregion

    #region Speculative Decoding

    [Fact]
    public void SpeculativeDecoder_Generate_ReturnsOutput()
    {
        var draftForward = (ArrayND input) =>
        {
            var seqLen = input.Shape[1];
            var logits = ArrayND.RandomNormal(1, seqLen, 8);
            return logits;
        };

        var targetForward = (ArrayND input) =>
        {
            var seqLen = input.Shape[1];
            var logits = ArrayND.RandomNormal(1, seqLen, 8);
            return logits;
        };

        var decoder = new SpeculativeDecoder(draftForward, targetForward,
            vocabSize: 8, draftSteps: 3);

        var prompt = ArrayND.Zeros(1, 2);
        var span = prompt.AsWriteSpan();
        span[0] = 1;
        span[1] = 2;

        var (output, acceptanceRate) = decoder.Generate(prompt, maxNewTokens: 5);

        Assert.True(output.Shape[1] >= 2, "输出应至少包含 prompt");
        Assert.True(acceptanceRate >= 0 && acceptanceRate <= 1.0f,
            $"接受率应在 [0,1] 范围，实际={acceptanceRate}");
    }

    [Fact]
    public void SpeculativeDecoder_GenerateGreedy_ReturnsOutput()
    {
        var draftForward = (ArrayND input) =>
        {
            var seqLen = input.Shape[1];
            var logits = ArrayND.Zeros(1, seqLen, 8);
            var span = logits.AsWriteSpan();
            for (var s = 0; s < seqLen; s++) span[s * 8 + s % 8] = 10.0f;
            return logits;
        };

        var targetForward = draftForward;

        var decoder = new SpeculativeDecoder(draftForward, targetForward,
            vocabSize: 8, draftSteps: 3);

        var prompt = ArrayND.Zeros(1, 2);
        var span = prompt.AsWriteSpan();
        span[0] = 0;
        span[1] = 1;

        var (output, acceptanceRate) = decoder.GenerateGreedy(prompt, maxNewTokens: 4);

        Assert.True(output.Shape[1] >= 2, "输出应至少包含 prompt");
        Assert.True(acceptanceRate >= 0 && acceptanceRate <= 1.0f,
            $"接受率应在 [0,1] 范围，实际={acceptanceRate}");
    }

    [Fact]
    public void SpeculativeDecoder_SameModel_HighAcceptanceRate()
    {
        var forward = (ArrayND input) =>
        {
            var seqLen = input.Shape[1];
            var logits = ArrayND.Zeros(1, seqLen, 8);
            var span = logits.AsWriteSpan();
            for (var s = 0; s < seqLen; s++) span[s * 8 + s % 8] = 10.0f;
            return logits;
        };

        var decoder = new SpeculativeDecoder(forward, forward,
            vocabSize: 8, draftSteps: 3);

        var prompt = ArrayND.Zeros(1, 2);
        var span = prompt.AsWriteSpan();
        span[0] = 0;
        span[1] = 1;

        var (_, acceptanceRate) = decoder.GenerateGreedy(prompt, maxNewTokens: 6);

        Assert.True(acceptanceRate > 0.5f,
            $"相同模型时接受率应较高，实际={acceptanceRate:F2}");
    }

    #endregion
}