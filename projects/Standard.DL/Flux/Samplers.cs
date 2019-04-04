namespace Std.DL.Flux;

/// <summary>
///     LLM 采样策略 —— Top-k / Top-p (Nucleus) / Temperature
///     用于自回归文本生成的 token 采样
/// </summary>
public static class Samplers
{
    /// <summary>
    ///     Temperature 缩放：logits /= temperature
    ///     temperature > 1 更随机，temperature < 1 更确定， temperature → 0 趋近 argmax
    /// </summary>
    /// <param name="logits">原始 logits [batch, vocabSize]</param>
    /// <param name="temperature">温度参数</param>
    /// <returns>缩放后的 logits</returns>
    public static ArrayND ApplyTemperature(ArrayND logits, float temperature)
    {
        if (MathF.Abs(temperature - 1.0f) < 1e-6f) return logits;

        var result = ArrayND.Zeros(logits.Shape);
        var spanIn = logits.AsSpan();
        var spanOut = result.AsWriteSpan();
        var invTemp = 1.0f / temperature;

        for (var i = 0; i < spanIn.Length; i++) spanOut[i] = spanIn[i] * invTemp;

        return result;
    }

    /// <summary>
    ///     Top-k 过滤：只保留概率最高的 k 个 token，其余设为 -Inf
    /// </summary>
    /// <param name="logits">原始 logits [batch, vocabSize]</param>
    /// <param name="k">保留的 token 数量</param>
    /// <returns>过滤后的 logits</returns>
    public static ArrayND TopK(ArrayND logits, int k)
    {
        var batch = logits.Shape[0];
        var vocabSize = logits.Shape[1];
        var result = ArrayND.Zeros(logits.Shape);
        var spanIn = logits.AsSpan();
        var spanOut = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var offset = n * vocabSize;

            var values = new float[vocabSize];
            var indices = new int[vocabSize];
            for (var i = 0; i < vocabSize; i++)
            {
                values[i] = spanIn[offset + i];
                indices[i] = i;
            }

            Array.Sort(indices, (a, b) => values[b].CompareTo(values[a]));

            for (var i = 0; i < vocabSize; i++) spanOut[offset + i] = float.NegativeInfinity;

            for (var i = 0; i < System.Math.Min(k, vocabSize); i++) spanOut[offset + indices[i]] = values[indices[i]];
        }

        return result;
    }

    /// <summary>
    ///     Top-p (Nucleus) 过滤：保留累积概率达到 p 的最小 token 集合
    /// </summary>
    /// <param name="logits">原始 logits [batch, vocabSize]</param>
    /// <param name="p">累积概率阈值 (0, 1]</param>
    /// <returns>过滤后的 logits</returns>
    public static ArrayND TopP(ArrayND logits, float p)
    {
        var batch = logits.Shape[0];
        var vocabSize = logits.Shape[1];
        var result = ArrayND.Zeros(logits.Shape);
        var spanIn = logits.AsSpan();
        var spanOut = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var offset = n * vocabSize;

            var maxLogit = float.NegativeInfinity;
            for (var i = 0; i < vocabSize; i++)
                if (spanIn[offset + i] > maxLogit)
                    maxLogit = spanIn[offset + i];

            var expSum = 0.0f;
            for (var i = 0; i < vocabSize; i++) expSum += MathF.Exp(spanIn[offset + i] - maxLogit);

            var values = new float[vocabSize];
            var probs = new float[vocabSize];
            var indices = new int[vocabSize];
            for (var i = 0; i < vocabSize; i++)
            {
                values[i] = spanIn[offset + i];
                probs[i] = MathF.Exp(values[i] - maxLogit) / expSum;
                indices[i] = i;
            }

            Array.Sort(indices, (a, b) => probs[b].CompareTo(probs[a]));

            for (var i = 0; i < vocabSize; i++) spanOut[offset + i] = float.NegativeInfinity;

            var cumProb = 0.0f;
            for (var i = 0; i < vocabSize; i++)
            {
                var idx = indices[i];
                spanOut[offset + idx] = values[idx];
                cumProb += probs[idx];
                if (cumProb >= p) break;
            }
        }

        return result;
    }

    /// <summary>
    ///     Softmax 采样：从 logits 计算概率分布并随机采样 token
    /// </summary>
    /// <param name="logits">logits [batch, vocabSize]</param>
    /// <param name="seed">随机种子</param>
    /// <returns>采样的 token 索引 [batch, 1]</returns>
    public static ArrayND Sample(ArrayND logits, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var batch = logits.Shape[0];
        var vocabSize = logits.Shape[1];
        var result = ArrayND.Zeros(batch, 1);
        var spanOut = result.AsWriteSpan();
        var spanIn = logits.AsSpan();

        for (var n = 0; n < batch; n++)
        {
            var offset = n * vocabSize;

            var maxLogit = float.NegativeInfinity;
            for (var i = 0; i < vocabSize; i++)
                if (spanIn[offset + i] > maxLogit)
                    maxLogit = spanIn[offset + i];

            var expSum = 0.0f;
            var probs = new float[vocabSize];
            for (var i = 0; i < vocabSize; i++)
            {
                probs[i] = MathF.Exp(spanIn[offset + i] - maxLogit);
                expSum += probs[i];
            }

            for (var i = 0; i < vocabSize; i++) probs[i] /= expSum;

            var r = rng.NextSingle();
            var cumProb = 0.0f;
            var token = 0;
            for (var i = 0; i < vocabSize; i++)
            {
                cumProb += probs[i];
                if (cumProb >= r)
                {
                    token = i;
                    break;
                }
            }

            spanOut[n] = token;
        }

        return result;
    }

    /// <summary>
    ///     Greedy 解码：选择概率最高的 token
    /// </summary>
    /// <param name="logits">logits [batch, vocabSize]</param>
    /// <returns>最高概率 token 索引 [batch, 1]</returns>
    public static ArrayND GreedyDecode(ArrayND logits)
    {
        var batch = logits.Shape[0];
        var vocabSize = logits.Shape[1];
        var result = ArrayND.Zeros(batch, 1);
        var spanOut = result.AsWriteSpan();
        var spanIn = logits.AsSpan();

        for (var n = 0; n < batch; n++)
        {
            var offset = n * vocabSize;
            var maxVal = float.NegativeInfinity;
            var maxIdx = 0;
            for (var i = 0; i < vocabSize; i++)
                if (spanIn[offset + i] > maxVal)
                {
                    maxVal = spanIn[offset + i];
                    maxIdx = i;
                }

            spanOut[n] = maxIdx;
        }

        return result;
    }

    /// <summary>
    ///     完整采样管线：Temperature → Top-k → Top-p → Sample
    /// </summary>
    /// <param name="logits">原始 logits [batch, vocabSize]</param>
    /// <param name="temperature">温度</param>
    /// <param name="topK">Top-k 值（0 表示不过滤）</param>
    /// <param name="topP">Top-p 值（1.0 表示不过滤）</param>
    /// <param name="seed">随机种子</param>
    /// <returns>采样的 token 索引 [batch, 1]</returns>
    public static ArrayND GenerateToken(
        ArrayND logits,
        float temperature = 1.0f,
        int topK = 0,
        float topP = 1.0f,
        int? seed = null)
    {
        var scaled = ApplyTemperature(logits, temperature);

        if (topK > 0) scaled = TopK(scaled, topK);

        if (topP < 1.0f) scaled = TopP(scaled, topP);

        return Sample(scaled, seed);
    }
}