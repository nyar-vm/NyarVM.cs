namespace Std.DL.Flux;

/// <summary>
///     端到端文本生成管线 —— 连接 Tokenizer + Model + Sampling
///     提供从文本到文本的完整生成流程
/// </summary>
public sealed class TextGenerationPipeline
{
    private readonly int _maxNewTokens;
    private readonly int _padTokenId;
    private readonly float _repetitionPenalty;
    private readonly float _temperature;
    private readonly int _topK;
    private readonly float _topP;

    /// <summary>
    ///     创建文本生成管线
    /// </summary>
    /// <param name="tokenizer">分词器</param>
    /// <param name="model">权重共享 GPT 模型</param>
    /// <param name="temperature">采样温度</param>
    /// <param name="topK">Top-k 采样</param>
    /// <param name="topP">Top-p 采样</param>
    /// <param name="maxNewTokens">最大新 token 数</param>
    /// <param name="repetitionPenalty">重复惩罚（1.0 = 无惩罚）</param>
    public TextGenerationPipeline(
        ITokenizer tokenizer,
        WeightTiedGPTModel model,
        float temperature = 0.8f,
        int topK = 0,
        float topP = 1.0f,
        int maxNewTokens = 128,
        float repetitionPenalty = 1.0f)
    {
        Tokenizer = tokenizer;
        Model = model;
        _temperature = temperature;
        _topK = topK;
        _topP = topP;
        _maxNewTokens = maxNewTokens;
        _repetitionPenalty = repetitionPenalty;
        _padTokenId = tokenizer.PadTokenId;
    }

    /// <summary>
    ///     分词器
    /// </summary>
    public ITokenizer Tokenizer { get; }

    /// <summary>
    ///     模型
    /// </summary>
    public WeightTiedGPTModel Model { get; }

    /// <summary>
    ///     从文本 prompt 生成文本
    /// </summary>
    /// <param name="prompt">输入文本</param>
    /// <param name="seed">随机种子</param>
    /// <returns>生成的文本</returns>
    public string Generate(string prompt, int seed = 42)
    {
        var inputIds = Tokenizer.Encode(prompt);
        var inputTensor = ArrayND.Zeros(1, inputIds.Length);
        var spanIn = inputTensor.AsWriteSpan();
        for (var i = 0; i < inputIds.Length; i++) spanIn[i] = inputIds[i];

        var generator = Model.CreateGenerator();
        var outputTensor = generator.Generate(inputTensor, _maxNewTokens, _temperature, _topK, seed);

        var outputIds = new int[outputTensor.Shape[1]];
        var spanOut = outputTensor.AsSpan();
        for (var i = 0; i < outputIds.Length; i++) outputIds[i] = (int)spanOut[i];

        return Tokenizer.Decode(outputIds);
    }

    /// <summary>
    ///     贪心生成（确定性）
    /// </summary>
    /// <param name="prompt">输入文本</param>
    /// <returns>生成的文本</returns>
    public string GenerateGreedy(string prompt)
    {
        var inputIds = Tokenizer.Encode(prompt);
        var inputTensor = ArrayND.Zeros(1, inputIds.Length);
        var spanIn = inputTensor.AsWriteSpan();
        for (var i = 0; i < inputIds.Length; i++) spanIn[i] = inputIds[i];

        var generator = Model.CreateGenerator();
        var outputTensor = generator.GenerateGreedy(inputTensor, _maxNewTokens);

        var outputIds = new int[outputTensor.Shape[1]];
        var spanOut = outputTensor.AsSpan();
        for (var i = 0; i < outputIds.Length; i++) outputIds[i] = (int)spanOut[i];

        return Tokenizer.Decode(outputIds);
    }

    /// <summary>
    ///     带 KV Cache 的生成（更快）
    /// </summary>
    /// <param name="prompt">输入文本</param>
    /// <param name="seed">随机种子</param>
    /// <returns>生成的文本</returns>
    public string GenerateWithCache(string prompt, int seed = 42)
    {
        var inputIds = Tokenizer.Encode(prompt);
        var inputTensor = ArrayND.Zeros(1, inputIds.Length);
        var spanIn = inputTensor.AsWriteSpan();
        for (var i = 0; i < inputIds.Length; i++) spanIn[i] = inputIds[i];

        var cachedModel = new CachedGPTModel(Model);
        var outputTensor = cachedModel.Generate(inputTensor, _maxNewTokens, _temperature, _topK, seed);

        var outputIds = new int[outputTensor.Shape[1]];
        var spanOut = outputTensor.AsSpan();
        for (var i = 0; i < outputIds.Length; i++) outputIds[i] = (int)spanOut[i];

        return Tokenizer.Decode(outputIds);
    }

    /// <summary>
    ///     带 Beam Search 的生成（更高质量）
    /// </summary>
    /// <param name="prompt">输入文本</param>
    /// <param name="beamWidth">束宽</param>
    /// <returns>生成的文本</returns>
    public string GenerateBeamSearch(string prompt, int beamWidth = 3)
    {
        var inputIds = Tokenizer.Encode(prompt);
        var inputTensor = ArrayND.Zeros(1, inputIds.Length);
        var spanIn = inputTensor.AsWriteSpan();
        for (var i = 0; i < inputIds.Length; i++) spanIn[i] = inputIds[i];

        var beamSearch = new BeamSearch(beamWidth, Model.VocabSize, Tokenizer.EosTokenId, maxLen: _maxNewTokens);

        Func<ArrayND, ArrayND> forwardFn = input =>
        {
            var logits = Model.forward(input);
            var seqLen = logits.Shape[1];
            return logits.Slice(1, seqLen - 1, 1).Reshape(1, Model.VocabSize);
        };

        var (tokens, _) = beamSearch.Search(forwardFn, inputTensor);
        return Tokenizer.Decode(tokens);
    }

    /// <summary>
    ///     批量生成
    /// </summary>
    /// <param name="prompts">文本列表</param>
    /// <param name="seed">随机种子</param>
    /// <returns>生成文本列表</returns>
    public List<string> GenerateBatch(List<string> prompts, int seed = 42)
    {
        var results = new List<string>(prompts.Count);
        for (var i = 0; i < prompts.Count; i++) results.Add(Generate(prompts[i], seed + i));

        return results;
    }

    /// <summary>
    ///     计算文本的困惑度
    /// </summary>
    /// <param name="text">输入文本</param>
    /// <returns>困惑度</returns>
    public float ComputePerplexity(string text)
    {
        var inputIds = Tokenizer.Encode(text);
        if (inputIds.Length < 2) return float.MaxValue;

        var inputTensor = ArrayND.Zeros(1, inputIds.Length);
        var spanIn = inputTensor.AsWriteSpan();
        for (var i = 0; i < inputIds.Length; i++) spanIn[i] = inputIds[i];

        var ctx = new AutogradContext();
        var logits = Model.ForwardForTraining(inputTensor, ctx);

        var targetIds = new int[inputIds.Length - 1];
        for (var i = 1; i < inputIds.Length; i++) targetIds[i - 1] = inputIds[i];

        var targetTensor = ArrayND.Zeros(1, targetIds.Length);
        var spanT = targetTensor.AsWriteSpan();
        for (var i = 0; i < targetIds.Length; i++) spanT[i] = targetIds[i];

        var (loss, _) = Losses.SoftmaxCrossEntropyForward(logits, targetTensor);
        return MathF.Exp(loss);
    }
}