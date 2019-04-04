using Plotter.Core;

namespace Plotter.Grammar;

/// <summary>
///     坐标系构建器，用于配置图表的坐标系类型。
/// </summary>
public sealed class CoordinateSystemBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="CoordinateSystemBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public CoordinateSystemBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     坐标系类型。
    /// </summary>
    internal CoordinateType _coordinate_type { get; private set; }

    /// <summary>
    ///     设置坐标系类型。
    /// </summary>
    /// <param name="coordinate_type">坐标系类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CoordinateSystemBuilder with_coordinate_type(CoordinateType coordinate_type)
    {
        _coordinate_type = coordinate_type;
        return this;
    }

    /// <summary>
    ///     完成坐标系配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}