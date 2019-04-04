namespace Galatea.Tests;

/// <summary>
///     GQA / LlamaModel / FlashAttention / ModelPersistence 综合测试
/// </summary>
public class LlamaModelTests
{
    #region GroupedQueryAttention

    [Fact]
    public void GQA_StandardMHA_NumKVHeadsEqualsNumHeads()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 4);

        Assert.Equal(16, gqa.DModel);
        Assert.Equal(4, gqa.NumHeads);
        Assert.Equal(4, gqa.NumKVHeads);
        Assert.Equal(4, gqa.DK);

        var x = ArrayND.RandomNormal(2, 5, 16);
        var output = gqa.Forward(x);

        Assert.Equal(new[] { 2, 5, 16 }, output.Shape);
    }

    [Fact]
    public void GQA_GroupedQuery_NumKVHeadsLessThanNumHeads()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2);

        Assert.Equal(4, gqa.NumHeads);
        Assert.Equal(2, gqa.NumKVHeads);
        Assert.Equal(4, gqa.DK);

        var x = ArrayND.RandomNormal(2, 5, 16);
        var output = gqa.Forward(x);

        Assert.Equal(new[] { 2, 5, 16 }, output.Shape);
    }

    [Fact]
    public void GQA_MultiQueryAttention_NumKVHeadsIs1()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 1);

        var x = ArrayND.RandomNormal(2, 5, 16);
        var output = gqa.Forward(x);

        Assert.Equal(new[] { 2, 5, 16 }, output.Shape);
    }

    [Fact]
    public void GQA_InvalidNumKVHeads_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 3));
    }

    [Fact]
    public void GQA_WithCausalMask()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2);
        var x = ArrayND.RandomNormal(1, 6, 16);
        var mask = TransformerDecoderLayer.CreateCausalMask(6);

        var output = gqa.Forward(x, mask);

        Assert.Equal(new[] { 1, 6, 16 }, output.Shape);
    }

    [Fact]
    public void GQA_ForwardQKV_ReturnsCorrectShapes()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2);
        var x = ArrayND.RandomNormal(2, 5, 16);

        var (q, k, v) = gqa.ForwardQKV(x);

        Assert.Equal(new[] { 2 * 4, 5, 4 }, q.Shape);
        Assert.Equal(new[] { 2 * 2, 5, 4 }, k.Shape);
        Assert.Equal(new[] { 2 * 2, 5, 4 }, v.Shape);
    }

    [Fact]
    public void GQA_Parameters_Count()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2);
        var params_ = gqa.Parameters().ToList();

        Assert.Equal(8, params_.Count);
    }

    #endregion

    #region LlamaDecoderLayer

    [Fact]
    public void LlamaDecoderLayer_Forward_CorrectShape()
    {
        var layer = new LlamaDecoderLayer(dModel: 16, numHeads: 4, numKVHeads: 2);
        var x = ArrayND.RandomNormal(2, 5, 16);

        var output = layer.Forward(x);

        Assert.Equal(new[] { 2, 5, 16 }, output.Shape);
    }

    [Fact]
    public void LlamaDecoderLayer_WithMask_CorrectShape()
    {
        var layer = new LlamaDecoderLayer(dModel: 16, numHeads: 4, numKVHeads: 2);
        var x = ArrayND.RandomNormal(1, 8, 16);
        var mask = TransformerDecoderLayer.CreateCausalMask(8);

        var output = layer.Forward(x, mask);

        Assert.Equal(new[] { 1, 8, 16 }, output.Shape);
    }

    [Fact]
    public void LlamaDecoderLayer_Parameters_Count()
    {
        var layer = new LlamaDecoderLayer(dModel: 16, numHeads: 4, numKVHeads: 2);
        var params_ = layer.Parameters().ToList();

        Assert.True(params_.Count > 0, "解码器层应有可训练参数");
    }

    #endregion

    #region LlamaModel

    [Fact]
    public void LlamaModel_Forward_CorrectOutputShape()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, maxSeqLen: 16);

        var inputIds = ArrayND.Zeros(1, 5);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 1, 5, 32 }, logits.Shape);
    }

    [Fact]
    public void LlamaModel_ForwardWithMask_CorrectShape()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, maxSeqLen: 16);

        var inputIds = ArrayND.Zeros(2, 5);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var mask = TransformerDecoderLayer.CreateCausalMask(5);
        var logits = model.Forward(inputIds, mask);

        Assert.Equal(new[] { 2, 5, 32 }, logits.Shape);
    }

    [Fact]
    public void LlamaModel_ForwardForTraining_LastTokenLogits()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, maxSeqLen: 16);

        var inputIds = ArrayND.Zeros(2, 5);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var ctx = new AutogradContext();
        var logits = model.ForwardForTraining(inputIds, ctx);

        Assert.Equal(new[] { 2, 32 }, logits.Shape);
    }

    [Fact]
    public void LlamaModel_Parameters_Count()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, maxSeqLen: 16);

        var params_ = model.Parameters().ToList();

        Assert.True(params_.Count > 0, "模型应有可训练参数");

        var embeddingParams = model.TokenEmbedding.Parameters().Count();
        var layerParams = model.GetLayer(0).Parameters().Count();
        var normParams = model.FinalNorm.Parameters().Count();
        var headParams = model.LMHead.Parameters().Count();

        var expected = embeddingParams + 2 * layerParams + normParams + headParams;
        Assert.Equal(expected, params_.Count);
    }

    [Fact]
    public void LlamaModel_CreateGenerator_ReturnsGenerator()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, maxSeqLen: 16);

        var generator = model.CreateGenerator();

        Assert.NotNull(generator);
        Assert.Equal(32, generator.VocabSize);
    }

    [Fact]
    public void LlamaModel_GQA_vs_MHA_SameOutputShape()
    {
        var vocabSize = 32;
        var dModel = 16;
        var numLayers = 1;

        var gqaModel = new LlamaModel(vocabSize, dModel, numHeads: 4, numKVHeads: 2, numLayers);
        var mhaModel = new LlamaModel(vocabSize, dModel, numHeads: 4, numKVHeads: 4, numLayers);

        var inputIds = ArrayND.Zeros(1, 4);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % vocabSize;

        var gqaLogits = gqaModel.Forward(inputIds);
        var mhaLogits = mhaModel.Forward(inputIds);

        Assert.Equal(gqaLogits.Shape, mhaLogits.Shape);
    }

    [Fact]
    public void LlamaModel_MQA_MinimalKVHeads()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 1,
            numLayers: 2, maxSeqLen: 16);

        var inputIds = ArrayND.Zeros(1, 3);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 1, 3, 32 }, logits.Shape);
    }

    #endregion

    #region FlashAttention

    [Fact]
    public void FlashAttention_Compute_CorrectOutputShape()
    {
        var q = ArrayND.RandomNormal(2, 5, 8);
        var k = ArrayND.RandomNormal(2, 5, 8);
        var v = ArrayND.RandomNormal(2, 5, 8);

        var output = FlashAttention.Compute(q, k, v);

        Assert.Equal(new[] { 2, 5, 8 }, output.Shape);
    }

    [Fact]
    public void FlashAttention_Compute_WithMask()
    {
        var q = ArrayND.RandomNormal(1, 4, 8);
        var k = ArrayND.RandomNormal(1, 4, 8);
        var v = ArrayND.RandomNormal(1, 4, 8);
        var mask = TransformerDecoderLayer.CreateCausalMask(4);

        var output = FlashAttention.Compute(q, k, v, mask);

        Assert.Equal(new[] { 1, 4, 8 }, output.Shape);
    }

    [Fact]
    public void FlashAttention_Compute_DifferentSeqLenQK()
    {
        var q = ArrayND.RandomNormal(1, 3, 8);
        var k = ArrayND.RandomNormal(1, 6, 8);
        var v = ArrayND.RandomNormal(1, 6, 8);

        var output = FlashAttention.Compute(q, k, v);

        Assert.Equal(new[] { 1, 3, 8 }, output.Shape);
    }

    [Fact]
    public void FlashAttention_ComputeMemoryEfficient_CorrectShape()
    {
        var q = ArrayND.RandomNormal(2, 5, 8);
        var k = ArrayND.RandomNormal(2, 5, 8);
        var v = ArrayND.RandomNormal(2, 5, 8);

        var output = FlashAttention.ComputeMemoryEfficient(q, k, v);

        Assert.Equal(new[] { 2, 5, 8 }, output.Shape);
    }

    [Fact]
    public void FlashAttention_Compute_WithCustomBlockSize()
    {
        var q = ArrayND.RandomNormal(1, 8, 4);
        var k = ArrayND.RandomNormal(1, 8, 4);
        var v = ArrayND.RandomNormal(1, 8, 4);

        var output = FlashAttention.Compute(q, k, v, blockSize: 2);

        Assert.Equal(new[] { 1, 8, 4 }, output.Shape);
    }

    [Fact]
    public void FlashAttention_OutputNotNaN()
    {
        var q = ArrayND.RandomNormal(2, 5, 8);
        var k = ArrayND.RandomNormal(2, 5, 8);
        var v = ArrayND.RandomNormal(2, 5, 8);

        var output = FlashAttention.Compute(q, k, v);
        var span = output.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            Assert.False(float.IsNaN(span[i]), $"输出包含 NaN 在位置 {i}");
            Assert.False(float.IsInfinity(span[i]), $"输出包含 Infinity 在位置 {i}");
        }
    }

    [Fact]
    public void FlashAttention_MatchesStandardAttention()
    {
        var q = ArrayND.RandomNormal(1, 4, 8);
        var k = ArrayND.RandomNormal(1, 4, 8);
        var v = ArrayND.RandomNormal(1, 4, 8);

        var standard = Attention.ScaledDotProductAttention(q, k, v);
        var flash = FlashAttention.Compute(q, k, v);

        Assert.Equal(standard.Shape, flash.Shape);

        var spanStd = standard.AsSpan();
        var spanFlash = flash.AsSpan();

        var maxDiff = 0.0f;
        for (var i = 0; i < spanStd.Length; i++)
        {
            var diff = MathF.Abs(spanStd[i] - spanFlash[i]);
            if (diff > maxDiff) maxDiff = diff;
        }

        Assert.True(maxDiff < 0.1f, $"Flash Attention 与标准注意力最大差异 {maxDiff:F6} 应小于 0.1");
    }

    #endregion

    #region ModelPersistence

    [Fact]
    public void ModelPersistence_SaveAndLoadFromFile()
    {
        var model = new LlamaModel(
            vocabSize: 16, dModel: 8, numHeads: 2, numKVHeads: 1,
            numLayers: 1, maxSeqLen: 8);

        var path = Path.GetTempFileName();
        try
        {
            ModelPersistence.SaveToFile(model, path);
            Assert.True(File.Exists(path), "文件应存在");
            Assert.True(new FileInfo(path).Length > 0, "文件不应为空");

            var model2 = new LlamaModel(
                vocabSize: 16, dModel: 8, numHeads: 2, numKVHeads: 1,
                numLayers: 1, maxSeqLen: 8);

            ModelPersistence.LoadFromFile(model2, path);

            var inputIds = ArrayND.Zeros(1, 3);
            var span = inputIds.AsWriteSpan();
            for (var i = 0; i < span.Length; i++) span[i] = i % 16;

            var out1 = model.Forward(inputIds);
            var out2 = model2.Forward(inputIds);

            var span1 = out1.AsSpan();
            var span2 = out2.AsSpan();
            var maxErr = 0.0f;
            for (var i = 0; i < span1.Length; i++)
            {
                var err = MathF.Abs(span1[i] - span2[i]);
                if (err > maxErr) maxErr = err;
            }

            Assert.True(maxErr < 1e-5f, $"加载后输出应一致，最大误差={maxErr}");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ModelPersistence_SaveAndLoadFromBytes()
    {
        var model = new Dense(4, 3);
        var bytes = ModelPersistence.SaveToBytes(model);

        Assert.True(bytes.Length > 0, "字节数组不应为空");

        var model2 = new Dense(4, 3);
        var spanBefore = model.Weight.AsSpan();

        ModelPersistence.LoadFromBytes(model2, bytes);

        var spanAfter = model2.Weight.AsSpan();
        for (var i = 0; i < spanBefore.Length; i++) Assert.Equal(spanBefore[i], spanAfter[i], 1e-5f);
    }

    [Fact]
    public void ModelPersistence_EstimateSize()
    {
        var model = new Dense(4, 3);
        var estimatedSize = ModelPersistence.EstimateSize(model);
        var actualBytes = ModelPersistence.SaveToBytes(model);

        Assert.True(Math.Abs(estimatedSize - actualBytes.Length) <= 64,
            $"估计大小 {estimatedSize} 应接近实际 {actualBytes.Length}");
    }

    [Fact]
    public void ModelPersistence_InvalidMagic_Throws()
    {
        var model = new Dense(4, 3);
        var badBytes = new byte[100];
        for (var i = 0; i < badBytes.Length; i++) badBytes[i] = 0xFF;

        Assert.Throws<InvalidDataException>(() =>
            ModelPersistence.LoadFromBytes(model, badBytes));
    }

    [Fact]
    public void ModelPersistence_SaveWithParamNames()
    {
        var model = new Dense(4, 3);
        var path = Path.GetTempFileName();
        try
        {
            var names = new[] { "weight", "bias" };
            ModelPersistence.SaveToFile(model, path, names);

            var model2 = new Dense(4, 3);
            var loadedNames = ModelPersistence.LoadFromFile(model2, path);

            Assert.Equal(2, loadedNames.Length);
            Assert.Equal("weight", loadedNames[0]);
            Assert.Equal("bias", loadedNames[1]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    #endregion

    #region RoPE Integration

    [Fact]
    public void GQA_WithRoPE_CorrectOutputShape()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2,
            useRoPE: true, roPETheta: 10000.0f);

        Assert.True(gqa.UseRoPE);

        var x = ArrayND.RandomNormal(2, 5, 16);
        var output = gqa.Forward(x);

        Assert.Equal(new[] { 2, 5, 16 }, output.Shape);
    }

    [Fact]
    public void GQA_WithRoPE_OutputNotNaN()
    {
        var gqa = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2,
            useRoPE: true);

        var x = ArrayND.RandomNormal(1, 8, 16);
        var output = gqa.Forward(x);
        var span = output.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            Assert.False(float.IsNaN(span[i]), $"RoPE 输出包含 NaN 在位置 {i}");
            Assert.False(float.IsInfinity(span[i]), $"RoPE 输出包含 Infinity 在位置 {i}");
        }
    }

    [Fact]
    public void GQA_WithRoPE_DifferentFromWithoutRoPE()
    {
        var gqaWith = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2, useRoPE: true);
        var gqaWithout = new GroupedQueryAttention(dModel: 16, numHeads: 4, numKVHeads: 2, useRoPE: false);

        var x = ArrayND.RandomNormal(1, 4, 16);

        var outWith = gqaWith.Forward(x);
        var outWithout = gqaWithout.Forward(x);

        var spanWith = outWith.AsSpan();
        var spanWithout = outWithout.AsSpan();

        var anyDifferent = false;
        for (var i = 0; i < spanWith.Length; i++)
            if (MathF.Abs(spanWith[i] - spanWithout[i]) > 1e-6f)
            {
                anyDifferent = true;
                break;
            }

        Assert.True(anyDifferent, "RoPE 应使输出不同于无位置编码");
    }

    [Fact]
    public void LlamaModel_WithRoPE_CorrectOutputShape()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, maxSeqLen: 16, useRoPE: true);

        var inputIds = ArrayND.Zeros(1, 5);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var logits = model.Forward(inputIds);

        Assert.Equal(new[] { 1, 5, 32 }, logits.Shape);
    }

    [Fact]
    public void LlamaModel_WithRoPE_OutputNotNaN()
    {
        var model = new LlamaModel(
            vocabSize: 32, dModel: 16, numHeads: 4, numKVHeads: 2,
            numLayers: 2, maxSeqLen: 16, useRoPE: true);

        var inputIds = ArrayND.Zeros(1, 8);
        var span = inputIds.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 32;

        var logits = model.Forward(inputIds);
        var logitSpan = logits.AsSpan();

        for (var i = 0; i < logitSpan.Length; i++)
            Assert.False(float.IsNaN(logitSpan[i]), $"RoPE LlamaModel 输出包含 NaN 在位置 {i}");
    }

    [Fact]
    public void RotaryPositionEmbedding_Apply_ModifiesQandK()
    {
        var q = ArrayND.Ones(2, 4, 8);
        var k = ArrayND.Ones(2, 4, 8);

        var qBefore = q.AsSpan().ToArray();
        var kBefore = k.AsSpan().ToArray();

        RotaryPositionEmbedding.ApplyRotaryEmbedding(q, k, startPos: 0, thetaBase: 10000.0f);

        var qAfter = q.AsSpan();
        var kAfter = k.AsSpan();

        var qChanged = false;
        var kChanged = false;
        for (var i = 0; i < qAfter.Length; i++)
        {
            if (MathF.Abs(qAfter[i] - qBefore[i]) > 1e-6f) qChanged = true;

            if (MathF.Abs(kAfter[i] - kBefore[i]) > 1e-6f) kChanged = true;
        }

        Assert.True(qChanged, "RoPE 应修改 Q");
        Assert.True(kChanged, "RoPE 应修改 K");
    }

    [Fact]
    public void RotaryPositionEmbedding_PositionDependent()
    {
        var q1 = ArrayND.Ones(1, 1, 8);
        var k1 = ArrayND.Ones(1, 1, 8);
        RotaryPositionEmbedding.ApplyRotaryEmbedding(q1, k1, startPos: 0);

        var q2 = ArrayND.Ones(1, 1, 8);
        var k2 = ArrayND.Ones(1, 1, 8);
        RotaryPositionEmbedding.ApplyRotaryEmbedding(q2, k2, startPos: 5);

        var spanQ1 = q1.AsSpan();
        var spanQ2 = q2.AsSpan();

        var anyDifferent = false;
        for (var i = 0; i < spanQ1.Length; i++)
            if (MathF.Abs(spanQ1[i] - spanQ2[i]) > 1e-6f)
            {
                anyDifferent = true;
                break;
            }

        Assert.True(anyDifferent, "不同位置应有不同的旋转编码");
    }

    #endregion
}