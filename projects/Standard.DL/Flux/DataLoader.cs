namespace Std.DL.Flux;

/// <summary>
///     数据加载器 —— 批处理、打乱、变长序列填充
///     用于 LLM 训练中高效加载和预处理数据
/// </summary>
public sealed class DataLoader
{
    private readonly int _batchSize;
    private readonly ArrayND _data;
    private readonly int _padTokenId;
    private readonly int _seed;
    private readonly bool _shuffle;
    private int[] _indices;
    private int _position;

    /// <summary>
    ///     创建数据加载器
    /// </summary>
    /// <param name="data">数据 [N, seqLen]</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="shuffle">是否打乱</param>
    /// <param name="padTokenId">填充 token ID（用于变长序列）</param>
    /// <param name="seed">随机种子</param>
    public DataLoader(ArrayND data, int batchSize, bool shuffle = true, int padTokenId = 0, int seed = 42)
    {
        _data = data;
        _batchSize = batchSize;
        _shuffle = shuffle;
        _padTokenId = padTokenId;
        _seed = seed;
        _indices = CreateIndices();
        _position = 0;
    }

    /// <summary>
    ///     数据集大小
    /// </summary>
    public int Count => _data.Shape[0];

    /// <summary>
    ///     每个 epoch 的批次数
    /// </summary>
    public int NumBatches => (Count + _batchSize - 1) / _batchSize;

    /// <summary>
    ///     获取下一个批次
    /// </summary>
    /// <returns>批次数据 [batch, seqLen]，epoch 结束返回 null</returns>
    public ArrayND? NextBatch()
    {
        if (_position >= _indices.Length) return null;

        var end = System.Math.Min(_position + _batchSize, _indices.Length);
        var actualBatchSize = end - _position;

        var seqLen = _data.Shape[1];
        var batch = ArrayND.Zeros(actualBatchSize, seqLen);

        if (_padTokenId != 0)
        {
            var spanBatch = batch.AsWriteSpan();
            for (var i = 0; i < spanBatch.Length; i++) spanBatch[i] = _padTokenId;
        }

        var spanSrc = _data.AsSpan();
        var spanDst = batch.AsWriteSpan();

        for (var i = 0; i < actualBatchSize; i++)
        {
            var srcIdx = _indices[_position + i];
            var srcOff = srcIdx * seqLen;
            var dstOff = i * seqLen;
            for (var j = 0; j < seqLen; j++) spanDst[dstOff + j] = spanSrc[srcOff + j];
        }

        _position = end;
        return batch;
    }

    /// <summary>
    ///     重置到 epoch 开头（重新打乱）
    /// </summary>
    public void Reset()
    {
        _position = 0;
        if (_shuffle) _indices = CreateIndices();
    }

    /// <summary>
    ///     遍历一个 epoch 的所有批次
    /// </summary>
    /// <returns>批次枚举器</returns>
    public IEnumerable<ArrayND> Epoch()
    {
        Reset();
        while (true)
        {
            var batch = NextBatch();
            if (batch == null) break;

            yield return batch;
        }
    }

    /// <summary>
    ///     从变长序列列表创建填充后的批次
    /// </summary>
    /// <param name="sequences">变长序列列表</param>
    /// <param name="maxLen">最大长度（0 表示自动取最长）</param>
    /// <returns>填充后的批次 [batch, maxLen] + 长度数组 [batch]</returns>
    public static (ArrayND padded, ArrayND lengths) PadSequences(List<int[]> sequences, int maxLen = 0,
        int padTokenId = 0)
    {
        var batch = sequences.Count;
        if (maxLen <= 0)
        {
            maxLen = 0;
            for (var i = 0; i < batch; i++)
                if (sequences[i].Length > maxLen)
                    maxLen = sequences[i].Length;
        }

        var padded = ArrayND.Zeros(batch, maxLen);
        var lengths = ArrayND.Zeros(batch);

        var spanPadded = padded.AsWriteSpan();
        var spanLens = lengths.AsWriteSpan();

        for (var i = 0; i < batch; i++)
        {
            var seq = sequences[i];
            var len = System.Math.Min(seq.Length, maxLen);
            spanLens[i] = len;

            for (var j = 0; j < len; j++) spanPadded[i * maxLen + j] = seq[j];

            for (var j = len; j < maxLen; j++) spanPadded[i * maxLen + j] = padTokenId;
        }

        return (padded, lengths);
    }

    /// <summary>
    ///     从文本 token 序列创建训练样本（输入 + 目标对）
    /// </summary>
    /// <param name="tokens">完整 token 序列</param>
    /// <param name="seqLen">序列长度</param>
    /// <param name="stride">滑动步长</param>
    /// <returns>输入 [N, seqLen] + 目标 [N, 1]</returns>
    public static (ArrayND inputs, ArrayND targets) CreateTrainingPairs(int[] tokens, int seqLen, int stride = 0)
    {
        if (stride <= 0) stride = seqLen;

        var numSamples = System.Math.Max(0, (tokens.Length - seqLen) / stride + 1);
        if (numSamples == 0) return (ArrayND.Zeros(0, seqLen), ArrayND.Zeros(0, 1));

        var inputs = ArrayND.Zeros(numSamples, seqLen);
        var targets = ArrayND.Zeros(numSamples, 1);

        var spanIn = inputs.AsWriteSpan();
        var spanT = targets.AsWriteSpan();

        for (var i = 0; i < numSamples; i++)
        {
            var start = i * stride;
            for (var j = 0; j < seqLen; j++) spanIn[i * seqLen + j] = tokens[start + j];

            spanT[i] = tokens[System.Math.Min(start + seqLen, tokens.Length - 1)];
        }

        return (inputs, targets);
    }

    private int[] CreateIndices()
    {
        var n = _data.Shape[0];
        var indices = new int[n];
        for (var i = 0; i < n; i++) indices[i] = i;

        if (!_shuffle) return indices;

        var rng = new Random(_seed + _position);
        for (var i = n - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices;
    }
}