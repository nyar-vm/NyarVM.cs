using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     Mistral 风格语言模型 —— GQA + Sliding Window Attention + RoPE + SwiGLU
///     与 LlamaModel 的区别：使用滑动窗口注意力，支持超长序列推理
///     代表模型：Mistral-7B、Mixtral-8x7B、Phi-2 等
/// </summary>
public sealed class MistralModel : ITrainableModel
{
    private readonly MistralDecoderLayer[] _layers;
    private readonly int _maxSeqLen;
    private readonly int _numHeads;
    private readonly int _numKVHeads;

    /// <summary>
    ///     创建 Mistral 模型
    /// </summary>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">Query 头数</param>
    /// <param name="numKVHeads">KV 头数</param>
    /// <param name="numLayers">层数</param>
    /// <param name="windowSize">滑动窗口大小</param>
    /// <param name="dFF">FFN 中间维度（0 = 4×dModel）</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    /// <param name="roPETheta">RoPE 频率基数</param>
    public MistralModel(int vocabSize, int dModel, int numHeads, int numKVHeads, int numLayers,
        int windowSize = 4096, int dFF = 0, int maxSeqLen = 32768, float roPETheta = 10000.0f)
    {
        VocabSize = vocabSize;
        DModel = dModel;
        _numHeads = numHeads;
        _numKVHeads = numKVHeads;
        NumLayers = numLayers;
        _maxSeqLen = maxSeqLen;
        WindowSize = windowSize;

        TokenEmbedding = new Embedding(vocabSize, dModel);
        _layers = new MistralDecoderLayer[numLayers];
        for (var i = 0; i < numLayers; i++)
            _layers[i] = new MistralDecoderLayer(dModel, numHeads, numKVHeads, windowSize,
                dFF > 0 ? dFF : dModel * 4, true, roPETheta);

        FinalNorm = new RMSNorm(dModel);
        LMHead = new Dense(dModel, vocabSize);
    }

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     模型维度
    /// </summary>
    public int DModel { get; }

    /// <summary>
    ///     层数
    /// </summary>
    public int NumLayers { get; }

    /// <summary>
    ///     滑动窗口大小
    /// </summary>
    public int WindowSize { get; }

    /// <summary>
    ///     Token 嵌入层
    /// </summary>
    public Embedding TokenEmbedding { get; }

    /// <summary>
    ///     最终归一化层
    /// </summary>
    public RMSNorm FinalNorm { get; }

    /// <summary>
    ///     LM Head
    /// </summary>
    public Dense LMHead { get; }

    /// <summary>
    ///     前向传播
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        return ForwardInternal(input, null, null);
    }

    /// <summary>
    ///     带自动微分的前向
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        return ForwardInternal(input, null, ctx);
    }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        var result = TokenEmbedding.Parameters();
        for (var i = 0; i < NumLayers; i++)
            result = result.Concat(_layers[i].Parameters());
        return result
            .Concat(FinalNorm.Parameters())
            .Concat(LMHead.Parameters());
    }

    /// <summary>
    ///     获取指定层
    /// </summary>
    /// <param name="index">层索引</param>
    /// <returns>解码器层</returns>
    public MistralDecoderLayer GetLayer(int index)
    {
        return _layers[index];
    }

    /// <summary>
    ///     带掩码的前向传播
    /// </summary>
    public ArrayND Forward(ArrayND input, ArrayND? mask)
    {
        return ForwardInternal(input, mask, null);
    }

    /// <summary>
    ///     带自动微分和掩码的前向
    /// </summary>
    public ArrayND Forward(ArrayND input, ArrayND? mask, AutogradContext ctx)
    {
        return ForwardInternal(input, mask, ctx);
    }

    private ArrayND ForwardInternal(ArrayND input, ArrayND? mask, AutogradContext? ctx)
    {
        ArrayND x;
        x = ctx != null
            ? TokenEmbedding.Forward(input, ctx)
            : TokenEmbedding.Forward(input);

        for (var i = 0; i < NumLayers; i++)
            x = ctx != null
                ? _layers[i].Forward(x, mask, ctx)
                : _layers[i].Forward(x, mask);

        x = ctx != null
            ? FinalNorm.forward(x, ctx)
            : FinalNorm.forward(x);

        return ctx != null
            ? LMHead.forward(x, ctx)
            : LMHead.forward(x);
    }

    /// <summary>
    ///     训练用前向（提取最后 token 的 logits）
    /// </summary>
    /// <param name="input">输入 [batch, seqLen]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>logits [batch, vocabSize]</returns>
    public ArrayND ForwardForTraining(ArrayND input, AutogradContext ctx)
    {
        var x = TokenEmbedding.Forward(input, ctx);
        var seqLen = input.Shape[1];

        for (var i = 0; i < NumLayers; i++)
            x = _layers[i].Forward(x, null, ctx);

        x = FinalNorm.forward(x, ctx);

        var batchSize = x.Shape[0];
        var lastHidden = ArrayND.Zeros(batchSize, DModel);
        var spanX = x.AsSpan();
        var spanLH = lastHidden.AsWriteSpan();
        for (var b = 0; b < batchSize; b++)
        {
            var srcOff = b * seqLen * DModel + (seqLen - 1) * DModel;
            var dstOff = b * DModel;
            for (var d = 0; d < DModel; d++)
                spanLH[dstOff + d] = spanX[srcOff + d];
        }

        ctx.Record(lastHidden, [x], grads =>
        {
            var dLastHidden = grads[0];
            var dX = ArrayND.Zeros(x.Shape);
            var spanDLH = dLastHidden.AsSpan();
            var spanDX = dX.AsWriteSpan();
            for (var b = 0; b < batchSize; b++)
            {
                var dstOff = b * seqLen * DModel + (seqLen - 1) * DModel;
                var srcOff = b * DModel;
                for (var d = 0; d < DModel; d++)
                    spanDX[dstOff + d] += spanDLH[srcOff + d];
            }

            return [dX];
        });

        return LMHead.forward(lastHidden, ctx);
    }

    /// <summary>
    ///     创建自回归生成器
    /// </summary>
    public AutoregressiveGenerator CreateGenerator()
    {
        return new AutoregressiveGenerator(input => forward(input), VocabSize);
    }

    /// <summary>
    ///     创建推测性解码器（使用自身作为 Target，指定 Draft 模型）
    /// </summary>
    /// <param name="draftModel">Draft 模型（小型模型）</param>
    /// <param name="draftSteps">Draft 每次推测步数</param>
    /// <returns>推测性解码器</returns>
    public SpeculativeDecoder CreateSpeculativeDecoder(ITrainableModel draftModel, int draftSteps = 5)
    {
        return new SpeculativeDecoder(
            input => draftModel.forward(input),
            input => forward(input),
            VocabSize, draftSteps);
    }
}