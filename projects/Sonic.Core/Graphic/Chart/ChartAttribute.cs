using System;

namespace Core.Graphic.Chart;

/// <summary>
///     标记类为图表组件。Source Generator 将为标记的类生成图表注册和渲染胶水代码。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ChartAttribute : Attribute
{
    /// <summary>
    ///     获取或设置图表宽度，默认 800。
    /// </summary>
    public int width { get; init; } = 800;

    /// <summary>
    ///     获取或设置图表高度，默认 600。
    /// </summary>
    public int height { get; init; } = 600;
}