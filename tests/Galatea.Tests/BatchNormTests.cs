namespace Galatea.Tests;

/// <summary>
///     Batch Normalization 层前向传播与数值梯度测试
/// </summary>
public class BatchNormTests : GradientTestBase
{
    #region 前向传播

    /// <summary>
    ///     验证 BatchNorm 前向传播不改变输入形状
    /// </summary>
    [Fact]
    public void BatchNorm_Forward_PreservesShape()
    {
        var bn = new BatchNorm(numFeatures: 3);
        var input = RandomInput(4, 3);
        var output = bn.Forward(input);
        Assert.Equal(4, output.Shape[0]);
        Assert.Equal(3, output.Shape[1]);
    }

    #endregion

    #region Gamma 梯度

    /// <summary>
    ///     验证 Gamma 参数的 Autograd 梯度与中心差分数值梯度一致
    /// </summary>
    [Fact]
    public void BatchNorm_GammaGradient_MatchesNumericalGradient()
    {
        var bn = new BatchNorm(numFeatures: 3);
        var input = RandomInput(4, 3);

        var numGrad = NumericalGradient(bn.Gamma.Clone(), g =>
        {
            var b = new BatchNorm(numFeatures: 3);
            b.Gamma.AsWriteSpan().Clear();
            g.AsSpan().CopyTo(b.Gamma.AsWriteSpan());
            b.Beta.AsWriteSpan().Clear();
            bn.Beta.AsSpan().CopyTo(b.Beta.AsWriteSpan());
            var output = b.Forward(input);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var autogradOutput = bn.Forward(input, ctx);
        ctx.Backward(autogradOutput);

        Assert.NotNull(bn.Gamma.Grad);
        var error = MaxAbsoluteError(numGrad, bn.Gamma.Grad!);
        var tolerance = GradientTolerance * 5.0f;
        Assert.True(error < tolerance, $"BatchNorm Gamma 梯度误差 {error} 超过容差 {tolerance}");
    }

    #endregion

    #region Beta 梯度

    /// <summary>
    ///     验证 Beta 参数的 Autograd 梯度与中心差分数值梯度一致
    /// </summary>
    [Fact]
    public void BatchNorm_BetaGradient_MatchesNumericalGradient()
    {
        var bn = new BatchNorm(numFeatures: 3);
        var input = RandomInput(4, 3);

        var numGrad = NumericalGradient(bn.Beta.Clone(), b =>
        {
            var d = new BatchNorm(numFeatures: 3);
            d.Gamma.AsWriteSpan().Clear();
            bn.Gamma.AsSpan().CopyTo(d.Gamma.AsWriteSpan());
            d.Beta.AsWriteSpan().Clear();
            b.AsSpan().CopyTo(d.Beta.AsWriteSpan());
            var output = d.Forward(input);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var autogradOutput = bn.Forward(input, ctx);
        ctx.Backward(autogradOutput);

        Assert.NotNull(bn.Beta.Grad);
        var error = MaxAbsoluteError(numGrad, bn.Beta.Grad!);
        var tolerance = GradientTolerance * 5.0f;
        Assert.True(error < tolerance, $"BatchNorm Beta 梯度误差 {error} 超过容差 {tolerance}");
    }

    #endregion

    #region 输入梯度

    /// <summary>
    ///     验证输入张量的 Autograd 梯度与中心差分数值梯度一致
    /// </summary>
    [Fact]
    public void BatchNorm_InputGradient_MatchesNumericalGradient()
    {
        var bn = new BatchNorm(numFeatures: 3);
        var input = RandomInput(4, 3);

        var numGrad = NumericalGradient(input.Clone(), x =>
        {
            var b = new BatchNorm(numFeatures: 3);
            b.Gamma.AsWriteSpan().Clear();
            bn.Gamma.AsSpan().CopyTo(b.Gamma.AsWriteSpan());
            b.Beta.AsWriteSpan().Clear();
            bn.Beta.AsSpan().CopyTo(b.Beta.AsWriteSpan());
            var output = b.Forward(x);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var autogradOutput = bn.Forward(input, ctx);
        ctx.Backward(autogradOutput);

        Assert.NotNull(input.Grad);
        var error = MaxAbsoluteError(numGrad, input.Grad!);
        var tolerance = GradientTolerance * 5.0f;
        Assert.True(error < tolerance, $"BatchNorm 输入梯度误差 {error} 超过容差 {tolerance}");
    }

    #endregion
}