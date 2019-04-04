using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     权重共享 GPT 模型 —— Embedding 和 LM Head 共享同一权重矩阵
///     GPT-2 风格：lm_head.logits = input @ embedding.Weight^T
///     节省 vocabSize × dModel 的参数量（对小词表模型可节省 50%+ 参数）
/// </summary>
public sealed class WeightTiedGPTModel : ITrainableModel
{
    private readonly TransformerDecoderLayer[] _layers;

    /// <summary>
    ///     创建权重共享 GPT 模型
    /// </summary>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="numLayers">解码器层数</param>
    /// <param name="dFF">前馈维度（0 则自动计算）</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    public WeightTiedGPTModel(int vocabSize, int dModel, int numHeads, int numLayers, int dFF = 0, int maxSeqLen = 512)
    {
        VocabSize = vocabSize;
        DModel = dModel;
        NumLayers = numLayers;
        NumHeads = numHeads;
        MaxSeqLen = maxSeqLen;

        TokenEmbedding = new Embedding(vocabSize, dModel);
        PosEmbedding = new Embedding(maxSeqLen, dModel);

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
    }

    /// <summary>
    ///     Token 嵌入层
    /// </summary>
    public Embedding TokenEmbedding { get; }

    /// <summary>
    ///     位置嵌入层
    /// </summary>
    public Embedding PosEmbedding { get; }

    /// <summary>
    ///     Transformer 解码器层
    /// </summary>
    public ReadOnlySpan<TransformerDecoderLayer> Layers => _layers;

    /// <summary>
    ///     最终归一化层
    /// </summary>
    public RMSNorm FinalNorm { get; }

    /// <summary>
    ///     模型维度
    /// </summary>
    public int DModel { get; }

    /// <summary>
    ///     层数
    /// </summary>
    public int NumLayers { get; }

    /// <summary>
    ///     注意力头数
    /// </summary>
    public int NumHeads { get; }

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
    ///     LM Head 使用 Embedding.Weight 的转置（权重共享）
    /// </summary>
    /// <param name="inputIds">输入 token ID [batch, seqLen]</param>
    /// <returns>logits [batch, seqLen, vocabSize]</returns>
    public ArrayND forward(ArrayND inputIds)
    {
        var x = TokenEmbedding.Forward(inputIds);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x);

        x = FinalNorm.forward(x);
        return ComputeLogits(x);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public ArrayND forward(ArrayND inputIds, AutogradContext ctx)
    {
        var x = TokenEmbedding.Forward(inputIds, ctx);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x, ctx);

        x = FinalNorm.forward(x, ctx);
        return ComputeLogitsWithGrad(x, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数（Embedding 权重被共享，不重复计入）
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in TokenEmbedding.Parameters()) yield return p;

        for (var i = 0; i < _layers.Length; i++)
            foreach (var p in _layers[i].Parameters())
                yield return p;

        foreach (var p in FinalNorm.Parameters()) yield return p;
    }

    /// <summary>
    ///     训练用前向传播：返回 2D logits [batch, vocabSize]
    /// </summary>
    public ArrayND ForwardForTraining(ArrayND inputIds, AutogradContext ctx)
    {
        var x = TokenEmbedding.Forward(inputIds, ctx);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x, ctx);

        x = FinalNorm.forward(x, ctx);

        var batch = x.Shape[0];
        var seqLen = x.Shape[1];

        var lastHidden = ArrayND.Zeros(batch, DModel);
        var spanX = x.AsSpan();
        var spanLast = lastHidden.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var srcOffset = n * seqLen * DModel + (seqLen - 1) * DModel;
            var dstOffset = n * DModel;
            for (var d = 0; d < DModel; d++) spanLast[dstOffset + d] = spanX[srcOffset + d];
        }

        ctx.Record(lastHidden, [x], outputGrads =>
        {
            var dLast = outputGrads[0];
            var dX = ArrayND.Zeros(batch, seqLen, DModel);
            var spanDLast = dLast.AsSpan();
            var spanDX = dX.AsWriteSpan();

            for (var n = 0; n < batch; n++)
            {
                var dstOffset = n * seqLen * DModel + (seqLen - 1) * DModel;
                var srcOffset = n * DModel;
                for (var d = 0; d < DModel; d++) spanDX[dstOffset + d] = spanDLast[srcOffset + d];
            }

            return [dX];
        });

        return ComputeLogitsWithGrad(lastHidden, ctx);
    }

    /// <summary>
    ///     创建自回归生成器
    /// </summary>
    public AutoregressiveGenerator CreateGenerator()
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

    /// <summary>
    ///     获取指定层的引用
    /// </summary>
    /// <param name="index">层索引</param>
    /// <returns>Transformer 解码器层</returns>
    public TransformerDecoderLayer GetLayer(int index)
    {
        return _layers[index];
    }

    /// <summary>
    ///     使用 Embedding.Weight^T 计算 logits（权重共享）
    ///     logits = hidden @ Weight^T
    /// </summary>
    public ArrayND ComputeLogits(ArrayND hidden)
    {
        var weightT = TokenEmbedding.Weight.Transpose();
        return ArrayND.BatchMatMul(hidden, weightT);
    }

    /// <summary>
    ///     带自动微分的 logits 计算（权重共享）
    /// </summary>
    private ArrayND ComputeLogitsWithGrad(ArrayND hidden, AutogradContext ctx)
    {
        var weightT = TokenEmbedding.Weight.Transpose();
        var logits = ArrayND.BatchMatMul(hidden, weightT);

        ctx.Record(logits, [hidden, TokenEmbedding.Weight], outputGrads =>
        {
            var dLogits = outputGrads[0];
            var dHidden = ArrayND.BatchMatMul(dLogits, TokenEmbedding.Weight);
            var dWeight = ArrayND.BatchMatMul(hidden.Transpose(), dLogits).Transpose();
            return [dHidden, dWeight];
        });

        return logits;
    }
}