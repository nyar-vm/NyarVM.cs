namespace Galatea.Tests;

/// <summary>
///     激活函数梯度测试
/// </summary>
public class ActivationsTests : GradientTestBase
{
    #region ReLU

    [Fact]
    public void ReLU_Forward_ProducesNonNegativeOutput()
    {
        var input = RandomInput(2, 8);
        var output = Activations.ReLUForward(input);
        var span = output.AsSpan();
        for (var i = 0; i < span.Length; i++) Assert.True(span[i] >= 0.0f);
    }

    [Fact]
    public void ReLU_Gradient_MatchesNumericalGradient_SmallInput()
    {
        var error = VerifyActivationGradient(Activations.ReLU, Activations.ReLUForward, 1, 10);
        Assert.True(error < GradientTolerance, $"梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void ReLU_Gradient_MatchesNumericalGradient_BatchInput()
    {
        var error = VerifyActivationGradient(Activations.ReLU, Activations.ReLUForward, 4, 8);
        Assert.True(error < GradientTolerance, $"梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void ReLU_Backward_GradientEqualsZero_ForNegativeInput()
    {
        var input = ArrayND.FromArray(new[] { -1.0f, -2.0f, -0.5f }, 1, 3);
        var output = Activations.ReLUForward(input);
        var upstream = ArrayND.FromArray(new[] { 1.0f, 1.0f, 1.0f }, 1, 3);
        var grad = Activations.ReLUBackward(output, upstream);
        var span = grad.AsSpan();
        for (var i = 0; i < span.Length; i++) Assert.Equal(0.0f, span[i]);
    }

    #endregion

    #region Sigmoid

    [Fact]
    public void Sigmoid_Forward_ProducesOutputInRange()
    {
        var input = RandomInput(2, 8);
        var output = Activations.SigmoidForward(input);
        var span = output.AsSpan();
        for (var i = 0; i < span.Length; i++) Assert.True(span[i] > 0.0f && span[i] < 1.0f);
    }

    [Fact]
    public void Sigmoid_Gradient_MatchesNumericalGradient()
    {
        var error = VerifyActivationGradient(Activations.Sigmoid, Activations.SigmoidForward, 1, 8);
        Assert.True(error < GradientTolerance, $"梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void Sigmoid_Backward_ReturnsCorrectGradient()
    {
        var input = ArrayND.FromArray(new[] { 0.0f, 1.0f, -1.0f }, 1, 3);
        var output = Activations.SigmoidForward(input);
        var upstream = ArrayND.FromArray(new[] { 1.0f, 1.0f, 1.0f }, 1, 3);
        var grad = Activations.SigmoidBackward(output, upstream);
        var span = grad.AsSpan();
        Assert.True(span[0] > 0.0f);
        Assert.True(span[1] > 0.0f);
        Assert.True(span[2] > 0.0f);
    }

    #endregion

    #region Tanh

    [Fact]
    public void Tanh_Forward_ProducesOutputInRange()
    {
        var input = RandomInput(2, 8);
        var output = Activations.TanhForward(input);
        var span = output.AsSpan();
        for (var i = 0; i < span.Length; i++) Assert.True(span[i] > -1.0f && span[i] < 1.0f);
    }

    [Fact]
    public void Tanh_Gradient_MatchesNumericalGradient()
    {
        var error = VerifyActivationGradient(Activations.Tanh, Activations.TanhForward, 1, 8);
        Assert.True(error < GradientTolerance, $"梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void Tanh_Backward_ReturnsCorrectGradient()
    {
        var input = ArrayND.FromArray(new[] { 0.0f, 1.0f, -1.0f }, 1, 3);
        var output = Activations.TanhForward(input);
        var upstream = ArrayND.FromArray(new[] { 1.0f, 1.0f, 1.0f }, 1, 3);
        var grad = Activations.TanhBackward(output, upstream);
        var span = grad.AsSpan();
        Assert.True(span[0] > 0.0f);
        Assert.True(span[1] < 1.0f);
        Assert.True(span[2] < 1.0f);
    }

    #endregion

    #region Softmax

    [Fact]
    public void Softmax_Forward_ProducesValidProbabilityDistribution()
    {
        var input = RandomInput(2, 5);
        var output = Activations.SoftmaxForward(input);
        var span = output.AsSpan();

        for (var n = 0; n < 2; n++)
        {
            var sum = 0.0f;
            for (var c = 0; c < 5; c++) sum += span[n * 5 + c];
            Assert.True(MathF.Abs(sum - 1.0f) < 1e-5f, $"样本 {n} 的 softmax 和不为 1：{sum}");
        }
    }

    [Fact]
    public void Softmax_Gradient_MatchesNumericalGradient()
    {
        var error = VerifyActivationGradient(Activations.Softmax, Activations.SoftmaxForward, 1, 5);
        Assert.True(error < GradientTolerance, $"梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void Softmax_Backward_GradientSumsToZero_PerSample()
    {
        var input = RandomInput(2, 5);
        var output = Activations.SoftmaxForward(input);
        var upstream = ArrayND.FromArray(new[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f }, 2, 5);
        var grad = Activations.SoftmaxBackward(output, upstream);
        var span = grad.AsSpan();

        for (var n = 0; n < 2; n++)
        {
            var gradSum = 0.0f;
            for (var c = 0; c < 5; c++) gradSum += span[n * 5 + c];
            Assert.True(MathF.Abs(gradSum) < 1e-6f, $"样本 {n} 的梯度和不接近零：{gradSum}");
        }
    }

    #endregion

    #region GELU

    [Fact]
    public void GELU_Forward_ProducesReasonableOutput()
    {
        var input = RandomInput(2, 8);
        var output = Activations.GELUForward(input);
        var spanIn = input.AsSpan();
        var spanOut = output.AsSpan();
        for (var i = 0; i < spanIn.Length; i++) Assert.True(spanOut[i] > -1.0f);
    }

    [Fact]
    public void GELU_Gradient_MatchesNumericalGradient()
    {
        var error = VerifyActivationGradient(Activations.GELU, Activations.GELUForward, 1, 8);
        Assert.True(error < GradientTolerance, $"梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void GELU_Backward_ReturnsCorrectGradientSign()
    {
        var input = ArrayND.FromArray(new[] { 2.0f, 0.0f, -2.0f }, 1, 3);
        var upstream = ArrayND.FromArray(new[] { 1.0f, 1.0f, 1.0f }, 1, 3);
        var grad = Activations.GELUBackward(input, upstream);
        var span = grad.AsSpan();
        Assert.True(span[0] > 0.5f);
        Assert.True(span[1] > 0.0f);
        Assert.True(span[2] < 0.1f);
    }

    #endregion

    #region SiLU

    [Fact]
    public void SiLU_Forward_ProducesReasonableOutput()
    {
        var input = RandomInput(2, 8);
        var output = Activations.SiLUForward(input);
        var spanIn = input.AsSpan();
        var spanOut = output.AsSpan();
        for (var i = 0; i < spanIn.Length; i++) Assert.True(spanOut[i] > -0.3f);
    }

    [Fact]
    public void SiLU_Gradient_MatchesNumericalGradient()
    {
        var error = VerifyActivationGradient(Activations.SiLU, Activations.SiLUForward, 1, 8);
        Assert.True(error < GradientTolerance, $"梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void SiLU_Backward_ReturnsCorrectGradientSign()
    {
        var input = ArrayND.FromArray(new[] { 2.0f, 0.0f, -2.0f }, 1, 3);
        var upstream = ArrayND.FromArray(new[] { 1.0f, 1.0f, 1.0f }, 1, 3);
        var grad = Activations.SiLUBackward(input, upstream);
        var span = grad.AsSpan();
        Assert.True(span[0] > 0.5f);
        Assert.True(span[1] > 0.0f);
        Assert.True(span[2] < 0.3f);
    }

    #endregion
}