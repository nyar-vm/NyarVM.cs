namespace Std.DL.Flux;

/// <summary>
///     自回归生成器 —— 将 GPT 风格模型包装为可逐步生成 token 的推理引擎
///     支持 greedy / sampling / top-k / top-p / temperature 等多种解码策略
/// </summary>
public sealed class AutoregressiveGenerator
{
    private readonly Func<ArrayND, ArrayND> _forwardFn;

    /// <summary>
    ///     创建自回归生成器
    /// </summary>
    /// <param name="forwardFn">前向函数：inputIds [batch, seqLen] → logits [batch, vocabSize]</param>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="eosTokenId">终止符 token ID（-1 表示不使用 EOS 检测）</param>
    public AutoregressiveGenerator(Func<ArrayND, ArrayND> forwardFn, int vocabSize, int eosTokenId = -1)
    {
        _forwardFn = forwardFn;
        VocabSize = vocabSize;
        EosTokenId = eosTokenId;
    }

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     终止符 token ID
    /// </summary>
    public int EosTokenId { get; }

    /// <summary>
    ///     生成 token 序列
    /// </summary>
    /// <param name="promptIds">提示 token [batch, promptLen]</param>
    /// <param name="maxNewTokens">最大生成 token 数</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="topK">Top-k 值（0 表示不过滤）</param>
    /// <param name="topP">Top-p 值（1.0 表示不过滤）</param>
    /// <param name="seed">随机种子</param>
    /// <returns>完整序列 [batch, promptLen + maxNewTokens]</returns>
    public ArrayND Generate(
        ArrayND promptIds,
        int maxNewTokens,
        float temperature = 1.0f,
        int topK = 0,
        float topP = 1.0f,
        int? seed = null)
    {
        var batch = promptIds.Shape[0];
        var promptLen = promptIds.Shape[1];
        var totalLen = promptLen + maxNewTokens;

        var allIds = new float[batch * totalLen];
        var spanPrompt = promptIds.AsSpan();
        for (var n = 0; n < batch; n++)
            Array.Copy(
                spanPrompt.ToArray(), n * promptLen,
                allIds, n * totalLen,
                promptLen);

        for (var step = 0; step < maxNewTokens; step++)
        {
            var currentLen = promptLen + step;
            var inputIds = ArrayND.FromArray(allIds, batch, totalLen)
                .Slice(1, 0, currentLen);

            var logits = _forwardFn(inputIds);

            var newTokens = Samplers.GenerateToken(logits,
                temperature,
                topK,
                topP,
                seed + step);

            var spanNewTokens = newTokens.AsSpan();
            for (var n = 0; n < batch; n++)
            {
                var tokenId = (int)spanNewTokens[n];
                allIds[n * totalLen + currentLen] = tokenId;
            }
        }

        return ArrayND.FromArray(allIds, batch, totalLen);
    }

    /// <summary>
    ///     Greedy 生成（每步选择概率最高的 token）
    /// </summary>
    /// <param name="promptIds">提示 token [batch, promptLen]</param>
    /// <param name="maxNewTokens">最大生成 token 数</param>
    /// <returns>完整序列 [batch, promptLen + maxNewTokens]</returns>
    public ArrayND GenerateGreedy(ArrayND promptIds, int maxNewTokens)
    {
        var batch = promptIds.Shape[0];
        var promptLen = promptIds.Shape[1];
        var totalLen = promptLen + maxNewTokens;

        var allIds = new float[batch * totalLen];
        var spanPrompt = promptIds.AsSpan();
        for (var n = 0; n < batch; n++)
            Array.Copy(
                spanPrompt.ToArray(), n * promptLen,
                allIds, n * totalLen,
                promptLen);

        for (var step = 0; step < maxNewTokens; step++)
        {
            var currentLen = promptLen + step;
            var inputIds = ArrayND.FromArray(allIds, batch, totalLen)
                .Slice(1, 0, currentLen);

            var logits = _forwardFn(inputIds);
            var newTokens = Samplers.GreedyDecode(logits);

            var spanNewTokens = newTokens.AsSpan();
            for (var n = 0; n < batch; n++) allIds[n * totalLen + currentLen] = spanNewTokens[n];
        }

        return ArrayND.FromArray(allIds, batch, totalLen);
    }
}