namespace Galatea.Tests;

/// <summary>
///     DepthwiseConv2D / MobileNet 轻量模型集成测试
/// </summary>
public class LightweightModelTests : GradientTestBase
{
    #region Helpers

    private static float EvalModel(Sequential model, ArrayND inputs, ArrayND labels)
    {
        var logits = model.Forward(inputs);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, labels);
        return lossVal;
    }

    #endregion

    #region DepthwiseConv2D

    [Fact]
    public void DepthwiseConv2D_Forward_OutputShapeCorrect()
    {
        var conv = new DepthwiseConv2D(channels: 4, kernelSize: 3, stride: 1, padding: 0, inH: 8, inW: 8);

        var x = ArrayND.Ones(2, 4 * 8 * 8);
        var (output, outH, outW) = conv.Forward(x, 8, 8);

        Assert.Equal(6, outH);
        Assert.Equal(6, outW);
        Assert.Equal(new[] { 2, 4 * 6 * 6 }, output.Shape);
    }

    [Fact]
    public void DepthwiseConv2D_Forward_SameWeightsDifferentChannels()
    {
        var conv = new DepthwiseConv2D(channels: 3, kernelSize: 3, stride: 1, padding: 0, inH: 8, inW: 8);

        var x = ArrayND.Zeros(1, 3 * 8 * 8);
        var spanX = x.AsWriteSpan();
        for (var i = 0; i < spanX.Length; i++) spanX[i] = i % 7 / 3.0f;

        var (output, _, _) = conv.Forward(x, 8, 8);

        Assert.False(output.AsSpan()[0] == output.AsSpan()[6 * 6],
            "不同通道使用不同卷积核，输出应不同");
    }

    [Fact]
    public void DepthwiseConv2D_Gradient_MatchesNumerical()
    {
        var conv = new DepthwiseConv2D(channels: 2, kernelSize: 3, stride: 1, padding: 0, inH: 6, inW: 6);

        var x = RandomInput(1, 2 * 6 * 6);
        var xClone = x.Clone();

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var (output, _, _) = conv.Forward(modifiedX, 6, 6);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var (autoOut, _, _) = conv.Forward(xClone, 6, 6, ctx);
        ctx.Backward(autoOut);
        var autoGrad = xClone.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance,
            $"DepthwiseConv2D 梯度误差 {error:E3} 超 {GradientTolerance:E3}");
    }

    [Fact]
    public void DepthwiseConv2D_Gradient_WeightAndBias()
    {
        var conv = new DepthwiseConv2D(channels: 2, kernelSize: 3, stride: 1, padding: 0, inH: 6, inW: 6);

        var x = RandomInput(2, 2 * 6 * 6);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var (output, _, _) = conv.Forward(x, 6, 6, ctx);
        ctx.Backward(output);

        Assert.NotNull(conv.Weight.Grad);
        Assert.NotNull(conv.Bias.Grad);

        var spanW = conv.Weight.Grad!.AsSpan();
        Assert.True(spanW.Length > 0);
        for (var i = 0; i < spanW.Length; i++)
        {
            Assert.False(float.IsNaN(spanW[i]), $"Weight 梯度包含 NaN 于索引 {i}");
            Assert.False(float.IsInfinity(spanW[i]), $"Weight 梯度包含 Inf 于索引 {i}");
        }

        var spanB = conv.Bias.Grad!.AsSpan();
        for (var i = 0; i < spanB.Length; i++)
        {
            Assert.False(float.IsNaN(spanB[i]), $"Bias 梯度包含 NaN 于索引 {i}");
            Assert.False(float.IsInfinity(spanB[i]), $"Bias 梯度包含 Inf 于索引 {i}");
        }
    }

    [Fact]
    public void DepthwiseConv2D_EndToEnd_TrainsCorrectly()
    {
        var conv = new DepthwiseConv2D(channels: 3, kernelSize: 3, stride: 1, padding: 0, inH: 8, inW: 8);

        var inputs = ArrayND.RandomNormal(50, 3 * 8 * 8);
        var labels = ArrayND.Zeros(50, 1);
        var spanL = labels.AsWriteSpan();
        for (var i = 0; i < 50; i++) spanL[i] = i % 3;

        var model = new Sequential(
            conv,
            new Dense(3 * 6 * 6, 3)
        );

        var initialLoss = EvalModel(model, inputs, labels);

        var optimizer = new Adam(learningRate: 0.02f);
        for (var epoch = 0; epoch < 10; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = model.Forward(inputs, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(model.Parameters(), maxNorm: 1.0f);
            optimizer.Step(model.Parameters());
            optimizer.ZeroGrad(model.Parameters());
        }

        var finalLoss = EvalModel(model, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.9f,
            $"DepthwiseConv2D 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    [Fact]
    public void DepthwiseConv2D_Stride2_ReducesSpatialDims()
    {
        var conv = new DepthwiseConv2D(channels: 4, kernelSize: 3, stride: 2, padding: 1, inH: 8, inW: 8);

        var x = ArrayND.Ones(2, 4 * 8 * 8);
        var (output, outH, outW) = conv.Forward(x, 8, 8);

        Assert.Equal(4, outH);
        Assert.Equal(4, outW);
        Assert.Equal(new[] { 2, 4 * 4 * 4 }, output.Shape);
    }

    #endregion

    #region MobileNet Block

    [Fact]
    public void MobileNetBlock_Forward_ChainsCorrectly()
    {
        var model = new Sequential(
            new DepthwiseConv2D(channels: 4, kernelSize: 3, stride: 1, padding: 1, inH: 8, inW: 8),
            new Conv2D(inChannels: 4, outChannels: 8, kernelSize: 1, stride: 1, padding: 0, inH: 8, inW: 8)
        );

        var x = ArrayND.Ones(2, 4 * 8 * 8);
        var output = model.Forward(x);

        Assert.Equal(new[] { 2, 8 * 8 * 8 }, output.Shape);
    }

    [Fact]
    public void MobileNetBlock_Gradient_FlowsCorrectly()
    {
        var model = new Sequential(
            new DepthwiseConv2D(channels: 3, kernelSize: 3, stride: 1, padding: 0, inH: 6, inW: 6),
            new Conv2D(inChannels: 3, outChannels: 4, kernelSize: 1, stride: 1, padding: 0, inH: 4, inW: 4)
        );

        var x = RandomInput(2, 3 * 6 * 6);
        var target = ArrayND.Zeros(2, 1);
        var span = target.AsWriteSpan();
        span[0] = 0;
        span[1] = 1;

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var convOut = model.Forward(x, ctx);
        var flatOut = convOut.Reshape(2, 4 * 4 * 4);
        var (loss, _) = Losses.SoftmaxCrossEntropy(flatOut, target, ctx);
        ctx.Backward(loss);

        foreach (var p in model.Parameters())
        {
            Assert.NotNull(p.Grad);
            var gradSpan = p.Grad!.AsSpan();
            for (var i = 0; i < gradSpan.Length; i++)
            {
                Assert.False(float.IsNaN(gradSpan[i]), "参数梯度包含 NaN");
                Assert.False(float.IsInfinity(gradSpan[i]), "参数梯度包含 Inf");
            }
        }
    }

    [Fact]
    public void MobileNetBlock_EndToEnd_TrainsCorrectly()
    {
        var model = new Sequential(
            new DepthwiseConv2D(channels: 3, kernelSize: 3, stride: 1, padding: 0, inH: 8, inW: 8),
            new Conv2D(inChannels: 3, outChannels: 6, kernelSize: 1, stride: 1, padding: 0, inH: 6, inW: 6),
            new Dense(6 * 6 * 6, 3)
        );

        var inputs = ArrayND.RandomNormal(30, 3 * 8 * 8);
        var labels = ArrayND.Zeros(30, 1);
        var spanL = labels.AsWriteSpan();
        for (var i = 0; i < 30; i++) spanL[i] = i % 3;

        var initialLoss = EvalModel(model, inputs, labels);

        var optimizer = new Adam(learningRate: 0.02f);
        for (var epoch = 0; epoch < 12; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = model.Forward(inputs, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(model.Parameters(), maxNorm: 1.0f);
            optimizer.Step(model.Parameters());
            optimizer.ZeroGrad(model.Parameters());
        }

        var finalLoss = EvalModel(model, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.9f,
            $"MobileNet 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 90%");
    }

    [Fact]
    public void DepthwiseVsStandardConv_ParamCount_IsLower()
    {
        var dwConv = new DepthwiseConv2D(channels: 16, kernelSize: 3);
        var standardConv = new Conv2D(inChannels: 16, outChannels: 16, kernelSize: 3);

        var dwParams = ModelSummary.CountParameters(dwConv.Parameters());
        var standardParams = ModelSummary.CountParameters(standardConv.Parameters());

        Assert.True(dwParams < standardParams,
            $"逐通道卷积参数量 {dwParams} 应小于标准卷积 {standardParams}");
    }

    #endregion
}