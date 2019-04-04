using Plotter.Core;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     图形图层构建器，用于配置图表的形状类型和样式，并在构建时注册图层到画布。
/// </summary>
public sealed class ShapeLayerBuilder
{
    #region 构造函数

    /// <summary>
    ///     初始化 <see cref="ShapeLayerBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public ShapeLayerBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    #endregion

    #region 构建方法

    /// <summary>
    ///     完成图形图层配置，创建 <see cref="ShapeLayer" /> 实例并注册到画布的图层管理器。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        var layer = new ShapeLayer();

        layer.set_shape_type(_shape_type);
        layer.set_line_width(_line_width);
        layer.set_layer_opacity(_layer_opacity);
        layer.set_x_field(_x_field);
        layer.set_y_field(_y_field);
        layer.set_fill_color_field(_fill_color_field);
        layer.set_stroke_color_field(_stroke_color_field);
        layer.set_size_field(_size_field);
        layer.set_fixed_fill(_fixed_fill);
        layer.set_fixed_stroke(_fixed_stroke);
        layer.set_fixed_size(_fixed_size);
        layer.set_x_scale_type(_x_scale_type);
        layer.set_y_scale_type(_y_scale_type);
        layer.set_coordinate_type(_coordinate_type);
        layer.set_tick_count(_tick_count);

        layer.opacity = _layer_opacity;
        layer.z_index = _z_index;

        _canvas.add_layer(layer);

        return _canvas;
    }

    #endregion

    #region 字段

    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     图形类型。
    /// </summary>
    private ShapeType _shape_type;

    /// <summary>
    ///     线宽。
    /// </summary>
    private float _line_width = 1.0f;

    /// <summary>
    ///     图层不透明度。
    /// </summary>
    private double _layer_opacity = 1.0;

    /// <summary>
    ///     X 轴字段名称。
    /// </summary>
    private string? _x_field;

    /// <summary>
    ///     Y 轴字段名称。
    /// </summary>
    private string? _y_field;

    /// <summary>
    ///     填充颜色字段名称。
    /// </summary>
    private string? _fill_color_field;

    /// <summary>
    ///     描边颜色字段名称。
    /// </summary>
    private string? _stroke_color_field;

    /// <summary>
    ///     大小字段名称。
    /// </summary>
    private string? _size_field;

    /// <summary>
    ///     固定填充颜色。
    /// </summary>
    private PlotColor? _fixed_fill;

    /// <summary>
    ///     固定描边颜色。
    /// </summary>
    private PlotColor? _fixed_stroke;

    /// <summary>
    ///     固定大小。
    /// </summary>
    private double? _fixed_size;

    /// <summary>
    ///     X 轴刻度类型。
    /// </summary>
    private ScaleType _x_scale_type;

    /// <summary>
    ///     Y 轴刻度类型。
    /// </summary>
    private ScaleType _y_scale_type;

    /// <summary>
    ///     坐标系类型。
    /// </summary>
    private CoordinateType _coordinate_type;

    /// <summary>
    ///     刻度数量。
    /// </summary>
    private int _tick_count = 5;

    /// <summary>
    ///     图层渲染顺序。
    /// </summary>
    private int _z_index;

    #endregion

    #region 内部访问器

    /// <summary>
    ///     获取图形类型。
    /// </summary>
    internal ShapeType shape_type => _shape_type;

    /// <summary>
    ///     获取线宽。
    /// </summary>
    internal float line_width => _line_width;

    /// <summary>
    ///     获取图层不透明度。
    /// </summary>
    internal double layer_opacity => _layer_opacity;

    #endregion

    #region 配置方法

    /// <summary>
    ///     设置图形类型。
    /// </summary>
    /// <param name="shape_type">图形类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_shape_type(ShapeType shape_type)
    {
        _shape_type = shape_type;
        return this;
    }

    /// <summary>
    ///     设置线宽。
    /// </summary>
    /// <param name="width">线宽值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_line_width(float width)
    {
        _line_width = width;
        return this;
    }

    /// <summary>
    ///     设置图层不透明度。
    /// </summary>
    /// <param name="opacity">不透明度，范围 [0, 1]。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_layer_opacity(double opacity)
    {
        _layer_opacity = opacity;
        return this;
    }

    /// <summary>
    ///     映射 X 轴字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder map_x_field(string field_name)
    {
        _x_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射 Y 轴字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder map_y_field(string field_name)
    {
        _y_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射填充颜色字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder map_fill_color(string field_name)
    {
        _fill_color_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射描边颜色字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder map_stroke_color(string field_name)
    {
        _stroke_color_field = field_name;
        return this;
    }

    /// <summary>
    ///     映射大小字段。
    /// </summary>
    /// <param name="field_name">字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder map_size(string field_name)
    {
        _size_field = field_name;
        return this;
    }

    /// <summary>
    ///     设置固定填充颜色。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_fixed_fill(PlotColor color)
    {
        _fixed_fill = color;
        return this;
    }

    /// <summary>
    ///     设置固定描边颜色。
    /// </summary>
    /// <param name="color">描边颜色。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_fixed_stroke(PlotColor color)
    {
        _fixed_stroke = color;
        return this;
    }

    /// <summary>
    ///     设置固定大小。
    /// </summary>
    /// <param name="size">大小值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_fixed_size(double size)
    {
        _fixed_size = size;
        return this;
    }

    /// <summary>
    ///     设置 X 轴刻度类型。
    /// </summary>
    /// <param name="scale_type">刻度类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_x_scale_type(ScaleType scale_type)
    {
        _x_scale_type = scale_type;
        return this;
    }

    /// <summary>
    ///     设置 Y 轴刻度类型。
    /// </summary>
    /// <param name="scale_type">刻度类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_y_scale_type(ScaleType scale_type)
    {
        _y_scale_type = scale_type;
        return this;
    }

    /// <summary>
    ///     设置坐标系类型。
    /// </summary>
    /// <param name="coordinate_type">坐标系类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_coordinate_type(CoordinateType coordinate_type)
    {
        _coordinate_type = coordinate_type;
        return this;
    }

    /// <summary>
    ///     设置刻度数量。
    /// </summary>
    /// <param name="count">刻度数量。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_tick_count(int count)
    {
        _tick_count = count;
        return this;
    }

    /// <summary>
    ///     设置图层渲染顺序。
    /// </summary>
    /// <param name="index">渲染顺序值，值越大越靠前渲染。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ShapeLayerBuilder with_z_index(int index)
    {
        _z_index = index;
        return this;
    }

    #endregion
}