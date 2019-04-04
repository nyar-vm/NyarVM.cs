namespace Galatea.Tests;

/// <summary>
///     Tokenizer / BeamSearch / LoRA / LRFinder 测试
/// </summary>
public class AdvancedLLMTests
{
    #region CharacterTokenizer

    [Fact]
    public void CharacterTokenizer_EncodeDecode_RoundTrip()
    {
        var alphabet = "abcdefghijklmnopqrstuvwxyz ";
        var tokenizer = new CharacterTokenizer(alphabet);

        var text = "hello world";
        var ids = tokenizer.Encode(text);
        var decoded = tokenizer.Decode(ids);

        Assert.Equal(text, decoded);
    }

    [Fact]
    public void CharacterTokenizer_VocabSize_IncludesSpecial()
    {
        var alphabet = "abc";
        var tokenizer = new CharacterTokenizer(alphabet, addSpecialTokens: true);

        Assert.Equal(7, tokenizer.VocabSize);
    }

    [Fact]
    public void CharacterTokenizer_EncodeToTensor_CorrectShape()
    {
        var alphabet = "abc";
        ITokenizer tokenizer = new CharacterTokenizer(alphabet);

        var tensor = tokenizer.EncodeToTensor("abc");
        Assert.Equal(new[] { 1, 5 }, tensor.Shape);
    }

    [Fact]
    public void CharacterTokenizer_SpecialTokens_InSequence()
    {
        var alphabet = "ab";
        var tokenizer = new CharacterTokenizer(alphabet);

        var ids = tokenizer.Encode("ab");

        Assert.Equal(tokenizer.BosTokenId, ids[0]);
        Assert.Equal(tokenizer.EosTokenId, ids[^1]);
    }

    [Fact]
    public void CharacterTokenizer_UnknownChar_GetsUnkId()
    {
        var alphabet = "abc";
        var tokenizer = new CharacterTokenizer(alphabet);

        var ids = tokenizer.Encode("ax");
        Assert.Equal(tokenizer.UnkTokenId, ids[2]);
    }

    [Fact]
    public void CharacterTokenizer_EncodeBatch_PadsCorrectly()
    {
        var alphabet = "abc";
        ITokenizer tokenizer = new CharacterTokenizer(alphabet);

        var (inputIds, lengths) = tokenizer.EncodeBatch(new List<string> { "ab", "abc" });

        Assert.Equal(new[] { 2, 5 }, inputIds.Shape);
    }

    #endregion

    #region BPETokenizer

    [Fact]
    public void BPETokenizer_EncodeDecode_RoundTrip()
    {
        var trainingText = "the cat sat on the mat the cat ate the rat";
        var tokenizer = new BPETokenizer(vocabSize: 30, trainingText: trainingText);

        var text = "the cat";
        var ids = tokenizer.Encode(text);
        var decoded = tokenizer.Decode(ids);

        Assert.Equal(text, decoded);
    }

    [Fact]
    public void BPETokenizer_VocabSize_MatchesTarget()
    {
        var trainingText = "abcabcabc";
        var tokenizer = new BPETokenizer(vocabSize: 20, trainingText: trainingText);

        Assert.True(tokenizer.VocabSize <= 25, $"词表大小 {tokenizer.VocabSize} 应接近目标 20");
    }

    [Fact]
    public void BPETokenizer_MergeRules_Learned()
    {
        var trainingText = "ab ab ab ab ab";
        var tokenizer = new BPETokenizer(vocabSize: 20, trainingText: trainingText);

        Assert.True(tokenizer.MergeRules.Count > 0, "应学到合并规则");
    }

    #endregion

    #region BeamSearch

    [Fact]
    public void BeamSearch_ReturnsValidSequence()
    {
        var vocabSize = 8;
        var model = new WeightTiedGPTModel(vocabSize: vocabSize, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 32);

        Func<ArrayND, ArrayND> forwardFn = inputIds =>
        {
            var logits = model.Forward(inputIds);
            var seqLen = logits.Shape[1];
            return logits.Slice(1, seqLen - 1, 1).Reshape(1, vocabSize);
        };

        var beamSearch = new BeamSearch(beamWidth: 3, vocabSize: vocabSize, eosTokenId: 7, maxLen: 5);
        var prompt = ArrayND.FromArray(new float[] { 1, 2 }, 1, 2);

        var (tokens, score) = beamSearch.Search(forwardFn, prompt);

        Assert.True(tokens.Length >= 2, $"Beam Search 结果长度 {tokens.Length} 应 >= prompt 长度 2");
        Assert.True(float.IsFinite(score), $"得分 {score} 应为有限值");
    }

    [Fact]
    public void BeamSearch_TopN_ReturnsMultipleResults()
    {
        var vocabSize = 8;
        var model = new WeightTiedGPTModel(vocabSize: vocabSize, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 32);

        Func<ArrayND, ArrayND> forwardFn = inputIds =>
        {
            var logits = model.Forward(inputIds);
            var seqLen = logits.Shape[1];
            return logits.Slice(1, seqLen - 1, 1).Reshape(1, vocabSize);
        };

        var beamSearch = new BeamSearch(beamWidth: 3, vocabSize: vocabSize, eosTokenId: 7, maxLen: 3);
        var prompt = ArrayND.FromArray(new float[] { 1 }, 1, 1);

        var results = beamSearch.SearchTopN(forwardFn, prompt, topN: 2);

        Assert.True(results.Count >= 1, "应至少返回 1 个结果");
    }

    #endregion

    #region LoRA

    [Fact]
    public void LoRA_Forward_ProducesCorrectShape()
    {
        var baseLayer = new Dense(fanIn: 4, fanOut: 3);
        var lora = new LoRAAdapter(baseLayer, rank: 2);

        var input = ArrayND.RandomNormal(2, 4);
        var output = lora.Forward(input);

        Assert.Equal(new[] { 2, 3 }, output.Shape);
    }

    [Fact]
    public void LoRA_Parameters_OnlyContainsAB()
    {
        var baseLayer = new Dense(fanIn: 4, fanOut: 3);
        var lora = new LoRAAdapter(baseLayer, rank: 2);

        var paramsList = lora.Parameters().ToList();

        Assert.Equal(2, paramsList.Count);
        Assert.Equal(new[] { 4, 2 }, paramsList[0].Value.Shape);
        Assert.Equal(new[] { 2, 3 }, paramsList[1].Value.Shape);
    }

    [Fact]
    public void LoRA_Training_GradientsFlowToAB()
    {
        var baseLayer = new Dense(fanIn: 4, fanOut: 3);
        var w1Span = baseLayer.Weight.AsWriteSpan();
        for (var i = 0; i < w1Span.Length; i++) w1Span[i] = 0.1f;

        var lora = new LoRAAdapter(baseLayer, rank: 2);
        var bSpan = lora.MatrixB.AsWriteSpan();
        for (var i = 0; i < bSpan.Length; i++) bSpan[i] = 0.05f;

        var input = ArrayND.FromArray(new float[] { 1, 1, 1, 1 }, 1, 4);
        var target = ArrayND.FromArray(new float[] { 0, 0, 0 }, 1, 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();

        var output = lora.Forward(input, ctx);

        var allOnesGrad = ArrayND.Ones(output.Shape);
        ctx.BackwardFromGradient(output, allOnesGrad);

        Assert.NotNull(lora.MatrixA.Grad);
        Assert.NotNull(lora.MatrixB.Grad);

        var spanAGrad = lora.MatrixA.Grad!.AsSpan();
        var hasAGrad = false;
        for (var i = 0; i < spanAGrad.Length; i++)
            if (MathF.Abs(spanAGrad[i]) > 1e-10f)
            {
                hasAGrad = true;
                break;
            }

        Assert.True(hasAGrad, "矩阵 A 应有非零梯度");
    }

    [Fact]
    public void LoRA_MergeWeights_ProducesCorrectShape()
    {
        var baseLayer = new Dense(fanIn: 4, fanOut: 3);
        var lora = new LoRAAdapter(baseLayer, rank: 2);

        var merged = lora.MergeWeights();

        Assert.Equal(new[] { 4, 3 }, merged.Weight.Shape);
        Assert.Equal(new[] { 1, 3 }, merged.Bias.Shape);
    }

    [Fact]
    public void LoRA_MergeWeights_SameOutput()
    {
        var baseLayer = new Dense(fanIn: 4, fanOut: 3);
        var w1Span = baseLayer.Weight.AsWriteSpan();
        for (var i = 0; i < w1Span.Length; i++) w1Span[i] = 0.1f;

        var lora = new LoRAAdapter(baseLayer, rank: 2);
        var bSpan = lora.MatrixB.AsWriteSpan();
        for (var i = 0; i < bSpan.Length; i++) bSpan[i] = 0.01f;

        var input = ArrayND.Ones(1, 4);

        var loraOutput = lora.Forward(input);

        var merged = lora.MergeWeights();
        var mergedOutput = merged.Forward(input);

        var spanLora = loraOutput.AsSpan();
        var spanMerged = mergedOutput.AsSpan();

        for (var i = 0; i < spanLora.Length; i++)
            Assert.True(MathF.Abs(spanLora[i] - spanMerged[i]) < 0.01f,
                $"LoRA 输出 {spanLora[i]} 应约等于合并输出 {spanMerged[i]}");
    }

    [Fact]
    public void LoRAModel_ParameterEfficiency_IsLow()
    {
        var model = new GPTModel(vocabSize: 16, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var loraModel = new LoRAModel(model, rank: 2);

        var efficiency = loraModel.ParameterEfficiency();

        Assert.True(efficiency < 0.5f, $"参数效率 {efficiency:P} 应小于 50%");
        Assert.True(loraModel.TrainableParameterCount() < ModelSummary.CountParameters(model.Parameters()),
            "LoRA 可训练参数应少于原始模型参数");
    }

    #endregion

    #region LRFinder

    [Fact]
    public void LRFinder_Run_ReturnsResults()
    {
        var model = new GPTModel(vocabSize: 8, dModel: 16, numHeads: 2, numLayers: 1, maxSeqLen: 16);
        var optimizer = new Adam(learningRate: 0.001f);

        var inputs = ArrayND.FromArray(new float[] { 1, 2, 3, 4 }, 1, 4);
        var targets = ArrayND.FromArray(new float[] { 5 }, 1, 1);

        var finder = new LRFinder(model, optimizer, startLR: 1e-6f, endLR: 1.0f, numSteps: 10);
        var result = finder.Run(inputs, targets);

        Assert.True(result.LearningRates.Count > 0, "应有学习率记录");
        Assert.Equal(result.LearningRates.Count, result.Losses.Count);
    }

    [Fact]
    public void LRFinder_SuggestLR_ReturnsPositiveValue()
    {
        var lrs = new List<float> { 0.00001f, 0.0001f, 0.001f, 0.01f, 0.1f };
        var losses = new List<float> { 5.0f, 4.0f, 2.5f, 3.0f, 10.0f };

        var result = new LRFinderResult(lrs, losses);
        var suggested = LRFinder.SuggestLR(result);

        Assert.True(suggested > 0, $"建议学习率 {suggested} 应为正数");
    }

    #endregion
}