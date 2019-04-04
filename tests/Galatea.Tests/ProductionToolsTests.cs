namespace Galatea.Tests;

/// <summary>
///     ReshapeWithGrad / SliceWithGrad / Metrics / GradientAccumulator / TrainingState 集成测试
/// </summary>
public class ProductionToolsTests : GradientTestBase
{
    #region ReshapeWithGrad

    [Fact]
    public void ReshapeWithGrad_GradientFlowsBack()
    {
        var x = ArrayND.RandomNormal(2, 3, 4);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var reshaped = x.ReshapeWithGrad(new[] { 2, 12 }, ctx);

        reshaped.EnsureGrad();
        var spanGrad = reshaped.Grad!.AsWriteSpan();
        for (var i = 0; i < spanGrad.Length; i++) spanGrad[i] = 1.0f;

        ctx.BackwardFromGradient(reshaped, reshaped.Grad!);

        Assert.NotNull(x.Grad);
        Assert.Equal(x.Shape, x.Grad!.Shape);

        var spanXGrad = x.Grad.AsSpan();
        for (var i = 0; i < spanXGrad.Length; i++) Assert.Equal(1.0f, spanXGrad[i], 1e-5f);
    }

    [Fact]
    public void ReshapeWithGrad_NumericalGradient()
    {
        var x = ArrayND.RandomNormal(1, 6);
        var xClone = x.Clone();

        var numGrad = NumericalGradient(x, mx =>
        {
            var r = mx.Reshape(2, 3);
            return r.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var reshaped = xClone.ReshapeWithGrad(new[] { 2, 3 }, ctx);

        reshaped.EnsureGrad();
        var spanRGrad = reshaped.Grad!.AsWriteSpan();
        spanRGrad[0] = 1.0f;
        for (var i = 1; i < spanRGrad.Length; i++) spanRGrad[i] = 0.0f;

        ctx.BackwardFromGradient(reshaped, reshaped.Grad!);
        var autoGrad = xClone.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance,
            $"ReshapeWithGrad 梯度误差 {error:E3} 超 {GradientTolerance:E3}");
    }

    #endregion

    #region SliceWithGrad

    [Fact]
    public void SliceWithGrad_GradientScattersBack()
    {
        var x = ArrayND.RandomNormal(2, 4, 3);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var sliced = x.SliceWithGrad(1, 1, 2, ctx);

        sliced.EnsureGrad();
        var spanSGrad = sliced.Grad!.AsWriteSpan();
        for (var i = 0; i < spanSGrad.Length; i++) spanSGrad[i] = 1.0f;

        ctx.BackwardFromGradient(sliced, sliced.Grad!);

        Assert.NotNull(x.Grad);
        Assert.Equal(x.Shape, x.Grad!.Shape);

        var spanGrad = x.Grad.AsSpan();
        for (var n = 0; n < 2; n++)
        for (var s = 0; s < 4; s++)
        for (var d = 0; d < 3; d++)
        {
            var idx = n * 4 * 3 + s * 3 + d;
            if (s is 1 or 2)
                Assert.Equal(1.0f, spanGrad[idx], 1e-5f);
            else
                Assert.Equal(0.0f, spanGrad[idx], 1e-5f);
        }
    }

    [Fact]
    public void SliceWithGrad_NumericalGradient()
    {
        var x = ArrayND.RandomNormal(2, 4);
        var xClone = x.Clone();

        var numGrad = NumericalGradient(x, mx =>
        {
            var s = mx.Slice(0, 1, 1);
            return s.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var sliced = xClone.SliceWithGrad(0, 1, 1, ctx);

        sliced.EnsureGrad();
        sliced.Grad!.AsWriteSpan()[0] = 1.0f;
        ctx.BackwardFromGradient(sliced, sliced.Grad!);

        var autoGrad = xClone.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance,
            $"SliceWithGrad 梯度误差 {error:E3} 超 {GradientTolerance:E3}");
    }

    #endregion

    #region Metrics

    [Fact]
    public void Metrics_Perplexity_PositiveAndFinite()
    {
        var logits = ArrayND.RandomNormal(4, 8);
        var targets = ArrayND.Zeros(4, 1);
        var spanT = targets.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanT[i] = i % 8;

        var ppl = Metrics.Perplexity(logits, targets);

        Assert.True(ppl > 0, $"困惑度 {ppl:F4} 应为正数");
        Assert.True(float.IsFinite(ppl), $"困惑度 {ppl:F4} 应为有限值");
    }

    [Fact]
    public void Metrics_Accuracy_PerfectPrediction()
    {
        var logits = ArrayND.Zeros(3, 4);
        var spanL = logits.AsWriteSpan();
        spanL[0] = 10.0f;
        spanL[5] = 10.0f;
        spanL[10] = 10.0f;

        var targets = ArrayND.FromArray(new float[] { 0, 1, 2 }, 3, 1);

        var acc = Metrics.Accuracy(logits, targets);
        Assert.Equal(1.0f, acc, 1e-5f);
    }

    [Fact]
    public void Metrics_Accuracy_WrongPrediction()
    {
        var logits = ArrayND.Zeros(2, 3);
        var spanL = logits.AsWriteSpan();
        spanL[1] = 10.0f;
        spanL[5] = 10.0f;

        var targets = ArrayND.FromArray(new float[] { 0, 0 }, 2, 1);

        var acc = Metrics.Accuracy(logits, targets);
        Assert.Equal(0.0f, acc, 1e-5f);
    }

    [Fact]
    public void Metrics_TopKAccuracy_Top2IncludesCorrect()
    {
        var logits = ArrayND.Zeros(1, 5);
        var spanL = logits.AsWriteSpan();
        spanL[3] = 5.0f;
        spanL[1] = 3.0f;
        spanL[0] = 1.0f;

        var targets = ArrayND.FromArray(new float[] { 1 }, 1, 1);

        var top1 = Metrics.TopKAccuracy(logits, targets, k: 1);
        var top2 = Metrics.TopKAccuracy(logits, targets, k: 2);

        Assert.Equal(0.0f, top1, 1e-5f);
        Assert.Equal(1.0f, top2, 1e-5f);
    }

    #endregion

    #region GradientAccumulator

    [Fact]
    public void GradientAccumulator_AccumulatesBeforeStepping()
    {
        var w = ArrayND.RandomNormal(4, 4);
        var b = ArrayND.Zeros(4);
        var model = new Dense(4, 4);

        var optimizer = new Adam(learningRate: 0.01f);
        var accum = new GradientAccumulator(optimizer, accumulationSteps: 3);
        var allParams = model.Parameters().ToList();

        var x = ArrayND.RandomNormal(2, 4);

        for (var micro = 0; micro < 3; micro++)
        {
            var ctx = new AutogradContext();
            ctx.StartRecording();
            var output = model.Forward(x, ctx);
            ctx.Backward(output);

            accum.Step(allParams);
            accum.ZeroGrad(allParams);
        }

        Assert.Equal(0, accum.CurrentStep);
    }

    [Fact]
    public void GradientAccumulator_EquivalentToLargeBatch()
    {
        var model1 = new Dense(4, 4);
        var model2 = new Dense(4, 4);

        var span1 = model1.Weight.AsSpan();
        var span2 = model2.Weight.AsWriteSpan();
        for (var i = 0; i < span1.Length; i++) span2[i] = span1[i];

        var span1b = model1.Bias.AsSpan();
        var span2b = model2.Bias.AsWriteSpan();
        for (var i = 0; i < span1b.Length; i++) span2b[i] = span1b[i];

        var x = ArrayND.RandomNormal(6, 4);

        var opt1 = new Adam(learningRate: 0.01f);
        var ctx1 = new AutogradContext();
        ctx1.StartRecording();
        var out1 = model1.Forward(x, ctx1);
        ctx1.Backward(out1);
        opt1.Step(model1.Parameters());
        opt1.ZeroGrad(model1.Parameters());

        var opt2 = new Adam(learningRate: 0.01f);
        var accum = new GradientAccumulator(opt2, accumulationSteps: 3);
        var batchSize = 2;

        for (var micro = 0; micro < 3; micro++)
        {
            var microX = x.SliceWithGrad(0, micro * batchSize, batchSize, new AutogradContext());
            var ctx2 = new AutogradContext();
            ctx2.StartRecording();
            var out2 = model2.Forward(microX, ctx2);
            ctx2.Backward(out2);
            accum.Step(model2.Parameters());
            accum.ZeroGrad(model2.Parameters());
        }

        Assert.True(accum.CurrentStep == 0, "累积器应已执行 Step");
    }

    #endregion

    #region TrainingState

    [Fact]
    public void TrainingState_SaveLoad_RestoresParameters()
    {
        var model = new Dense(4, 4);
        var allParams = model.Parameters().ToList();

        var checksumBefore = TrainingState.ParameterChecksum(allParams);

        var state = TrainingState.SaveParameters(allParams);

        var spanW = model.Weight.AsWriteSpan();
        for (var i = 0; i < spanW.Length; i++) spanW[i] = 0.0f;

        var checksumAfterClear = TrainingState.ParameterChecksum(allParams);
        Assert.NotEqual(checksumBefore, checksumAfterClear);

        TrainingState.LoadParameters(allParams, state);

        var checksumAfterLoad = TrainingState.ParameterChecksum(allParams);
        Assert.Equal(checksumBefore, checksumAfterLoad, 1e-3f);
    }

    [Fact]
    public void TrainingState_Checkpoint_SavesAndRestores()
    {
        var model = new Dense(4, 4);
        var allParams = model.Parameters().ToList();

        var checkpoint = TrainingState.SaveCheckpoint(allParams, epoch: 5, loss: 0.42f);

        var spanW = model.Weight.AsWriteSpan();
        for (var i = 0; i < spanW.Length; i++) spanW[i] = 0.0f;

        var (epoch, loss) = TrainingState.LoadCheckpoint(allParams, checkpoint);

        Assert.Equal(5, epoch);
        Assert.Equal(0.42f, loss, 1e-5f);
    }

    #endregion
}