using Std.DL.Flux;

namespace Std.DL.Diagnostics;

/// <summary>默认梯度审计器实现</summary>
public sealed class GradientAuditor : IGradientAuditor
{
    /// <summary>审计梯度</summary>
    public GradientStats Audit(ArrayND gradient)
    {
        var span = gradient.AsSpan();
        float min = float.MaxValue, max = float.MinValue, sum = 0, sumSq = 0;
        var zeros = 0;
        bool hasNaN = false, hasInf = false;

        foreach (var v in span)
        {
            if (float.IsNaN(v)) hasNaN = true;

            if (float.IsInfinity(v)) hasInf = true;

            if (v == 0) zeros++;

            min = MathF.Min(min, v);
            max = MathF.Max(max, v);
            sum += v;
            sumSq += v * v;
        }

        var mean = sum / span.Length;
        var std = MathF.Sqrt(sumSq / span.Length - mean * mean);
        var l2 = MathF.Sqrt(sumSq);

        return new GradientStats
        {
            Min = min,
            Max = max,
            Mean = mean,
            Std = std,
            L2Norm = l2,
            ZeroCount = zeros,
            HasNaN = hasNaN,
            HasInf = hasInf
        };
    }

    /// <summary>判断梯度是否健康</summary>
    public bool IsHealthy(GradientStats stats)
    {
        return stats is { HasNaN: false, HasInf: false, L2Norm: < 1e10f };
    }

    /// <summary>获取梯度报告</summary>
    public string GetReport(GradientStats stats)
    {
        return
            $"梯度: Min={stats.Min:F4}, Max={stats.Max:F4}, Mean={stats.Mean:F4}, Std={stats.Std:F4}, L2={stats.L2Norm:F4}, Zeros={stats.ZeroCount}, NaN={stats.HasNaN}, Inf={stats.HasInf}";
    }
}