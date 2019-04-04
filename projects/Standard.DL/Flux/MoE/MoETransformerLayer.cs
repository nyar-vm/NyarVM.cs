using Std.DL.Training;

namespace Std.DL.Flux.MoE;

/// <summary>
///     MoE Transformer 解码器层 —— 用混合专家前馈网络替代标准 FFN 的 Transformer 层
///     Pre-Norm 结构：x = x + Attention(RMSNorm(x))
///     x = x + MoE(RMSNorm(x))
/// </summary>
public sealed class MoETransformerLayer : ITrainableModel
{
    private readonly float _dropoutRate;
    private Func<int, ArrayND>? _causalMaskFn;

    /// <summary>
    ///     创建 MoE Transformer 解码器层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dFFN">前馈维度</param>
    /// <param name="numExperts">专家数量</param>
    /// <param name="topK">每个 token 选择的专家数</param>
    /// <param name="dropoutRate">Dropout 比率（保留参数，暂未使用）</param>
    public MoETransformerLayer(
        int dModel,
        int numHeads,
        int dFFN,
        int numExperts,
        int topK = 2,
        float dropoutRate = 0.0f)
    {
        Attention = new MultiHeadAttention(dModel, numHeads);
        MoE = new MoELayer(dModel, dFFN, numExperts, topK);
        Norm1 = new RMSNorm(dModel);
        Norm2 = new RMSNorm(dModel);
        _causalMaskFn = null;
        _dropoutRate = dropoutRate;
    }

    /// <summary>多头注意力层</summary>
    public MultiHeadAttention Attention { get; }

    /// <summary>混合专家前馈网络</summary>
    public MoELayer MoE { get; }

    /// <summary>注意力前的归一化层</summary>
    public RMSNorm Norm1 { get; }

    /// <summary>MoE 前的归一化层</summary>
    public RMSNorm Norm2 { get; }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    /// <param name="input">输入张量 [batch, seqLen, dModel]</param>
    /// <returns>输出张量 [batch, seqLen, dModel]</returns>
    public ArrayND forward(ArrayND input)
    {
        var normed1 = Norm1.forward(input);
        var attnOut = _causalMaskFn != null
            ? Attention.Forward(normed1, _causalMaskFn(normed1.Shape[1]))
            : Attention.Forward(normed1);
        var res1 = AddArrays(input, attnOut);

        var normed2 = Norm2.forward(res1);
        var moeOut = MoE.forward(normed2);
        var res2 = AddArrays(res1, moeOut);

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
        var attnOut = _causalMaskFn != null
            ? Attention.Forward(normed1, _causalMaskFn(normed1.Shape[1]), ctx)
            : Attention.Forward(normed1, null, ctx);
        var res1 = AddArraysWithGrad(input, attnOut, ctx);

        var normed2 = Norm2.forward(res1, ctx);
        var moeOut = MoE.forward(normed2, ctx);
        var res2 = AddArraysWithGrad(res1, moeOut, ctx);

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

        foreach (var p in MoE.Parameters()) yield return p;
    }

    /// <summary>
    ///     设置因果掩码生成函数
    /// </summary>
    /// <param name="causalMaskFn">因果掩码生成函数（输入 seqLen，返回掩码）</param>
    public void SetCausalMask(Func<int, ArrayND> causalMaskFn)
    {
        _causalMaskFn = causalMaskFn;
    }

    /// <summary>
    ///     创建标准因果掩码（下三角矩阵，-inf 在上三角）
    /// </summary>
    /// <param name="seqLen">序列长度</param>
    /// <returns>[seqLen, seqLen] 因果掩码</returns>
    public static ArrayND CreateCausalMask(int seqLen)
    {
        var mask = ArrayND.Zeros(seqLen, seqLen);
        var span = mask.AsWriteSpan();

        for (var i = 0; i < seqLen; i++)
        for (var j = 0; j < seqLen; j++)
            if (j > i)
                span[i * seqLen + j] = float.NegativeInfinity;

        return mask;
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