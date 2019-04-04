using System;

namespace Core.Observability;

/// <summary>
///     标记一个方法或属性为指标采集点
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class MetricAttribute : Attribute
{
    /// <summary>
    ///     指标名称
    /// </summary>
    public string? name { get; set; }

    /// <summary>
    ///     指标类型，默认为计数器
    /// </summary>
    public MetricType type { get; set; } = MetricType.counter;
}