using Std.DL.Flux;

namespace Std.DL.Cluster;

/// <summary>
///     AllReduce 梯度同步器 —— 将多个工作节点/数据分片的梯度进行汇总平均，
///     实现数据并行训练中的梯度一致性
/// </summary>
public static class GradientSynchronizer
{
    /// <summary>
    ///     将多个 worker 计算的梯度累加结果归一化（除以 worker 数量）
    ///     在梯度累加模式下，参数梯度已经是各 worker 梯度之和，直接除以 N 即可
    /// </summary>
    /// <param name="parameters">已累加各 worker 梯度的参数列表</param>
    /// <param name="numWorkers">参与累加的 worker 数量</param>
    public static void NormalizeAccumulatedGradients(IReadOnlyList<IParameter> parameters, int numWorkers)
    {
        if (numWorkers <= 1) return;

        foreach (var p in parameters)
        {
            var grad = p.Grad;
            if (grad is null) continue;

            var spanGrad = grad.AsWriteSpan();
            for (var i = 0; i < spanGrad.Length; i++) spanGrad[i] /= numWorkers;
        }
    }

    /// <summary>
    ///     对多个来源的参数梯度执行 AllReduce 均值同步
    ///     将所有源的平均梯度写入目标参数
    /// </summary>
    /// <param name="targetParams">目标参数（接收平均梯度）</param>
    /// <param name="sourceParamGroups">各来源的参数梯度组</param>
    public static void AllReduceAverage(
        IReadOnlyList<IParameter> targetParams,
        IReadOnlyList<IReadOnlyList<IParameter>> sourceParamGroups)
    {
        var numSources = sourceParamGroups.Count;

        if (numSources == 0) return;

        for (var pIdx = 0; pIdx < targetParams.Count; pIdx++)
        {
            var targetGradSpan = GetGradWriteSpan(targetParams[pIdx]);
            if (targetGradSpan.Length == 0) continue;

            targetGradSpan.Clear();

            foreach (var sourceParams in sourceParamGroups)
            {
                if (pIdx >= sourceParams.Count) continue;

                var sourceGradSpan = GetGradSpan(sourceParams[pIdx]);
                for (var i = 0; i < System.Math.Min(targetGradSpan.Length, sourceGradSpan.Length); i++)
                    targetGradSpan[i] += sourceGradSpan[i];
            }

            for (var i = 0; i < targetGradSpan.Length; i++) targetGradSpan[i] /= numSources;
        }
    }

    /// <summary>
    ///     计算两个参数组的梯度平均 L2 范数差异（用于验证同步一致性）
    /// </summary>
    /// <param name="paramsA">参数组 A</param>
    /// <param name="paramsB">参数组 B</param>
    /// <returns>最大 L2 范数差异</returns>
    public static float MaxGradientDiffNorm(
        IReadOnlyList<IParameter> paramsA,
        IReadOnlyList<IParameter> paramsB)
    {
        var maxDiff = 0.0f;

        for (var pIdx = 0; pIdx < System.Math.Min(paramsA.Count, paramsB.Count); pIdx++)
        {
            var gradA = GetGradSpan(paramsA[pIdx]);
            var gradB = GetGradSpan(paramsB[pIdx]);
            var len = System.Math.Min(gradA.Length, gradB.Length);

            for (var i = 0; i < len; i++) maxDiff = MathF.Max(maxDiff, MathF.Abs(gradA[i] - gradB[i]));
        }

        return maxDiff;
    }

    private static Span<float> GetGradWriteSpan(IParameter p)
    {
        var grad = p.Grad;
        return grad is not null ? grad.AsWriteSpan() : Span<float>.Empty;
    }

    private static ReadOnlySpan<float> GetGradSpan(IParameter p)
    {
        var grad = p.Grad;
        return grad is not null ? grad.AsSpan() : ReadOnlySpan<float>.Empty;
    }
}