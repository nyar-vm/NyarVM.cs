using Plotter.Core;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     视觉映射构建器，用于配置数据字段到视觉通道的映射关系。
///     构建时将映射配置应用到所有已注册的 <see cref="ShapeLayer" /> 图层。
/// </summary>
public sealed class VisualMappingBuilder
{
    #region 构造函数

    /// <summary>
    ///     初始化 <see cref="VisualMappingBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public VisualMappingBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    #endregion

    #region 构建方法

    /// <summary>
    ///     完成视觉映射配置，将映射配置应用到画布上所有 <see cref="ShapeLayer" /> 图层，并返回画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        _canvas.apply_visual_mapping(this);
        return _canvas;
    }

    #endregion

    #region 内部方法

    /// <summary>
    ///     将当前映射配置应用到指定的 <see cref="ShapeLayer" /> 实例。
    /// </summary>
    /// <param name="layer">目标图形图层。</param>
    internal void apply_to(ShapeLayer layer)
    {
        if (!string.IsNullOrEmpty(_x_field)) layer.set_x_field(_x_field);

        if (!string.IsNullOrEmpty(_y_field)) layer.set_y_field(_y_field);

        if (!string.IsNullOrEmpty(_fill_color_field)) layer.set_fill_color_field(_fill_color_field);

        if (!string.IsNullOrEmpty(_stroke_color_field)) layer.set_stroke_color_field(_stroke_color_field);

        if (!string.IsNullOrEmpty(_size_field)) layer.set_size_field(_size_field);

        if (_fixed_fill.HasValue) layer.set_fixed_fill(_fixed_fill.Value);

        if (_fixed_stroke.HasValue) layer.set_fixed_stroke(_fixed_stroke.Value);

        if (_fixed_size.HasValue) layer.set_fixed_size(_fixed_size.Value);
    }

    #endregion

    #region 字段

    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     X 轴字段名称。
    /// </summary>
    private string _x_field = "";

    /// <summary>
    ///     Y 轴字段名称。
    /// </summary>
    private string _y_field = "";

    /// <summary>
    ///     填充颜色字段名称。
    /// </summary>
    private string _fill_color_field = "";

    /// <summary>
    ///     描边颜色字段名称。
    /// </summary>
    private string _stroke_color_field = "";

    /// <summary>
    ///     大小字段名称。
    /// </summary>
    private string _size_field = "";

    /// <summary>
    ///     固定填充颜色。
    /// </summary>
    private PlotColor? _fixed_fill;

    /// <summary>
    ///     固定大小。
    /// </summary>
    private double? _fixed_size;

    /// <summary>
    ///     固定描边颜色。
    /// </summary>
    private PlotColor? _fixed_stroke;

    #endregion

    #region 内部访问器

    /// <summary>
    ///     获取 X 轴字段名称。
    /// </summary>
    internal string x_field => _x_field;

    /// <summary>
    ///     获取 Y 轴字段名称。
    /// </summary>
    internal string y_field => _y_field;

    /// <summary>
    ///     获取填充颜色字段名称。
    /// </summary>
    internal string fill_color_field => _fill_color_field;

    /// <summary>
    ///     获取描边颜色字段名称。
    /// </summary>
    internal string stroke_color_field => _stroke_color_field;

    /// <summary>
    ///     获取大小字段名称。
    /// </summary>
    internal string size_field => _size_field;

    /// <summary>
    ///     获取固定填充颜色。
    /// </summary>
    internal PlotColor? fixed_fill => _fixed_fill;

    /// <summary>
    ///     获取固定描边颜色。
    /// </summary>
    internal PlotColor? fixed_stroke => _fixed_stroke;

    /// <summary>
    ///     获取固定大小。
    /// </summary>
    internal double? fixed_size => _fixed_size;

    #endregion

    #region 配置方法

    /// <summary>
    ///     映射 X 轴字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder map_x_field(string field_name)
    {
        _x_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射 Y 轴字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder map_y_field(string field_name)
    {
        _y_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射填充颜色字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder map_fill_color(string field_name)
    {
        _fill_color_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射描边颜色字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder map_stroke_color(string field_name)
    {
        _stroke_color_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射大小字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder map_size(string field_name)
    {
        _size_field = field_name;
        return this;
    }

    /// <summary>
    ///     设置固定填充颜色。
    /// </summary>
    /// <param name="color">颜色值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder with_fixed_fill(string color)
    {
        _fixed_fill = PlotColor.from_hex(color);
        return this;
    }

    /// <summary>
    ///     设置固定填充颜色。
    /// </summary>
    /// <param name="color">颜色值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder with_fixed_fill_color(PlotColor color)
    {
        _fixed_fill = color;
        return this;
    }

    /// <summary>
    ///     设置固定大小。
    /// </summary>
    /// <param name="size">大小值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder with_fixed_size(double size)
    {
        _fixed_size = size;
        return this;
    }

    /// <summary>
    ///     设置固定描边颜色。
    /// </summary>
    /// <param name="color">颜色值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder with_fixed_stroke(string color)
    {
        _fixed_stroke = PlotColor.from_hex(color);
        return this;
    }

    /// <summary>
    ///     设置固定描边颜色。
    /// </summary>
    /// <param name="color">颜色值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public VisualMappingBuilder with_fixed_stroke_color(PlotColor color)
    {
        _fixed_stroke = color;
        return this;
    }

    #endregion
}