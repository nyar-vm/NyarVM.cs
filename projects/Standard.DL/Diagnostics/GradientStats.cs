namespace Std.DL.Diagnostics;

/// <summary>梯度统计数据</summary>
public sealed class GradientStats
{
    /// <summary>最小值</summary>
    public float Min { get; init; }

    /// <summary>最大值</summary>
    public float Max { get; init; }

    /// <summary>均值</summary>
    public float Mean { get; init; }

    /// <summary>标准差</summary>
    public float Std { get; init; }

    /// <summary>L2 范数</summary>
    public float L2Norm { get; init; }

    /// <summary>零值数量</summary>
    public int ZeroCount { get; init; }

    /// <summary>是否包含 NaN</summary>
    public bool HasNaN { get; init; }

    /// <summary>是否包含 Inf</summary>
    public bool HasInf { get; init; }
}