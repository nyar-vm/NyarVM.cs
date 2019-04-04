namespace Std.DL.Flux;

/// <summary>
///     Speculative Decoding —— 推测性解码加速 LLM 推理
///     使用小型 Draft 模型快速生成候选 token，大模型 Target 并行验证
///     接受的 token 无需重新计算，拒绝的 token 由 Target 重新采样
///     理论上不改变输出分布，但大幅减少 Target 模型前向调用次数
/// </summary>
public sealed class SpeculativeDecoder
{
    private readonly Func<ArrayND, ArrayND> _draftForward;
    private readonly int _eosTokenId;
    private readonly Func<ArrayND, ArrayND> _targetForward;

    /// <summary>
    ///     创建推测性解码器
    /// </summary>
    /// <param name="draftForward">Draft 模型前向函数：inputIds [1, seqLen] → logits [1, vocabSize]</param>
    /// <param name="targetForward">Target 模型前向函数：inputIds [1, seqLen] → logits [1, vocabSize]</param>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="draftSteps">Draft 每次推测的步数（默认 5）</param>
    /// <param name="eosTokenId">终止符 token ID（-1 表示不检测）</param>
    public SpeculativeDecoder(
        Func<ArrayND, ArrayND> draftForward,
        Func<ArrayND, ArrayND> targetForward,
        int vocabSize,
        int draftSteps = 5,
        int eosTokenId = -1)
    {
        _draftForward = draftForward;
        _targetForward = targetForward;
        VocabSize = vocabSize;
        DraftSteps = draftSteps;
        _eosTokenId = eosTokenId;
    }

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     Draft 每次推测的步数
    /// </summary>
    public int DraftSteps { get; }

    /// <summary>
    ///     执行推测性解码生成
    /// </summary>
    /// <param name="promptIds">输入 prompt [1, seqLen]</param>
    /// <param name="maxNewTokens">最大生成 token 数</param>
    /// <param name="temperature">采样温度</param>
    /// <returns>(生成的 token IDs [1, seqLen+newTokens], 接受率)</returns>
    public (ArrayND outputIds, float acceptanceRate) Generate(
        ArrayND promptIds, int maxNewTokens, float temperature = 1.0f)
    {
        var seqLen = promptIds.Shape[1];
        var totalLen = seqLen + maxNewTokens;
        var output = new int[totalLen];
        var spanP = promptIds.AsSpan();
        for (var i = 0; i < seqLen; i++) output[i] = (int)spanP[i];

        var currentLen = seqLen;
        var totalDraftTokens = 0;
        var totalAcceptedTokens = 0;
        var rng = new Random(42);

        while (currentLen < totalLen)
        {
            var remaining = totalLen - currentLen;
            var draftCount = System.Math.Min(DraftSteps, remaining - 1);
            if (draftCount <= 0) draftCount = 1;

            var draftTokens = new int[draftCount];
            var draftProbs = new float[draftCount][];

            var draftInputLen = currentLen;
            var draftInput = BuildInput(output, draftInputLen);

            for (var s = 0; s < draftCount; s++)
            {
                var draftLogits = _draftForward(draftInput);
                var probs = Softmax(draftLogits, temperature);
                draftProbs[s] = probs;
                draftTokens[s] = SampleFromProbs(probs, rng);

                if (currentLen + s < totalLen) output[currentLen + s] = draftTokens[s];

                if (_eosTokenId >= 0 && draftTokens[s] == _eosTokenId)
                {
                    draftCount = s + 1;
                    break;
                }

                draftInput = BuildInput(output, draftInputLen + s + 1);
            }

            totalDraftTokens += draftCount;

            var targetInputLen = System.Math.Min(currentLen + draftCount, totalLen);
            var targetInput = BuildInput(output, targetInputLen);
            var targetLogits = _targetForward(targetInput);

            var accepted = 0;
            for (var s = 0; s < draftCount; s++)
            {
                var targetPos = currentLen + s - 1;
                if (targetPos < 0 || targetPos >= targetInputLen) break;

                var targetProbs = SoftmaxAtPosition(targetLogits, targetPos, temperature);
                var draftP = draftProbs[s][draftTokens[s]];
                var targetP = targetProbs[draftTokens[s]];

                var acceptThreshold = MathF.Min(1.0f, targetP / MathF.Max(draftP, 1e-10f));
                var rand = (float)rng.NextDouble();

                if (rand < acceptThreshold)
                {
                    accepted++;
                }
                else
                {
                    if (currentLen + s < totalLen) output[currentLen + s] = SampleFromProbs(targetProbs, rng);

                    currentLen += s + 1;
                    break;
                }

                if (s == draftCount - 1)
                {
                    var bonusPos = currentLen + draftCount - 1;
                    if (bonusPos < targetInputLen && currentLen + draftCount < totalLen)
                    {
                        var bonusProbs = SoftmaxAtPosition(targetLogits, bonusPos, temperature);
                        output[currentLen + draftCount] = SampleFromProbs(bonusProbs, rng);
                        currentLen += draftCount + 1;
                    }
                    else
                    {
                        currentLen += draftCount;
                    }
                }
            }

            totalAcceptedTokens += accepted;

            if (_eosTokenId >= 0)
            {
                var eosFound = false;
                for (var i = currentLen - accepted - 1; i < currentLen; i++)
                    if (i >= 0 && i < totalLen && output[i] == _eosTokenId)
                    {
                        currentLen = i + 1;
                        eosFound = true;
                        break;
                    }

                if (eosFound) break;
            }
        }

        var resultLen = System.Math.Min(currentLen, totalLen);
        var result = ArrayND.Zeros(1, resultLen);
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < resultLen; i++) spanR[i] = output[i];

        var acceptanceRate = totalDraftTokens > 0
            ? (float)totalAcceptedTokens / totalDraftTokens
            : 0.0f;

        return (result, acceptanceRate);
    }

    /// <summary>
    ///     贪心推测性解码（确定性）
    /// </summary>
    /// <param name="promptIds">输入 prompt [1, seqLen]</param>
    /// <param name="maxNewTokens">最大生成 token 数</param>
    /// <returns>(生成的 token IDs, 接受率)</returns>
    public (ArrayND outputIds, float acceptanceRate) GenerateGreedy(
        ArrayND promptIds, int maxNewTokens)
    {
        var seqLen = promptIds.Shape[1];
        var totalLen = seqLen + maxNewTokens;
        var output = new int[totalLen];
        var spanP = promptIds.AsSpan();
        for (var i = 0; i < seqLen; i++) output[i] = (int)spanP[i];

        var currentLen = seqLen;
        var totalDraftTokens = 0;
        var totalAcceptedTokens = 0;

        while (currentLen < totalLen)
        {
            var remaining = totalLen - currentLen;
            var draftCount = System.Math.Min(DraftSteps, remaining - 1);
            if (draftCount <= 0) draftCount = 1;

            var draftTokens = new int[draftCount];
            var draftInput = BuildInput(output, currentLen);

            for (var s = 0; s < draftCount; s++)
            {
                var draftLogits = _draftForward(draftInput);
                draftTokens[s] = ArgMax(draftLogits);

                if (currentLen + s < totalLen) output[currentLen + s] = draftTokens[s];

                if (_eosTokenId >= 0 && draftTokens[s] == _eosTokenId)
                {
                    draftCount = s + 1;
                    break;
                }

                draftInput = BuildInput(output, currentLen + s + 1);
            }

            totalDraftTokens += draftCount;

            var targetInputLen = System.Math.Min(currentLen + draftCount, totalLen);
            var targetInput = BuildInput(output, targetInputLen);
            var targetLogits = _targetForward(targetInput);

            var accepted = 0;
            for (var s = 0; s < draftCount; s++)
            {
                var targetPos = currentLen + s - 1;
                if (targetPos < 0 || targetPos >= targetInputLen) break;

                var targetToken = ArgMaxAtPosition(targetLogits, targetPos);

                if (targetToken == draftTokens[s])
                {
                    accepted++;
                }
                else
                {
                    if (currentLen + s < totalLen) output[currentLen + s] = targetToken;

                    currentLen += s + 1;
                    break;
                }

                if (s == draftCount - 1)
                {
                    var bonusPos = currentLen + draftCount - 1;
                    if (bonusPos < targetInputLen && currentLen + draftCount < totalLen)
                    {
                        var bonusToken = ArgMaxAtPosition(targetLogits, bonusPos);
                        output[currentLen + draftCount] = bonusToken;
                        currentLen += draftCount + 1;
                    }
                    else
                    {
                        currentLen += draftCount;
                    }
                }
            }

            totalAcceptedTokens += accepted;
        }

        var resultLen = System.Math.Min(currentLen, totalLen);
        var result = ArrayND.Zeros(1, resultLen);
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < resultLen; i++) spanR[i] = output[i];

        var acceptanceRate = totalDraftTokens > 0
            ? (float)totalAcceptedTokens / totalDraftTokens
            : 0.0f;

        return (result, acceptanceRate);
    }

    private static ArrayND BuildInput(int[] tokens, int len)
    {
        var input = ArrayND.Zeros(1, len);
        var span = input.AsWriteSpan();
        for (var i = 0; i < len; i++) span[i] = tokens[i];
        return input;
    }

    private static float[] Softmax(ArrayND logits, float temperature)
    {
        var span = logits.AsSpan();
        var lastOff = span.Length - logits.Shape[logits.Shape.Length - 1];
        var v = logits.Shape[logits.Shape.Length - 1];

        var probs = new float[v];
        var max = float.NegativeInfinity;
        for (var i = 0; i < v; i++)
        {
            var val = span[lastOff + i] / temperature;
            if (val > max) max = val;
        }

        var sum = 0.0f;
        for (var i = 0; i < v; i++)
        {
            probs[i] = MathF.Exp(span[lastOff + i] / temperature - max);
            sum += probs[i];
        }

        for (var i = 0; i < v; i++) probs[i] /= sum;
        return probs;
    }

    private static float[] SoftmaxAtPosition(ArrayND logits, int pos, float temperature)
    {
        var vocabSize = logits.Shape[logits.Shape.Length - 1];
        var span = logits.AsSpan();
        var off = pos * vocabSize;

        var probs = new float[vocabSize];
        var max = float.NegativeInfinity;
        for (var i = 0; i < vocabSize; i++)
        {
            var val = span[off + i] / temperature;
            if (val > max) max = val;
        }

        var sum = 0.0f;
        for (var i = 0; i < vocabSize; i++)
        {
            probs[i] = MathF.Exp(span[off + i] / temperature - max);
            sum += probs[i];
        }

        for (var i = 0; i < vocabSize; i++) probs[i] /= sum;
        return probs;
    }

    private static int SampleFromProbs(float[] probs, Random rng)
    {
        var r = (float)rng.NextDouble();
        var cumSum = 0.0f;
        for (var i = 0; i < probs.Length; i++)
        {
            cumSum += probs[i];
            if (r < cumSum) return i;
        }

        return probs.Length - 1;
    }

    private static int ArgMax(ArrayND logits)
    {
        var span = logits.AsSpan();
        var lastOff = span.Length - logits.Shape[logits.Shape.Length - 1];
        var v = logits.Shape[logits.Shape.Length - 1];

        var maxIdx = 0;
        var maxVal = span[lastOff];
        for (var i = 1; i < v; i++)
            if (span[lastOff + i] > maxVal)
            {
                maxVal = span[lastOff + i];
                maxIdx = i;
            }

        return maxIdx;
    }

    private static int ArgMaxAtPosition(ArrayND logits, int pos)
    {
        var vocabSize = logits.Shape[logits.Shape.Length - 1];
        var span = logits.AsSpan();
        var off = pos * vocabSize;

        var maxIdx = 0;
        var maxVal = span[off];
        for (var i = 1; i < vocabSize; i++)
            if (span[off + i] > maxVal)
            {
                maxVal = span[off + i];
                maxIdx = i;
            }

        return maxIdx;
    }
}