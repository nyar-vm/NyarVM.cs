using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     GPT 模型 —— 多层 Transformer 解码器堆叠
///     结构：TokenEmbedding + PositionEmbedding + N × TransformerDecoderLayer + RMSNorm + LM Head
///     支持 LLaMA 风格（SwiGLU + RMSNorm + RoPE）和 GPT 风格（Dense FFN + LayerNorm）
/// </summary>
public sealed class GPTModel : ITrainableModel
{
    private readonly TransformerDecoderLayer[] _layers;

    /// <summary>
    ///     创建 GPT 模型
    /// </summary>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="numLayers">解码器层数</param>
    /// <param name="dFF">前馈维度（0 则自动计算为 dModel * 8/3 向上取整到 64 倍数）</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    public GPTModel(int vocabSize, int dModel, int numHeads, int numLayers, int dFF = 0, int maxSeqLen = 512)
    {
        VocabSize = vocabSize;
        DModel = dModel;
        NumLayers = numLayers;
        MaxSeqLen = maxSeqLen;

        TokenEmbedding = new Embedding(vocabSize, dModel);

        if (dFF <= 0) dFF = (int)MathF.Ceiling(dModel * 8.0f / 3.0f / 64.0f) * 64;

        _layers = new TransformerDecoderLayer[numLayers];
        for (var i = 0; i < numLayers; i++)
            _layers[i] = new TransformerDecoderLayer(
                new MultiHeadAttention(dModel, numHeads),
                new SwiGLUFFN(dModel, dFF),
                new RMSNorm(dModel),
                new RMSNorm(dModel),
                seqLen => TransformerDecoderLayer.CreateCausalMask(seqLen)
            );

        FinalNorm = new RMSNorm(dModel);
        LmHead = new Dense(dModel, vocabSize);
    }

    /// <summary>
    ///     Token 嵌入层
    /// </summary>
    public Embedding TokenEmbedding { get; }

    /// <summary>
    ///     Transformer 解码器层
    /// </summary>
    public ReadOnlySpan<TransformerDecoderLayer> Layers => _layers;

    /// <summary>
    ///     最终归一化层
    /// </summary>
    public RMSNorm FinalNorm { get; }

    /// <summary>
    ///     语言模型头
    /// </summary>
    public Dense LmHead { get; }

    /// <summary>
    ///     模型维度
    /// </summary>
    public int DModel { get; }

    /// <summary>
    ///     层数
    /// </summary>
    public int NumLayers { get; }

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     最大序列长度
    /// </summary>
    public int MaxSeqLen { get; }

    /// <summary>
    ///     前向传播：inputIds → logits
    /// </summary>
    /// <param name="inputIds">输入 token ID [batch, seqLen]</param>
    /// <returns>logits [batch, seqLen, vocabSize]</returns>
    public ArrayND forward(ArrayND inputIds)
    {
        var x = TokenEmbedding.Forward(inputIds);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x);

        x = FinalNorm.forward(x);
        return LmHead.forward(x);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）：inputIds → logits
    ///     返回 [batch, seqLen, vocabSize] 的 3D logits
    /// </summary>
    public ArrayND forward(ArrayND inputIds, AutogradContext ctx)
    {
        var x = TokenEmbedding.Forward(inputIds, ctx);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x, ctx);

        x = FinalNorm.forward(x, ctx);
        return LmHead.forward(x, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        var result = new List<IParameter>();
        foreach (var p in TokenEmbedding.Parameters()) result.Add(p);

        for (var i = 0; i < _layers.Length; i++)
            foreach (var p in _layers[i].Parameters())
                result.Add(p);

        foreach (var p in FinalNorm.Parameters()) result.Add(p);

        foreach (var p in LmHead.Parameters()) result.Add(p);

        return result;
    }

    /// <summary>
    ///     训练用前向传播：返回 2D logits [batch, vocabSize]
    ///     取最后一个 token 位置的 logits，避免 Slice/Reshape 导致梯度断连
    /// </summary>
    /// <param name="inputIds">输入 token ID [batch, seqLen]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>logits [batch, vocabSize]</returns>
    public ArrayND ForwardForTraining(ArrayND inputIds, AutogradContext ctx)
    {
        var x = TokenEmbedding.Forward(inputIds, ctx);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x, ctx);

        x = FinalNorm.forward(x, ctx);

        var batch = x.Shape[0];
        var seqLen = x.Shape[1];
        var dModel = x.Shape[2];

        var lastHidden = ArrayND.Zeros(batch, dModel);
        var spanX = x.AsSpan();
        var spanLast = lastHidden.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var srcOffset = n * seqLen * dModel + (seqLen - 1) * dModel;
            var dstOffset = n * dModel;
            for (var d = 0; d < dModel; d++) spanLast[dstOffset + d] = spanX[srcOffset + d];
        }

        ctx.Record(lastHidden, [x], outputGrads =>
        {
            var dLast = outputGrads[0];
            var dX = ArrayND.Zeros(batch, seqLen, dModel);
            var spanDLast = dLast.AsSpan();
            var spanDX = dX.AsWriteSpan();

            for (var n = 0; n < batch; n++)
            {
                var dstOffset = n * seqLen * dModel + (seqLen - 1) * dModel;
                var srcOffset = n * dModel;
                for (var d = 0; d < dModel; d++) spanDX[dstOffset + d] = spanDLast[srcOffset + d];
            }

            return [dX];
        });

        return LmHead.forward(lastHidden, ctx);
    }

    /// <summary>
    ///     创建自回归生成器
    /// </summary>
    /// <param name="temperature">温度</param>
    /// <param name="topK">Top-k</param>
    /// <param name="topP">Top-p</param>
    /// <returns>自回归生成器</returns>
    public AutoregressiveGenerator CreateGenerator(
        float temperature = 1.0f,
        int topK = 0,
        float topP = 1.0f)
    {
        return new AutoregressiveGenerator(
            inputIds =>
            {
                var logits = forward(inputIds);
                var seqLen = logits.Shape[1];
                return logits.Slice(1, seqLen - 1, 1).Reshape(logits.Shape[0], VocabSize);
            },
            VocabSize);
    }
}