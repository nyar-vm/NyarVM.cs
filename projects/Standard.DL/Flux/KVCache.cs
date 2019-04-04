namespace Std.DL.Flux;

/// <summary>
///     KV Cache —— LLM 自回归推理加速
///     缓存已计算的 Key 和 Value，避免重复计算
///     每次推理只需计算新 token 的 Q/K/V，K/V 追加到缓存
/// </summary>
[Obsolete("请使用 PagedKVCache 替代")]
public sealed class KVCache
{
    private readonly int _batch;
    private readonly int[] _currentLen;
    private readonly int _dK;
    private readonly ArrayND[] _keyCache;
    private readonly int _numHeads;
    private readonly int _numLayers;
    private readonly ArrayND[] _valueCache;

    /// <summary>
    ///     创建 KV Cache
    /// </summary>
    /// <param name="numLayers">Transformer 层数</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dK">每个头的维度</param>
    /// <param name="batch">批次大小</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    public KVCache(int numLayers, int numHeads, int dK, int batch = 1, int maxSeqLen = 2048)
    {
        _numLayers = numLayers;
        _numHeads = numHeads;
        _dK = dK;
        _batch = batch;
        MaxSequenceLength = maxSeqLen;

        _keyCache = new ArrayND[numLayers];
        _valueCache = new ArrayND[numLayers];
        _currentLen = new int[batch];

        for (var i = 0; i < numLayers; i++)
        {
            _keyCache[i] = ArrayND.Zeros(batch * numHeads, maxSeqLen, dK);
            _valueCache[i] = ArrayND.Zeros(batch * numHeads, maxSeqLen, dK);
        }
    }

    /// <summary>
    ///     当前缓存序列长度
    /// </summary>
    public int CurrentLength => _currentLen[0];

    /// <summary>
    ///     最大序列长度
    /// </summary>
    public int MaxSequenceLength { get; }

    /// <summary>
    ///     更新某一层的 KV 缓存，返回拼接后的完整 K 和 V
    /// </summary>
    /// <param name="layerIdx">层索引</param>
    /// <param name="newKeys">新 Key [batch*numHeads, newSeqLen, dK]</param>
    /// <param name="newValues">新 Value [batch*numHeads, newSeqLen, dK]</param>
    /// <returns>完整 Key 和 Value [batch*numHeads, totalSeqLen, dK]</returns>
    public (ArrayND keys, ArrayND values) Update(int layerIdx, ArrayND newKeys, ArrayND newValues)
    {
        var newSeqLen = newKeys.Shape[1];
        var start = _currentLen[0];

        var spanK = _keyCache[layerIdx].AsWriteSpan();
        var spanV = _valueCache[layerIdx].AsWriteSpan();
        var spanNewK = newKeys.AsSpan();
        var spanNewV = newValues.AsSpan();

        var totalEntries = _batch * _numHeads;

        for (var bh = 0; bh < totalEntries; bh++)
        for (var s = 0; s < newSeqLen; s++)
        {
            var dstOffK = bh * MaxSequenceLength * _dK + (start + s) * _dK;
            var srcOffK = bh * newSeqLen * _dK + s * _dK;
            for (var d = 0; d < _dK; d++) spanK[dstOffK + d] = spanNewK[srcOffK + d];

            var dstOffV = bh * MaxSequenceLength * _dK + (start + s) * _dK;
            var srcOffV = bh * newSeqLen * _dK + s * _dK;
            for (var d = 0; d < _dK; d++) spanV[dstOffV + d] = spanNewV[srcOffV + d];
        }

        if (layerIdx == 0)
            for (var b = 0; b < _batch; b++)
                _currentLen[b] += newSeqLen;

        var totalLen = _currentLen[0];
        var fullKeys = _keyCache[layerIdx].Slice(1, 0, totalLen);
        var fullValues = _valueCache[layerIdx].Slice(1, 0, totalLen);

        return (fullKeys, fullValues);
    }

    /// <summary>
    ///     获取某一层当前缓存的 Key 和 Value
    /// </summary>
    /// <param name="layerIdx">层索引</param>
    /// <returns>Key 和 Value [batch*numHeads, currentLen, dK]</returns>
    public (ArrayND keys, ArrayND values) Get(int layerIdx)
    {
        var totalLen = _currentLen[0];
        return (_keyCache[layerIdx].Slice(1, 0, totalLen), _valueCache[layerIdx].Slice(1, 0, totalLen));
    }

    /// <summary>
    ///     清空缓存
    /// </summary>
    public void Clear()
    {
        for (var i = 0; i < _numLayers; i++)
        {
            var spanK = _keyCache[i].AsWriteSpan();
            var spanV = _valueCache[i].AsWriteSpan();
            for (var j = 0; j < spanK.Length; j++) spanK[j] = 0;
            for (var j = 0; j < spanV.Length; j++) spanV[j] = 0;
        }

        for (var b = 0; b < _batch; b++) _currentLen[b] = 0;
    }

    /// <summary>
    ///     截断缓存到指定长度
    /// </summary>
    /// <param name="length">目标长度</param>
    public void Truncate(int length)
    {
        for (var b = 0; b < _batch; b++) _currentLen[b] = System.Math.Min(_currentLen[b], length);
    }
}

/// <summary>
///     带 KV Cache 的 GPT 推理模型
///     使用缓存加速自回归生成，避免重复计算历史 token 的 K/V
/// </summary>
public sealed class CachedGPTModel
{
    private readonly int _dK;
    private readonly int _numHeads;
    private readonly int _numLayers;

    /// <summary>
    ///     创建带缓存的 GPT 推理模型
    /// </summary>
    /// <param name="model">权重共享 GPT 模型</param>
    public CachedGPTModel(WeightTiedGPTModel model)
    {
        Model = model;
        _numLayers = model.NumLayers;
        _numHeads = model.NumHeads;
        _dK = model.DModel / model.NumHeads;
    }

    /// <summary>
    ///     底层模型
    /// </summary>
    public WeightTiedGPTModel Model { get; }

    /// <summary>
    ///     当前缓存
    /// </summary>
    public KVCache? Cache { get; private set; }

    /// <summary>
    ///     预填充 prompt（一次性处理所有 prompt token）
    /// </summary>
    /// <param name="inputIds">prompt token [1, promptLen]</param>
    /// <returns>最后一个 token 的 logits [1, vocabSize]</returns>
    public ArrayND Prefill(ArrayND inputIds)
    {
        Cache = new KVCache(_numLayers, _numHeads, _dK, 1, 2048);

        var seqLen = inputIds.Shape[1];
        var x = Model.TokenEmbedding.Forward(inputIds);

        var posIds = ArrayND.Zeros(1, seqLen);
        var spanPos = posIds.AsWriteSpan();
        for (var i = 0; i < seqLen; i++) spanPos[i] = i;
        x = x + Model.PosEmbedding.Forward(posIds);

        for (var i = 0; i < _numLayers; i++) x = ForwardLayerWithCache(i, x, CreateCausalMaskForPrefill(seqLen));

        x = Model.FinalNorm.forward(x);
        var lastHidden = x.Slice(1, seqLen - 1, 1).Reshape(1, Model.DModel);
        return Model.ComputeLogits(lastHidden);
    }

    /// <summary>
    ///     生成下一个 token（使用缓存，只处理最新 token）
    /// </summary>
    /// <param name="lastTokenId">最新 token ID [1, 1]</param>
    /// <returns>logits [1, vocabSize]</returns>
    public ArrayND GenerateNext(ArrayND lastTokenId)
    {
        if (Cache == null) throw new InvalidOperationException("请先调用 Prefill 初始化缓存");

        var pos = Cache.CurrentLength;

        var x = Model.TokenEmbedding.Forward(lastTokenId);

        var posIds = ArrayND.FromArray([pos], 1, 1);
        x = x + Model.PosEmbedding.Forward(posIds);

        for (var i = 0; i < _numLayers; i++) x = ForwardLayerWithCache(i, x, null);

        x = Model.FinalNorm.forward(x);
        x = x.Reshape(1, Model.DModel);
        return Model.ComputeLogits(x);
    }

    /// <summary>
    ///     清空缓存，开始新的生成
    /// </summary>
    public void Reset()
    {
        Cache?.Clear();
        Cache = null;
    }

    /// <summary>
    ///     使用缓存生成多个 token
    /// </summary>
    /// <param name="promptIds">prompt token [1, promptLen]</param>
    /// <param name="maxNewTokens">最大新 token 数</param>
    /// <param name="temperature">采样温度</param>
    /// <param name="topK">Top-k 采样</param>
    /// <param name="seed">随机种子</param>
    /// <returns>完整输出 [1, promptLen + maxNewTokens]</returns>
    public ArrayND Generate(ArrayND promptIds, int maxNewTokens, float temperature = 0.8f, int topK = 0, int seed = 42)
    {
        var promptLen = promptIds.Shape[1];
        var vocabSize = Model.VocabSize;

        var result = new List<float>();
        var spanPrompt = promptIds.AsSpan();
        for (var i = 0; i < promptLen; i++) result.Add(spanPrompt[i]);

        var logits = Prefill(promptIds);
        var lastToken = SampleToken(logits, temperature, topK, seed);
        result.Add(lastToken);

        for (var t = 1; t < maxNewTokens; t++)
        {
            var lastTokenId = ArrayND.FromArray([lastToken], 1, 1);
            logits = GenerateNext(lastTokenId);
            lastToken = SampleToken(logits, temperature, topK, seed + t);
            result.Add(lastToken);
        }

        var output = ArrayND.Zeros(1, result.Count);
        var spanOut = output.AsWriteSpan();
        for (var i = 0; i < result.Count; i++) spanOut[i] = result[i];
        return output;
    }

    private ArrayND ForwardLayerWithCache(int layerIdx, ArrayND x, ArrayND? mask)
    {
        var layer = Model.GetLayer(layerIdx);

        var normed = layer.Norm1.forward(x);
        var attnOut = ForwardAttentionWithCache(layerIdx, normed, mask);
        x = AddArrays(x, attnOut);

        var normed2 = layer.Norm2.forward(x);
        var ffnOut = layer.FFN.forward(normed2);
        x = AddArrays(x, ffnOut);

        return x;
    }

    private ArrayND ForwardAttentionWithCache(int layerIdx, ArrayND x, ArrayND? mask)
    {
        var batch = x.Shape[0];
        var seqLen = x.Shape[1];
        var mha = Model.GetLayer(layerIdx).Attention;

        var (q, k, v) = mha.ForwardQKV(x);

        var (fullK, fullV) = Cache!.Update(layerIdx, k, v);

        var maskB = mask != null
            ? RepeatMaskForHeads(mask, batch, _numHeads, seqLen, fullK.Shape[1])
            : null;

        var attnOut = Attention.ScaledDotProductAttention(q, fullK, fullV, maskB);
        attnOut = attnOut.Reshape(batch, _numHeads, seqLen, _dK).Transpose(1, 2);
        attnOut = attnOut.Reshape(batch, seqLen, Model.DModel);

        return mha.ForwardOutput(attnOut);
    }

    private static ArrayND? RepeatMaskForHeads(ArrayND mask, int batch, int numHeads, int seqLenQ, int seqLenK)
    {
        if (mask.Shape.Length == 4)
        {
            var result = ArrayND.Zeros(batch * numHeads, seqLenQ, seqLenK);
            var spanM = mask.AsSpan();
            var spanR = result.AsWriteSpan();
            for (var b = 0; b < batch; b++)
            for (var h = 0; h < numHeads; h++)
            {
                var srcOff = b * seqLenQ * seqLenK;
                var dstOff = (b * numHeads + h) * seqLenQ * seqLenK;
                for (var i = 0; i < seqLenQ * seqLenK && srcOff + i < spanM.Length && dstOff + i < spanR.Length; i++)
                    spanR[dstOff + i] = spanM[srcOff + i];
            }

            return result;
        }

        return null;
    }

    private static ArrayND CreateCausalMaskForPrefill(int seqLen)
    {
        var mask = ArrayND.Zeros(1, 1, seqLen, seqLen);
        var span = mask.AsWriteSpan();
        for (var i = 0; i < seqLen; i++)
        for (var j = i + 1; j < seqLen; j++)
            span[i * seqLen + j] = float.NegativeInfinity;

        return mask;
    }

    private static ArrayND AddArrays(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];

        return result;
    }

    private static int SampleToken(ArrayND logits, float temperature, int topK, int seed)
    {
        var spanLogits = logits.AsSpan();
        var vocabSize = spanLogits.Length;

        if (temperature > 0)
        {
            var maxLogit = float.NegativeInfinity;
            for (var i = 0; i < vocabSize; i++)
                if (spanLogits[i] > maxLogit)
                    maxLogit = spanLogits[i];

            var probs = new float[vocabSize];
            var sum = 0.0f;
            for (var i = 0; i < vocabSize; i++)
            {
                probs[i] = MathF.Exp((spanLogits[i] - maxLogit) / temperature);
                sum += probs[i];
            }

            if (topK > 0 && topK < vocabSize)
            {
                var indexed = new (float prob, int idx)[vocabSize];
                for (var i = 0; i < vocabSize; i++) indexed[i] = (probs[i], i);
                Array.Sort(indexed, (a, b) => b.prob.CompareTo(a.prob));

                var topKSum = 0.0f;
                var inTopK = new bool[vocabSize];
                for (var i = 0; i < topK; i++)
                {
                    topKSum += indexed[i].prob;
                    inTopK[indexed[i].idx] = true;
                }

                for (var i = 0; i < vocabSize; i++)
                    if (!inTopK[i])
                        probs[i] = 0;

                sum = topKSum;
            }

            for (var i = 0; i < vocabSize; i++) probs[i] /= sum;

            var rng = new Random(seed);
            var r = (float)rng.NextDouble();
            var cumSum = 0.0f;
            for (var i = 0; i < vocabSize; i++)
            {
                cumSum += probs[i];
                if (cumSum >= r) return i;
            }

            return vocabSize - 1;
        }

        var maxIdx = 0;
        for (var i = 1; i < vocabSize; i++)
            if (spanLogits[i] > spanLogits[maxIdx])
                maxIdx = i;

        return maxIdx;
    }
}