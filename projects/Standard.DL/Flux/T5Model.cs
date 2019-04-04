using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     T5 编码器层 —— Pre-Norm Transformer 编码器构建块
///     结构：x = x + SelfAttention(RMSNorm(x))
///     x = x + SwiGLUFFN(RMSNorm(x))
/// </summary>
public sealed class T5EncoderLayer : ITrainableModel
{
    /// <summary>
    ///     创建 T5 编码器层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dFFN">前馈网络隐藏维度</param>
    public T5EncoderLayer(int dModel, int numHeads, int dFFN)
    {
        Norm1 = new RMSNorm(dModel);
        SelfAttention = new MultiHeadAttention(dModel, numHeads);
        Norm2 = new RMSNorm(dModel);
        FFN = new SwiGLUFFN(dModel, dFFN);
    }

    /// <summary>
    ///     第一个归一化层（自注意力前）
    /// </summary>
    public RMSNorm Norm1 { get; }

    /// <summary>
    ///     多头自注意力层
    /// </summary>
    public MultiHeadAttention SelfAttention { get; }

    /// <summary>
    ///     第二个归一化层（FFN 前）
    /// </summary>
    public RMSNorm Norm2 { get; }

    /// <summary>
    ///     SwiGLU 前馈网络
    /// </summary>
    public SwiGLUFFN FFN { get; }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    /// <param name="input">输入张量 [batch, seqLen, dModel]</param>
    /// <returns>输出张量 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND input)
    {
        var normed1 = Norm1.forward(input);
        var attnOut = SelfAttention.Forward(normed1);
        var res1 = AddArrays(input, attnOut);

        var normed2 = Norm2.forward(res1);
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
        var normed1 = Norm1.forward(input, ctx);
        var attnOut = SelfAttention.Forward(normed1, null, ctx);
        var res1 = AddArraysWithGrad(input, attnOut, ctx);

        var normed2 = Norm2.forward(res1, ctx);
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

        foreach (var p in SelfAttention.Parameters()) yield return p;

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
///     T5 解码器层 —— Pre-Norm Transformer 解码器构建块
///     结构：x = x + SelfAttention(RMSNorm(x))  [causal]
///     x = x + CrossAttention(RMSNorm(x), encoderOutput)
///     x = x + SwiGLUFFN(RMSNorm(x))
/// </summary>
public sealed class T5DecoderLayer : ILayer
{
    /// <summary>
    ///     创建 T5 解码器层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dFFN">前馈网络隐藏维度</param>
    public T5DecoderLayer(int dModel, int numHeads, int dFFN)
    {
        Norm1 = new RMSNorm(dModel);
        SelfAttention = new MultiHeadAttention(dModel, numHeads);
        Norm2 = new RMSNorm(dModel);
        CrossAttention = new CrossAttentionLayer(dModel, dModel, numHeads);
        Norm3 = new RMSNorm(dModel);
        FFN = new SwiGLUFFN(dModel, dFFN);
    }

    /// <summary>
    ///     第一个归一化层（自注意力前）
    /// </summary>
    public RMSNorm Norm1 { get; }

    /// <summary>
    ///     多头自注意力层（因果掩码）
    /// </summary>
    public MultiHeadAttention SelfAttention { get; }

    /// <summary>
    ///     第二个归一化层（交叉注意力前）
    /// </summary>
    public RMSNorm Norm2 { get; }

    /// <summary>
    ///     交叉注意力层
    /// </summary>
    public CrossAttentionLayer CrossAttention { get; }

    /// <summary>
    ///     第三个归一化层（FFN 前）
    /// </summary>
    public RMSNorm Norm3 { get; }

    /// <summary>
    ///     SwiGLU 前馈网络
    /// </summary>
    public SwiGLUFFN FFN { get; }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in Norm1.Parameters()) yield return p;

        foreach (var p in SelfAttention.Parameters()) yield return p;

        foreach (var p in Norm2.Parameters()) yield return p;

        foreach (var p in CrossAttention.Parameters()) yield return p;

        foreach (var p in Norm3.Parameters()) yield return p;

        foreach (var p in FFN.Parameters()) yield return p;
    }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    /// <param name="decoderInput">解码器输入 [batch, decSeqLen, dModel]</param>
    /// <param name="encoderOutput">编码器输出 [batch, encSeqLen, dModel]</param>
    /// <returns>解码器输出 [batch, decSeqLen, dModel]</returns>
    public ArrayND Forward(ArrayND decoderInput, ArrayND encoderOutput)
    {
        var normed1 = Norm1.forward(decoderInput);
        var causalMask = TransformerDecoderLayer.CreateCausalMask(normed1.Shape[1]);
        var selfAttnOut = SelfAttention.Forward(normed1, causalMask);
        var res1 = AddArrays(decoderInput, selfAttnOut);

        var normed2 = Norm2.forward(res1);
        var crossAttnOut = CrossAttention.Forward(normed2, encoderOutput);
        var res2 = AddArrays(res1, crossAttnOut);

        var normed3 = Norm3.forward(res2);
        var ffnOut = FFN.forward(normed3);
        var res3 = AddArrays(res2, ffnOut);

        return res3;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="decoderInput">解码器输入 [batch, decSeqLen, dModel]</param>
    /// <param name="encoderOutput">编码器输出 [batch, encSeqLen, dModel]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>解码器输出 [batch, decSeqLen, dModel]</returns>
    public ArrayND Forward(ArrayND decoderInput, ArrayND encoderOutput, AutogradContext ctx)
    {
        var normed1 = Norm1.forward(decoderInput, ctx);
        var causalMask = TransformerDecoderLayer.CreateCausalMask(normed1.Shape[1]);
        var selfAttnOut = SelfAttention.Forward(normed1, causalMask, ctx);
        var res1 = AddArraysWithGrad(decoderInput, selfAttnOut, ctx);

        var normed2 = Norm2.forward(res1, ctx);
        var crossAttnOut = CrossAttention.Forward(normed2, encoderOutput);
        ctx.Record(crossAttnOut, [normed2, encoderOutput], outputGrads =>
        {
            var dOut = outputGrads[0];
            return [dOut, dOut];
        });
        var res2 = AddArraysWithGrad(res1, crossAttnOut, ctx);

        var normed3 = Norm3.forward(res2, ctx);
        var ffnOut = FFN.forward(normed3, ctx);
        var res3 = AddArraysWithGrad(res2, ffnOut, ctx);

        return res3;
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
///     T5 模型 —— 编码器-解码器 Transformer
///     编码器：TokenEmbedding + PositionEmbedding + N × T5EncoderLayer + RMSNorm
///     解码器：TokenEmbedding + PositionEmbedding + N × T5DecoderLayer + RMSNorm + LM Head
///     编码器和解码器共享 Token 嵌入和位置嵌入
/// </summary>
public sealed class T5Model : ITrainableModel
{
    #region 构造函数

    /// <summary>
    ///     创建 T5 模型
    /// </summary>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="numEncoderLayers">编码器层数</param>
    /// <param name="numDecoderLayers">解码器层数</param>
    /// <param name="dFFN">前馈网络隐藏维度</param>
    /// <param name="dropoutRate">Dropout 丢弃率</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    public T5Model(
        int vocabSize,
        int dModel = 512,
        int numHeads = 8,
        int numEncoderLayers = 6,
        int numDecoderLayers = 6,
        int dFFN = 2048,
        float dropoutRate = 0.1f,
        int maxSeqLen = 512)
    {
        VocabSize = vocabSize;
        DModel = dModel;
        NumEncoderLayers = numEncoderLayers;
        NumDecoderLayers = numDecoderLayers;
        MaxSeqLen = maxSeqLen;

        TokenEmbedding = new Embedding(vocabSize, dModel);
        PosEmbedding = new Embedding(maxSeqLen, dModel);

        _encoderLayers = new T5EncoderLayer[numEncoderLayers];
        for (var i = 0; i < numEncoderLayers; i++) _encoderLayers[i] = new T5EncoderLayer(dModel, numHeads, dFFN);

        EncoderFinalNorm = new RMSNorm(dModel);

        _decoderLayers = new T5DecoderLayer[numDecoderLayers];
        for (var i = 0; i < numDecoderLayers; i++) _decoderLayers[i] = new T5DecoderLayer(dModel, numHeads, dFFN);

        DecoderFinalNorm = new RMSNorm(dModel);
        LmHead = new Dense(dModel, vocabSize);
    }

    #endregion

    #region 字段

    private readonly T5EncoderLayer[] _encoderLayers;
    private readonly T5DecoderLayer[] _decoderLayers;

    #endregion

    #region 属性

    /// <summary>
    ///     Token 嵌入层（编码器和解码器共享）
    /// </summary>
    public Embedding TokenEmbedding { get; }

    /// <summary>
    ///     位置嵌入层（编码器和解码器共享）
    /// </summary>
    public Embedding PosEmbedding { get; }

    /// <summary>
    ///     T5 编码器层
    /// </summary>
    public ReadOnlySpan<T5EncoderLayer> EncoderLayers => _encoderLayers;

    /// <summary>
    ///     编码器最终归一化层
    /// </summary>
    public RMSNorm EncoderFinalNorm { get; }

    /// <summary>
    ///     T5 解码器层
    /// </summary>
    public ReadOnlySpan<T5DecoderLayer> DecoderLayers => _decoderLayers;

    /// <summary>
    ///     解码器最终归一化层
    /// </summary>
    public RMSNorm DecoderFinalNorm { get; }

    /// <summary>
    ///     语言模型头
    /// </summary>
    public Dense LmHead { get; }

    /// <summary>
    ///     模型维度
    /// </summary>
    public int DModel { get; }

    /// <summary>
    ///     编码器层数
    /// </summary>
    public int NumEncoderLayers { get; }

    /// <summary>
    ///     解码器层数
    /// </summary>
    public int NumDecoderLayers { get; }

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     最大序列长度
    /// </summary>
    public int MaxSeqLen { get; }

    #endregion

    #region ITrainableModel 实现

    /// <summary>
    ///     前向传播：仅编码器前向，返回编码器输出
    ///     ITrainableModel 接口实现
    /// </summary>
    /// <param name="input">编码器输入 token ID [batch, seqLen]</param>
    /// <returns>编码器输出 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND input)
    {
        return Encode(input);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）：仅编码器前向
    /// </summary>
    /// <param name="input">编码器输入 token ID [batch, seqLen]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>编码器输出 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        return Encode(input, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数（共享嵌入不重复计入）
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in TokenEmbedding.Parameters()) yield return p;

        foreach (var p in PosEmbedding.Parameters()) yield return p;

        for (var i = 0; i < _encoderLayers.Length; i++)
            foreach (var p in _encoderLayers[i].Parameters())
                yield return p;

        foreach (var p in EncoderFinalNorm.Parameters()) yield return p;

        for (var i = 0; i < _decoderLayers.Length; i++)
            foreach (var p in _decoderLayers[i].Parameters())
                yield return p;

        foreach (var p in DecoderFinalNorm.Parameters()) yield return p;

        foreach (var p in LmHead.Parameters()) yield return p;
    }

    #endregion

    #region 编码器

    /// <summary>
    ///     编码器前向传播：encoderInput → encoderOutput
    /// </summary>
    /// <param name="encoderInput">编码器输入 token ID [batch, encSeqLen]</param>
    /// <returns>编码器输出 [batch, encSeqLen, dModel]</returns>
    public ArrayND Encode(ArrayND encoderInput)
    {
        var batch = encoderInput.Shape[0];
        var seqLen = encoderInput.Shape[1];

        var tokenEmb = TokenEmbedding.Forward(encoderInput);
        var posIds = CreatePositionIds(batch, seqLen);
        var posEmb = PosEmbedding.Forward(posIds);
        var x = tokenEmb + posEmb;

        for (var i = 0; i < _encoderLayers.Length; i++) x = _encoderLayers[i].forward(x);

        return EncoderFinalNorm.forward(x);
    }

    /// <summary>
    ///     编码器前向传播（带自动微分记录）
    /// </summary>
    /// <param name="encoderInput">编码器输入 token ID [batch, encSeqLen]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>编码器输出 [batch, encSeqLen, dModel]</returns>
    public ArrayND Encode(ArrayND encoderInput, AutogradContext ctx)
    {
        var batch = encoderInput.Shape[0];
        var seqLen = encoderInput.Shape[1];

        var tokenEmb = TokenEmbedding.Forward(encoderInput, ctx);
        var posIds = CreatePositionIds(batch, seqLen);
        var posEmb = PosEmbedding.Forward(posIds, ctx);
        var x = AddArraysWithGrad(tokenEmb, posEmb, ctx);

        for (var i = 0; i < _encoderLayers.Length; i++) x = _encoderLayers[i].forward(x, ctx);

        return EncoderFinalNorm.forward(x, ctx);
    }

    #endregion

    #region T5 完整前向传播

    /// <summary>
    ///     T5 完整前向传播：编码器 + 解码器 → logits
    /// </summary>
    /// <param name="encoderInput">编码器输入 token ID [batch, encSeqLen]</param>
    /// <param name="decoderInput">解码器输入 token ID [batch, decSeqLen]</param>
    /// <returns>logits [batch, decSeqLen, vocabSize]</returns>
    public ArrayND ForwardT5(ArrayND encoderInput, ArrayND decoderInput)
    {
        var encoderOutput = Encode(encoderInput);

        var batch = decoderInput.Shape[0];
        var decSeqLen = decoderInput.Shape[1];

        var decTokenEmb = TokenEmbedding.Forward(decoderInput);
        var decPosIds = CreatePositionIds(batch, decSeqLen);
        var decPosEmb = PosEmbedding.Forward(decPosIds);
        var x = decTokenEmb + decPosEmb;

        for (var i = 0; i < _decoderLayers.Length; i++) x = _decoderLayers[i].Forward(x, encoderOutput);

        x = DecoderFinalNorm.forward(x);
        return LmHead.forward(x);
    }

    /// <summary>
    ///     T5 完整前向传播（带自动微分记录）
    /// </summary>
    /// <param name="encoderInput">编码器输入 token ID [batch, encSeqLen]</param>
    /// <param name="decoderInput">解码器输入 token ID [batch, decSeqLen]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>logits [batch, decSeqLen, vocabSize]</returns>
    public ArrayND ForwardT5(ArrayND encoderInput, ArrayND decoderInput, AutogradContext ctx)
    {
        var encoderOutput = Encode(encoderInput, ctx);

        var batch = decoderInput.Shape[0];
        var decSeqLen = decoderInput.Shape[1];

        var decTokenEmb = TokenEmbedding.Forward(decoderInput, ctx);
        var decPosIds = CreatePositionIds(batch, decSeqLen);
        var decPosEmb = PosEmbedding.Forward(decPosIds, ctx);
        var x = AddArraysWithGrad(decTokenEmb, decPosEmb, ctx);

        for (var i = 0; i < _decoderLayers.Length; i++) x = _decoderLayers[i].Forward(x, encoderOutput, ctx);

        x = DecoderFinalNorm.forward(x, ctx);
        return LmHead.forward(x, ctx);
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