namespace Std.DL.Flux;

/// <summary>
///     Beam Search 推理 —— 比贪心/采样更高质量的生成策略
///     维护 beamWidth 个候选序列，每步扩展并保留得分最高的 beamWidth 个
/// </summary>
public sealed class BeamSearch
{
    private readonly int _beamWidth;
    private readonly int _eosTokenId;
    private readonly float _lengthPenalty;
    private readonly int _maxLen;
    private readonly int _vocabSize;

    /// <summary>
    ///     创建 Beam Search
    /// </summary>
    /// <param name="beamWidth">束宽</param>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="eosTokenId">结束 token ID</param>
    /// <param name="lengthPenalty">
    ///     长度惩罚（>1 偏好长序列，<1 偏好短序列）</param>
    /// <param name="maxLen">最大生成长度</param>
    public BeamSearch(int beamWidth, int vocabSize, int eosTokenId, float lengthPenalty = 1.0f, int maxLen = 128)
    {
        _beamWidth = beamWidth;
        _vocabSize = vocabSize;
        _eosTokenId = eosTokenId;
        _lengthPenalty = lengthPenalty;
        _maxLen = maxLen;
    }

    /// <summary>
    ///     执行 Beam Search 生成
    /// </summary>
    /// <param name="forwardFn">前向函数：inputIds [1, seqLen] → logits [1, vocabSize]</param>
    /// <param name="promptIds">prompt token ID [1, promptLen]</param>
    /// <returns>最佳序列 token ID 列表 + 得分</returns>
    public (int[] tokens, float score) Search(
        Func<ArrayND, ArrayND> forwardFn,
        ArrayND promptIds)
    {
        var promptLen = promptIds.Shape[1];
        var spanPrompt = promptIds.AsSpan();
        var promptTokens = new int[promptLen];
        for (var i = 0; i < promptLen; i++) promptTokens[i] = (int)spanPrompt[i];

        var beams = new List<BeamHypothesis>
        {
            new([.. promptTokens], 0.0f, false)
        };

        var completed = new List<BeamHypothesis>();

        for (var step = 0; step < _maxLen; step++)
        {
            var candidates = new List<BeamHypothesis>();

            foreach (var beam in beams)
            {
                if (beam.IsFinished)
                {
                    completed.Add(beam);
                    continue;
                }

                var inputIds = ArrayND.Zeros(1, beam.Tokens.Count);
                var spanIn = inputIds.AsWriteSpan();
                for (var i = 0; i < beam.Tokens.Count; i++) spanIn[i] = beam.Tokens[i];

                var logits = forwardFn(inputIds);
                var spanLogits = logits.AsSpan();

                var logProbs = ComputeLogProbs(spanLogits, _vocabSize);

                var topK = System.Math.Min(_beamWidth * 2, _vocabSize);
                var topIndices = GetTopKIndices(logProbs, topK);

                for (var k = 0; k < topIndices.Length; k++)
                {
                    var tokenId = topIndices[k];
                    var newScore = beam.Score + logProbs[tokenId];
                    var newTokens = new List<int>(beam.Tokens) { tokenId };
                    var isEos = tokenId == _eosTokenId;
                    candidates.Add(new BeamHypothesis(newTokens, newScore, isEos));
                }
            }

            candidates.Sort((a, b) => NormalizeScore(b.Score, b.Tokens.Count)
                .CompareTo(NormalizeScore(a.Score, a.Tokens.Count)));

            beams.Clear();
            for (var i = 0; i < System.Math.Min(_beamWidth, candidates.Count); i++)
                if (candidates[i].IsFinished)
                    completed.Add(candidates[i]);
                else
                    beams.Add(candidates[i]);

            if (beams.Count == 0) break;
        }

        completed.AddRange(beams);

        if (completed.Count == 0) return (promptTokens, 0.0f);

        completed.Sort((a, b) => NormalizeScore(b.Score, b.Tokens.Count)
            .CompareTo(NormalizeScore(a.Score, a.Tokens.Count)));

        var best = completed[0];
        return ([.. best.Tokens], best.Score);
    }

    /// <summary>
    ///     执行 Beam Search 并返回 top-n 结果
    /// </summary>
    /// <param name="forwardFn">前向函数</param>
    /// <param name="promptIds">prompt token ID</param>
    /// <param name="topN">返回前 n 个结果</param>
    /// <returns>候选序列列表</returns>
    public List<(int[] tokens, float score)> SearchTopN(
        Func<ArrayND, ArrayND> forwardFn,
        ArrayND promptIds,
        int topN = 3)
    {
        var promptLen = promptIds.Shape[1];
        var spanPrompt = promptIds.AsSpan();
        var promptTokens = new int[promptLen];
        for (var i = 0; i < promptLen; i++) promptTokens[i] = (int)spanPrompt[i];

        var beams = new List<BeamHypothesis>
        {
            new([.. promptTokens], 0.0f, false)
        };

        var completed = new List<BeamHypothesis>();

        for (var step = 0; step < _maxLen; step++)
        {
            var candidates = new List<BeamHypothesis>();

            foreach (var beam in beams)
            {
                if (beam.IsFinished)
                {
                    completed.Add(beam);
                    continue;
                }

                var inputIds = ArrayND.Zeros(1, beam.Tokens.Count);
                var spanIn = inputIds.AsWriteSpan();
                for (var i = 0; i < beam.Tokens.Count; i++) spanIn[i] = beam.Tokens[i];

                var logits = forwardFn(inputIds);
                var spanLogits = logits.AsSpan();

                var logProbs = ComputeLogProbs(spanLogits, _vocabSize);
                var topK = System.Math.Min(_beamWidth * 2, _vocabSize);
                var topIndices = GetTopKIndices(logProbs, topK);

                for (var k = 0; k < topIndices.Length; k++)
                {
                    var tokenId = topIndices[k];
                    var newScore = beam.Score + logProbs[tokenId];
                    var newTokens = new List<int>(beam.Tokens) { tokenId };
                    var isEos = tokenId == _eosTokenId;
                    candidates.Add(new BeamHypothesis(newTokens, newScore, isEos));
                }
            }

            candidates.Sort((a, b) => NormalizeScore(b.Score, b.Tokens.Count)
                .CompareTo(NormalizeScore(a.Score, a.Tokens.Count)));

            beams.Clear();
            for (var i = 0; i < System.Math.Min(_beamWidth, candidates.Count); i++)
                if (candidates[i].IsFinished)
                    completed.Add(candidates[i]);
                else
                    beams.Add(candidates[i]);

            if (beams.Count == 0) break;
        }

        completed.AddRange(beams);
        completed.Sort((a, b) => NormalizeScore(b.Score, b.Tokens.Count)
            .CompareTo(NormalizeScore(a.Score, a.Tokens.Count)));

        var results = new List<(int[] tokens, float score)>();
        for (var i = 0; i < System.Math.Min(topN, completed.Count); i++)
            results.Add(([.. completed[i].Tokens], completed[i].Score));

        return results;
    }

    private float NormalizeScore(float score, int length)
    {
        if (_lengthPenalty == 1.0f) return score;

        var lp = MathF.Pow((5.0f + length) / 6.0f, _lengthPenalty);
        return score / lp;
    }

    private static float[] ComputeLogProbs(ReadOnlySpan<float> logits, int vocabSize)
    {
        var maxLogit = float.NegativeInfinity;
        for (var i = 0; i < vocabSize && i < logits.Length; i++)
            if (logits[i] > maxLogit)
                maxLogit = logits[i];

        var expSum = 0.0f;
        var logProbs = new float[vocabSize];
        for (var i = 0; i < vocabSize && i < logits.Length; i++)
        {
            logProbs[i] = MathF.Exp(logits[i] - maxLogit);
            expSum += logProbs[i];
        }

        var logExpSum = maxLogit + MathF.Log(expSum);
        for (var i = 0; i < vocabSize && i < logits.Length; i++) logProbs[i] = logits[i] - logExpSum;

        return logProbs;
    }

    private static int[] GetTopKIndices(float[] values, int k)
    {
        var indices = new int[values.Length];
        for (var i = 0; i < values.Length; i++) indices[i] = i;
        Array.Sort(indices, (a, b) => values[b].CompareTo(values[a]));

        var result = new int[System.Math.Min(k, values.Length)];
        Array.Copy(indices, result, result.Length);
        return result;
    }

    private sealed class BeamHypothesis(List<int> tokens, float score, bool isFinished)
    {
        public List<int> Tokens { get; } = tokens;
        public float Score { get; } = score;
        public bool IsFinished { get; } = isFinished;
    }
}