namespace Galatea.Tests;

/// <summary>
///     DDPM Noise Scheduler / UNet ResBlock / ConcatWithGrad / 扩散管线集成测试
/// </summary>
public class DiffusionPipelineTests : GradientTestBase
{
    #region Mini DDPM Training

    [Fact]
    public void MiniDDPM_TrainingLossDecreases()
    {
        var scheduler =
            new DDPMScheduler(numTrainTimesteps: 100, betaStart: 0.0001f, betaEnd: 0.02f, schedule: "linear");
        var features = 32;

        var body = new Sequential(
            new Dense(features, features),
            new Dense(features, features)
        );
        var model = new ResidualBlock(body);
        var noiseHead = new Dense(features, features);

        var optimizer = new Adam(learningRate: 0.005f);
        var allParams = model.Parameters().Concat(noiseHead.Parameters());

        var x0 = ArrayND.RandomNormal(4, features);

        float initialLoss = 0;
        float finalLoss = 0;

        for (var epoch = 0; epoch < 30; epoch++)
        {
            var timesteps = scheduler.SampleTimesteps(4, seed: epoch);
            var noise = DDPMScheduler.SampleNoise(new[] { 4, features }, seed: epoch);
            var xt = scheduler.AddNoise(x0, noise, timesteps);

            var ctx = new AutogradContext();
            ctx.StartRecording();
            var features_out = model.Forward(xt, ctx);
            var predictedNoise = noiseHead.Forward(features_out, ctx);
            var loss = Losses.MSE(predictedNoise, noise, ctx);
            ctx.Backward(loss);

            if (epoch == 0) initialLoss = loss.AsSpan()[0];

            if (epoch == 29) finalLoss = loss.AsSpan()[0];

            GradientClipping.ClipGradNorm(allParams, maxNorm: 1.0f);
            optimizer.Step(allParams);
            optimizer.ZeroGrad(allParams);
        }

        Assert.True(finalLoss < initialLoss,
            $"DDPM 训练后损失 {finalLoss:F4} 应低于初始 {initialLoss:F4}");
    }

    #endregion

    #region DDPM Scheduler

    [Fact]
    public void DDPMScheduler_LinearSchedule_AlphasMonotonicallyDecrease()
    {
        var scheduler = new DDPMScheduler(numTrainTimesteps: 100, schedule: "linear");

        for (var i = 1; i < 100; i++)
            Assert.True(scheduler.AlphasCumprod[i] < scheduler.AlphasCumprod[i - 1],
                $"累积 alpha 应单调递减，但 t={i} 时 {scheduler.AlphasCumprod[i]} >= {scheduler.AlphasCumprod[i - 1]}");
    }

    [Fact]
    public void DDPMScheduler_AddNoise_MoreNoiseAtHigherTimestep()
    {
        var scheduler = new DDPMScheduler(numTrainTimesteps: 1000, schedule: "linear");

        var x0 = ArrayND.Ones(2, 8);
        var noise = ArrayND.RandomNormal(2, 8);

        var tLow = ArrayND.FromArray(new float[] { 50, 50 }, 2, 1);
        var tHigh = ArrayND.FromArray(new float[] { 900, 900 }, 2, 1);

        var xtLow = scheduler.AddNoise(x0, noise, tLow);
        var xtHigh = scheduler.AddNoise(x0, noise, tHigh);

        var diffLow = 0.0f;
        var diffHigh = 0.0f;
        var spanLow = xtLow.AsSpan();
        var spanHigh = xtHigh.AsSpan();
        var spanX0 = x0.AsSpan();
        for (var i = 0; i < spanLow.Length; i++)
        {
            diffLow += MathF.Abs(spanLow[i] - spanX0[i]);
            diffHigh += MathF.Abs(spanHigh[i] - spanX0[i]);
        }

        Assert.True(diffHigh > diffLow,
            $"高时间步噪声偏移 {diffHigh:F4} 应大于低时间步 {diffLow:F4}");
    }

    [Fact]
    public void DDPMScheduler_AddNoise_PreservesShape()
    {
        var scheduler = new DDPMScheduler(numTrainTimesteps: 100);
        var x0 = ArrayND.Ones(4, 16);
        var noise = ArrayND.RandomNormal(4, 16);
        var t = scheduler.SampleTimesteps(4, seed: 42);

        var xt = scheduler.AddNoise(x0, noise, t);

        Assert.Equal(x0.Shape, xt.Shape);
    }

    [Fact]
    public void DDPMScheduler_Step_ReducesNoise()
    {
        var scheduler = new DDPMScheduler(numTrainTimesteps: 1000, schedule: "linear");

        var predictedNoise = ArrayND.RandomNormal(2, 8);
        var xT = ArrayND.RandomNormal(2, 8);

        var xTMinus1 = scheduler.Step(predictedNoise, xT, timestep: 500);
        var x0 = scheduler.Step(predictedNoise, xT, timestep: 0);

        var spanTMinus1 = xTMinus1.AsSpan();
        var span0 = x0.AsSpan();
        var normTMinus1 = 0.0f;
        var norm0 = 0.0f;
        for (var i = 0; i < spanTMinus1.Length; i++)
        {
            normTMinus1 += spanTMinus1[i] * spanTMinus1[i];
            norm0 += span0[i] * span0[i];
        }

        Assert.True(norm0 < normTMinus1,
            $"t=0 去噪结果范数 {norm0:F4} 应小于 t=500 的 {normTMinus1:F4}");
    }

    [Fact]
    public void DDPMScheduler_SampleTimesteps_InRange()
    {
        var scheduler = new DDPMScheduler(numTrainTimesteps: 100);
        var t = scheduler.SampleTimesteps(32, seed: 42);

        var spanT = t.AsSpan();
        for (var i = 0; i < spanT.Length; i++)
            Assert.True(spanT[i] >= 0 && spanT[i] < 100,
                $"时间步 {spanT[i]} 超出范围 [0, 100)");
    }

    #endregion

    #region UNet ResBlock

    [Fact]
    public void UNetResBlock_Forward_PreservesSpatialDims()
    {
        var block = new UNetResBlock(inChannels: 4, outChannels: 4, timeEmbDim: 16, inH: 8, inW: 8);

        var x = ArrayND.RandomNormal(2, 4 * 8 * 8);
        var timeEmb = ArrayND.RandomNormal(2, 16);

        var output = block.Forward(x, timeEmb);

        Assert.Equal(new[] { 2, 4 * 8 * 8 }, output.Shape);
    }

    [Fact]
    public void UNetResBlock_Forward_ChannelChange()
    {
        var block = new UNetResBlock(inChannels: 4, outChannels: 8, timeEmbDim: 16, inH: 8, inW: 8);

        var x = ArrayND.RandomNormal(2, 4 * 8 * 8);
        var timeEmb = ArrayND.RandomNormal(2, 16);

        var output = block.Forward(x, timeEmb);

        Assert.Equal(new[] { 2, 8 * 8 * 8 }, output.Shape);
    }

    [Fact]
    public void UNetResBlock_Autograd_GradientsValid()
    {
        var block = new UNetResBlock(inChannels: 2, outChannels: 2, timeEmbDim: 8, numGroups: 1, inH: 4, inW: 4);

        var x = ArrayND.RandomNormal(2, 2 * 4 * 4);
        var timeEmb = ArrayND.RandomNormal(2, 8);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = block.Forward(x, timeEmb, ctx);
        ctx.Backward(output);

        foreach (var p in block.Parameters())
        {
            if (p.Grad == null) continue;

            var span = p.Grad.AsSpan();
            for (var i = 0; i < span.Length; i++) Assert.False(float.IsNaN(span[i]), "参数梯度包含 NaN");
        }
    }

    #endregion

    #region ConcatWithGrad

    [Fact]
    public void ConcatWithGrad_Axis0_CorrectShape()
    {
        var a = ArrayND.Ones(3, 4);
        var b = ArrayND.Ones(5, 4);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var result = ArrayND.ConcatWithGrad(0, ctx, a, b);

        Assert.Equal(new[] { 8, 4 }, result.Shape);
    }

    [Fact]
    public void ConcatWithGrad_Gradient_SplitsCorrectly()
    {
        var a = ArrayND.RandomNormal(3, 4);
        var b = ArrayND.RandomNormal(5, 4);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var result = ArrayND.ConcatWithGrad(0, ctx, a, b);

        var loss = result;
        ctx.Backward(loss);

        Assert.NotNull(a.Grad);
        Assert.NotNull(b.Grad);

        var spanAGrad = a.Grad!.AsSpan();
        var spanBGrad = b.Grad!.AsSpan();

        Assert.Equal(1.0f, spanAGrad[0], 1e-5f);
        for (var i = 1; i < spanAGrad.Length; i++) Assert.Equal(0.0f, spanAGrad[i], 1e-5f);
        for (var i = 0; i < spanBGrad.Length; i++) Assert.Equal(0.0f, spanBGrad[i], 1e-5f);
    }

    #endregion
}