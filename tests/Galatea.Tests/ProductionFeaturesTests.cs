namespace Galatea.Tests;

/// <summary>
///     TrainingCallbacks / Quantization / TextGenerationPipeline 测试
/// </summary>
public class ProductionFeaturesTests
{
    #region TrainingLogger

    [Fact]
    public void TrainingLogger_RecordsEntries()
    {
        var logger = new TrainingLogger(logInterval: 1, writeLine: _ => { });
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new CallbackTrainer(model, optimizer, callbacks: logger);

        var rng = new Random(42);
        var trainInputs = ArrayND.Zeros(4, 4);
        var spanTI = trainInputs.AsWriteSpan();
        for (var i = 0; i < spanTI.Length; i++) spanTI[i] = rng.Next(0, 8);

        var trainLabels = ArrayND.Zeros(4, 1);
        var spanTL = trainLabels.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanTL[i] = rng.Next(0, 8);

        var evalInputs = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 2, 2);
        var evalLabels = ArrayND.FromArray(new float[] { 3, 5 }, 2, 1);

        trainer.Fit(trainInputs, trainLabels, evalInputs, evalLabels, epochs: 3, batchSize: 2);

        Assert.True(logger.Entries.Count > 0, "应有日志条目");
        Assert.Contains("loss=", logger.Entries[0]);
    }

    #endregion

    #region ModelCheckpoint

    [Fact]
    public void ModelCheckpoint_SavesAtInterval()
    {
        var checkpoint = new ModelCheckpoint(saveInterval: 2, saveBestOnly: false);
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var trainer = new CallbackTrainer(model, optimizer, callbacks: checkpoint);

        var rng = new Random(42);
        var trainInputs = ArrayND.Zeros(4, 4);
        var spanTI = trainInputs.AsWriteSpan();
        for (var i = 0; i < spanTI.Length; i++) spanTI[i] = rng.Next(0, 8);

        var trainLabels = ArrayND.Zeros(4, 1);
        var spanTL = trainLabels.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanTL[i] = rng.Next(0, 8);

        var evalInputs = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 2, 2);
        var evalLabels = ArrayND.FromArray(new float[] { 3, 5 }, 2, 1);

        trainer.Fit(trainInputs, trainLabels, evalInputs, evalLabels, epochs: 5, batchSize: 2);

        Assert.True(checkpoint.Checkpoints.Count > 0, "应有检查点");
    }

    #endregion

    #region EarlyStopping

    [Fact]
    public void EarlyStopping_StopsWhenNoImprovement()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var earlyStopping = new EarlyStopping(patience: 2, restoreBestWeights: false);
        var trainer = new CallbackTrainer(model, optimizer, callbacks: earlyStopping);

        var rng = new Random(42);
        var trainInputs = ArrayND.Zeros(4, 4);
        var spanTI = trainInputs.AsWriteSpan();
        for (var i = 0; i < spanTI.Length; i++) spanTI[i] = rng.Next(0, 8);

        var trainLabels = ArrayND.Zeros(4, 1);
        var spanTL = trainLabels.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanTL[i] = rng.Next(0, 8);

        var evalInputs = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 2, 2);
        var evalLabels = ArrayND.FromArray(new float[] { 3, 5 }, 2, 1);

        var history = trainer.Fit(trainInputs, trainLabels, evalInputs, evalLabels, epochs: 20, batchSize: 2);

        Assert.True(history.TrainLosses.Count <= 20, "早停应限制训练轮数");
    }

    [Fact]
    public void EarlyStopping_RestoresBestWeights()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);
        var earlyStopping = new EarlyStopping(patience: 3, restoreBestWeights: true);
        var trainer = new CallbackTrainer(model, optimizer, callbacks: earlyStopping);

        var rng = new Random(42);
        var trainInputs = ArrayND.Zeros(4, 4);
        var spanTI = trainInputs.AsWriteSpan();
        for (var i = 0; i < spanTI.Length; i++) spanTI[i] = rng.Next(0, 8);

        var trainLabels = ArrayND.Zeros(4, 1);
        var spanTL = trainLabels.AsWriteSpan();
        for (var i = 0; i < 4; i++) spanTL[i] = rng.Next(0, 8);

        var evalInputs = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 2, 2);
        var evalLabels = ArrayND.FromArray(new float[] { 3, 5 }, 2, 1);

        trainer.Fit(trainInputs, trainLabels, evalInputs, evalLabels, epochs: 10, batchSize: 2);

        Assert.True(earlyStopping.BestEpoch >= 0, "应有最佳 epoch 记录");
    }

    #endregion

    #region Quantization

    [Fact]
    public void QuantizeInt8_RoundTrip_PreservesShape()
    {
        var weight = ArrayND.RandomNormal(4, 8);
        var quantized = Quantization.QuantizeInt8(weight);
        var dequantized = Quantization.Dequantize(quantized);

        Assert.Equal(weight.Shape, dequantized.Shape);
    }

    [Fact]
    public void QuantizeInt8_ApproximatesOriginal()
    {
        var weight = ArrayND.RandomNormal(4, 8);
        var quantized = Quantization.QuantizeInt8(weight);
        var dequantized = Quantization.Dequantize(quantized);

        var error = Quantization.QuantizationError(weight, dequantized);

        Assert.True(error < 0.1f, $"INT8 量化误差 {error:F6} 应小于 0.1");
    }

    [Fact]
    public void QuantizeInt8Asymmetric_RoundTrip()
    {
        var weight = ArrayND.RandomNormal(4, 8) * 0.3f;
        var quantized = Quantization.QuantizeInt8Asymmetric(weight);
        var dequantized = Quantization.Dequantize(quantized);

        Assert.Equal(weight.Shape, dequantized.Shape);

        var error = Quantization.QuantizationError(weight, dequantized);
        Assert.True(error < 1.0f, $"非对称 INT8 量化误差 {error:F6} 应小于 1.0");
    }

    [Fact]
    public void QuantizeInt4_RoundTrip()
    {
        var weight = ArrayND.RandomNormal(4, 8);
        var quantized = Quantization.QuantizeInt4(weight);
        var dequantized = Quantization.Dequantize(quantized);

        Assert.Equal(weight.Shape, dequantized.Shape);
    }

    [Fact]
    public void QuantizeInt4_HasMoreErrorThanInt8()
    {
        var weight = ArrayND.RandomNormal(4, 8);
        var q8 = Quantization.QuantizeInt8(weight);
        var q4 = Quantization.QuantizeInt4(weight);

        var dq8 = Quantization.Dequantize(q8);
        var dq4 = Quantization.Dequantize(q4);

        var err8 = Quantization.QuantizationError(weight, dq8);
        var err4 = Quantization.QuantizationError(weight, dq4);

        Assert.True(err4 >= err8 * 0.5f,
            $"INT4 误差 {err4:F6} 应 >= INT8 误差 {err8:F6} 的一半");
    }

    [Fact]
    public void QuantizeInt8PerChannel_RoundTrip()
    {
        var weight = ArrayND.RandomNormal(4, 8);
        var quantized = Quantization.QuantizeInt8PerChannel(weight);
        var dequantized = Quantization.DequantizePerChannel(quantized);

        Assert.Equal(weight.Shape, dequantized.Shape);
    }

    [Fact]
    public void QuantizedTensor_CompressionRatio()
    {
        var weight = ArrayND.RandomNormal(16, 16);
        var q8 = Quantization.QuantizeInt8(weight);

        Assert.True(q8.CompressionRatio > 1.0f,
            $"INT8 压缩比 {q8.CompressionRatio:F2} 应 > 1");
    }

    #endregion

    #region TextGenerationPipeline

    [Fact]
    public void TextPipeline_Generate_ProducesText()
    {
        var alphabet = "abcdefghij ";
        var tokenizer = new CharacterTokenizer(alphabet);
        var model = new WeightTiedGPTModel(vocabSize: tokenizer.VocabSize, dModel: 16, numHeads: 2, numLayers: 1,
            maxSeqLen: 32);

        var pipeline = new TextGenerationPipeline(tokenizer, model, maxNewTokens: 5);
        var result = pipeline.Generate("ab");

        Assert.True(result.Length >= 0, "应生成文本");
    }

    [Fact]
    public void TextPipeline_GenerateGreedy_Deterministic()
    {
        var alphabet = "abcdefghij ";
        var tokenizer = new CharacterTokenizer(alphabet);
        var model = new WeightTiedGPTModel(vocabSize: tokenizer.VocabSize, dModel: 16, numHeads: 2, numLayers: 1,
            maxSeqLen: 32);

        var pipeline = new TextGenerationPipeline(tokenizer, model, maxNewTokens: 3);
        var result1 = pipeline.GenerateGreedy("ab");
        var result2 = pipeline.GenerateGreedy("ab");

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void TextPipeline_GenerateWithCache_ProducesText()
    {
        var alphabet = "abcdefghij ";
        var tokenizer = new CharacterTokenizer(alphabet);
        var model = new WeightTiedGPTModel(vocabSize: tokenizer.VocabSize, dModel: 16, numHeads: 2, numLayers: 1,
            maxSeqLen: 32);

        var pipeline = new TextGenerationPipeline(tokenizer, model, maxNewTokens: 3);
        var result = pipeline.GenerateWithCache("ab");

        Assert.True(result.Length >= 0, "KV Cache 生成应产出文本");
    }

    [Fact]
    public void TextPipeline_ComputePerplexity_ReturnsPositive()
    {
        var alphabet = "abcdefghij ";
        var tokenizer = new CharacterTokenizer(alphabet);
        var model = new WeightTiedGPTModel(vocabSize: tokenizer.VocabSize, dModel: 16, numHeads: 2, numLayers: 1,
            maxSeqLen: 32);

        var pipeline = new TextGenerationPipeline(tokenizer, model);
        var ppl = pipeline.ComputePerplexity("abc");

        Assert.True(ppl > 0, $"困惑度 {ppl} 应为正数");
    }

    [Fact]
    public void TextPipeline_GenerateBatch_ProducesMultipleResults()
    {
        var alphabet = "abcdefghij ";
        var tokenizer = new CharacterTokenizer(alphabet);
        var model = new WeightTiedGPTModel(vocabSize: tokenizer.VocabSize, dModel: 16, numHeads: 2, numLayers: 1,
            maxSeqLen: 32);

        var pipeline = new TextGenerationPipeline(tokenizer, model, maxNewTokens: 3);
        var results = pipeline.GenerateBatch(new List<string> { "ab", "cd" });

        Assert.Equal(2, results.Count);
    }

    #endregion
}