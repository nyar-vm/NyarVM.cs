namespace Galatea.Tests;

/// <summary>
///     损失函数测试
/// </summary>
public class LossesTests : GradientTestBase
{
    #region MSE

    [Fact]
    public void MSE_Forward_ReturnsZero_ForPerfectPrediction()
    {
        var predicted = ArrayND.FromArray(new[] { 1.0f, 2.0f, 3.0f }, 1, 3);
        var target = ArrayND.FromArray(new[] { 1.0f, 2.0f, 3.0f }, 1, 3);
        var loss = Losses.MSEForward(predicted, target);
        Assert.Equal(0.0f, loss);
    }

    [Fact]
    public void MSE_Forward_ReturnsPositive_ForImperfectPrediction()
    {
        var predicted = ArrayND.FromArray(new[] { 1.0f, 2.0f, 3.0f }, 1, 3);
        var target = ArrayND.FromArray(new[] { 0.0f, 0.0f, 0.0f }, 1, 3);
        var loss = Losses.MSEForward(predicted, target);
        Assert.True(loss > 0.0f);
    }

    [Fact]
    public void MSE_Gradient_MatchesNumericalGradient()
    {
        var predicted = RandomInput(1, 6);
        var target = RandomInput(1, 6);

        var numGrad = NumericalGradient(predicted.Clone(), x => Losses.MSEForward(x, target));
        var autoGrad = Losses.MSEBackward(predicted, target);

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance, $"MSE 梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    #endregion

    #region SoftmaxCrossEntropy

    [Fact]
    public void SoftmaxCrossEntropy_Forward_ReturnsPositiveLoss()
    {
        var logits = RandomInput(2, 5);
        var labels = ArrayND.FromArray(new[] { 0.0f, 1.0f }, 2, 1);
        var (loss, probs) = Losses.SoftmaxCrossEntropyForward(logits, labels);
        Assert.True(loss > 0.0f);
    }

    [Fact]
    public void SoftmaxCrossEntropy_Backward_ShapeMatchesLogits()
    {
        var logits = RandomInput(2, 5);
        var labels = ArrayND.FromArray(new[] { 0.0f, 1.0f }, 2, 1);
        var (_, probs) = Losses.SoftmaxCrossEntropyForward(logits, labels);
        var grad = Losses.SoftmaxCrossEntropyBackward(probs, labels);
        Assert.Equal(2, grad.Shape[0]);
        Assert.Equal(5, grad.Shape[1]);
    }

    #endregion

    #region BCE

    [Fact]
    public void BCE_Forward_ReturnsZero_ForPerfectPrediction()
    {
        var predicted = ArrayND.FromArray(new[] { 0.999f, 0.001f }, 1, 2);
        var target = ArrayND.FromArray(new[] { 1.0f, 0.0f }, 1, 2);
        var loss = Losses.BCEForward(predicted, target);
        Assert.True(loss < 0.1f);
    }

    [Fact]
    public void BCE_Forward_ReturnsLargeLoss_ForWrongPrediction()
    {
        var predicted = ArrayND.FromArray(new[] { 0.001f, 0.999f }, 1, 2);
        var target = ArrayND.FromArray(new[] { 1.0f, 0.0f }, 1, 2);
        var loss = Losses.BCEForward(predicted, target);
        Assert.True(loss > 1.0f);
    }

    [Fact]
    public void BCE_Gradient_MatchesNumericalGradient()
    {
        var predicted = ArrayND.FromArray(new[] { 0.7f, 0.3f, 0.8f, 0.2f }, 1, 4);
        var target = ArrayND.FromArray(new[] { 1.0f, 0.0f, 1.0f, 0.0f }, 1, 4);

        var numGrad = NumericalGradient(predicted.Clone(), x => Losses.BCEForward(x, target));
        var autoGrad = Losses.BCEBackward(predicted, target);

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance, $"BCE 梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    #endregion

    #region KLDiv

    [Fact]
    public void KLDiv_Forward_ReturnsZero_ForIdenticalDistributions()
    {
        var predicted = ArrayND.FromArray(new[] { 0.5f, 0.5f }, 1, 2);
        var target = ArrayND.FromArray(new[] { 0.5f, 0.5f }, 1, 2);
        var loss = Losses.KLDivForward(predicted, target);
        Assert.True(loss < 1e-4f);
    }

    [Fact]
    public void KLDiv_Forward_ReturnsPositive_ForDifferentDistributions()
    {
        var predicted = ArrayND.FromArray(new[] { 0.9f, 0.1f }, 1, 2);
        var target = ArrayND.FromArray(new[] { 0.1f, 0.9f }, 1, 2);
        var loss = Losses.KLDivForward(predicted, target);
        Assert.True(loss > 0.0f);
    }

    [Fact]
    public void KLDiv_Gradient_MatchesNumericalGradient()
    {
        var predicted = ArrayND.FromArray(new[] { 0.7f, 0.3f, 0.6f, 0.4f }, 2, 2);
        var target = ArrayND.FromArray(new[] { 0.5f, 0.5f, 0.5f, 0.5f }, 2, 2);

        var numGrad = NumericalGradient(predicted.Clone(), x => Losses.KLDivForward(x, target));
        var autoGrad = Losses.KLDivBackward(predicted, target);

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance, $"KLDiv 梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    #endregion

    #region Huber

    [Fact]
    public void Huber_Forward_ReturnsZero_ForPerfectPrediction()
    {
        var predicted = ArrayND.FromArray(new[] { 1.0f, 2.0f, 3.0f }, 1, 3);
        var target = ArrayND.FromArray(new[] { 1.0f, 2.0f, 3.0f }, 1, 3);
        var loss = Losses.HuberForward(predicted, target);
        Assert.Equal(0.0f, loss);
    }

    [Fact]
    public void Huber_Forward_GraduallyIncreases_WithError()
    {
        var predicted = ArrayND.FromArray(new[] { 0.0f }, 1, 1);
        var target = ArrayND.FromArray(new[] { 0.0f }, 1, 1);

        var loss0 = Losses.HuberForward(predicted, target);

        target = ArrayND.FromArray(new[] { 0.5f }, 1, 1);
        var lossSmall = Losses.HuberForward(predicted, target);

        target = ArrayND.FromArray(new[] { 2.0f }, 1, 1);
        var lossLarge = Losses.HuberForward(predicted, target);

        Assert.True(loss0 < lossSmall, "小误差应产生大于零的损失");
        Assert.True(lossSmall < lossLarge, "大误差应产生更大的损失");

        var expectedSmall = 0.5f * 0.5f * 0.5f;
        Assert.Equal(expectedSmall, lossSmall, 3);

        var expectedLarge = 1.0f * (2.0f - 0.5f);
        Assert.Equal(expectedLarge, lossLarge, 3);
    }

    [Fact]
    public void Huber_Gradient_MatchesNumericalGradient()
    {
        var predicted = RandomInput(1, 6);
        var target = RandomInput(1, 6);

        var numGrad = NumericalGradient(predicted.Clone(), x => Losses.HuberForward(x, target));
        var autoGrad = Losses.HuberBackward(predicted, target);

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance, $"Huber 梯度误差 {error} 超过容差 {GradientTolerance}");
    }

    [Fact]
    public void Huber_Gradient_ClipsLargeErrors()
    {
        var predicted = ArrayND.FromArray(new[] { 10.0f, -10.0f }, 1, 2);
        var target = ArrayND.FromArray(new[] { 0.0f, 0.0f }, 1, 2);
        var n = predicted.Size;

        var grad = Losses.HuberBackward(predicted, target);
        var spanGrad = grad.AsSpan();

        var expectedMagnitude = 1.0f / n;
        Assert.True(MathF.Abs(spanGrad[0]) < expectedMagnitude + 1e-6f, "大正误差梯度应被裁剪");
        Assert.True(MathF.Abs(spanGrad[0]) > expectedMagnitude - 1e-6f, "大正误差梯度应接近 ±delta/N");
        Assert.True(MathF.Abs(spanGrad[1]) < expectedMagnitude + 1e-6f, "大负误差梯度应被裁剪");
        Assert.True(MathF.Abs(spanGrad[1]) > expectedMagnitude - 1e-6f, "大负误差梯度应接近 ±delta/N");

        Assert.True(spanGrad[0] > 0.0f, "预测过高（正 diff）应产生正梯度");
        Assert.True(spanGrad[1] < 0.0f, "预测过低（负 diff）应产生负梯度");
    }

    #endregion
}