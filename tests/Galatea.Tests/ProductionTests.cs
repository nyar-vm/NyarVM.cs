namespace Galatea.Tests;

/// <summary>
///     Gradient Clipping / CNN 端到端训练 / 混合模型集成测试
/// </summary>
public class ProductionTests : GradientTestBase
{
    #region MobileNet-Style Production Training

    [Fact]
    public void EndToEnd_MobileNet_Converges()
    {
        var batch = 4;
        var inCh = 1;
        var expandCh = 8;
        var inH = 8;
        var inW = 8;

        var dwConv = new DepthwiseConv2D(channels: inCh, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var pwConv = new Conv2D(inChannels: inCh, outChannels: expandCh, kernelSize: 1, padding: 0, inH: inH, inW: inW);
        var pool = new MaxPool2D(poolSize: 2, stride: 2);
        var dense = new Dense(expandCh * (inH / 2) * (inW / 2), 3);

        var (inputs, labels) = GenerateSimpleImageData(batch, inCh, inH, inW);

        var optimizer = new Adam(learningRate: 0.01f);

        var initialLoss = EvalMobileNet(dwConv, pwConv, pool, dense, inputs, labels);

        for (var epoch = 0; epoch < 20; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var (dwOut, _, _) = dwConv.Forward(inputs, inH, inW, ctx);
            var (pwOut, _, _) = pwConv.Forward(dwOut, inH, inW, ctx);
            var (poolOut, ph, pw) = pool.Forward(pwOut, expandCh, inH, inW, ctx);
            var logits = dense.Forward(poolOut, ctx);
            var transformedLabels = labels.Slice(0, 0, batch);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, transformedLabels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(
                dwConv.Parameters().Concat(pwConv.Parameters()).Concat(dense.Parameters()),
                maxNorm: 1.0f);

            var allParams = dwConv.Parameters().Concat(pwConv.Parameters()).Concat(dense.Parameters());
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        var finalLoss = EvalMobileNet(dwConv, pwConv, pool, dense, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.9f,
            $"MobileNet 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    #endregion

    #region Sequential with Checkpoint

    [Fact]
    public void EndToEnd_Sequential_Checkpoint_Converges()
    {
        var batch = 4;
        var inCh = 1;
        var inH = 6;
        var inW = 6;

        var inner = new Sequential(
            new Conv2D(inCh, 3, kernelSize: 3, padding: 0, inH: inH, inW: inW),
            new Conv2D(3, 6, kernelSize: 1, padding: 0, inH: 4, inW: 4)
        );
        var cp = new Checkpoint(inner);
        var head = new Dense(6 * 4 * 4, 3);

        var (inputs, labels) = GenerateSimpleImageData(batch, inCh, inH, inW);
        var optimizer = new Adam(learningRate: 0.01f);

        var initialLoss = EvalCheckpoint(cp, head, inputs, labels);

        for (var epoch = 0; epoch < 25; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var convOut = cp.Forward(inputs, ctx);
            var logits = head.Forward(convOut, ctx);
            var transformedLabels = labels.Slice(0, 0, batch);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, transformedLabels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(head.Parameters(), maxNorm: 1.0f);
            optimizer.Step(head.Parameters());
            optimizer.ZeroGrad(head.Parameters());
        }

        var finalLoss = EvalCheckpoint(cp, head, inputs, labels);
        Assert.True(finalLoss < initialLoss * 0.9f,
            $"Checkpoint 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    #endregion

    #region Gradient Clipping

    [Fact]
    public void ClipGradNorm_ReducesLargeGradients()
    {
        var dense = new Dense(4, 3);
        var x = ArrayND.Ones(1, 4);
        var target = ArrayND.Zeros(1, 1);
        target.AsWriteSpan()[0] = 0;

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var logits = dense.Forward(x, ctx);
        var (loss, _) = Losses.SoftmaxCrossEntropy(logits, target, ctx);
        ctx.Backward(loss);

        var gradBefore = dense.Weight.Grad!.AsSpan()[0];

        var norm = GradientClipping.ClipGradNorm(dense.Parameters(), maxNorm: 0.1f);

        var gradAfter = dense.Weight.Grad!.AsSpan()[0];

        Assert.True(norm > 0.1f, $"应触发裁剪 (norm={norm:F4})");
        Assert.True(Math.Abs(gradAfter) < Math.Abs(gradBefore),
            $"裁剪后梯度 {gradAfter:F6} 应小于裁剪前 {gradBefore:F6}");
    }

    [Fact]
    public void ClipGradNorm_DoesNotClipSmallGradients()
    {
        var dense = new Dense(4, 3);
        var x = ArrayND.Ones(1, 4);
        var target = ArrayND.Zeros(1, 1);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var logits = dense.Forward(x, ctx);
        var (loss, _) = Losses.SoftmaxCrossEntropy(logits, target, ctx);
        ctx.Backward(loss);

        GradientClipping.ClipGradNorm(dense.Parameters(), maxNorm: 1000.0f, out var norm, out var wasClipped);

        Assert.False(wasClipped, $"小梯度不应触发裁剪 (norm={norm:F4})");
    }

    [Fact]
    public void ClipGradValue_ClampsExtremeValues()
    {
        var p = ArrayND.Zeros(3);
        var span = p.AsWriteSpan();
        span[0] = 5.0f;
        span[1] = -3.0f;
        span[2] = 0.5f;
        p.EnsureGrad();
        var gradSpan = p.Grad!.AsWriteSpan();
        gradSpan[0] = 5.0f;
        gradSpan[1] = -3.0f;
        gradSpan[2] = 0.5f;

        var clipped = GradientClipping.ClipGradValue(
            new[] { new Parameter(p) }, clipValue: 2.0f);

        Assert.True(clipped > 0);
        Assert.Equal(2.0f, p.Grad!.AsSpan()[0]);
        Assert.Equal(-2.0f, p.Grad!.AsSpan()[1]);
        Assert.Equal(0.5f, p.Grad!.AsSpan()[2]);
    }

    #endregion

    #region CNN End-to-End Training

    [Fact]
    public void EndToEnd_CNN_MNIST_Style_Converges()
    {
        var batch = 4;
        var inCh = 1;
        var outCh = 4;
        var inH = 8;
        var inW = 8;

        var conv1 = new Conv2D(inCh, outCh, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var pool1 = new MaxPool2D(poolSize: 2, stride: 2);
        var dense1 = new Dense(outCh * (inH / 2) * (inW / 2), 10);

        var (inputs, labels) = GenerateSimpleImageData(batch, inCh, inH, inW);

        var optimizer = new Adam(learningRate: 0.01f);

        var initialLoss = EvaluateCNN(conv1, pool1, dense1, inputs, labels);

        for (var epoch = 0; epoch < 20; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var (convOut, oh, ow) = conv1.Forward(inputs, inH: inH, inW: inW, ctx);
            var (pooledOut, ph, pw) = pool1.Forward(convOut, outCh, oh, ow, ctx);
            var logits = dense1.Forward(pooledOut, ctx);
            var transformedLabels = labels.Slice(0, 0, batch);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, transformedLabels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(
                conv1.Parameters().Concat(dense1.Parameters()), maxNorm: 1.0f);

            var allParams = conv1.Parameters().Concat(dense1.Parameters());
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        var finalLoss = EvaluateCNN(conv1, pool1, dense1, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.9f,
            $"CNN 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    [Fact]
    public void EndToEnd_CNN_WithDropout_Converges()
    {
        var batch = 4;
        var inCh = 1;
        var midCh = 4;
        var inH = 8;
        var inW = 8;

        var conv1 = new Conv2D(inCh, midCh, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var pool1 = new MaxPool2D(poolSize: 2, stride: 2);
        var dropout = new Dropout(rate: 0.3f);
        dropout.IsTraining = true;
        var dense1 = new Dense(midCh * (inH / 2) * (inW / 2), 3);

        var (inputs, labels) = GenerateSimpleImageData(batch, inCh, inH, inW);

        var optimizer = new AdamW(learningRate: 0.01f, weightDecay: 0.0001f);

        var initialLoss = EvaluateCNN(conv1, pool1, dense1, inputs, labels);

        for (var epoch = 0; epoch < 30; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var (convOut, oh2, ow2) = conv1.Forward(inputs, inH: inH, inW: inW, ctx);
            var (pooledOut, ph2, pw2) = pool1.Forward(convOut, midCh, oh2, ow2, ctx);
            var dropped = dropout.Forward(pooledOut, ctx);
            var logits = dense1.Forward(dropped, ctx);
            var transformedLabels = labels.Slice(0, 0, batch);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, transformedLabels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(
                conv1.Parameters().Concat(dense1.Parameters()), maxNorm: 1.0f);

            var allParams = conv1.Parameters().Concat(dense1.Parameters());
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        var finalLoss = EvaluateCNN(conv1, pool1, dense1, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.85f,
            $"CNN+Dropout 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 85%");
    }

    #endregion

    #region CNN + Transformer Hybrid

    [Fact]
    public void EndToEnd_CNN_MHA_Hybrid_Converges()
    {
        var batch = 2;
        var inCh = 1;
        var midCh = 4;
        var inH = 12;
        var inW = 12;
        var dModel = 16;
        var numHeads = 4;

        var conv1 = new Conv2D(inCh, midCh, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var pool1 = new MaxPool2D(poolSize: 2, stride: 2);
        var proj = new Dense(midCh * (inH / 2) * (inW / 2), dModel);
        var mha = new MultiHeadAttention(dModel, numHeads);
        var head = new Dense(dModel, 3);

        var (inputs, labels) = GenerateSimpleImageData(batch, inCh, inH, inW);

        var optimizer = new Adam(learningRate: 0.005f);

        var initialLoss = EvaluateHybrid(conv1, pool1, proj, mha, head, inputs, labels);

        for (var epoch = 0; epoch < 25; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var (convOut, oh, ow) = conv1.Forward(inputs, inH: inH, inW: inW, ctx);
            var (pooledOut, ph, pw) = pool1.Forward(convOut, midCh, oh, ow, ctx);
            var features = proj.Forward(pooledOut, ctx);

            // 将 CNN 特征 reshape 为序列格式 [batch, 1, dModel]
            var seqFeatures = features.Reshape(batch, 1, dModel);
            var attended = mha.Forward(seqFeatures, null, ctx);
            var flatAttended = attended.Reshape(batch, dModel);
            var logits = head.Forward(flatAttended, ctx);
            var transformedLabels = labels.Slice(0, 0, batch);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, transformedLabels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(
                conv1.Parameters()
                    .Concat(proj.Parameters())
                    .Concat(mha.Parameters())
                    .Concat(head.Parameters()),
                maxNorm: 0.5f);

            var allParams = conv1.Parameters()
                .Concat(proj.Parameters())
                .Concat(mha.Parameters())
                .Concat(head.Parameters());
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        var finalLoss = EvaluateHybrid(conv1, pool1, proj, mha, head, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.9f,
            $"CNN+MHA Hybrid 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    [Fact]
    public void EndToEnd_Hybrid_WithScheduler_Converges()
    {
        var batch = 2;
        var inCh = 1;
        var midCh = 4;
        var inH = 8;
        var inW = 8;
        var dModel = 16;
        var numHeads = 4;

        var conv1 = new Conv2D(inCh, midCh, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var pool1 = new MaxPool2D(poolSize: 2, stride: 2);
        var proj = new Dense(midCh * (inH / 2) * (inW / 2), dModel);
        var mha = new MultiHeadAttention(dModel, numHeads);
        var head = new Dense(dModel, 3);

        var (inputs, labels) = GenerateSimpleImageData(batch, inCh, inH, inW);

        var optimizer = new SGD(learningRate: 0.05f);
        var scheduler = new CosineAnnealingLR(optimizer, totalSteps: 30, etaMin: 0.001f);

        var initialLoss = EvaluateHybrid(conv1, pool1, proj, mha, head, inputs, labels);

        for (var epoch = 0; epoch < 30; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var (convOut, oh, ow) = conv1.Forward(inputs, inH: inH, inW: inW, ctx);
            var (pooledOut, ph, pw) = pool1.Forward(convOut, midCh, oh, ow, ctx);
            var features = proj.Forward(pooledOut, ctx);
            var seqFeatures = features.Reshape(batch, 1, dModel);
            var attended = mha.Forward(seqFeatures, null, ctx);
            var flatAttended = attended.Reshape(batch, dModel);
            var logits = head.Forward(flatAttended, ctx);
            var transformedLabels = labels.Slice(0, 0, batch);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, transformedLabels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(
                conv1.Parameters()
                    .Concat(proj.Parameters())
                    .Concat(mha.Parameters())
                    .Concat(head.Parameters()),
                maxNorm: 1.0f);

            var allParams = conv1.Parameters()
                .Concat(proj.Parameters())
                .Concat(mha.Parameters())
                .Concat(head.Parameters());
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
            scheduler.Step();
        }

        var finalLoss = EvaluateHybrid(conv1, pool1, proj, mha, head, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.85f,
            $"Hybrid + CosineLR 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 85%");
    }

    #endregion

    #region Helpers

    private static (ArrayND Inputs, ArrayND Labels) GenerateSimpleImageData(
        int batch, int channels, int inH, int inW)
    {
        var rng = Random.Shared;
        var inputs = ArrayND.Zeros(batch, channels * inH * inW);
        var labels = ArrayND.Zeros(batch, 1);
        var spanIn = inputs.AsWriteSpan();
        var spanLb = labels.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            spanLb[n] = n % 3;

            for (var c = 0; c < channels; c++)
            for (var h = 0; h < inH; h++)
            for (var w = 0; w < inW; w++)
            {
                var pattern = (n % 3) switch
                {
                    0 => h < inH / 2 ? 1.0f : -1.0f,
                    1 => w < inW / 2 ? 1.0f : -1.0f,
                    _ => (h + w) % 2 == 0 ? 1.0f : -1.0f
                };
                spanIn[n * channels * inH * inW + c * inH * inW + h * inW + w] =
                    pattern + 0.2f * BoxMullerSimple(rng);
            }
        }

        return (inputs, labels);
    }

    private static float EvaluateCNN(
        Conv2D conv, MaxPool2D pool, Dense dense,
        ArrayND inputs, ArrayND labels)
    {
        var batch = inputs.Shape[0];
        var inH = (int)MathF.Sqrt(inputs.Shape[1]);

        var (convOut, oh, ow) = conv.Forward(inputs, inH: inH, inW: inH);
        var (pooledOut, ph, pw) = pool.Forward(convOut, conv.OutChannels, oh, ow);
        var logits = dense.Forward(pooledOut);
        var transformedLabels = labels.Slice(0, 0, batch);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, transformedLabels);
        return lossVal;
    }

    private static float EvaluateHybrid(
        Conv2D conv, MaxPool2D pool, Dense proj,
        MultiHeadAttention mha, Dense head,
        ArrayND inputs, ArrayND labels)
    {
        var batch = inputs.Shape[0];
        var inH = (int)MathF.Sqrt(inputs.Shape[1]);
        var dModel = head.Weight.Shape[0];

        var (convOut, oh, ow) = conv.Forward(inputs, inH: inH, inW: inH);
        var (pooledOut, ph, pw) = pool.Forward(convOut, conv.OutChannels, oh, ow);
        var features = proj.Forward(pooledOut);
        var seqFeatures = features.Reshape(batch, 1, dModel);
        var attended = mha.Forward(seqFeatures);
        var flatAttended = attended.Reshape(batch, dModel);
        var logits = head.Forward(flatAttended);
        var transformedLabels = labels.Slice(0, 0, batch);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, transformedLabels);
        return lossVal;
    }

    private static float BoxMullerSimple(Random rng)
    {
        var u1 = 1.0f - rng.NextSingle();
        var u2 = 1.0f - rng.NextSingle();
        return MathF.Sqrt(-2.0f * MathF.Log(MathF.Max(u1, 1e-10f))) * MathF.Cos(2.0f * MathF.PI * u2);
    }

    private static float EvalMobileNet(
        DepthwiseConv2D dw, Conv2D pw, MaxPool2D pool, Dense dense,
        ArrayND inputs, ArrayND labels)
    {
        var batch = inputs.Shape[0];
        var inH = (int)MathF.Sqrt(inputs.Shape[1]);

        var (dwOut, _, _) = dw.Forward(inputs, inH, inW: inH);
        var (pwOut, _, _) = pw.Forward(dwOut, inH, inW: inH);
        var (poolOut, _, _) = pool.Forward(pwOut, pw.OutChannels, inH, inH);
        var logits = dense.Forward(poolOut);
        var transformedLabels = labels.Slice(0, 0, batch);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, transformedLabels);
        return lossVal;
    }

    private static float EvalCheckpoint(
        Checkpoint cp, Dense head,
        ArrayND inputs, ArrayND labels)
    {
        var batch = inputs.Shape[0];
        var convOut = cp.Forward(inputs);
        var logits = head.Forward(convOut);
        var transformedLabels = labels.Slice(0, 0, batch);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, transformedLabels);
        return lossVal;
    }

    #endregion
}