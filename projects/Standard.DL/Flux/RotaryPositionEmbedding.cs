namespace Std.DL.Flux;

/// <summary>
///     Rotary Position Embedding —— LLaMA / Qwen / Mistral 标准位置编码
///     对 Q 和 K 的最后一维按位置旋转
/// </summary>
public static class RotaryPositionEmbedding
{
    /// <summary>
    ///     对 Q 和 K 施加旋转位置编码（原地修改）
    ///     q: [batch*numHeads, seqLen, dK]
    ///     k: [batch*numHeads, seqLen, dK]
    /// </summary>
    /// <param name="q">查询张量</param>
    /// <param name="k">键张量</param>
    /// <param name="startPos">起始位置（KV-Cache 场景下为非零）</param>
    /// <param name="thetaBase">频率基数，默认 10000.0</param>
    public static void ApplyRotaryEmbedding(ArrayND q, ArrayND k, int startPos = 0, float thetaBase = 10000.0f)
    {
        var seqLen = q.Shape[1];
        var dK = q.Shape[2];
        var halfD = dK / 2;

        var spanQ = q.AsWriteSpan();
        var spanK = k.AsWriteSpan();

        for (var pos = 0; pos < seqLen; pos++)
        {
            var absPos = startPos + pos;

            for (var i = 0; i < halfD; i++)
            {
                var theta = 1.0f / MathF.Pow(thetaBase, 2.0f * i / dK);
                var cos = MathF.Cos(absPos * theta);
                var sin = MathF.Sin(absPos * theta);

                var qOff = pos * dK;
                var kOff = pos * dK;

                var q0 = spanQ[qOff + i];
                var q1 = spanQ[qOff + i + halfD];
                spanQ[qOff + i] = q0 * cos - q1 * sin;
                spanQ[qOff + i + halfD] = q0 * sin + q1 * cos;

                var k0 = spanK[kOff + i];
                var k1 = spanK[kOff + i + halfD];
                spanK[kOff + i] = k0 * cos - k1 * sin;
                spanK[kOff + i + halfD] = k0 * sin + k1 * cos;
            }
        }
    }
}