namespace Galatea.Tests;

/// <summary>
///     ResidualBlock / ConvTranspose2D / Upsample2D / 扩散模型组件集成测试
/// </summary>
public class DiffusionModelTests : GradientTestBase
{
    #region ResidualBlock

    [Fact]
    public void ResidualBlock_Forward_OutputShapeCorrect()
    {
        var body = new Sequential(
            new Dense(8, 8),
            new Dense(8, 8)
        );
        var block = new ResidualBlock(body);

        var x = ArrayND.Ones(2, 8);
        var output = block.Forward(x);

        Assert.Equal(new[] { 2, 8 }, output.Shape);
    }

    [Fact]
    public void ResidualBlock_Forward_AddsIdentity()
    {
        var body = new Sequential(
            new Dense(4, 4)
        );
        var block = new ResidualBlock(body, activationFn: (x, _) => x, activationForwardFn: x => x);

        var x = ArrayND.Ones(1, 4);
        var bodyOut = body.Forward(x);
        var resOut = block.Forward(x);

        var spanBody = bodyOut.AsSpan();
        var spanRes = resOut.AsSpan();
        for (var i = 0; i < spanBody.Length; i++)
        {
            var expected = spanBody[i] + x.AsSpan()[i];
            Assert.Equal(expected, spanRes[i], 1e-5f);
        }
    }

    [Fact]
    public void ResidualBlock_Gradient_MatchesNumerical()
    {
        var body = new Sequential(
            new Dense(4, 4),
            new Dense(4, 4)
        );
        var block = new ResidualBlock(body);

        var x = RandomInput(1, 4);
        var xClone = x.Clone();

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var output = block.Forward(modifiedX);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = block.Forward(xClone, ctx);
        ctx.Backward(output);
        var autoGrad = xClone.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance * 10.0f,
            $"ResidualBlock 梯度误差 {error:E3} 超 {GradientTolerance * 10.0f:E3}");
    }

    [Fact]
    public void ResidualBlock_WithProjection_ShapeMismatch()
    {
        var body = new Sequential(
            new Dense(4, 8),
            new Dense(8, 8)
        );
        var shortcut = new Dense(4, 8);
        var block = new ResidualBlock(body, shortcut);

        var x = ArrayND.Ones(2, 4);
        var output = block.Forward(x);

        Assert.Equal(new[] { 2, 8 }, output.Shape);
    }

    [Fact]
    public void ResidualBlock_EndToEnd_TrainsCorrectly()
    {
        var body = new Sequential(
            new Dense(6, 6),
            new Dense(6, 6)
        );
        var block = new ResidualBlock(body);

        var inputs = ArrayND.RandomNormal(50, 6);
        var labels = ArrayND.Zeros(50, 1);
        var spanL = labels.AsWriteSpan();
        for (var i = 0; i < 50; i++) spanL[i] = i % 3;

        var head = new Dense(6, 3);

        var initialLoss = EvalResidual(block, head, inputs, labels);

        var optimizer = new Adam(learningRate: 0.02f);
        for (var epoch = 0; epoch < 20; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var features = block.Forward(inputs, ctx);
            var logits = head.Forward(features, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(block.Parameters().Concat(head.Parameters()), maxNorm: 1.0f);
            optimizer.Step(block.Parameters().Concat(head.Parameters()));
            optimizer.ZeroGrad(block.Parameters().Concat(head.Parameters()));
        }

        var finalLoss = EvalResidual(block, head, inputs, labels);
        Assert.True(finalLoss < initialLoss * 0.9f,
            $"ResidualBlock 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    #endregion

    #region ConvTranspose2D

    [Fact]
    public void ConvTranspose2D_Forward_UpsamplesSpatialDims()
    {
        var conv = new ConvTranspose2D(inChannels: 3, outChannels: 2, kernelSize: 3, stride: 2, padding: 1, inH: 4,
            inW: 4);

        var x = ArrayND.Ones(1, 3 * 4 * 4);
        var (output, outH, outW) = conv.Forward(x, 4, 4);

        Assert.Equal(7, outH);
        Assert.Equal(7, outW);
        Assert.Equal(new[] { 1, 2 * 7 * 7 }, output.Shape);
    }

    [Fact]
    public void ConvTranspose2D_Forward_Stride2_DoublesSize()
    {
        var conv = new ConvTranspose2D(inChannels: 2, outChannels: 2, kernelSize: 4, stride: 2, padding: 1, inH: 4,
            inW: 4);

        var x = ArrayND.Ones(1, 2 * 4 * 4);
        var (output, outH, outW) = conv.Forward(x, 4, 4);

        Assert.Equal(8, outH);
        Assert.Equal(8, outW);
    }

    [Fact]
    public void ConvTranspose2D_Gradient_MatchesNumerical()
    {
        var conv = new ConvTranspose2D(inChannels: 2, outChannels: 2, kernelSize: 3, stride: 1, padding: 0, inH: 4,
            inW: 4);

        var x = RandomInput(1, 2 * 4 * 4);
        var xClone = x.Clone();

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var (output, _, _) = conv.Forward(modifiedX, 4, 4);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var (autoOut, _, _) = conv.Forward(xClone, 4, 4, ctx);
        ctx.Backward(autoOut);
        var autoGrad = xClone.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance,
            $"ConvTranspose2D 梯度误差 {error:E3} 超 {GradientTolerance:E3}");
    }

    [Fact]
    public void ConvTranspose2D_Gradient_WeightAndBias()
    {
        var conv = new ConvTranspose2D(inChannels: 2, outChannels: 3, kernelSize: 3, stride: 1, padding: 0, inH: 4,
            inW: 4);

        var x = RandomInput(2, 2 * 4 * 4);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var (output, _, _) = conv.Forward(x, 4, 4, ctx);
        ctx.Backward(output);

        Assert.NotNull(conv.Weight.Grad);
        Assert.NotNull(conv.Bias.Grad);

        var spanW = conv.Weight.Grad!.AsSpan();
        for (var i = 0; i < spanW.Length; i++)
        {
            Assert.False(float.IsNaN(spanW[i]), "Weight 梯度包含 NaN");
            Assert.False(float.IsInfinity(spanW[i]), "Weight 梯度包含 Inf");
        }
    }

    #endregion

    #region Upsample2D

    [Fact]
    public void Upsample2D_Forward_DoublesSpatialDims()
    {
        var up = new Upsample2D(scaleFactor: 2);

        var x = ArrayND.Ones(1, 3 * 4 * 4);
        var (output, outH, outW) = up.Forward(x, channels: 3, inH: 4, inW: 4);

        Assert.Equal(8, outH);
        Assert.Equal(8, outW);
        Assert.Equal(new[] { 1, 3 * 8 * 8 }, output.Shape);
    }

    [Fact]
    public void Upsample2D_Forward_NearestNeighborValues()
    {
        var up = new Upsample2D(scaleFactor: 2);

        var x = ArrayND.Zeros(1, 1 * 2 * 2);
        var spanX = x.AsWriteSpan();
        spanX[0] = 1.0f;
        spanX[1] = 2.0f;
        spanX[2] = 3.0f;
        spanX[3] = 4.0f;

        var (output, _, _) = up.Forward(x, channels: 1, inH: 2, inW: 2);
        var spanOut = output.AsSpan();

        // NCHW flattened: input [[1,2],[3,4]] → output [[1,1,2,2],[1,1,2,2],[3,3,4,4],[3,3,4,4]]
        Assert.Equal(1.0f, spanOut[0]);
        Assert.Equal(1.0f, spanOut[1]);
        Assert.Equal(2.0f, spanOut[2]);
        Assert.Equal(2.0f, spanOut[3]);
        Assert.Equal(1.0f, spanOut[4]);
        Assert.Equal(1.0f, spanOut[5]);
        Assert.Equal(2.0f, spanOut[6]);
        Assert.Equal(2.0f, spanOut[7]);
        Assert.Equal(3.0f, spanOut[8]);
        Assert.Equal(3.0f, spanOut[9]);
        Assert.Equal(4.0f, spanOut[10]);
        Assert.Equal(4.0f, spanOut[11]);
        Assert.Equal(3.0f, spanOut[12]);
        Assert.Equal(3.0f, spanOut[13]);
        Assert.Equal(4.0f, spanOut[14]);
        Assert.Equal(4.0f, spanOut[15]);
    }

    [Fact]
    public void Upsample2D_Gradient_MatchesNumerical()
    {
        var up = new Upsample2D(scaleFactor: 2);

        var x = RandomInput(1, 2 * 3 * 3);
        var xClone = x.Clone();

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var (output, _, _) = up.Forward(modifiedX, channels: 2, inH: 3, inW: 3);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var (autoOut, _, _) = up.Forward(xClone, channels: 2, inH: 3, inW: 3, ctx);
        ctx.Backward(autoOut);
        var autoGrad = xClone.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance,
            $"Upsample2D 梯度误差 {error:E3} 超 {GradientTolerance:E3}");
    }

    #endregion

    #region U-Net Decoder Path

    [Fact]
    public void UNetDecoder_UpsampleConvTranspose_Converges()
    {
        var batch = 4;
        var inCh = 4;
        var midCh = 2;
        var inH = 4;
        var inW = 4;

        var upsample = new Upsample2D(scaleFactor: 2);
        var convT = new ConvTranspose2D(inChannels: inCh, outChannels: midCh, kernelSize: 3, stride: 1, padding: 1,
            inH: inH * 2, inW: inW * 2);
        var head = new Dense(midCh * inH * 2 * inW * 2, 3);

        var (inputs, labels) = GenerateSimpleImageData(batch, inCh, inH, inW);

        var initialLoss = EvalUNetDecoder(upsample, convT, head, inputs, labels, inCh, inH, inW);

        var optimizer = new Adam(learningRate: 0.01f);
        for (var epoch = 0; epoch < 15; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var (upOut, upH, upW) = upsample.Forward(inputs, inCh, inH, inW, ctx);
            var (convOut, _, _) = convT.Forward(upOut, upH, upW, ctx);
            var logits = head.Forward(convOut, ctx);
            var transformedLabels = labels.Slice(0, 0, batch);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, transformedLabels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(convT.Parameters().Concat(head.Parameters()), maxNorm: 1.0f);
            optimizer.Step(convT.Parameters().Concat(head.Parameters()));
            optimizer.ZeroGrad(convT.Parameters().Concat(head.Parameters()));
        }

        var finalLoss = EvalUNetDecoder(upsample, convT, head, inputs, labels, inCh, inH, inW);
        Assert.True(finalLoss < initialLoss * 0.9f,
            $"U-Net 解码器训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    [Fact]
    public void UNetEncoderDecoder_ResidualBlock_Converges()
    {
        var batch = 4;
        var dModel = 16;

        var body = new Sequential(
            new Dense(dModel, dModel),
            new Dense(dModel, dModel)
        );
        var resBlock = new ResidualBlock(body);
        var head = new Dense(dModel, 3);

        var inputs = ArrayND.RandomNormal(batch, dModel);
        var labels = ArrayND.Zeros(batch, 1);
        var spanL = labels.AsWriteSpan();
        for (var i = 0; i < batch; i++) spanL[i] = i % 3;

        var initialLoss = EvalResidual(resBlock, head, inputs, labels);

        var optimizer = new Adam(learningRate: 0.01f);
        for (var epoch = 0; epoch < 20; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var features = resBlock.Forward(inputs, ctx);
            var logits = head.Forward(features, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(resBlock.Parameters().Concat(head.Parameters()), maxNorm: 1.0f);
            optimizer.Step(resBlock.Parameters().Concat(head.Parameters()));
            optimizer.ZeroGrad(resBlock.Parameters().Concat(head.Parameters()));
        }

        var finalLoss = EvalResidual(resBlock, head, inputs, labels);
        Assert.True(finalLoss < initialLoss * 0.9f,
            $"ResidualBlock 编解码训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    #endregion

    #region Helpers

    private static float EvalResidual(ResidualBlock block, Dense head, ArrayND inputs, ArrayND labels)
    {
        var features = block.Forward(inputs);
        var logits = head.Forward(features);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, labels);
        return lossVal;
    }

    private static float EvalUNetDecoder(
        Upsample2D upsample, ConvTranspose2D convT, Dense head,
        ArrayND inputs, ArrayND labels, int channels, int inH, int inW)
    {
        var batch = inputs.Shape[0];
        var (upOut, upH, upW) = upsample.Forward(inputs, channels, inH, inW);
        var (convOut, _, _) = convT.Forward(upOut, upH, upW);
        var logits = head.Forward(convOut);
        var transformedLabels = labels.Slice(0, 0, batch);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, transformedLabels);
        return lossVal;
    }

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

    private static float BoxMullerSimple(Random rng)
    {
        var u1 = 1.0f - rng.NextSingle();
        var u2 = 1.0f - rng.NextSingle();
        return MathF.Sqrt(-2.0f * MathF.Log(MathF.Max(u1, 1e-10f))) * MathF.Cos(2.0f * MathF.PI * u2);
    }

    #endregion
}