namespace Galatea.Tests;

/// <summary>
///     DataLoader + CachedGPTModel 集成测试
/// </summary>
public class LLMInferenceTests
{
    #region Flux DataLoader

    [Fact]
    public void FluxDataLoader_NextBatch_ReturnsCorrectShape()
    {
        var data = ArrayND.Zeros(10, 4);
        var span = data.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 8;

        var loader = new DataLoader(data, batchSize: 3, shuffle: false);

        var batch1 = loader.NextBatch();
        Assert.NotNull(batch1);
        Assert.Equal(new[] { 3, 4 }, batch1.Shape);

        var batch2 = loader.NextBatch();
        Assert.NotNull(batch2);
        Assert.Equal(new[] { 3, 4 }, batch2.Shape);

        var batch3 = loader.NextBatch();
        Assert.NotNull(batch3);
        Assert.Equal(new[] { 3, 4 }, batch3.Shape);

        var batch4 = loader.NextBatch();
        Assert.NotNull(batch4);
        Assert.Equal(new[] { 1, 4 }, batch4.Shape);

        var batch5 = loader.NextBatch();
        Assert.Null(batch5);
    }

    [Fact]
    public void FluxDataLoader_Epoch_IteratesAllBatches()
    {
        var data = ArrayND.Zeros(8, 4);
        var span = data.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = i % 8;

        var loader = new DataLoader(data, batchSize: 3, shuffle: false);

        var count = 0;
        foreach (var batch in loader.Epoch())
        {
            Assert.True(batch.Shape[0] <= 3);
            Assert.Equal(4, batch.Shape[1]);
            count++;
        }

        Assert.Equal(3, count);
    }

    [Fact]
    public void FluxDataLoader_PadSequences_PadsCorrectly()
    {
        var sequences = new List<int[]>
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5 },
            new[] { 6, 7, 8, 9 }
        };

        var (padded, lengths) = DataLoader.PadSequences(sequences, padTokenId: 0);

        Assert.Equal(new[] { 3, 4 }, padded.Shape);
        Assert.Equal(new[] { 3 }, lengths.Shape);

        var spanPadded = padded.AsSpan();
        var spanLens = lengths.AsSpan();

        Assert.Equal(3, (int)spanLens[0]);
        Assert.Equal(2, (int)spanLens[1]);
        Assert.Equal(4, (int)spanLens[2]);

        Assert.Equal(1, spanPadded[0]);
        Assert.Equal(2, spanPadded[1]);
        Assert.Equal(3, spanPadded[2]);
        Assert.Equal(0, spanPadded[3]);

        Assert.Equal(4, spanPadded[4]);
        Assert.Equal(5, spanPadded[5]);
        Assert.Equal(0, spanPadded[6]);
        Assert.Equal(0, spanPadded[7]);
    }

    [Fact]
    public void FluxDataLoader_CreateTrainingPairs_CorrectSlicing()
    {
        var tokens = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var (inputs, targets) = DataLoader.CreateTrainingPairs(tokens, seqLen: 4, stride: 4);

        Assert.Equal(new[] { 2, 4 }, inputs.Shape);
        Assert.Equal(new[] { 2, 1 }, targets.Shape);

        var spanIn = inputs.AsSpan();
        Assert.Equal(1, spanIn[0]);
        Assert.Equal(2, spanIn[1]);
        Assert.Equal(3, spanIn[2]);
        Assert.Equal(4, spanIn[3]);

        var spanT = targets.AsSpan();
        Assert.Equal(5, spanT[0]);
        Assert.Equal(9, spanT[1]);
    }

    #endregion

    #region KVCache

    [Fact]
    public void KVCache_UpdateAccumulates_Keys()
    {
        var cache = new KVCache(numLayers: 2, numHeads: 2, dK: 8, batch: 1, maxSeqLen: 32);

        var k1 = ArrayND.Ones(2, 3, 8);
        var v1 = ArrayND.Ones(2, 3, 8);
        var (fullK1, fullV1) = cache.Update(0, k1, v1);

        Assert.Equal(3, cache.CurrentLength);
        Assert.Equal(new[] { 2, 3, 8 }, fullK1.Shape);

        var k2 = ArrayND.Zeros(2, 2, 8);
        var v2 = ArrayND.Zeros(2, 2, 8);
        var (fullK2, fullV2) = cache.Update(0, k2, v2);

        Assert.Equal(5, cache.CurrentLength);
        Assert.Equal(new[] { 2, 5, 8 }, fullK2.Shape);
    }

    [Fact]
    public void KVCache_MultiLayer_SeparateCaches()
    {
        var cache = new KVCache(numLayers: 2, numHeads: 2, dK: 8, batch: 1, maxSeqLen: 32);

        var k = ArrayND.Ones(2, 3, 8);
        var v = ArrayND.Ones(2, 3, 8);

        cache.Update(0, k, v);
        cache.Update(1, k, v);
        Assert.Equal(3, cache.CurrentLength);

        var (k0, _) = cache.Get(0);
        Assert.Equal(new[] { 2, 3, 8 }, k0.Shape);

        var (k1, _) = cache.Get(1);
        Assert.Equal(new[] { 2, 3, 8 }, k1.Shape);
    }

    [Fact]
    public void KVCache_Truncate_ReducesLength()
    {
        var cache = new KVCache(numLayers: 1, numHeads: 2, dK: 8, batch: 1, maxSeqLen: 32);

        var k = ArrayND.Ones(2, 5, 8);
        var v = ArrayND.Ones(2, 5, 8);
        cache.Update(0, k, v);
        Assert.Equal(5, cache.CurrentLength);

        cache.Truncate(3);
        Assert.Equal(3, cache.CurrentLength);

        var (fullK, _) = cache.Get(0);
        Assert.Equal(new[] { 2, 3, 8 }, fullK.Shape);
    }

    #endregion

    #region CachedGPTModel

    [Fact]
    public void CachedGPT_Prefill_ReturnsLogits()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 32);
        var cached = new CachedGPTModel(model);

        var prompt = ArrayND.FromArray(new float[] { 1, 2, 3 }, 1, 3);
        var logits = cached.Prefill(prompt);

        Assert.Equal(new[] { 1, 8 }, logits.Shape);

        var span = logits.AsSpan();
        for (var i = 0; i < span.Length; i++) Assert.True(float.IsFinite(span[i]), $"logits[{i}] 应为有限值");
    }

    [Fact]
    public void CachedGPT_GenerateNext_UsesCache()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 32);
        var cached = new CachedGPTModel(model);

        var prompt = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);
        cached.Prefill(prompt);
        Assert.Equal(2, cached.Cache!.CurrentLength);

        var nextToken = ArrayND.FromArray(new float[] { 3 }, 1, 1);
        var logits = cached.GenerateNext(nextToken);
        Assert.Equal(3, cached.Cache.CurrentLength);

        Assert.Equal(new[] { 1, 8 }, logits.Shape);
    }

    [Fact]
    public void CachedGPT_Generate_ProducesValidTokens()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 32);
        var cached = new CachedGPTModel(model);

        var prompt = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);
        var output = cached.Generate(prompt, maxNewTokens: 3, temperature: 0.8f, topK: 4, seed: 42);

        Assert.Equal(new[] { 1, 5 }, output.Shape);

        var span = output.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var tokenId = (int)span[i];
            Assert.True(tokenId >= 0 && tokenId < 8,
                $"token {tokenId} 应在 [0, 8) 范围内");
        }
    }

    [Fact]
    public void CachedGPT_Reset_ClearsCache()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 32);
        var cached = new CachedGPTModel(model);

        var prompt = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);
        cached.Prefill(prompt);
        Assert.NotNull(cached.Cache);

        cached.Reset();
        Assert.Null(cached.Cache);
    }

    [Fact]
    public void CachedGPT_GenerateGreedy_Deterministic()
    {
        var model = new WeightTiedGPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 32);

        var prompt = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);

        var cached1 = new CachedGPTModel(model);
        var output1 = cached1.Generate(prompt, maxNewTokens: 5, temperature: 0, seed: 0);

        var cached2 = new CachedGPTModel(model);
        var output2 = cached2.Generate(prompt, maxNewTokens: 5, temperature: 0, seed: 0);

        var span1 = output1.AsSpan();
        var span2 = output2.AsSpan();
        for (var i = 0; i < span1.Length; i++) Assert.Equal(span1[i], span2[i]);
    }

    #endregion
}