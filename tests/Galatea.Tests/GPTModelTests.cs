namespace Galatea.Tests;

/// <summary>
///     SwiGLU / Samplers / TransformerDecoderLayer / GPT 风格模型集成测试
/// </summary>
public class GPTModelTests : GradientTestBase
{
    #region Helpers

    private static float EvalSwiGLU(SwiGLUFFN ffn, Dense head, ArrayND inputs, ArrayND labels)
    {
        var features = ffn.Forward(inputs);
        var logits = head.Forward(features);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, labels);
        return lossVal;
    }

    #endregion

    #region SwiGLU

    [Fact]
    public void SwiGLUFFN_Forward_OutputShapeCorrect()
    {
        var ffn = new SwiGLUFFN(dModel: 16, dFF: 64);

        var x = ArrayND.RandomNormal(2, 4, 16);
        var output = ffn.Forward(x);

        Assert.Equal(new[] { 2, 4, 16 }, output.Shape);
    }

    [Fact]
    public void SwiGLUFFN_Forward_DifferentFromStandardFFN()
    {
        var ffn = new SwiGLUFFN(dModel: 8, dFF: 32);

        var x = ArrayND.Ones(1, 4, 8);
        var output = ffn.Forward(x);

        var spanOut = output.AsSpan();
        var allSame = true;
        for (var i = 1; i < spanOut.Length; i++)
            if (MathF.Abs(spanOut[i] - spanOut[0]) > 1e-5f)
            {
                allSame = false;
                break;
            }

        Assert.False(allSame, "SwiGLU 输出应有变化（门控机制引入非线性）");
    }

    [Fact]
    public void SwiGLUFFN_Autograd_GradientsValid()
    {
        var ffn = new SwiGLUFFN(dModel: 8, dFF: 32);

        var x = ArrayND.RandomNormal(2, 4, 8);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = ffn.Forward(x, ctx);
        ctx.Backward(output);

        foreach (var p in ffn.Parameters())
        {
            if (p.Grad == null) continue;

            var span = p.Grad.AsSpan();
            for (var i = 0; i < span.Length; i++) Assert.False(float.IsNaN(span[i]), "SwiGLU 参数梯度包含 NaN");
        }
    }

    [Fact]
    public void SwiGLUFFN_TrainsCorrectly()
    {
        var ffn = new SwiGLUFFN(dModel: 8, dFF: 32);
        var head = new Dense(8, 3);

        var inputs = ArrayND.RandomNormal(50, 8);
        var labels = ArrayND.Zeros(50, 1);
        var spanL = labels.AsWriteSpan();
        for (var i = 0; i < 50; i++) spanL[i] = i % 3;

        var initialLoss = EvalSwiGLU(ffn, head, inputs, labels);

        var optimizer = new Adam(learningRate: 0.01f);
        var allParams = ffn.Parameters().Concat(head.Parameters());

        for (var epoch = 0; epoch < 20; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();
            var features = ffn.Forward(inputs, ctx);
            var logits = head.Forward(features, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(allParams, maxNorm: 1.0f);
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        var finalLoss = EvalSwiGLU(ffn, head, inputs, labels);
        Assert.True(finalLoss < initialLoss,
            $"SwiGLU 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4}");
    }

    #endregion

    #region Samplers

    [Fact]
    public void Samplers_ApplyTemperature_HighTempSmooths()
    {
        var logits = ArrayND.FromArray(new[] { 1.0f, 2.0f, 3.0f, 4.0f }, 1, 4);

        var lowTemp = Samplers.ApplyTemperature(logits, 0.5f);
        var highTemp = Samplers.ApplyTemperature(logits, 2.0f);

        var spanLow = lowTemp.AsSpan();
        var spanHigh = highTemp.AsSpan();

        var lowRange = spanLow[3] - spanLow[0];
        var highRange = spanHigh[3] - spanHigh[0];

        Assert.True(lowRange > highRange,
            $"低温应放大差异（{lowRange:F2}），高温应平滑差异（{highRange:F2}）");
    }

    [Fact]
    public void Samplers_TopK_OnlyKeepsTopK()
    {
        var logits = ArrayND.FromArray(new[] { 1.0f, 5.0f, 3.0f, 2.0f, 4.0f }, 1, 5);
        var filtered = Samplers.TopK(logits, k: 2);

        var span = filtered.AsSpan();
        Assert.Equal(5.0f, span[1], 1e-5f);
        Assert.Equal(4.0f, span[4], 1e-5f);
        Assert.True(float.IsNegativeInfinity(span[0]));
        Assert.True(float.IsNegativeInfinity(span[2]));
        Assert.True(float.IsNegativeInfinity(span[3]));
    }

    [Fact]
    public void Samplers_TopP_KeepsNucleus()
    {
        var logits = ArrayND.FromArray(new[] { 10.0f, 1.0f, 0.0f, -1.0f }, 1, 4);
        var filtered = Samplers.TopP(logits, p: 0.9f);

        var span = filtered.AsSpan();
        Assert.Equal(10.0f, span[0], 1e-5f);
        Assert.True(float.IsNegativeInfinity(span[3]));
    }

    [Fact]
    public void Samplers_GreedyDecode_PicksMax()
    {
        var logits = ArrayND.FromArray(new[] { 1.0f, 5.0f, 3.0f }, 1, 3);
        var token = Samplers.GreedyDecode(logits);

        Assert.Equal(1, (int)token.AsSpan()[0]);
    }

    [Fact]
    public void Samplers_Sample_ReturnsValidIndex()
    {
        var logits = ArrayND.FromArray(new[] { 1.0f, 5.0f, 3.0f }, 1, 3);
        var token = Samplers.Sample(logits, seed: 42);

        var idx = (int)token.AsSpan()[0];
        Assert.True(idx >= 0 && idx < 3, $"采样索引 {idx} 超出范围 [0, 3)");
    }

    [Fact]
    public void Samplers_GenerateToken_FullPipeline()
    {
        var logits = ArrayND.RandomNormal(2, 10);
        var token = Samplers.GenerateToken(logits, temperature: 0.8f, topK: 5, topP: 0.9f, seed: 42);

        Assert.Equal(new[] { 2, 1 }, token.Shape);
        for (var n = 0; n < 2; n++)
        {
            var idx = (int)token.AsSpan()[n];
            Assert.True(idx >= 0 && idx < 10, $"采样索引 {idx} 超出范围");
        }
    }

    #endregion

    #region TransformerDecoderLayer

    [Fact]
    public void TransformerDecoderLayer_Forward_PreservesShape()
    {
        var dModel = 16;
        var numHeads = 4;
        var dFF = 64;

        var layer = new TransformerDecoderLayer(
            attention: new MultiHeadAttention(dModel, numHeads),
            ffn: new SwiGLUFFN(dModel, dFF),
            norm1: new RMSNorm(dModel),
            norm2: new RMSNorm(dModel),
            causalMaskFn: seqLen => TransformerDecoderLayer.CreateCausalMask(seqLen)
        );

        var x = ArrayND.RandomNormal(2, 4, dModel);
        var output = layer.Forward(x);

        Assert.Equal(new[] { 2, 4, dModel }, output.Shape);
    }

    [Fact]
    public void TransformerDecoderLayer_CausalMask_IsLowerTriangular()
    {
        var mask = TransformerDecoderLayer.CreateCausalMask(4);
        var span = mask.AsSpan();

        for (var i = 0; i < 4; i++)
        for (var j = 0; j < 4; j++)
            if (j > i)
                Assert.True(float.IsNegativeInfinity(span[i * 4 + j]),
                    $"因果掩码 [{i},{j}] 应为 -inf");
            else
                Assert.Equal(0.0f, span[i * 4 + j], 1e-5f);
    }

    [Fact]
    public void TransformerDecoderLayer_Autograd_GradientsValid()
    {
        var dModel = 8;
        var numHeads = 2;
        var dFF = 32;

        var layer = new TransformerDecoderLayer(
            attention: new MultiHeadAttention(dModel, numHeads),
            ffn: new SwiGLUFFN(dModel, dFF),
            norm1: new RMSNorm(dModel),
            norm2: new RMSNorm(dModel)
        );

        var x = ArrayND.RandomNormal(2, 4, dModel);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = layer.Forward(x, ctx);
        ctx.Backward(output);

        foreach (var p in layer.Parameters())
        {
            if (p.Grad == null) continue;

            var span = p.Grad.AsSpan();
            for (var i = 0; i < span.Length; i++) Assert.False(float.IsNaN(span[i]), "TransformerDecoder 参数梯度包含 NaN");
        }
    }

    #endregion

    #region Mini GPT Training

    [Fact]
    public void MiniGPT_TrainingLossDecreases()
    {
        var dModel = 16;
        var numHeads = 2;
        var dFF = 64;
        var vocabSize = 8;
        var seqLen = 4;
        var batch = 4;

        var embedding = new Embedding(vocabSize, dModel);
        var layer = new TransformerDecoderLayer(
            attention: new MultiHeadAttention(dModel, numHeads),
            ffn: new SwiGLUFFN(dModel, dFF),
            norm1: new RMSNorm(dModel),
            norm2: new RMSNorm(dModel),
            causalMaskFn: seqLen => TransformerDecoderLayer.CreateCausalMask(seqLen)
        );
        var lmHead = new Dense(dModel, vocabSize);

        var rng = new Random(42);
        var inputIds = ArrayND.Zeros(batch, seqLen);
        var spanIds = inputIds.AsWriteSpan();
        for (var i = 0; i < spanIds.Length; i++) spanIds[i] = rng.Next(0, vocabSize);

        var targetIds = ArrayND.Zeros(batch, 1);
        var spanTarget = targetIds.AsWriteSpan();
        for (var i = 0; i < batch; i++) spanTarget[i] = rng.Next(0, vocabSize);

        var allParams = embedding.Parameters()
            .Concat(layer.Parameters())
            .Concat(lmHead.Parameters());

        var optimizer = new Adam(learningRate: 0.005f);

        float initialLoss = 0;
        float finalLoss = 0;

        for (var epoch = 0; epoch < 30; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var embOut = embedding.Forward(inputIds, ctx);
            var layerOut = layer.Forward(embOut, ctx);
            var lastHidden = layerOut.Slice(1, seqLen - 1, 1);
            lastHidden = lastHidden.Reshape(batch, dModel);
            var logits = lmHead.Forward(lastHidden, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, targetIds, ctx);
            ctx.Backward(loss);

            if (epoch == 0) initialLoss = loss.AsSpan()[0];

            if (epoch == 29) finalLoss = loss.AsSpan()[0];

            GradientClipping.ClipGradNorm(allParams, maxNorm: 1.0f);
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        Assert.True(finalLoss < initialLoss,
            $"MiniGPT 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4}");
    }

    [Fact]
    public void MiniGPT_Generation_ProducesValidTokens()
    {
        var dModel = 8;
        var numHeads = 2;
        var dFF = 32;
        var vocabSize = 5;

        var embedding = new Embedding(vocabSize, dModel);
        var layer = new TransformerDecoderLayer(
            attention: new MultiHeadAttention(dModel, numHeads),
            ffn: new SwiGLUFFN(dModel, dFF),
            norm1: new RMSNorm(dModel),
            norm2: new RMSNorm(dModel),
            causalMaskFn: seqLen => TransformerDecoderLayer.CreateCausalMask(seqLen)
        );
        var lmHead = new Dense(dModel, vocabSize);

        var promptIds = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);
        var embOut = embedding.Forward(promptIds);
        var layerOut = layer.Forward(embOut);
        var lastHidden = layerOut.Slice(1, 1, 1).Reshape(1, dModel);
        var logits = lmHead.Forward(lastHidden);

        var token = Samplers.GenerateToken(logits, temperature: 1.0f, topK: 3, seed: 42);
        var tokenId = (int)token.AsSpan()[0];

        Assert.True(tokenId >= 0 && tokenId < vocabSize,
            $"生成的 token {tokenId} 应在 [0, {vocabSize}) 范围内");
    }

    #endregion
}