namespace Std.DL.Flux;

/// <summary>
///     Sliding Window Attention —— Mistral 风格局部注意力
///     每个 token 只关注最近 windowSize 个 token，而非全部历史
///     内存从 O(N²) 降到 O(N × W)，支持超长序列
///     与 KV Cache 配合时，只需缓存最近 windowSize 个 KV 对
/// </summary>
public static class SlidingWindowAttention
{
    /// <summary>
    ///     创建滑动窗口因果掩码
    ///     位置 i 只能关注 [max(0, i - windowSize + 1), i] 范围内的位置
    /// </summary>
    /// <param name="seqLen">序列长度</param>
    /// <param name="windowSize">窗口大小</param>
    /// <returns>掩码 [seqLen, seqLen]，0 表示可关注，-inf 表示屏蔽</returns>
    public static ArrayND CreateSlidingWindowMask(int seqLen, int windowSize)
    {
        var mask = ArrayND.Zeros(seqLen, seqLen);
        var span = mask.AsWriteSpan();

        for (var i = 0; i < seqLen; i++)
        {
            var start = System.Math.Max(0, i - windowSize + 1);
            for (var j = 0; j < seqLen; j++)
                if (j > i || j < start)
                    span[i * seqLen + j] = float.NegativeInfinity;
        }

        return mask;
    }

    /// <summary>
    ///     创建带填充的滑动窗口因果掩码
    ///     同时处理 padding 和滑动窗口限制
    /// </summary>
    /// <param name="seqLen">序列长度</param>
    /// <param name="windowSize">窗口大小</param>
    /// <param name="paddingMask">填充掩码 [batch, seqLen]，1=有效，0=padding</param>
    /// <returns>掩码 [batch, seqLen, seqLen]</returns>
    public static ArrayND CreateSlidingWindowMaskWithPadding(int seqLen, int windowSize, ArrayND paddingMask)
    {
        var batch = paddingMask.Shape[0];
        var mask = ArrayND.Zeros(batch, seqLen, seqLen);
        var spanM = mask.AsWriteSpan();
        var spanP = paddingMask.AsSpan();

        for (var b = 0; b < batch; b++)
        for (var i = 0; i < seqLen; i++)
        {
            var iValid = spanP[b * seqLen + i] > 0.5f;
            var start = System.Math.Max(0, i - windowSize + 1);

            for (var j = 0; j < seqLen; j++)
            {
                var jValid = spanP[b * seqLen + j] > 0.5f;
                var off = b * seqLen * seqLen + i * seqLen + j;

                if (!iValid || !jValid || j > i || j < start) spanM[off] = float.NegativeInfinity;
            }
        }

        return mask;
    }

    /// <summary>
    ///     滑动窗口缩放点积注意力
    ///     等价于标准 ScaledDotProductAttention + 滑动窗口掩码
    /// </summary>
    /// <param name="q">查询 [batch*heads, seqLenQ, dK]</param>
    /// <param name="k">键 [batch*heads, seqLenK, dK]</param>
    /// <param name="v">值 [batch*heads, seqLenK, dV]</param>
    /// <param name="windowSize">窗口大小</param>
    /// <returns>注意力输出 [batch*heads, seqLenQ, dV]</returns>
    public static ArrayND Compute(ArrayND q, ArrayND k, ArrayND v, int windowSize)
    {
        var seqLen = q.Shape[1];
        var mask = CreateSlidingWindowMask(seqLen, windowSize);
        return Attention.ScaledDotProductAttention(q, k, v, mask);
    }

    /// <summary>
    ///     计算滑动窗口注意力的有效 KV 缓存长度
    ///     对于位置 pos，只需缓存 [max(0, pos - windowSize + 1), pos] 范围的 KV
    /// </summary>
    /// <param name="currentPos">当前序列位置</param>
    /// <param name="windowSize">窗口大小</param>
    /// <returns>需要缓存的最小 KV 数量</returns>
    public static int EffectiveCacheLength(int currentPos, int windowSize)
    {
        return System.Math.Min(currentPos + 1, windowSize);
    }
}

/// <summary>
///     Mistral 风格解码器层 —— GQA + SwiGLU + RMSNorm + RoPE + Sliding Window Attention
///     与 LlamaDecoderLayer 的区别：使用滑动窗口注意力，支持超长序列
/// </summary>
public sealed class MistralDecoderLayer : ILayer
{
    private readonly RMSNorm _norm1;
    private readonly RMSNorm _norm2;

    /// <summary>
    ///     创建 Mistral 解码器层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">Query 头数</param>
    /// <param name="numKVHeads">KV 头数</param>
    /// <param name="windowSize">滑动窗口大小</param>
    /// <param name="dFF">FFN 中间维度</param>
    /// <param name="useRoPE">是否使用旋转位置编码</param>
    /// <param name="roPETheta">RoPE 频率基数</param>
    public MistralDecoderLayer(int dModel, int numHeads, int numKVHeads, int windowSize,
        int dFF = 0, bool useRoPE = true, float roPETheta = 10000.0f)
    {
        WindowSize = windowSize;
        _norm1 = new RMSNorm(dModel);
        Attention = new GroupedQueryAttention(dModel, numHeads, numKVHeads, useRoPE, roPETheta);
        _norm2 = new RMSNorm(dModel);
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
    ///     窗口大小
    /// </summary>
    public int WindowSize { get; }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _norm1.Parameters()
            .Concat(Attention.Parameters())
            .Concat(_norm2.Parameters())
            .Concat(FFN.Parameters());
    }

    /// <summary>
    ///     前向传播（Pre-Norm + 残差 + 滑动窗口注意力）
    /// </summary>
    /// <param name="x">输入 [batch, seqLen, dModel]</param>
    /// <param name="mask">注意力掩码（可选，null 时自动创建滑动窗口掩码）</param>
    /// <returns>输出 [batch, seqLen, dModel]</returns>
    public ArrayND Forward(ArrayND x, ArrayND? mask = null)
    {
        var seqLen = x.Shape[1];
        var swMask = mask ?? SlidingWindowAttention.CreateSlidingWindowMask(seqLen, WindowSize);

        var normed1 = _norm1.forward(x);
        var attnOut = Attention.Forward(normed1, swMask);
        x = AddArrays(x, attnOut);

        var normed2 = _norm2.forward(x);
        var ffnOut = FFN.forward(normed2);
        x = AddArrays(x, ffnOut);

        return x;
    }

    /// <summary>
    ///     带自动微分的前向
    /// </summary>
    public ArrayND Forward(ArrayND x, ArrayND? mask, AutogradContext ctx)
    {
        var seqLen = x.Shape[1];
        var swMask = mask ?? SlidingWindowAttention.CreateSlidingWindowMask(seqLen, WindowSize);

        var normed1 = _norm1.forward(x, ctx);
        var attnOut = Attention.Forward(normed1, swMask, ctx);
        var residual1 = AddArraysWithGrad(x, attnOut, ctx);

        var normed2 = _norm2.forward(residual1, ctx);
        var ffnOut = FFN.forward(normed2, ctx);
        return AddArraysWithGrad(residual1, ffnOut, ctx);
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