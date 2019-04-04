using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     Grouped Query Attention (GQA) —— LLaMA 2/3 风格注意力
///     多个 query head 共享同一组 key/value head，减少 KV Cache 大小
///     当 numKVHeads == numHeads 时退化为标准 MHA
///     当 numKVHeads == 1 时退化为 Multi-Query Attention (MQA)
/// </summary>
public sealed class GroupedQueryAttention : ILayer
{
    private readonly Dense _kProj;
    private readonly int _numGroups;
    private readonly Dense _outProj;
    private readonly Dense _qProj;
    private readonly float _roPETheta;
    private readonly Dense _vProj;

    /// <summary>
    ///     创建 Grouped Query Attention
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">Query 头数</param>
    /// <param name="numKVHeads">
    ///     KV 头数（<= numHeads，必须能整除 numHeads）</param>
    /// <param name="useRoPE">是否使用旋转位置编码</param>
    /// <param name="roPETheta">RoPE 频率基数</param>
    public GroupedQueryAttention(int dModel, int numHeads, int numKVHeads,
        bool useRoPE = false, float roPETheta = 10000.0f)
    {
        if (numHeads % numKVHeads != 0)
            throw new ArgumentException($"numHeads ({numHeads}) 必须能被 numKVHeads ({numKVHeads}) 整除");

        DModel = dModel;
        NumHeads = numHeads;
        NumKVHeads = numKVHeads;
        DK = dModel / numHeads;
        _numGroups = numHeads / numKVHeads;
        UseRoPE = useRoPE;
        _roPETheta = roPETheta;

        _qProj = new Dense(dModel, numHeads * DK);
        _kProj = new Dense(dModel, numKVHeads * DK);
        _vProj = new Dense(dModel, numKVHeads * DK);
        _outProj = new Dense(dModel, dModel);
    }

    /// <summary>
    ///     模型维度
    /// </summary>
    public int DModel { get; }

    /// <summary>
    ///     Query 头数
    /// </summary>
    public int NumHeads { get; }

    /// <summary>
    ///     KV 头数
    /// </summary>
    public int NumKVHeads { get; }

    /// <summary>
    ///     每个头的维度
    /// </summary>
    public int DK { get; }

    /// <summary>
    ///     是否使用旋转位置编码
    /// </summary>
    public bool UseRoPE { get; }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _qProj.Parameters()
            .Concat(_kProj.Parameters())
            .Concat(_vProj.Parameters())
            .Concat(_outProj.Parameters());
    }

    /// <summary>
    ///     前向传播
    /// </summary>
    /// <param name="x">输入 [batch, seqLen, dModel]</param>
    /// <param name="mask">注意力掩码（可选）</param>
    /// <returns>输出 [batch, seqLen, dModel]</returns>
    public ArrayND Forward(ArrayND x, ArrayND? mask = null)
    {
        var batch = x.Shape[0];
        var seqLen = x.Shape[1];

        var q = _qProj.forward(x).Reshape(batch, seqLen, NumHeads, DK).Transpose(1, 2);
        q = q.Reshape(batch * NumHeads, seqLen, DK);

        var k = _kProj.forward(x).Reshape(batch, seqLen, NumKVHeads, DK).Transpose(1, 2);
        k = k.Reshape(batch * NumKVHeads, seqLen, DK);

        var v = _vProj.forward(x).Reshape(batch, seqLen, NumKVHeads, DK).Transpose(1, 2);
        v = v.Reshape(batch * NumKVHeads, seqLen, DK);

        if (UseRoPE) RotaryPositionEmbedding.ApplyRotaryEmbedding(q, k, 0, _roPETheta);

        k = RepeatKV(k, _numGroups, batch);
        v = RepeatKV(v, _numGroups, batch);

        var maskB = mask != null
            ? ExpandMaskForHeads(mask, batch, NumHeads, seqLen)
            : null;

        var attnOut = Attention.ScaledDotProductAttention(q, k, v, maskB);
        attnOut = attnOut.Reshape(batch, NumHeads, seqLen, DK).Transpose(1, 2);
        attnOut = attnOut.Reshape(batch, seqLen, DModel);

        return _outProj.forward(attnOut);
    }

    /// <summary>
    ///     带自动微分的前向
    /// </summary>
    public ArrayND Forward(ArrayND x, ArrayND? mask, AutogradContext ctx)
    {
        var output = Forward(x, mask);
        ctx.Record(output, [x], outputGrads => [outputGrads[0]]);
        return output;
    }

    /// <summary>
    ///     分离 QKV 投影（用于 KV Cache 推理）
    /// </summary>
    /// <param name="x">输入 [batch, seqLen, dModel]</param>
    /// <returns>Q [batch*numHeads, seqLen, dK], K [batch*numKVHeads, seqLen, dK], V [batch*numKVHeads, seqLen, dK]</returns>
    public (ArrayND q, ArrayND k, ArrayND v) ForwardQKV(ArrayND x)
    {
        var batch = x.Shape[0];
        var seqLen = x.Shape[1];

        var q = _qProj.forward(x).Reshape(batch, seqLen, NumHeads, DK).Transpose(1, 2);
        q = q.Reshape(batch * NumHeads, seqLen, DK);

        var k = _kProj.forward(x).Reshape(batch, seqLen, NumKVHeads, DK).Transpose(1, 2);
        k = k.Reshape(batch * NumKVHeads, seqLen, DK);

        var v = _vProj.forward(x).Reshape(batch, seqLen, NumKVHeads, DK).Transpose(1, 2);
        v = v.Reshape(batch * NumKVHeads, seqLen, DK);

        if (UseRoPE) RotaryPositionEmbedding.ApplyRotaryEmbedding(q, k, 0, _roPETheta);

        return (q, k, v);
    }

    /// <summary>
    ///     仅执行输出投影
    /// </summary>
    /// <param name="attnOut">注意力输出 [batch, seqLen, dModel]</param>
    /// <returns>投影后输出</returns>
    public ArrayND ForwardOutput(ArrayND attnOut)
    {
        return _outProj.forward(attnOut);
    }

    /// <summary>
    ///     将 KV 头重复以匹配 Q 头数
    ///     k: [batch*numKVHeads, seqLen, dK] → [batch*numHeads, seqLen, dK]
    /// </summary>
    private ArrayND RepeatKV(ArrayND kv, int numGroups, int batch)
    {
        var seqLen = kv.Shape[1];
        var dK = kv.Shape[2];
        var totalKVHeads = batch * NumKVHeads;
        var totalHeads = batch * NumHeads;

        var result = ArrayND.Zeros(totalHeads, seqLen, dK);
        var spanSrc = kv.AsSpan();
        var spanDst = result.AsWriteSpan();

        for (var b = 0; b < batch; b++)
        for (var kvH = 0; kvH < NumKVHeads; kvH++)
        for (var g = 0; g < numGroups; g++)
        {
            var qH = kvH * numGroups + g;
            var srcOff = (b * NumKVHeads + kvH) * seqLen * dK;
            var dstOff = (b * NumHeads + qH) * seqLen * dK;
            for (var i = 0; i < seqLen * dK; i++) spanDst[dstOff + i] = spanSrc[srcOff + i];
        }

        return result;
    }

    private static ArrayND ExpandMaskForHeads(ArrayND mask, int batch, int numHeads, int seqLen)
    {
        if (mask.Shape.Length == 2)
        {
            var result = ArrayND.Zeros(batch * numHeads, seqLen, seqLen);
            var spanM = mask.AsSpan();
            var spanR = result.AsWriteSpan();
            for (var bh = 0; bh < batch * numHeads; bh++)
            {
                var off = bh * seqLen * seqLen;
                for (var i = 0; i < seqLen * seqLen; i++)
                    spanR[off + i] = spanM[i];
            }

            return result;
        }

        mask = mask.Reshape(batch, seqLen, seqLen);
        var result2 = ArrayND.Zeros(batch * numHeads, seqLen, seqLen);
        var spanM2 = mask.AsSpan();
        var spanR2 = result2.AsWriteSpan();
        for (var b = 0; b < batch; b++)
        for (var h = 0; h < numHeads; h++)
        {
            var srcOff = b * seqLen * seqLen;
            var dstOff = (b * numHeads + h) * seqLen * seqLen;
            for (var i = 0; i < seqLen * seqLen; i++)
                spanR2[dstOff + i] = spanM2[srcOff + i];
        }

        return result2;
    }
}

/// <summary>
///     LLaMA 风格 Transformer 解码器层 —— 使用 GQA + SwiGLU + RMSNorm + RoPE
///     Pre-Norm 结构，支持 KV Cache 推理
/// </summary>
public sealed class LlamaDecoderLayer : ILayer
{
    private readonly int _dK;
    private readonly int _numHeads;
    private readonly int _numKVHeads;
    private readonly float _roPETheta;
    private readonly bool _useRoPE;

    /// <summary>
    ///     创建 LLaMA 解码器层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">Query 头数</param>
    /// <param name="numKVHeads">KV 头数</param>
    /// <param name="dFF">FFN 中间维度</param>
    /// <param name="useRoPE">是否使用旋转位置编码</param>
    /// <param name="roPETheta">RoPE 频率基数</param>
    public LlamaDecoderLayer(int dModel, int numHeads, int numKVHeads, int dFF = 0,
        bool useRoPE = false, float roPETheta = 10000.0f)
    {
        _numHeads = numHeads;
        _numKVHeads = numKVHeads;
        _dK = dModel / numHeads;
        _useRoPE = useRoPE;
        _roPETheta = roPETheta;

        Norm1 = new RMSNorm(dModel);
        Attention = new GroupedQueryAttention(dModel, numHeads, numKVHeads, useRoPE, roPETheta);
        Norm2 = new RMSNorm(dModel);
        FFN = new SwiGLUFFN(dModel, dFF > 0 ? dFF : dModel * 4);
    }

    /// <summary>
    ///     注意力层
    /// </summary>
    public GroupedQueryAttention Attention { get; }

    /// <summary>
    ///     FFN 层
    /// </summary>
    public SwiGLUFFN FFN { get; }

    /// <summary>
    ///     Norm1
    /// </summary>
    public RMSNorm Norm1 { get; }

    /// <summary>
    ///     Norm2
    /// </summary>
    public RMSNorm Norm2 { get; }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return Norm1.Parameters()
            .Concat(Attention.Parameters())
            .Concat(Norm2.Parameters())
            .Concat(FFN.Parameters());
    }

    /// <summary>
    ///     前向传播（Pre-Norm + 残差）
    /// </summary>
    /// <param name="x">输入 [batch, seqLen, dModel]</param>
    /// <param name="mask">注意力掩码</param>
    /// <returns>输出 [batch, seqLen, dModel]</returns>
    public ArrayND Forward(ArrayND x, ArrayND? mask = null)
    {
        var normed1 = Norm1.forward(x);
        var attnOut = Attention.Forward(normed1, mask);
        x = AddArrays(x, attnOut);

        var normed2 = Norm2.forward(x);
        var ffnOut = FFN.forward(normed2);
        x = AddArrays(x, ffnOut);

        return x;
    }

    /// <summary>
    ///     带自动微分的前向
    /// </summary>
    public ArrayND Forward(ArrayND x, ArrayND? mask, AutogradContext ctx)
    {
        var normed1 = Norm1.forward(x, ctx);
        var attnOut = Attention.Forward(normed1, mask, ctx);
        var residual1 = AddArraysWithGrad(x, attnOut, ctx);

        var normed2 = Norm2.forward(residual1, ctx);
        var ffnOut = FFN.forward(normed2, ctx);
        var output = AddArraysWithGrad(residual1, ffnOut, ctx);

        return output;
    }

    private static ArrayND AddArrays(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++)
            spanR[i] = spanA[i] + spanB[i];
        return result;
    }

    private static ArrayND AddArraysWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var sum = AddArrays(a, b);
        ctx.Record(sum, [a, b], grads =>
        {
            var dSum = grads[0];
            return [dSum, dSum];
        });
        return sum;
    }
}

/// <summary>
///     LLaMA 风格语言模型 —— GQA + SwiGLU + RMSNorm + RoPE
///     支持 LLaMA 2/3、Mistral、Qwen 等现代 LLM 架构
/// </summary>
public sealed class LlamaModel : ITrainableModel
{
    private readonly LlamaDecoderLayer[] _layers;
    private readonly int _maxSeqLen;
    private readonly int _numHeads;
    private readonly int _numKVHeads;
    private readonly float _roPETheta;
    private readonly bool _useRoPE;

    /// <summary>
    ///     创建 LLaMA 模型
    /// </summary>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">Query 头数</param>
    /// <param name="numKVHeads">
    ///     KV 头数（< numHeads 启用 GQA）</param>
    /// <param name="numLayers">层数</param>
    /// <param name="dFF">FFN 中间维度（0 = 4×dModel）</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    /// <param name="useRoPE">是否使用旋转位置编码</param>
    /// <param name="roPETheta">RoPE 频率基数</param>
    public LlamaModel(int vocabSize, int dModel, int numHeads, int numKVHeads, int numLayers,
        int dFF = 0, int maxSeqLen = 2048, bool useRoPE = true, float roPETheta = 10000.0f)
    {
        VocabSize = vocabSize;
        DModel = dModel;
        _numHeads = numHeads;
        _numKVHeads = numKVHeads;
        NumLayers = numLayers;
        _maxSeqLen = maxSeqLen;
        _useRoPE = useRoPE;
        _roPETheta = roPETheta;

        TokenEmbedding = new Embedding(vocabSize, dModel);
        _layers = new LlamaDecoderLayer[numLayers];
        for (var i = 0; i < numLayers; i++)
            _layers[i] = new LlamaDecoderLayer(dModel, numHeads, numKVHeads,
                dFF > 0 ? dFF : dModel * 4, useRoPE, roPETheta);

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
    public LlamaDecoderLayer GetLayer(int index)
    {
        return _layers[index];
    }

    /// <summary>
    ///     带掩码的前向传播
    /// </summary>
    /// <param name="input">输入 token ID [batch, seqLen]</param>
    /// <param name="mask">注意力掩码</param>
    /// <returns>logits [batch, seqLen, vocabSize]</returns>
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
        if (ctx != null)
            x = TokenEmbedding.Forward(input, ctx);
        else
            x = TokenEmbedding.Forward(input);

        if (mask == null)
        {
            var seqLen = input.Shape[1];
            mask = TransformerDecoderLayer.CreateCausalMask(seqLen);
        }

        for (var i = 0; i < NumLayers; i++)
            if (ctx != null)
                x = _layers[i].Forward(x, mask, ctx);
            else
                x = _layers[i].Forward(x, mask);

        if (ctx != null)
        {
            x = FinalNorm.forward(x, ctx);
            return LMHead.forward(x, ctx);
        }

        x = FinalNorm.forward(x);
        return LMHead.forward(x);
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
        var mask = TransformerDecoderLayer.CreateCausalMask(seqLen);

        for (var i = 0; i < NumLayers; i++) x = _layers[i].Forward(x, mask, ctx);

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
    /// <returns>生成器</returns>
    public AutoregressiveGenerator CreateGenerator()
    {
        return new AutoregressiveGenerator(input => forward(input), VocabSize);
    }
}