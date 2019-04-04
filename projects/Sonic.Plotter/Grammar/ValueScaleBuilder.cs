using Plotter.Core;

namespace Plotter.Grammar;

/// <summary>
///     刻度构建器，用于配置坐标轴的刻度方式和范围。
/// </summary>
public sealed class ValueScaleBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="ValueScaleBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public ValueScaleBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     X 轴刻度类型。
    /// </summary>
    internal ScaleType _x_scale_type { get; private set; }

    /// <summary>
    ///     Y 轴刻度类型。
    /// </summary>
    internal ScaleType _y_scale_type { get; private set; }

    /// <summary>
    ///     刻度范围最小值。
    /// </summary>
    internal double _range_min { get; private set; }

    /// <summary>
    ///     刻度范围最大值。
    /// </summary>
    internal double _range_max { get; private set; }

    /// <summary>
    ///     刻度数量。
    /// </summary>
    internal int _tick_count { get; private set; } = 5;

    /// <summary>
    ///     设置 X 轴刻度类型。
    /// </summary>
    /// <param name="scale_type">刻度类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ValueScaleBuilder with_x_scale_type(ScaleType scale_type)
    {
        _x_scale_type = scale_type;
        return this;
    }

    /// <summary>
    ///     设置 Y 轴刻度类型。
    /// </summary>
    /// <param name="scale_type">刻度类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ValueScaleBuilder with_y_scale_type(ScaleType scale_type)
    {
        _y_scale_type = scale_type;
        return this;
    }

    /// <summary>
    ///     设置刻度范围。
    /// </summary>
    /// <param name="min">范围最小值。</param>
    /// <param name="max">范围最大值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ValueScaleBuilder with_range(double min, double max)
    {
        _range_min = min;
        _range_max = max;
        return this;
    }

    /// <summary>
    ///     设置刻度数量。
    /// </summary>
    /// <param name="count">刻度数量。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ValueScaleBuilder with_tick_count(int count)
    {
        _tick_count = count;
        return this;
    }

    /// <summary>
    ///     完成刻度配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}