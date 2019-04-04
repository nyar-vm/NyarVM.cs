using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     标准前馈网络 —— Dense + GELU + Dense
///     BERT / ViT 等模型使用的经典 FFN 结构
/// </summary>
public sealed class StandardFFN : ITrainableModel
{
    /// <summary>
    ///     创建标准前馈网络
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="dFFN">前馈网络隐藏维度</param>
    public StandardFFN(int dModel, int dFFN)
    {
        FC1 = new Dense(dModel, dFFN);
        FC2 = new Dense(dFFN, dModel);
    }

    /// <summary>
    ///     第一层全连接（升维）
    /// </summary>
    public Dense FC1 { get; }

    /// <summary>
    ///     第二层全连接（降维）
    /// </summary>
    public Dense FC2 { get; }

    /// <summary>
    ///     前向传播：output = FC2(GELU(FC1(x)))
    /// </summary>
    /// <param name="input">输入张量 [..., dModel]</param>
    /// <returns>输出张量 [..., dModel]</returns>
    public ArrayND forward(ArrayND input)
    {
        var x = FC1.forward(input);
        x = Activations.GELUForward(x);
        return FC2.forward(x);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入张量 [..., dModel]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量 [..., dModel]</returns>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var x = FC1.forward(input, ctx);
        x = Activations.GELU(x, ctx);
        return FC2.forward(x, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in FC1.Parameters()) yield return p;

        foreach (var p in FC2.Parameters()) yield return p;
    }
}

/// <summary>
///     BERT 编码器层 —— Pre-Norm 双向 Transformer 编码器构建块
///     结构：x = x + Attention(LayerNorm(x))
///     x = x + FFN(LayerNorm(x))
///     不使用因果掩码，允许双向注意力
/// </summary>
public sealed class BertEncoderLayer : ITrainableModel
{
    /// <summary>
    ///     创建 BERT 编码器层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dFFN">前馈网络隐藏维度</param>
    public BertEncoderLayer(int dModel, int numHeads, int dFFN)
    {
        Norm1 = new LayerNorm(dModel);
        Attention = new MultiHeadAttention(dModel, numHeads);
        Norm2 = new LayerNorm(dModel);
        FFN = new StandardFFN(dModel, dFFN);
    }

    /// <summary>
    ///     第一个归一化层（注意力前）
    /// </summary>
    public LayerNorm Norm1 { get; }

    /// <summary>
    ///     多头自注意力层（双向）
    /// </summary>
    public MultiHeadAttention Attention { get; }

    /// <summary>
    ///     第二个归一化层（FFN 前）
    /// </summary>
    public LayerNorm Norm2 { get; }

    /// <summary>
    ///     前馈网络
    /// </summary>
    public StandardFFN FFN { get; }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    /// <param name="input">输入张量 [batch, seqLen, dModel]</param>
    /// <returns>输出张量 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND input)
    {
        var normed1 = Norm1.Forward(input);
        var attnOut = Attention.Forward(normed1);
        var res1 = AddArrays(input, attnOut);

        var normed2 = Norm2.Forward(res1);
        var ffnOut = FFN.forward(normed2);
        var res2 = AddArrays(res1, ffnOut);

        return res2;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入张量 [batch, seqLen, dModel]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var normed1 = Norm1.Forward(input, ctx);
        var attnOut = Attention.Forward(normed1, null, ctx);
        var res1 = AddArraysWithGrad(input, attnOut, ctx);

        var normed2 = Norm2.Forward(res1, ctx);
        var ffnOut = FFN.forward(normed2, ctx);
        var res2 = AddArraysWithGrad(res1, ffnOut, ctx);

        return res2;
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in Norm1.Parameters()) yield return p;

        foreach (var p in Attention.Parameters()) yield return p;

        foreach (var p in Norm2.Parameters()) yield return p;

        foreach (var p in FFN.Parameters()) yield return p;
    }

    private static ArrayND AddArrays(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];
        return result;
    }

    private static ArrayND AddArraysWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var sum = AddArrays(a, b);
        ctx.Record(sum, [a, b], outputGrads =>
        {
            var dSum = outputGrads[0];
            return [dSum, dSum];
        });
        return sum;
    }
}

/// <summary>
///     BERT 模型 —— 双向 Transformer 编码器
///     结构：TokenEmbedding + PositionEmbedding + SegmentEmbedding + N × BertEncoderLayer + LayerNorm
///     输出：sequenceOutput [batch, seqLen, dModel] 用于 token 级任务
///     pooledOutput [batch, dModel] = Dense(Tanh(firstToken)) 用于句子级任务
///     预训练头：MLM 头（Dense + GELU + LayerNorm + Dense(vocabSize)）
///     NSP 头（Dense(2) 作用于 pooledOutput）
/// </summary>
public sealed class BertModel : ITrainableModel
{
    #region 构造函数

    /// <summary>
    ///     创建 BERT 模型
    /// </summary>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="numLayers">编码器层数</param>
    /// <param name="dFFN">前馈网络隐藏维度</param>
    /// <param name="dropoutRate">Dropout 丢弃率</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    public BertModel(
        int vocabSize,
        int dModel = 768,
        int numHeads = 12,
        int numLayers = 12,
        int dFFN = 3072,
        float dropoutRate = 0.1f,
        int maxSeqLen = 512)
    {
        VocabSize = vocabSize;
        DModel = dModel;
        NumLayers = numLayers;
        MaxSeqLen = maxSeqLen;

        TokenEmbedding = new Embedding(vocabSize, dModel);
        PosEmbedding = new Embedding(maxSeqLen, dModel);
        SegmentEmbedding = new Embedding(2, dModel);

        _layers = new BertEncoderLayer[numLayers];
        for (var i = 0; i < numLayers; i++) _layers[i] = new BertEncoderLayer(dModel, numHeads, dFFN);

        FinalNorm = new LayerNorm(dModel);
        PoolerDense = new Dense(dModel, dModel);
        EmbeddingDropout = new Dropout(dropoutRate);

        MlmDense = new Dense(dModel, dModel);
        MlmNorm = new LayerNorm(dModel);
        MlmDecoder = new Dense(dModel, vocabSize);

        NspClassifier = new Dense(dModel, 2);
    }

    #endregion

    #region 字段

    private readonly BertEncoderLayer[] _layers;

    #endregion

    #region 属性

    /// <summary>
    ///     Token 嵌入层
    /// </summary>
    public Embedding TokenEmbedding { get; }

    /// <summary>
    ///     位置嵌入层
    /// </summary>
    public Embedding PosEmbedding { get; }

    /// <summary>
    ///     段落嵌入层
    /// </summary>
    public Embedding SegmentEmbedding { get; }

    /// <summary>
    ///     BERT 编码器层
    /// </summary>
    public ReadOnlySpan<BertEncoderLayer> Layers => _layers;

    /// <summary>
    ///     最终归一化层
    /// </summary>
    public LayerNorm FinalNorm { get; }

    /// <summary>
    ///     池化全连接层
    /// </summary>
    public Dense PoolerDense { get; }

    /// <summary>
    ///     嵌入层 Dropout
    /// </summary>
    public Dropout EmbeddingDropout { get; }

    /// <summary>
    ///     模型维度
    /// </summary>
    public int DModel { get; }

    /// <summary>
    ///     编码器层数
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
    ///     MLM 预测头第一层全连接
    /// </summary>
    public Dense MlmDense { get; }

    /// <summary>
    ///     MLM 预测头归一化层
    /// </summary>
    public LayerNorm MlmNorm { get; }

    /// <summary>
    ///     MLM 预测头解码层
    /// </summary>
    public Dense MlmDecoder { get; }

    /// <summary>
    ///     NSP 预测头分类器
    /// </summary>
    public Dense NspClassifier { get; }

    #endregion

    #region ITrainableModel 实现

    /// <summary>
    ///     前向传播：inputIds → sequenceOutput
    ///     ITrainableModel 接口实现，段 ID 默认全零
    /// </summary>
    /// <param name="inputIds">输入 token ID [batch, seqLen]</param>
    /// <returns>序列输出 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND inputIds)
    {
        var (sequenceOutput, _) = ForwardBert(inputIds, null);
        return sequenceOutput;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）：inputIds → sequenceOutput
    /// </summary>
    /// <param name="inputIds">输入 token ID [batch, seqLen]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>序列输出 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND inputIds, AutogradContext ctx)
    {
        var (sequenceOutput, _) = ForwardBert(inputIds, null, ctx);
        return sequenceOutput;
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in TokenEmbedding.Parameters()) yield return p;

        foreach (var p in PosEmbedding.Parameters()) yield return p;

        foreach (var p in SegmentEmbedding.Parameters()) yield return p;

        for (var i = 0; i < _layers.Length; i++)
            foreach (var p in _layers[i].Parameters())
                yield return p;

        foreach (var p in FinalNorm.Parameters()) yield return p;

        foreach (var p in PoolerDense.Parameters()) yield return p;

        foreach (var p in MlmDense.Parameters()) yield return p;

        foreach (var p in MlmNorm.Parameters()) yield return p;

        foreach (var p in MlmDecoder.Parameters()) yield return p;

        foreach (var p in NspClassifier.Parameters()) yield return p;
    }

    #endregion

    #region BERT 前向传播

    /// <summary>
    ///     BERT 完整前向传播：返回序列输出和池化输出
    /// </summary>
    /// <param name="inputIds">输入 token ID [batch, seqLen]</param>
    /// <param name="segmentIds">段 ID [batch, seqLen]，null 时默认全零</param>
    /// <returns>元组：(sequenceOutput [batch, seqLen, dModel], pooledOutput [batch, dModel])</returns>
    public (ArrayND sequenceOutput, ArrayND pooledOutput) ForwardBert(ArrayND inputIds, ArrayND? segmentIds)
    {
        var batch = inputIds.Shape[0];
        var seqLen = inputIds.Shape[1];

        var tokenEmb = TokenEmbedding.Forward(inputIds);
        var posIds = CreatePositionIds(batch, seqLen);
        var posEmb = PosEmbedding.Forward(posIds);
        var segIds = segmentIds ?? ArrayND.Zeros(batch, seqLen);
        var segEmb = SegmentEmbedding.Forward(segIds);

        var x = tokenEmb + posEmb + segEmb;
        x = EmbeddingDropout.Forward(x);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x);

        var sequenceOutput = FinalNorm.Forward(x);
        var firstToken = sequenceOutput.Slice(1, 0, 1).Reshape(batch, DModel);
        var pooledOutput = Activations.TanhForward(PoolerDense.forward(firstToken));

        return (sequenceOutput, pooledOutput);
    }

    /// <summary>
    ///     BERT 完整前向传播（带自动微分记录）
    /// </summary>
    /// <param name="inputIds">输入 token ID [batch, seqLen]</param>
    /// <param name="segmentIds">段 ID [batch, seqLen]，null 时默认全零</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>元组：(sequenceOutput [batch, seqLen, dModel], pooledOutput [batch, dModel])</returns>
    public (ArrayND sequenceOutput, ArrayND pooledOutput) ForwardBert(
        ArrayND inputIds, ArrayND? segmentIds, AutogradContext ctx)
    {
        var batch = inputIds.Shape[0];
        var seqLen = inputIds.Shape[1];

        var tokenEmb = TokenEmbedding.Forward(inputIds, ctx);
        var posIds = CreatePositionIds(batch, seqLen);
        var posEmb = PosEmbedding.Forward(posIds, ctx);
        var segIds = segmentIds ?? ArrayND.Zeros(batch, seqLen);
        var segEmb = SegmentEmbedding.Forward(segIds, ctx);

        var x = AddArraysWithGrad(tokenEmb, posEmb, ctx);
        x = AddArraysWithGrad(x, segEmb, ctx);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x, ctx);

        var sequenceOutput = FinalNorm.Forward(x, ctx);
        var firstToken = ExtractFirstToken(sequenceOutput, batch, seqLen, ctx);
        var pooledOutput = Activations.Tanh(PoolerDense.forward(firstToken, ctx), ctx);

        return (sequenceOutput, pooledOutput);
    }

    #endregion

    #region 预训练头

    /// <summary>
    ///     MLM 预测头：Dense + GELU + LayerNorm + Dense(vocabSize)
    /// </summary>
    /// <param name="sequenceOutput">序列输出 [batch, seqLen, dModel]</param>
    /// <returns>MLM logits [batch, seqLen, vocabSize]</returns>
    public ArrayND MlmHead(ArrayND sequenceOutput)
    {
        var x = Activations.GELUForward(MlmDense.forward(sequenceOutput));
        x = MlmNorm.Forward(x);
        return MlmDecoder.forward(x);
    }

    /// <summary>
    ///     MLM 预测头（带自动微分记录）
    /// </summary>
    /// <param name="sequenceOutput">序列输出 [batch, seqLen, dModel]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>MLM logits [batch, seqLen, vocabSize]</returns>
    public ArrayND MlmHead(ArrayND sequenceOutput, AutogradContext ctx)
    {
        var x = Activations.GELU(MlmDense.forward(sequenceOutput, ctx), ctx);
        x = MlmNorm.Forward(x, ctx);
        return MlmDecoder.forward(x, ctx);
    }

    /// <summary>
    ///     NSP 预测头：Dense(2) 作用于池化输出
    /// </summary>
    /// <param name="pooledOutput">池化输出 [batch, dModel]</param>
    /// <returns>NSP logits [batch, 2]</returns>
    public ArrayND NspHead(ArrayND pooledOutput)
    {
        return NspClassifier.forward(pooledOutput);
    }

    /// <summary>
    ///     NSP 预测头（带自动微分记录）
    /// </summary>
    /// <param name="pooledOutput">池化输出 [batch, dModel]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>NSP logits [batch, 2]</returns>
    public ArrayND NspHead(ArrayND pooledOutput, AutogradContext ctx)
    {
        return NspClassifier.forward(pooledOutput, ctx);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     创建位置 ID 张量 [batch, seqLen]，值为 0, 1, 2, ..., seqLen-1
    /// </summary>
    /// <param name="batch">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    /// <returns>位置 ID 张量</returns>
    private static ArrayND CreatePositionIds(int batch, int seqLen)
    {
        var posIds = ArrayND.Zeros(batch, seqLen);
        var span = posIds.AsWriteSpan();
        for (var b = 0; b < batch; b++)
        for (var s = 0; s < seqLen; s++)
            span[b * seqLen + s] = s;

        return posIds;
    }

    /// <summary>
    ///     从序列输出中提取第一个 token 的隐藏状态（带自动微分记录）
    /// </summary>
    /// <param name="sequenceOutput">序列输出 [batch, seqLen, dModel]</param>
    /// <param name="batch">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>第一个 token 的隐藏状态 [batch, dModel]</returns>
    private static ArrayND ExtractFirstToken(
        ArrayND sequenceOutput, int batch, int seqLen, AutogradContext ctx)
    {
        var dModel = sequenceOutput.Shape[2];
        var firstToken = ArrayND.Zeros(batch, dModel);
        var spanSrc = sequenceOutput.AsSpan();
        var spanDst = firstToken.AsWriteSpan();

        for (var b = 0; b < batch; b++)
        {
            var srcOff = b * seqLen * dModel;
            var dstOff = b * dModel;
            for (var d = 0; d < dModel; d++) spanDst[dstOff + d] = spanSrc[srcOff + d];
        }

        ctx.Record(firstToken, [sequenceOutput], outputGrads =>
        {
            var dFirst = outputGrads[0];
            var dSeq = ArrayND.Zeros(batch, seqLen, dModel);
            var spanDFirst = dFirst.AsSpan();
            var spanDSeq = dSeq.AsWriteSpan();

            for (var b = 0; b < batch; b++)
            {
                var dstOff = b * seqLen * dModel;
                var srcOff = b * dModel;
                for (var d = 0; d < dModel; d++) spanDSeq[dstOff + d] = spanDFirst[srcOff + d];
            }

            return [dSeq];
        });

        return firstToken;
    }

    private static ArrayND AddArraysWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];

        ctx.Record(result, [a, b], outputGrads =>
        {
            var dSum = outputGrads[0];
            return [dSum, dSum];
        });

        return result;
    }

    #endregion
}