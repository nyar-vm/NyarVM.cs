namespace Galatea.Tests;

/// <summary>
///     PaddingMask / LanguageModelTrainer / Profiler 集成测试
/// </summary>
public class TrainingInfrastructureTests
{
    #region PaddingMask

    [Fact]
    public void PaddingMask_Create_PadsCorrectPositions()
    {
        var seqLengths = ArrayND.FromArray(new float[] { 3, 5 }, 2);
        var mask = PaddingMask.Create(seqLengths, maxSeqLen: 6);

        Assert.Equal(new[] { 2, 1, 1, 6 }, mask.Shape);

        var span = mask.AsSpan();
        for (var n = 0; n < 2; n++)
        {
            var len = (int)seqLengths.AsSpan()[n];
            for (var s = 0; s < 6; s++)
            {
                var val = span[n * 6 + s];
                if (s >= len)
                    Assert.True(float.IsNegativeInfinity(val),
                        $"batch {n}, pos {s} (>= len {len}) 应为 -inf");
                else
                    Assert.Equal(0.0f, val);
            }
        }
    }

    [Fact]
    public void PaddingMask_CausalWithPadding_CombinesBoth()
    {
        var seqLengths = ArrayND.FromArray(new float[] { 3 }, 1);
        var mask = PaddingMask.CreateCausalWithPadding(seqLengths, maxSeqLen: 4);

        Assert.Equal(new[] { 1, 1, 4, 4 }, mask.Shape);

        var span = mask.AsSpan();

        Assert.Equal(0.0f, span[0 * 4 + 0]);

        Assert.True(float.IsNegativeInfinity(span[0 * 4 + 1]));

        Assert.Equal(0.0f, span[2 * 4 + 0]);

        Assert.True(float.IsNegativeInfinity(span[3 * 4 + 0]));

        Assert.True(float.IsNegativeInfinity(span[3 * 4 + 3]));
    }

    [Fact]
    public void PaddingMask_FromInputIds_PadsCorrectTokens()
    {
        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 0, 0, 3, 4, 5, 0 }, 2, 4);
        var mask = PaddingMask.FromInputIds(inputIds, padTokenId: 0);

        Assert.Equal(new[] { 2, 1, 1, 4 }, mask.Shape);

        var span = mask.AsSpan();
        Assert.True(float.IsNegativeInfinity(span[0 * 4 + 2]));
        Assert.True(float.IsNegativeInfinity(span[0 * 4 + 3]));
        Assert.Equal(0.0f, span[0 * 4 + 0]);
        Assert.Equal(0.0f, span[0 * 4 + 1]);

        Assert.True(float.IsNegativeInfinity(span[1 * 4 + 3]));
        Assert.Equal(0.0f, span[1 * 4 + 0]);
    }

    [Fact]
    public void PaddingMask_CausalFromInputIds_CombinesBoth()
    {
        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 0 }, 1, 3);
        var mask = PaddingMask.CreateCausalFromInputIds(inputIds, padTokenId: 0);

        Assert.Equal(new[] { 1, 1, 3, 3 }, mask.Shape);

        var span = mask.AsSpan();

        Assert.Equal(0.0f, span[0 * 3 + 0]);

        Assert.True(float.IsNegativeInfinity(span[0 * 3 + 1]));

        Assert.Equal(0.0f, span[1 * 3 + 0]);
        Assert.Equal(0.0f, span[1 * 3 + 1]);

        Assert.True(float.IsNegativeInfinity(span[1 * 3 + 2]));

        Assert.True(float.IsNegativeInfinity(span[2 * 3 + 0]));
        Assert.True(float.IsNegativeInfinity(span[2 * 3 + 1]));
        Assert.True(float.IsNegativeInfinity(span[2 * 3 + 2]));
    }

    [Fact]
    public void PaddingMask_ExpandCausalMask_ReplicatesForBatch()
    {
        var causalMask = TransformerDecoderLayer.CreateCausalMask(4);
        var expanded = PaddingMask.ExpandCausalMask(causalMask, batch: 3);

        Assert.Equal(new[] { 3, 1, 4, 4 }, expanded.Shape);

        var spanSrc = causalMask.AsSpan();
        var spanDst = expanded.AsSpan();

        for (var n = 0; n < 3; n++)
        for (var i = 0; i < 16; i++)
            Assert.Equal(spanSrc[i], spanDst[n * 16 + i]);
    }

    #endregion

    #region LanguageModelTrainer

    [Fact]
    public void LanguageModelTrainer_TrainStep_ReturnsFiniteLoss()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new LanguageModelTrainer(model, optimizer);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targets = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        var loss = trainer.TrainStep(inputIds, targets);

        Assert.True(float.IsFinite(loss), $"损失 {loss} 应为有限值");
        Assert.True(loss > 0, $"损失 {loss} 应为正数");
    }

    [Fact]
    public void LanguageModelTrainer_TrainEpoch_LossDecreases()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.005f);
        var scheduler = new CosineDecayWithWarmup(peakLR: 0.005f, warmupSteps: 2, totalSteps: 40);
        var trainer = new LanguageModelTrainer(model, optimizer, lrScheduler: scheduler, maxGradNorm: 1.0f);

        var rng = new Random(42);
        var inputs = ArrayND.Zeros(4, 4);
        var spanIn = inputs.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanIn[i] = rng.Next(0, 8);

        var targets = ArrayND.Zeros(4, 1);
        var spanT = targets.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanT[i] = rng.Next(0, 8);

        var loss1 = trainer.TrainEpoch(inputs, targets, batchSize: 2);
        for (var i = 0; i < 9; i++) trainer.TrainEpoch(inputs, targets, batchSize: 2);
        var loss10 = trainer.TrainEpoch(inputs, targets, batchSize: 2);

        Assert.True(loss10 < loss1,
            $"10 epoch 后损失 {loss10:F4} 应低于第 1 epoch 损失 {loss1:F4}");
    }

    [Fact]
    public void LanguageModelTrainer_WithWeightTiedModel_Works()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.005f);
        var trainer = new LanguageModelTrainer(model, optimizer, maxGradNorm: 1.0f);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targets = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        var loss = trainer.TrainStep(inputIds, targets);

        Assert.True(float.IsFinite(loss), $"WeightTiedGPT 损失 {loss} 应为有限值");
    }

    [Fact]
    public void LanguageModelTrainer_Evaluate_ReturnsPerplexity()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new LanguageModelTrainer(model, optimizer);

        var inputs = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 2, 2);
        var targets = ArrayND.FromArray(new float[] { 3, 5 }, 2, 1);

        var result = trainer.Evaluate(inputs, targets, batchSize: 2);

        Assert.True(result.Loss > 0, $"评估损失 {result.Loss} 应为正数");
        Assert.True(result.Perplexity > 1, $"困惑度 {result.Perplexity:F2} 应大于 1");
        Assert.True(float.IsFinite(result.Perplexity), $"困惑度 {result.Perplexity} 应为有限值");
    }

    [Fact]
    public void LanguageModelTrainer_Fit_ProducesHistory()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.005f);
        var trainer = new LanguageModelTrainer(model, optimizer, checkpointInterval: 2);

        var rng = new Random(42);
        var trainInputs = ArrayND.Zeros(4, 4);
        var spanTI = trainInputs.AsWriteSpan();
        for (var i = 0; i < spanTI.Length; i++) spanTI[i] = rng.Next(0, 8);

        var trainLabels = ArrayND.Zeros(4, 1);
        var spanTL = trainLabels.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanTL[i] = rng.Next(0, 8);

        var evalInputs = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 2, 2);
        var evalLabels = ArrayND.FromArray(new float[] { 3, 5 }, 2, 1);

        var history = trainer.Fit(trainInputs, trainLabels, evalInputs, evalLabels, epochs: 5, batchSize: 2);

        Assert.Equal(5, history.TrainLosses.Count);
        Assert.Equal(5, history.EvalLosses.Count);
        Assert.Equal(5, history.Perplexities.Count);
        Assert.True(history.Checkpoints.Count > 0, "应有检查点");
    }

    [Fact]
    public void LanguageModelTrainer_GradientAccumulation_Works()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new LanguageModelTrainer(model, optimizer, accumulationSteps: 2);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targets = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        var loss1 = trainer.TrainStep(inputIds, targets);
        var loss2 = trainer.TrainStep(inputIds, targets);

        Assert.True(float.IsFinite(loss1), $"累积步 1 损失 {loss1} 应为有限值");
        Assert.True(float.IsFinite(loss2), $"累积步 2 损失 {loss2} 应为有限值");
    }

    [Fact]
    public void LanguageModelTrainer_CheckpointRestore_PreservesState()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.005f);
        var trainer = new LanguageModelTrainer(model, optimizer, checkpointInterval: 1);

        var inputIds = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targets = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        trainer.TrainStep(inputIds, targets);
        var checksumBefore = TrainingState.ParameterChecksum(model.Parameters());

        var checkpoint = TrainingState.SaveCheckpoint(model.Parameters(), epoch: 0, loss: 2.0f);

        for (var i = 0; i < 5; i++) trainer.TrainStep(inputIds, targets);
        var checksumAfter = TrainingState.ParameterChecksum(model.Parameters());

        Assert.NotEqual(checksumBefore, checksumAfter);

        TrainingState.LoadCheckpoint(model.Parameters(), checkpoint);
        var checksumRestored = TrainingState.ParameterChecksum(model.Parameters());

        Assert.Equal(checksumBefore, checksumRestored);
    }

    #endregion

    #region Profiler

    [Fact]
    public void Profiler_RecordStep_TracksMetrics()
    {
        var profiler = new Profiler();

        profiler.RecordStep(step: 1, batchSize: 4, seqLen: 8, loss: 2.5f, elapsedMs: 100);
        profiler.RecordStep(step: 2, batchSize: 4, seqLen: 8, loss: 2.3f, elapsedMs: 95);

        Assert.Equal(2, profiler.Entries.Count);
        Assert.Equal(64, profiler.TotalTokens);
        Assert.Equal(2, profiler.TotalSteps);

        Assert.Equal(2.5f, profiler.Entries[0].Loss);
        Assert.Equal(2.3f, profiler.Entries[1].Loss);
    }

    [Fact]
    public void Profiler_AverageThroughput_IsPositive()
    {
        var profiler = new Profiler();

        profiler.RecordStep(step: 1, batchSize: 2, seqLen: 4, loss: 3.0f, elapsedMs: 50);

        var avgLatency = profiler.AverageStepLatency();

        Assert.True(avgLatency > 0, $"平均延迟 {avgLatency} 应为正数");

        var entryThroughput = profiler.Entries[0].ThroughputTokensPerSec;
        Assert.True(entryThroughput > 0, $"条目吞吐量 {entryThroughput} 应为正数");
    }

    [Fact]
    public void Profiler_AverageStepLatency_IsCorrect()
    {
        var profiler = new Profiler();

        profiler.RecordStep(step: 1, batchSize: 2, seqLen: 4, loss: 3.0f, elapsedMs: 100);
        profiler.RecordStep(step: 2, batchSize: 2, seqLen: 4, loss: 2.8f, elapsedMs: 200);

        var latency = profiler.AverageStepLatency();

        Assert.Equal(150.0, latency, 1e-6);
    }

    [Fact]
    public void Profiler_Summary_ContainsInfo()
    {
        var profiler = new Profiler();

        profiler.RecordStep(step: 1, batchSize: 4, seqLen: 8, loss: 2.5f, elapsedMs: 100);

        var summary = profiler.Summary();

        Assert.Contains("总步数: 1", summary);
        Assert.Contains("总 token: 32", summary);
        Assert.Contains("tokens/s", summary);
    }

    [Fact]
    public void Profiler_Reset_ClearsState()
    {
        var profiler = new Profiler();

        profiler.RecordStep(step: 1, batchSize: 4, seqLen: 8, loss: 2.5f, elapsedMs: 100);
        profiler.Reset();

        Assert.Equal(0, profiler.TotalSteps);
        Assert.Equal(0, profiler.TotalTokens);
        Assert.Empty(profiler.Entries);
    }

    [Fact]
    public void Profiler_PeakMemory_IsPositive()
    {
        var profiler = new Profiler();

        var mem = profiler.PeakMemoryBytes();

        Assert.True(mem > 0, $"峰值内存 {mem} 应为正数");
    }

    #endregion
}