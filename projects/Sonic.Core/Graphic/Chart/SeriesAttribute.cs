using System;

namespace Core.Graphic.Chart;

/// <summary>
///     Series 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SeriesAttribute : Attribute
{
    /// <summary>
    ///     系列名称
    /// </summary>
    public string? name { get; set; }


    /// <summary>
    ///     系列颜色
    /// </summary>
    public string? color { get; set; }
}