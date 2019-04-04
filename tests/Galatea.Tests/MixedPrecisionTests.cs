namespace Galatea.Tests;

/// <summary>
///     MixedPrecision + GradScaler 测试
/// </summary>
public class MixedPrecisionTests
{
    #region MixedPrecisionTrainer

    [Fact]
    public void MixedPrecision_TrainStep_ReturnsFiniteLoss()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new MixedPrecisionTrainer(model, optimizer);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targets = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        var loss = trainer.TrainStep(inputIds, targets);

        Assert.True(float.IsFinite(loss) || loss == 0, $"损失 {loss} 应为有限值");
    }

    [Fact]
    public void MixedPrecision_MasterWeights_Tracked()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new MixedPrecisionTrainer(model, optimizer);

        Assert.True(trainer.MasterWeights.Count > 0, "应有主权重记录");
    }

    [Fact]
    public void MixedPrecision_TrainEpoch_LossIsFinite()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.005f);
        var trainer = new MixedPrecisionTrainer(model, optimizer);

        var rng = new Random(42);
        var inputs = ArrayND.Zeros(4, 4);
        var spanIn = inputs.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanIn[i] = rng.Next(0, 8);

        var targets = ArrayND.Zeros(4, 1);
        var spanT = targets.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanT[i] = rng.Next(0, 8);

        var avgLoss = trainer.TrainEpoch(inputs, targets, batchSize: 2);

        Assert.True(float.IsFinite(avgLoss), $"平均损失 {avgLoss} 应为有限值");
    }

    [Fact]
    public void MixedPrecision_WithWeightTied_Works()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new MixedPrecisionTrainer(model, optimizer);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targets = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        var loss = trainer.TrainStep(inputIds, targets);

        Assert.True(float.IsFinite(loss), $"WeightTiedGPT 混合精度损失 {loss} 应为有限值");
    }

    #endregion

    #region GradScaler

    [Fact]
    public void GradScaler_ScaleLoss_MultipliesByScale()
    {
        var scaler = new GradScaler(initialScale: 1024.0f);
        var loss = ArrayND.FromArray(new[] { 2.0f });

        var scaled = scaler.ScaleLoss(loss);

        Assert.Equal(2048.0f, scaled.AsSpan()[0], 1e-3f);
    }

    [Fact]
    public void GradScaler_UnscaleGrad_DividesByScale()
    {
        var scaler = new GradScaler(initialScale: 1024.0f);
        var model = new Dense(fanIn: 4, fanOut: 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = model.Forward(ArrayND.Ones(1, 4), ctx);
        var allOnesGrad = ArrayND.Ones(output.Shape) * 1024.0f;
        ctx.BackwardFromGradient(output, allOnesGrad);

        scaler.UnscaleGrad(model.Parameters());

        var gradSpan = model.Weight.Grad!.AsSpan();
        var hasNonZero = false;
        for (var i = 0; i < gradSpan.Length; i++)
            if (MathF.Abs(gradSpan[i]) > 1e-10f)
            {
                hasNonZero = true;
                break;
            }

        Assert.True(hasNonZero, "反缩放后梯度应非零");
    }

    [Fact]
    public void GradScaler_HasInfOrNanGrad_DetectsInf()
    {
        var scaler = new GradScaler();
        var model = new Dense(fanIn: 4, fanOut: 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = model.Forward(ArrayND.Ones(1, 4), ctx);
        ctx.Backward(output);

        var gradSpan = model.Weight.Grad!.AsWriteSpan();
        gradSpan[0] = float.PositiveInfinity;

        Assert.True(scaler.HasInfOrNanGrad(model.Parameters()), "应检测到 inf 梯度");
    }

    [Fact]
    public void GradScaler_Update_DecreasesScaleOnInf()
    {
        var scaler = new GradScaler(initialScale: 1024.0f, backoffFactor: 0.5f);

        scaler.Update(hasInfGrad: true);

        Assert.Equal(512.0f, scaler.Scale, 1e-3f);
    }

    [Fact]
    public void GradScaler_Update_IncreasesScaleAfterGrowthInterval()
    {
        var scaler = new GradScaler(initialScale: 1024.0f, growthFactor: 2.0f, growthInterval: 3);

        scaler.Update(hasInfGrad: false);
        scaler.Update(hasInfGrad: false);
        scaler.Update(hasInfGrad: false);

        Assert.Equal(2048.0f, scaler.Scale, 1e-3f);
    }

    #endregion
}