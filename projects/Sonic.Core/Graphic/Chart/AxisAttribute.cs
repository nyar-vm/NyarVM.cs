using System;

namespace Core.Graphic.Chart;

/// <summary>
///     Axis 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AxisAttribute : Attribute
{
    /// <summary>
    ///     轴类型
    /// </summary>
    public string? type { get; set; }
}