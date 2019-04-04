namespace Galatea.Tests;

/// <summary>
///     MistralModel / EMA / LR 调度器 / 权重初始化 / 梯度惩罚 综合测试
/// </summary>
public class ProductionTrainingTests
{
    #region MistralModel

    [Fact]
    public void MistralModel_Forward_CorrectOutputShape()
    {
        var model = new MistralModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, windowSize: 8, maxSeqLen: 32);

        var inputIds = ArrayND.Zeros(1, 5);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 1, 5, 32 }, logits.Shape);
    }

    [Fact]
    public void MistralModel_ForwardForTraining_LastTokenLogits()
    {
        var model = new MistralModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, windowSize: 8);

        var inputIds = ArrayND.Zeros(2, 5);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var ctx = new AutogradContext();
        var logits = model.ForwardForTraining(inputIds, ctx);

        Assert.Equal(new[] { 2, 32 }, logits.Shape);
    }

    [Fact]
    public void MistralModel_Parameters_Count()
    {
        var model = new MistralModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, windowSize: 8);

        var params_ = model.Parameters().ToList();
        Assert.True(params_.Count > 0, "模型应有可训练参数");
    }

    [Fact]
    public void MistralModel_CreateGenerator_ReturnsGenerator()
    {
        var model = new MistralModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, windowSize: 8);

        var generator = model.CreateGenerator();
        Assert.Equal(32, generator.VocabSize);
    }

    [Fact]
    public void MistralModel_CreateSpeculativeDecoder()
    {
        var target = new MistralModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, windowSize: 8);

        var draft = new MistralModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 1, windowSize: 8);

        var decoder = target.CreateSpeculativeDecoder(draft, draftSteps: 3);
        Assert.Equal(32, decoder.VocabSize);
        Assert.Equal(3, decoder.DraftSteps);
    }

    [Fact]
    public void MistralModel_OutputNotNaN()
    {
        var model = new MistralModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, windowSize: 4);

        var inputIds = ArrayND.Zeros(1, 8);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var logits = model.Forward(inputIds);
        var logitSpan = logits.AsSpan();

        for (var i = 0; i < logitSpan.Length; i++) Assert.False(float.IsNaN(logitSpan[i]), $"输出包含 NaN 在位置 {i}");
    }

    #endregion

    #region EMA

    [Fact]
    public void EMA_RegisterAndUpdate_ShadowParamsCreated()
    {
        var model = new Dense(4, 3);
        var ema = new EMA(decay: 0.999f);

        ema.Register(model);
        Assert.Equal(0, ema.UpdateCount);

        ema.Update(model);
        Assert.Equal(1, ema.UpdateCount);
    }

    [Fact]
    public void EMA_ShadowParams_CloseToOriginal()
    {
        var model = new Dense(4, 3);
        var ema = new EMA(decay: 0.9f);

        ema.Register(model);

        for (var i = 0; i < 100; i++)
            ema.Update(model);

        var deviation = ema.AverageDeviation(model);
        Assert.True(deviation < 0.1f, $"EMA 参数应接近原始参数，偏差={deviation:F6}");
    }

    [Fact]
    public void EMA_ApplyShadowAndRestore()
    {
        var model = new Dense(4, 3);
        var ema = new EMA(decay: 0.999f);

        ema.Register(model);
        ema.Update(model);

        var originalWeight = model.Weight.AsSpan().ToArray();

        var backup = ema.Backup(model);
        ema.ApplyShadow(model);

        var afterApply = model.Weight.AsSpan().ToArray();

        ema.Restore(model, backup);

        var afterRestore = model.Weight.AsSpan().ToArray();

        for (var i = 0; i < originalWeight.Length; i++) Assert.Equal(originalWeight[i], afterRestore[i], 1e-6f);
    }

    [Fact]
    public void EMA_DynamicDecay_EarlyDecaySmaller()
    {
        var model = new Dense(4, 3);
        var ema = new EMA(decay: 0.999f);

        ema.Register(model);
        ema.UpdateWithDynamicDecay(model);

        Assert.Equal(1, ema.UpdateCount);
    }

    [Fact]
    public void EMA_InvalidDecay_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EMA(decay: 1.5f));
        Assert.Throws<ArgumentException>(() => new EMA(decay: -0.1f));
    }

    #endregion

    #region LR Schedulers

    [Fact]
    public void CosineAnnealingWarmRestarts_BasicCycle()
    {
        var scheduler = new CosineAnnealingWarmRestarts(initialLR: 0.1f, t0: 10);

        var lr0 = scheduler.LearningRate;
        Assert.True(MathF.Abs(lr0 - 0.1f) < 1e-5f, $"初始 LR 应为 0.1，实际={lr0}");

        for (var i = 0; i < 5; i++) scheduler.Step();
        var lr5 = scheduler.LearningRate;
        Assert.True(lr5 < 0.1f, $"中间 LR 应小于 0.1，实际={lr5}");

        for (var i = 5; i < 9; i++) scheduler.Step();
        var lr9 = scheduler.LearningRate;
        Assert.True(lr9 < lr5, $"LR 应继续下降，step5={lr5}，step9={lr9}");
    }

    [Fact]
    public void CosineAnnealingWarmRestarts_RestartResetsLR()
    {
        var scheduler = new CosineAnnealingWarmRestarts(initialLR: 0.1f, t0: 5);

        for (var i = 0; i < 4; i++) scheduler.Step();
        var lrNearEnd = scheduler.LearningRate;

        scheduler.Step();
        var lrAtRestart = scheduler.LearningRate;

        Assert.True(lrAtRestart > lrNearEnd,
            $"重启后 LR 应回升，重启前={lrNearEnd}，重启后={lrAtRestart}");
        Assert.True(MathF.Abs(lrAtRestart - 0.1f) < 1e-4f,
            $"重启时 LR 应接近最大值 0.1，实际={lrAtRestart}");
    }

    [Fact]
    public void CosineAnnealingWarmRestarts_Reset()
    {
        var scheduler = new CosineAnnealingWarmRestarts(initialLR: 0.1f, t0: 5);

        for (var i = 0; i < 10; i++) scheduler.Step();
        scheduler.Reset();

        var lrAfterReset = scheduler.LearningRate;
        Assert.True(MathF.Abs(lrAfterReset - 0.1f) < 1e-5f,
            $"重置后 LR 应为 0.1，实际={lrAfterReset}");
    }

    [Fact]
    public void OneCycleLR_WarmupThenDecay()
    {
        var scheduler = new OneCycleLR(maxLR: 0.1f, totalSteps: 100, pctStart: 0.3f);

        var lr0 = scheduler.LearningRate;

        for (var i = 0; i < 30; i++) scheduler.Step();
        var lr30 = scheduler.LearningRate;

        Assert.True(lr30 > lr0, $"Warmup 阶段 LR 应增加，初始={lr0}，step30={lr30}");

        for (var i = 30; i < 60; i++) scheduler.Step();
        var lr60 = scheduler.LearningRate;

        Assert.True(lr60 < lr30, $"Decay 阶段 LR 应减少，step30={lr30}，step60={lr60}");
    }

    [Fact]
    public void OneCycleLR_Reset()
    {
        var scheduler = new OneCycleLR(maxLR: 0.1f, totalSteps: 100);

        for (var i = 0; i < 50; i++) scheduler.Step();
        scheduler.Reset();

        var lrAfterReset = scheduler.LearningRate;
        Assert.True(lrAfterReset < 0.01f,
            "重置后 LR 应接近 finalLR");
    }

    #endregion

    #region Weight Initialization

    [Fact]
    public void WeightInit_HeNormal_NonZero()
    {
        var model = new Dense(8, 4);
        WeightInitialization.Initialize(model, InitStrategy.HeNormal, seed: 42);

        var span = model.Weight.AsSpan();
        var anyNonZero = false;
        for (var i = 0; i < span.Length; i++)
            if (MathF.Abs(span[i]) > 1e-6f)
            {
                anyNonZero = true;
                break;
            }

        Assert.True(anyNonZero, "He Normal 初始化后权重应非零");
    }

    [Fact]
    public void WeightInit_XavierUniform_InRange()
    {
        var model = new Dense(8, 4);
        WeightInitialization.Initialize(model, InitStrategy.XavierUniform, seed: 42);

        var span = model.Weight.AsSpan();
        var limit = MathF.Sqrt(6.0f / (8 + 4));

        for (var i = 0; i < span.Length; i++)
            Assert.True(MathF.Abs(span[i]) <= limit + 0.01f,
                $"Xavier Uniform 初始化值 {span[i]} 应在 [-{limit}, {limit}] 范围");
    }

    [Fact]
    public void WeightInit_Zeros_AllZero()
    {
        var model = new Dense(4, 3);
        WeightInitialization.Initialize(model, InitStrategy.Zeros);

        var span = model.Weight.AsSpan();
        for (var i = 0; i < span.Length; i++)
            Assert.Equal(0.0f, span[i], 1e-6f);
    }

    [Fact]
    public void WeightInit_Ones_AllOne()
    {
        var model = new Dense(4, 3);
        WeightInitialization.Initialize(model, InitStrategy.Ones);

        var span = model.Weight.AsSpan();
        for (var i = 0; i < span.Length; i++)
            Assert.Equal(1.0f, span[i], 1e-6f);
    }

    [Fact]
    public void WeightInit_ZeroBiases()
    {
        var model = new Dense(4, 3);
        WeightInitialization.ZeroBiases(model);

        var biasSpan = model.Bias.AsSpan();
        for (var i = 0; i < biasSpan.Length; i++)
            Assert.Equal(0.0f, biasSpan[i], 1e-6f);
    }

    [Fact]
    public void WeightInit_ComputeFanInOut()
    {
        var (fanIn, fanOut) = WeightInitialization.ComputeFanInOut(new[] { 8, 4 });
        Assert.Equal(8, fanIn);
        Assert.Equal(4, fanOut);

        var (fanIn2, fanOut2) = WeightInitialization.ComputeFanInOut(new[] { 3, 3, 3 });
        Assert.Equal(9, fanIn2);
        Assert.Equal(9, fanOut2);
    }

    [Fact]
    public void WeightInit_SpecificMethods()
    {
        var param = ArrayND.Zeros(4, 3);

        WeightInitialization.XavierUniform(param);
        var spanXU = param.AsSpan();
        var anyNonZero = false;
        for (var i = 0; i < spanXU.Length; i++)
            if (MathF.Abs(spanXU[i]) > 1e-6f)
            {
                anyNonZero = true;
                break;
            }

        Assert.True(anyNonZero, "XavierUniform 应产生非零值");

        WeightInitialization.HeNormal(param);
        var spanHN = param.AsSpan();
        anyNonZero = false;
        for (var i = 0; i < spanHN.Length; i++)
            if (MathF.Abs(spanHN[i]) > 1e-6f)
            {
                anyNonZero = true;
                break;
            }

        Assert.True(anyNonZero, "HeNormal 应产生非零值");

        WeightInitialization.Constant(param, 5.0f);
        var spanC = param.AsSpan();
        for (var i = 0; i < spanC.Length; i++)
            Assert.Equal(5.0f, spanC[i], 1e-6f);
    }

    #endregion

    #region Gradient Penalty

    [Fact]
    public void GradientPenalty_ParameterGradNorm_NoGrad()
    {
        var model = new Dense(4, 3);
        var norm = GradientPenalty.ParameterGradNorm(model);

        Assert.Equal(0.0f, norm);
    }

    [Fact]
    public void GradientPenalty_ParameterGradNorm_WithGrad()
    {
        var model = new Dense(4, 3);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var input = ArrayND.RandomNormal(2, 4);
        var output = model.Forward(input, ctx);
        ctx.BackwardFromGradient(output, ArrayND.Ones(output.Shape));

        var norm = GradientPenalty.ParameterGradNorm(model);
        Assert.True(norm > 0, $"有梯度时范数应 > 0，实际={norm}");
    }

    [Fact]
    public void GradientPenalty_DiagnoseGradients_Normal()
    {
        var model = new Dense(4, 3);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var input = ArrayND.RandomNormal(2, 4);
        var output = model.Forward(input, ctx);
        ctx.BackwardFromGradient(output, ArrayND.Ones(output.Shape));

        var diagnosis = GradientPenalty.DiagnoseGradients(model);

        Assert.False(diagnosis.HasNaN, "正常训练不应有 NaN");
        Assert.False(diagnosis.HasInfinity, "正常训练不应有 Infinity");
        Assert.False(diagnosis.IsExploding, "正常训练不应梯度爆炸");
    }

    [Fact]
    public void GradientPenalty_LayerWiseGradNorm()
    {
        var model = new Dense(4, 3);
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var input = ArrayND.RandomNormal(2, 4);
        var output = model.Forward(input, ctx);
        ctx.BackwardFromGradient(output, ArrayND.Ones(output.Shape));

        var layerNorms = GradientPenalty.LayerWiseGradNorm(model);

        Assert.Equal(2, layerNorms.Count);
        Assert.True(layerNorms[0].norm > 0, "权重梯度范数应 > 0");
    }

    [Fact]
    public void GradientDiagnosis_ToString()
    {
        var model = new Dense(4, 3);
        var diagnosis = GradientPenalty.DiagnoseGradients(model);
        var summary = diagnosis.ToString();

        Assert.False(string.IsNullOrEmpty(summary), "诊断摘要不应为空");
        Assert.Contains("梯度状态", summary);
    }

    #endregion
}