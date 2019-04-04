namespace Std.DL.Flux;

/// <summary>
///     Flash Attention（简化版）—— 内存高效的注意力计算
///     标准 ScaledDotProductAttention 需要存储完整的 [batch*heads, seqLenQ, seqLenK] 注意力矩阵
///     Flash Attention 分块计算，避免存储完整注意力矩阵，降低内存从 O(N²) 到 O(N)
///     本实现为简化版（非 CUDA kernel），演示分块计算思想
/// </summary>
public static class FlashAttention
{
    /// <summary>
    ///     分块缩放点积注意力
    ///     将 Q 和 K/V 分块计算，避免一次性分配完整注意力矩阵
    ///     适用于长序列场景（seqLen > 1024）
    /// </summary>
    /// <param name="q">查询 [batch*heads, seqLenQ, dK]</param>
    /// <param name="k">键 [batch*heads, seqLenK, dK]</param>
    /// <param name="v">值 [batch*heads, seqLenK, dV]</param>
    /// <param name="mask">掩码 [batch*heads, seqLenQ, seqLenK]（可选）</param>
    /// <param name="blockSize">分块大小（0 = 自动选择）</param>
    /// <returns>注意力输出 [batch*heads, seqLenQ, dV]</returns>
    public static ArrayND Compute(ArrayND q, ArrayND k, ArrayND v, ArrayND? mask = null, int blockSize = 0)
    {
        var totalBatches = q.Shape[0];
        var seqLenQ = q.Shape[1];
        var dK = q.Shape[2];
        var seqLenK = k.Shape[1];
        var dV = v.Shape[2];
        var scale = 1.0f / MathF.Sqrt(dK);

        if (blockSize <= 0) blockSize = System.Math.Max(32, System.Math.Min(256, seqLenK));

        var output = ArrayND.Zeros(totalBatches, seqLenQ, dV);
        var spanQ = q.AsSpan();
        var spanK = k.AsSpan();
        var spanV = v.AsSpan();
        var spanOut = output.AsWriteSpan();
        var hasMask = mask != null;
        var spanMask = hasMask ? mask!.AsSpan() : default;

        for (var b = 0; b < totalBatches; b++)
        {
            var qOff = b * seqLenQ * dK;
            var kOff = b * seqLenK * dK;
            var vOff = b * seqLenK * dV;
            var outOff = b * seqLenQ * dV;
            var maskOff = b * seqLenQ * seqLenK;

            var rowMax = new float[seqLenQ];
            var rowSum = new float[seqLenQ];
            for (var i = 0; i < seqLenQ; i++)
            {
                rowMax[i] = float.NegativeInfinity;
                rowSum[i] = 0;
            }

            for (var kStart = 0; kStart < seqLenK; kStart += blockSize)
            {
                var kEnd = System.Math.Min(kStart + blockSize, seqLenK);
                var kLen = kEnd - kStart;

                for (var qi = 0; qi < seqLenQ; qi++)
                {
                    var scores = new float[kLen];
                    for (var ki = 0; ki < kLen; ki++)
                    {
                        var dot = 0.0f;
                        for (var d = 0; d < dK; d++)
                            dot += spanQ[qOff + qi * dK + d] * spanK[kOff + (kStart + ki) * dK + d];
                        scores[ki] = dot * scale;

                        if (hasMask && maskOff + qi * seqLenK + kStart + ki < spanMask.Length)
                            scores[ki] += spanMask[maskOff + qi * seqLenK + kStart + ki];
                    }

                    var blockMax = float.NegativeInfinity;
                    for (var ki = 0; ki < kLen; ki++)
                        if (scores[ki] > blockMax)
                            blockMax = scores[ki];

                    var newMax = MathF.Max(rowMax[qi], blockMax);
                    if (float.IsNegativeInfinity(newMax)) newMax = 0;

                    var correctionOld = float.IsNegativeInfinity(rowMax[qi])
                        ? 0
                        : MathF.Exp(rowMax[qi] - newMax);
                    var correctionNew = float.IsNegativeInfinity(blockMax)
                        ? 0
                        : MathF.Exp(blockMax - newMax);

                    var expSum = 0.0f;
                    for (var ki = 0; ki < kLen; ki++)
                    {
                        scores[ki] = MathF.Exp(scores[ki] - newMax);
                        expSum += scores[ki];
                    }

                    var newSum = rowSum[qi] * correctionOld + expSum;
                    var outScale = rowSum[qi] > 0 ? correctionOld / (newSum > 0 ? newSum : 1) : 0;
                    var blockScale = 1.0f / (newSum > 0 ? newSum : 1);

                    for (var d = 0; d < dV; d++) spanOut[outOff + qi * dV + d] *= outScale;

                    for (var ki = 0; ki < kLen; ki++)
                    {
                        var weight = scores[ki] * blockScale;
                        for (var d = 0; d < dV; d++)
                            spanOut[outOff + qi * dV + d] += weight * spanV[vOff + (kStart + ki) * dV + d];
                    }

                    rowMax[qi] = newMax;
                    rowSum[qi] = newSum;
                }
            }
        }

        return output;
    }

    /// <summary>
    ///     标准 ScaledDotProductAttention 的内存优化版本
    ///     不存储完整注意力矩阵，直接计算输出
    /// </summary>
    /// <param name="q">查询 [batch*heads, seqLenQ, dK]</param>
    /// <param name="k">键 [batch*heads, seqLenK, dK]</param>
    /// <param name="v">值 [batch*heads, seqLenK, dV]</param>
    /// <param name="mask">掩码</param>
    /// <returns>注意力输出 [batch*heads, seqLenQ, dV]</returns>
    public static ArrayND ComputeMemoryEfficient(ArrayND q, ArrayND k, ArrayND v, ArrayND? mask = null)
    {
        return Compute(q, k, v, mask, 256);
    }
}