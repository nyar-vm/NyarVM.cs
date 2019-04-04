namespace Std.DL.Flux;

/// <summary>
///     缩放点积注意力 + 多头注意力
/// </summary>
public static class Attention
{
    /// <summary>
    ///     缩放点积注意力：Scores = softmax(Q × K^T / sqrt(d_k) + mask, axis=-1); Output = Scores × V
    /// </summary>
    /// <param name="q">查询 [..., seqLenQ, dK]</param>
    /// <param name="k">键 [..., seqLenK, dK]</param>
    /// <param name="v">值 [..., seqLenK, dV]</param>
    /// <param name="mask">掩码 [..., seqLenQ, seqLenK]（null 表示无掩码），-inf 处将被遮蔽</param>
    /// <returns>注意力输出 [..., seqLenQ, dV]</returns>
    public static ArrayND ScaledDotProductAttention(ArrayND q, ArrayND k, ArrayND v, ArrayND? mask = null)
    {
        var ndim = q.Shape.Length;
        var dK = q.Shape[ndim - 1];
        var scale = 1.0f / MathF.Sqrt(dK);

        var kT = k.Transpose(ndim - 2, ndim - 1);
        var scores = ArrayND.BatchMatMul(q, kT);

        var spanS = scores.AsWriteSpan();
        for (var i = 0; i < spanS.Length; i++) spanS[i] *= scale;

        if (mask != null)
        {
            var spanM = mask.AsSpan();
            for (var i = 0; i < spanS.Length && i < spanM.Length; i++) spanS[i] += spanM[i];
        }

        var weights = Activations.SoftmaxAxis(scores, ndim - 1);
        return ArrayND.BatchMatMul(weights, v);
    }
}

/// <summary>
///     多头注意力层
/// </summary>
public class MultiHeadAttention : ILayer
{
    internal readonly int _dK;
    internal readonly int _dModel;
    internal readonly int _dQkv;
    internal readonly int _numHeads;
    internal readonly Dense _outProj;
    internal readonly Dense _qkvProj;

    /// <summary>
    ///     创建多头注意力层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    public MultiHeadAttention(int dModel, int numHeads)
    {
        _dModel = dModel;
        _numHeads = numHeads;
        _dK = dModel / numHeads;
        _dQkv = dModel * 3;
        _qkvProj = new Dense(dModel, _dQkv);
        _outProj = new Dense(dModel, dModel);
    }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _qkvProj.Parameters().Concat(_outProj.Parameters());
    }

    /// <summary>
    ///     前向传播
    /// </summary>
    /// <param name="x">输入 [batch, seqLen, dModel]</param>
    /// <param name="mask">注意力掩码 [batch, seqLen, seqLen] 或 null</param>
    /// <returns>输出 [batch, seqLen, dModel]</returns>
    public ArrayND Forward(ArrayND x, ArrayND? mask = null)
    {
        var batch = x.Shape[0];
        var seqLen = x.Shape[1];

        var qkv = _qkvProj.forward(x);
        qkv = qkv.Reshape(batch, seqLen, 3, _numHeads, _dK);

        var q = qkv.Slice(2, 0, 1).Transpose(1, 2);
        q = q.Reshape(batch * _numHeads, seqLen, _dK);

        var k = qkv.Slice(2, 1, 1).Transpose(1, 2);
        k = k.Reshape(batch * _numHeads, seqLen, _dK);

        var v = qkv.Slice(2, 2, 1).Transpose(1, 2);
        v = v.Reshape(batch * _numHeads, seqLen, _dK);

        var maskB = mask != null
            ? RepeatMaskForHeads(mask, batch, _numHeads, seqLen)
            : null;

        var attnOut = Attention.ScaledDotProductAttention(q, k, v, maskB);
        attnOut = attnOut.Reshape(batch, _numHeads, seqLen, _dK).Transpose(1, 2);
        attnOut = attnOut.Reshape(batch, seqLen, _dModel);

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
    ///     仅执行 QKV 投影（用于 KV Cache 推理）
    /// </summary>
    /// <param name="x">输入 [batch, seqLen, dModel]</param>
    /// <returns>Q [batch*numHeads, seqLen, dK], K [batch*numHeads, seqLen, dK], V [batch*numHeads, seqLen, dK]</returns>
    public (ArrayND q, ArrayND k, ArrayND v) ForwardQKV(ArrayND x)
    {
        var batch = x.Shape[0];
        var seqLen = x.Shape[1];

        var qkv = _qkvProj.forward(x);
        qkv = qkv.Reshape(batch, seqLen, 3, _numHeads, _dK);

        var q = qkv.Slice(2, 0, 1).Transpose(1, 2).Reshape(batch * _numHeads, seqLen, _dK);
        var k = qkv.Slice(2, 1, 1).Transpose(1, 2).Reshape(batch * _numHeads, seqLen, _dK);
        var v = qkv.Slice(2, 2, 1).Transpose(1, 2).Reshape(batch * _numHeads, seqLen, _dK);

        return (q, k, v);
    }

    /// <summary>
    ///     仅执行输出投影（用于 KV Cache 推理，已手动计算注意力）
    /// </summary>
    /// <param name="attnOut">注意力输出 [batch, seqLen, dModel]</param>
    /// <returns>投影后输出 [batch, seqLen, dModel]</returns>
    public ArrayND ForwardOutput(ArrayND attnOut)
    {
        return _outProj.forward(attnOut);
    }

    /// <summary>
    ///     将 [batch, seqLen, seqLen] 掩码重复 numHeads 次得到 [batch*numHeads, seqLen, seqLen]
    /// </summary>
    private static ArrayND RepeatMaskForHeads(ArrayND mask, int batch, int numHeads, int seqLen)
    {
        if (mask.Shape.Length == 2)
        {
            var single = mask;
            var resultB = ArrayND.Zeros(batch * numHeads, seqLen, seqLen);
            var spanM = single.AsSpan();
            var spanRB = resultB.AsWriteSpan();
            for (var b = 0; b < batch; b++)
            for (var h = 0; h < numHeads; h++)
            {
                var dstOff = (b * numHeads + h) * seqLen * seqLen;
                for (var i = 0; i < seqLen * seqLen; i++) spanRB[dstOff + i] = spanM[i];
            }

            return resultB;
        }

        mask = mask.Reshape(batch, seqLen, seqLen);
        var result = ArrayND.Zeros(batch * numHeads, seqLen, seqLen);
        var spanM2 = mask.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var b = 0; b < batch; b++)
        for (var h = 0; h < numHeads; h++)
        {
            var srcOff = b * seqLen * seqLen;
            var dstOff = (b * numHeads + h) * seqLen * seqLen;
            for (var i = 0; i < seqLen * seqLen; i++) spanR[dstOff + i] = spanM2[srcOff + i];
        }

        return result;
    }
}