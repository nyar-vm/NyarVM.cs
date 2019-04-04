namespace Std.DL.Flux;

/// <summary>
///     梯度裁剪 —— 防止梯度爆炸，Transformer / LSTM 训练必备
/// </summary>
public static class GradientClipping
{
    /// <summary>
    ///     按 L2 范数裁剪梯度
    ///     if total_norm > maxNorm: grad *= maxNorm / total_norm
    /// </summary>
    /// <param name="parameters">模型参数集合</param>
    /// <param name="maxNorm">最大 L2 范数阈值</param>
    /// <returns>裁剪前的总范数</returns>
    public static float ClipGradNorm(IEnumerable<IParameter> parameters, float maxNorm)
    {
        var totalNormSq = 0.0f;

        foreach (var p in parameters)
        {
            var grad = p.Value.Grad;
            if (grad == null) continue;

            var span = grad.AsSpan();
            for (var i = 0; i < span.Length; i++) totalNormSq += span[i] * span[i];
        }

        var totalNorm = MathF.Sqrt(totalNormSq);

        if (totalNorm > maxNorm)
        {
            var scale = maxNorm / totalNorm;

            foreach (var p in parameters)
            {
                var grad = p.Value.Grad;
                if (grad == null) continue;

                var span = grad.AsWriteSpan();
                for (var i = 0; i < span.Length; i++) span[i] *= scale;
            }
        }

        return totalNorm;
    }

    /// <summary>
    ///     按绝对值裁剪梯度
    ///     grad = clamp(grad, -clipValue, clipValue)
    /// </summary>
    /// <param name="parameters">模型参数集合</param>
    /// <param name="clipValue">最大绝对值</param>
    /// <returns>被裁剪的元素总数</returns>
    public static int ClipGradValue(IEnumerable<IParameter> parameters, float clipValue)
    {
        var clippedCount = 0;

        foreach (var p in parameters)
        {
            var grad = p.Value.Grad;
            if (grad == null) continue;

            var span = grad.AsWriteSpan();
            for (var i = 0; i < span.Length; i++)
                if (span[i] > clipValue)
                {
                    span[i] = clipValue;
                    clippedCount++;
                }
                else if (span[i] < -clipValue)
                {
                    span[i] = -clipValue;
                    clippedCount++;
                }
        }

        return clippedCount;
    }

    /// <summary>
    ///     按 L2 范数裁剪梯度并返回裁剪状态（用于日志）
    /// </summary>
    /// <param name="parameters">模型参数集合</param>
    /// <param name="maxNorm">最大 L2 范数阈值</param>
    /// <param name="originalNorm">输出：裁剪前的总范数</param>
    /// <param name="wasClipped">输出：是否实际执行了裁剪</param>
    public static void ClipGradNorm(IEnumerable<IParameter> parameters, float maxNorm, out float originalNorm,
        out bool wasClipped)
    {
        originalNorm = ClipGradNorm(parameters, maxNorm);
        wasClipped = originalNorm > maxNorm;
    }
}