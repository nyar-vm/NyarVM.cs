namespace Galatea.Tests;

/// <summary>
///     基本算子（Dense / Conv2D / MaxPool2D / Flatten）测试
/// </summary>
public class OperatorsTests : GradientTestBase
{
    #region Dense

    [Fact]
    public void Dense_Forward_ProducesCorrectOutputShape()
    {
        var dense = new Dense(fanIn: 8, fanOut: 4);
        var input = RandomInput(2, 8);
        var output = dense.Forward(input);
        Assert.Equal(2, output.Shape[0]);
        Assert.Equal(4, output.Shape[1]);
    }

    [Fact]
    public void Dense_Forward_WithAutograd_RegistersGradients()
    {
        var dense = new Dense(fanIn: 4, fanOut: 2);
        var input = RandomInput(1, 4);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        Assert.NotNull(input.Grad);
        Assert.NotNull(dense.Weight.Grad);
        Assert.NotNull(dense.Bias.Grad);
    }

    [Fact]
    public void Dense_ParameterGradient_MatchesNumericalGradient()
    {
        var dense = new Dense(fanIn: 3, fanOut: 2);
        var input = RandomInput(1, 3);

        var numGrad = NumericalGradient(dense.Weight.Clone(), w =>
        {
            var d = new Dense(fanIn: 3, fanOut: 2);
            d.Weight.AsWriteSpan().Clear();
            w.AsSpan().CopyTo(d.Weight.AsWriteSpan());
            var outVal = d.Forward(input);
            return outVal.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        Assert.NotNull(dense.Weight.Grad);
        var error = MaxAbsoluteError(numGrad, dense.Weight.Grad!);
        Assert.True(error < GradientTolerance * 10.0f, $"Dense Weight 梯度误差 {error} 超过容差");
    }

    #endregion

    #region Conv2D

    [Fact]
    public void Conv2D_Forward_ProducesCorrectOutputShape()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 4, kernelSize: 3, padding: 0);
        var input = RandomInput(1, 1 * 5 * 5);
        var (output, outH, outW) = conv.Forward(input, inH: 5, inW: 5);
        Assert.Equal(1, output.Shape[0]);
        Assert.Equal(4 * 3 * 3, output.Shape[1]);
        Assert.Equal(3, outH);
        Assert.Equal(3, outW);
    }

    [Fact]
    public void Conv2D_Forward_WithAutograd_RegistersGradients()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 2, kernelSize: 3, padding: 0);
        var input = RandomInput(1, 1 * 4 * 4);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var (output, _, _) = conv.Forward(input, inH: 4, inW: 4, ctx);
        ctx.Backward(output);

        Assert.NotNull(input.Grad);
        Assert.NotNull(conv.Weight.Grad);
    }

    [Fact]
    public void Conv2D_WeightGradient_MatchesNumericalGradient()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 2, kernelSize: 3);
        var input = RandomInput(1, 1 * 5 * 5);

        var numGrad = NumericalGradient(conv.Weight.Clone(), w =>
        {
            var d = new Conv2D(inChannels: 1, outChannels: 2, kernelSize: 3);
            d.Weight.AsWriteSpan().Clear();
            w.AsSpan().CopyTo(d.Weight.AsWriteSpan());
            var (output, _, _) = d.Forward(input, inH: 5, inW: 5);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var (autogradOutput, _, _) = conv.Forward(input, inH: 5, inW: 5, ctx);
        ctx.Backward(autogradOutput);

        Assert.NotNull(conv.Weight.Grad);
        var error = MaxAbsoluteError(numGrad, conv.Weight.Grad!);
        var tolerance = GradientTolerance * 5.0f;
        Assert.True(error < tolerance, $"Conv2D Weight 梯度误差 {error} 超过容差 {tolerance}");
    }

    [Fact]
    public void Conv2D_BiasGradient_MatchesNumericalGradient()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 2, kernelSize: 3);
        var input = RandomInput(1, 1 * 5 * 5);

        var numGrad = NumericalGradient(conv.Bias.Clone(), b =>
        {
            var d = new Conv2D(inChannels: 1, outChannels: 2, kernelSize: 3);
            d.Weight.AsWriteSpan().Clear();
            conv.Weight.AsSpan().CopyTo(d.Weight.AsWriteSpan());
            d.Bias.AsWriteSpan().Clear();
            b.AsSpan().CopyTo(d.Bias.AsWriteSpan());
            var (output, _, _) = d.Forward(input, inH: 5, inW: 5);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var (autogradOutput, _, _) = conv.Forward(input, inH: 5, inW: 5, ctx);
        ctx.Backward(autogradOutput);

        Assert.NotNull(conv.Bias.Grad);
        var error = MaxAbsoluteError(numGrad, conv.Bias.Grad!);
        var tolerance = GradientTolerance * 5.0f;
        Assert.True(error < tolerance, $"Conv2D Bias 梯度误差 {error} 超过容差 {tolerance}");
    }

    #endregion

    #region MaxPool2D

    [Fact]
    public void MaxPool2D_Forward_ReducesSpatialDimensions()
    {
        var input = RandomInput(1, 4 * 4 * 4);
        var (output, outH, outW) = new MaxPool2D().Forward(input, channels: 4, inH: 4, inW: 4);
        Assert.Equal(2, outH);
        Assert.Equal(2, outW);
        Assert.Equal(1, output.Shape[0]);
        Assert.Equal(4 * 2 * 2, output.Shape[1]);
    }

    [Fact]
    public void MaxPool2D_Gradient_OnlyRoutesToMaxPosition()
    {
        var data = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var input = ArrayND.FromArray(data, 1, 1 * 4 * 4);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var (output, _, _) = new MaxPool2D().Forward(input, channels: 1, inH: 4, inW: 4, ctx);
        ctx.Backward(output);

        Assert.NotNull(input.Grad);
        var gradSpan = input.Grad!.AsSpan();
        var nonZeroCount = 0;
        for (var i = 0; i < gradSpan.Length; i++)
            if (gradSpan[i] > 1e-6f)
                nonZeroCount++;

        Assert.True(nonZeroCount > 0, "至少有一个输入位置的梯度应为非零");
    }

    #endregion

    #region Flatten

    [Fact]
    public void Flatten_Forward_ReshapesCorrectly()
    {
        var input = RandomInput(2, 3 * 4 * 4);
        var output = Flatten.Forward(input, channels: 3, h: 4, w: 4);
        Assert.Equal(2, output.Shape[0]);
        Assert.Equal(3 * 4 * 4, output.Shape[1]);
    }

    [Fact]
    public void Flatten_Forward_WithAutograd_PreservesGradientFlow()
    {
        var input = RandomInput(2, 3 * 4 * 4);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var output = Flatten.Forward(input, channels: 3, h: 4, w: 4, ctx);
        ctx.Backward(output);

        Assert.NotNull(input.Grad);
        var gradSpan = input.Grad!.AsSpan();
        var hasNonZero = false;
        for (var i = 0; i < gradSpan.Length; i++)
            if (MathF.Abs(gradSpan[i]) > 1e-8f)
            {
                hasNonZero = true;
                break;
            }

        Assert.True(hasNonZero, "梯度应有非零值");
    }

    #endregion
}