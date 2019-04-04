namespace Galatea.Tests;

/// <summary>
///     Sequential 容器 / ModelSummary / Checkpoint 检查点集成测试
/// </summary>
public class EngineeringTests : GradientTestBase
{
    #region Helpers

    private static float EvaluateSequential(Sequential model, ArrayND inputs, ArrayND labels)
    {
        var logits = model.Forward(inputs);
        var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, labels);
        return lossVal;
    }

    #endregion

    #region Sequential

    [Fact]
    public void Sequential_Forward_ChainsCorrectly()
    {
        var model = new Sequential(
            new Dense(4, 3),
            new Dense(3, 2)
        );

        var x = ArrayND.Ones(1, 4);
        var output = model.Forward(x);

        Assert.Equal(new[] { 1, 2 }, output.Shape);
    }

    [Fact]
    public void Sequential_Autograd_ChainsCorrectly()
    {
        var model = new Sequential(
            new Dense(4, 8),
            new Dense(8, 3)
        );

        var x = ArrayND.Ones(1, 4);
        var target = ArrayND.Zeros(1, 1);
        target.AsWriteSpan()[0] = 0;

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var logits = model.Forward(x, ctx);
        var (loss, _) = Losses.SoftmaxCrossEntropy(logits, target, ctx);
        ctx.Backward(loss);

        foreach (var p in model.Parameters())
        {
            Assert.NotNull(p.Grad);
            var span = p.Grad!.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                Assert.False(float.IsNaN(span[i]), "参数梯度包含 NaN");
                Assert.False(float.IsInfinity(span[i]), "参数梯度包含 Inf");
            }
        }
    }

    [Fact]
    public void Sequential_EndToEnd_TrainsCorrectly()
    {
        var model = new Sequential(
            new Dense(6, 12),
            new Dense(12, 3)
        );

        var inputs = ArrayND.RandomNormal(100, 6);
        var labels = ArrayND.Zeros(100, 1);
        var spanL = labels.AsWriteSpan();
        for (var i = 0; i < 100; i++) spanL[i] = i % 3;

        var initialLoss = EvaluateSequential(model, inputs, labels);

        var optimizer = new Adam(learningRate: 0.02f);
        for (var epoch = 0; epoch < 30; epoch++)
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

        var finalLoss = EvaluateSequential(model, inputs, labels);

        Assert.True(finalLoss < initialLoss * 0.95f,
            $"Sequential 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4} 的 80%");
    }

    [Fact]
    public void Sequential_ParameterAggregation_Correct()
    {
        var model = new Sequential(
            new Dense(4, 3),
            new Dense(3, 2)
        );

        var paramCount = model.Parameters().Count();
        Assert.Equal(4, paramCount);
    }

    #endregion

    #region ModelSummary

    [Fact]
    public void ModelSummary_CountParameters_Correct()
    {
        var model = new Sequential(
            new Dense(784, 128),
            new Dense(128, 10)
        );

        var total = ModelSummary.CountParameters(model.Parameters());

        var expected = 784 * 128 + 128 + 128 * 10 + 10;
        Assert.Equal(expected, total);
    }

    [Fact]
    public void ModelSummary_Summarize_ReturnsValidOutput()
    {
        var model = new Sequential(
            new Dense(4, 3),
            new Dense(3, 2)
        );

        var summary = ModelSummary.Summarize(model, new[] { 1, 4 });

        Assert.Contains("模型摘要", summary);
        Assert.Contains("总参数量", summary);
        Assert.Contains("Dense", summary);
    }

    #endregion

    #region Checkpoint

    [Fact]
    public void Checkpoint_Forward_ProducesSameOutput()
    {
        var inner = new Sequential(new Dense(4, 3));
        var cp = new Checkpoint(inner);

        var x = ArrayND.Ones(2, 4);

        var outDirect = inner.Forward(x);
        var outCP = cp.Forward(x);

        var spanD = outDirect.AsSpan();
        var spanC = outCP.AsSpan();
        for (var i = 0; i < spanD.Length; i++) Assert.Equal(spanD[i], spanC[i], 1e-6f);
    }

    [Fact]
    public void Checkpoint_Gradient_MatchesDirectGradient()
    {
        var inner = new Sequential(
            new Dense(4, 6),
            new Dense(6, 3)
        );
        var cp = new Checkpoint(inner);

        var x = ArrayND.RandomNormal(2, 4);

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var output = cp.Forward(modifiedX);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = cp.Forward(x, ctx);
        ctx.Backward(output);
        var autoGrad = x.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance * 10.0f,
            $"Checkpoint 梯度误差 {error:E3} 超 {GradientTolerance * 10.0f:E3}");
    }

    [Fact]
    public void Checkpoint_EndToEnd_TrainsCorrectly()
    {
        var inner = new Sequential(
            new Dense(6, 12),
            new Dense(12, 3)
        );
        var cp = new Checkpoint(inner);

        var inputs = ArrayND.RandomNormal(50, 6);
        var labels = ArrayND.Zeros(50, 1);
        var spanL = labels.AsWriteSpan();
        for (var i = 0; i < 50; i++) spanL[i] = i % 3;

        var optimizer = new Adam(learningRate: 0.02f);

        for (var epoch = 0; epoch < 15; epoch++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = cp.Forward(inputs, ctx);
            var (loss, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);
            ctx.Backward(loss);

            GradientClipping.ClipGradNorm(inner.Parameters(), maxNorm: 1.0f);
            optimizer.Step(inner.Parameters());
            optimizer.ZeroGrad(inner.Parameters());
        }

        var finalLoss = EvaluateSequential(inner, inputs, labels);
        Assert.True(finalLoss > 0.0f && !float.IsNaN(finalLoss),
            $"Checkpoint 训练后损失 {finalLoss} 应有效且非零");
    }

    #endregion
}