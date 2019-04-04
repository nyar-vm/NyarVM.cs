using System;

namespace Core.Data.Validation;

/// <summary>
///     验证数值字段的最小值和最大值范围。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RangeAttribute : Attribute
{
    /// <summary>
    ///     初始化数值范围验证特性。
    /// </summary>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    public RangeAttribute(double min, double max)
    {
        this.min = min;
        this.max = max;
    }

    /// <summary>
    ///     最小值。
    /// </summary>
    public double min { get; }

    /// <summary>
    ///     最大值。
    /// </summary>
    public double max { get; }

    /// <summary>
    ///     验证失败时的自定义错误消息。
    /// </summary>
    public string? error_message { get; set; }
}