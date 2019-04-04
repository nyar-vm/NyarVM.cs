using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     Transformer 解码器层 —— GPT / LLaMA / Mistral 的标准构建块
///     Pre-Norm 结构：x = x + Attention(RMSNorm(x))
///     x = x + FFN(RMSNorm(x))
/// </summary>
public sealed class TransformerDecoderLayer : ITrainableModel
{
    private readonly Func<int, ArrayND>? _causalMaskFn;

    /// <summary>
    ///     创建 Transformer 解码器层（Pre-Norm 结构）
    /// </summary>
    /// <param name="attention">多头注意力层</param>
    /// <param name="ffn">前馈网络（如 SwiGLUFFN）</param>
    /// <param name="norm1">注意力前的归一化层</param>
    /// <param name="norm2">FFN 前的归一化层</param>
    /// <param name="causalMaskFn">因果掩码生成函数（输入 seqLen，返回掩码）</param>
    public TransformerDecoderLayer(
        MultiHeadAttention attention,
        ITrainableModel ffn,
        RMSNorm norm1,
        RMSNorm norm2,
        Func<int, ArrayND>? causalMaskFn = null)
    {
        Attention = attention;
        FFN = ffn;
        Norm1 = norm1;
        Norm2 = norm2;
        _causalMaskFn = causalMaskFn;
    }

    /// <summary>
    ///     注意力层
    /// </summary>
    public MultiHeadAttention Attention { get; }

    /// <summary>
    ///     前馈网络
    /// </summary>
    public ITrainableModel FFN { get; }

    /// <summary>
    ///     第一个归一化层
    /// </summary>
    public RMSNorm Norm1 { get; }

    /// <summary>
    ///     第二个归一化层
    /// </summary>
    public RMSNorm Norm2 { get; }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        var normed1 = Norm1.forward(input);
        var attnOut = _causalMaskFn != null
            ? Attention.Forward(normed1, _causalMaskFn(normed1.Shape[1]))
            : Attention.Forward(normed1);
        var res1 = AddArrays(input, attnOut);

        var normed2 = Norm2.forward(res1);
        var ffnOut = FFN.forward(normed2);
        var res2 = AddArrays(res1, ffnOut);

        return res2;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var normed1 = Norm1.forward(input, ctx);
        var attnOut = _causalMaskFn != null
            ? Attention.Forward(normed1, _causalMaskFn(normed1.Shape[1]), ctx)
            : Attention.Forward(normed1, null, ctx);
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
}