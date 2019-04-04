using System;
using System.Collections.Generic;
using Plotter.Core;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     图形图层，根据图形类型生成对应绘制指令并渲染到画布。
///     支持点图、折线图、柱状图、条形图、面积图、饼图等图形类型。
/// </summary>
public class ShapeLayer : ChartLayer
{
    #region 笛卡尔坐标系渲染

    /// <summary>
    ///     在笛卡尔坐标系下渲染图形，先绘制坐标轴和网格，再绘制图形。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="fill">填充颜色。</param>
    /// <param name="stroke">描边颜色。</param>
    private void render_cartesian(IRenderCanvas canvas, PlotColor fill, PlotColor stroke)
    {
        var (_, _, _, y_ticks) = NiceScale.calculate(_y_min, _y_max, _tick_count);
        var (_, _, _, x_ticks) = NiceScale.calculate(_x_min, _x_max, _tick_count);

        AxisRenderer.render_grid(
            canvas, _plot_left, _plot_right, _plot_top, _plot_bottom,
            _y_min, _y_max, y_ticks);

        AxisRenderer.render_x_axis(
            canvas, _plot_left, _plot_right, _plot_bottom,
            _x_scale_type, _x_min, _x_max, x_ticks,
            get_categories());

        AxisRenderer.render_y_axis(
            canvas, _plot_left, _plot_top, _plot_bottom,
            _y_min, _y_max, y_ticks);

        var data_array = _data_points.ToArray();

        switch (_shape_type)
        {
            case ShapeType.Point:
                PointRenderer.render(
                    canvas, data_array,
                    _plot_left, _plot_top, _plot_right, _plot_bottom,
                    _x_min, _x_max, _y_min, _y_max,
                    fill, stroke, _palette, (float)(_fixed_size ?? 4.0));
                break;

            case ShapeType.Line:
                LineRenderer.render(
                    canvas, data_array,
                    _plot_left, _plot_top, _plot_right, _plot_bottom,
                    _x_min, _x_max, _y_min, _y_max,
                    stroke, fill.with_alpha((byte)(fill.a / 2)),
                    _line_width, true, 3.0f);
                break;

            case ShapeType.Area:
                LineRenderer.render(
                    canvas, data_array,
                    _plot_left, _plot_top, _plot_right, _plot_bottom,
                    _x_min, _x_max, _y_min, _y_max,
                    stroke, fill,
                    _line_width, false, 0);
                break;

            case ShapeType.Bar:
                BarRenderer.render_vertical(
                    canvas, data_array,
                    _plot_left, _plot_top, _plot_right, _plot_bottom,
                    _y_min, _y_max,
                    fill, stroke, _palette, true);
                break;

            case ShapeType.HorizontalBar:
                BarRenderer.render_horizontal(
                    canvas, data_array,
                    _plot_left, _plot_top, _plot_right, _plot_bottom,
                    _x_min, _x_max,
                    fill, stroke, _palette, true);
                break;

            case ShapeType.BoxPlot:
            case ShapeType.Heatmap:
            case ShapeType.Radar:
                render_unsupported(canvas, _shape_type.ToString());
                break;
        }
    }

    #endregion

    #region 极坐标系渲染

    /// <summary>
    ///     在极坐标系下渲染图形，目前支持饼图。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="fill">填充颜色。</param>
    /// <param name="stroke">描边颜色。</param>
    private void render_polar(IRenderCanvas canvas, PlotColor fill, PlotColor stroke)
    {
        var center_x = (_plot_left + _plot_right) / 2.0f;
        var center_y = (_plot_top + _plot_bottom) / 2.0f;
        var radius = Math.Min(_plot_right - _plot_left, _plot_bottom - _plot_top) / 2.0f * 0.8f;

        switch (_shape_type)
        {
            case ShapeType.Pie:
                PieRenderer.render(
                    canvas, [.. _data_points],
                    center_x, center_y, radius,
                    _palette, stroke, 0, true);
                break;

            default:
                render_unsupported(canvas, $"{_shape_type} in Polar coordinate");
                break;
        }
    }

    #endregion

    #region 字段

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
    ///     数据点列表。
    /// </summary>
    private List<DataPoint> _data_points = [];

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
    ///     X 轴数据最小值。
    /// </summary>
    private double _x_min;

    /// <summary>
    ///     X 轴数据最大值。
    /// </summary>
    private double _x_max;

    /// <summary>
    ///     Y 轴数据最小值。
    /// </summary>
    private double _y_min;

    /// <summary>
    ///     Y 轴数据最大值。
    /// </summary>
    private double _y_max;

    /// <summary>
    ///     刻度数量。
    /// </summary>
    private int _tick_count = 5;

    /// <summary>
    ///     坐标系类型。
    /// </summary>
    private CoordinateType _coordinate_type;

    /// <summary>
    ///     绘图区域左边界。
    /// </summary>
    private float _plot_left;

    /// <summary>
    ///     绘图区域上边界。
    /// </summary>
    private float _plot_top;

    /// <summary>
    ///     绘图区域右边界。
    /// </summary>
    private float _plot_right;

    /// <summary>
    ///     绘图区域下边界。
    /// </summary>
    private float _plot_bottom;

    /// <summary>
    ///     颜色调色板实例。
    /// </summary>
    private readonly ColorPalette _palette = new();

    #endregion

    #region 配置方法

    /// <summary>
    ///     设置图形类型。
    /// </summary>
    /// <param name="shape_type">图形类型。</param>
    public void set_shape_type(ShapeType shape_type)
    {
        _shape_type = shape_type;
        mark_dirty();
    }

    /// <summary>
    ///     设置线宽。
    /// </summary>
    /// <param name="width">线宽值。</param>
    public void set_line_width(float width)
    {
        _line_width = width;
        mark_dirty();
    }

    /// <summary>
    ///     设置图层不透明度。
    /// </summary>
    /// <param name="opacity">不透明度，范围 [0, 1]。</param>
    public void set_layer_opacity(double opacity)
    {
        _layer_opacity = opacity;
        mark_dirty();
    }

    /// <summary>
    ///     设置 X 轴字段名称。
    /// </summary>
    /// <param name="field">字段名称。</param>
    public void set_x_field(string? field)
    {
        _x_field = field;
        mark_dirty();
    }

    /// <summary>
    ///     设置 Y 轴字段名称。
    /// </summary>
    /// <param name="field">字段名称。</param>
    public void set_y_field(string? field)
    {
        _y_field = field;
        mark_dirty();
    }

    /// <summary>
    ///     设置填充颜色字段名称。
    /// </summary>
    /// <param name="field">字段名称。</param>
    public void set_fill_color_field(string? field)
    {
        _fill_color_field = field;
        mark_dirty();
    }

    /// <summary>
    ///     设置描边颜色字段名称。
    /// </summary>
    /// <param name="field">字段名称。</param>
    public void set_stroke_color_field(string? field)
    {
        _stroke_color_field = field;
        mark_dirty();
    }

    /// <summary>
    ///     设置大小字段名称。
    /// </summary>
    /// <param name="field">字段名称。</param>
    public void set_size_field(string? field)
    {
        _size_field = field;
        mark_dirty();
    }

    /// <summary>
    ///     设置固定填充颜色。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    public void set_fixed_fill(PlotColor? color)
    {
        _fixed_fill = color;
        mark_dirty();
    }

    /// <summary>
    ///     设置固定描边颜色。
    /// </summary>
    /// <param name="color">描边颜色。</param>
    public void set_fixed_stroke(PlotColor? color)
    {
        _fixed_stroke = color;
        mark_dirty();
    }

    /// <summary>
    ///     设置固定大小。
    /// </summary>
    /// <param name="size">大小值。</param>
    public void set_fixed_size(double? size)
    {
        _fixed_size = size;
        mark_dirty();
    }

    /// <summary>
    ///     设置 X 轴刻度类型。
    /// </summary>
    /// <param name="scale_type">刻度类型。</param>
    public void set_x_scale_type(ScaleType scale_type)
    {
        _x_scale_type = scale_type;
        mark_dirty();
    }

    /// <summary>
    ///     设置 Y 轴刻度类型。
    /// </summary>
    /// <param name="scale_type">刻度类型。</param>
    public void set_y_scale_type(ScaleType scale_type)
    {
        _y_scale_type = scale_type;
        mark_dirty();
    }

    /// <summary>
    ///     设置 X 轴数据范围。
    /// </summary>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    public void set_x_range(double min, double max)
    {
        _x_min = min;
        _x_max = max;
        mark_dirty();
    }

    /// <summary>
    ///     设置 Y 轴数据范围。
    /// </summary>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    public void set_y_range(double min, double max)
    {
        _y_min = min;
        _y_max = max;
        mark_dirty();
    }

    /// <summary>
    ///     设置刻度数量。
    /// </summary>
    /// <param name="count">刻度数量。</param>
    public void set_tick_count(int count)
    {
        _tick_count = count;
        mark_dirty();
    }

    /// <summary>
    ///     设置坐标系类型。
    /// </summary>
    /// <param name="coordinate_type">坐标系类型。</param>
    public void set_coordinate_type(CoordinateType coordinate_type)
    {
        _coordinate_type = coordinate_type;
        mark_dirty();
    }

    /// <summary>
    ///     设置绘图区域边界。
    /// </summary>
    /// <param name="left">左边界。</param>
    /// <param name="top">上边界。</param>
    /// <param name="right">右边界。</param>
    /// <param name="bottom">下边界。</param>
    public void set_plot_area(float left, float top, float right, float bottom)
    {
        _plot_left = left;
        _plot_top = top;
        _plot_right = right;
        _plot_bottom = bottom;
        mark_dirty();
    }

    /// <summary>
    ///     设置数据点列表。
    /// </summary>
    /// <param name="points">数据点列表。</param>
    public void set_data_points(List<DataPoint> points)
    {
        _data_points = points ?? [];
        mark_dirty();
    }

    #endregion

    #region ChartLayer 实现

    /// <summary>
    ///     更新图层数据，支持 <see cref="DataPoint" /> 数组或列表。
    /// </summary>
    /// <param name="data">数据对象，应为 <see cref="DataPoint" /> 数组或列表。</param>
    public override void update(object data)
    {
        if (data is DataPoint[] array)
            _data_points = [.. array];
        else if (data is List<DataPoint> list) _data_points = list;

        mark_dirty();
    }

    /// <summary>
    ///     将图层内容渲染到指定画布上。
    ///     根据坐标系类型和图形类型分发到对应的渲染器。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    public override void render(IRenderCanvas canvas)
    {
        if (_data_points.Count == 0) return;

        calculate_plot_area(canvas);
        calculate_data_range();

        var fill = _fixed_fill ?? PlotColor.from_hex("#5470C6");
        var stroke = _fixed_stroke ?? PlotColor.from_hex("#333333");

        if (_coordinate_type == CoordinateType.Polar)
            render_polar(canvas, fill, stroke);
        else
            render_cartesian(canvas, fill, stroke);

        is_dirty = false;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     根据画布尺寸计算绘图区域，留出边距。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    private void calculate_plot_area(IRenderCanvas canvas)
    {
        if (_plot_left > 0 || _plot_top > 0 || _plot_right > 0 || _plot_bottom > 0) return;

        var margin_left = 50.0f;
        var margin_right = 20.0f;
        var margin_top = 20.0f;
        var margin_bottom = 40.0f;

        _plot_left = margin_left;
        _plot_top = margin_top;
        _plot_right = canvas.width - margin_right;
        _plot_bottom = canvas.height - margin_bottom;
    }

    /// <summary>
    ///     根据数据点计算 X 和 Y 轴的数据范围。
    /// </summary>
    private void calculate_data_range()
    {
        if (_data_points.Count == 0) return;

        var has_x_range = Math.Abs(_x_max - _x_min) > double.Epsilon;
        var has_y_range = Math.Abs(_y_max - _y_min) > double.Epsilon;

        if (!has_x_range || !has_y_range)
        {
            var data_x_min = double.MaxValue;
            var data_x_max = double.MinValue;
            var data_y_min = double.MaxValue;
            var data_y_max = double.MinValue;

            for (var i = 0; i < _data_points.Count; i++)
            {
                var pt = _data_points[i];

                if (pt.x < data_x_min) data_x_min = pt.x;

                if (pt.x > data_x_max) data_x_max = pt.x;

                if (pt.y < data_y_min) data_y_min = pt.y;

                if (pt.y > data_y_max) data_y_max = pt.y;
            }

            if (!has_x_range)
            {
                var (nice_min, nice_max, _, _) = NiceScale.calculate(data_x_min, data_x_max, _tick_count);
                _x_min = nice_min;
                _x_max = nice_max;
            }

            if (!has_y_range)
            {
                var (nice_min, nice_max, _, _) = NiceScale.calculate(data_y_min, data_y_max, _tick_count);
                _y_min = nice_min;
                _y_max = nice_max;
            }
        }
    }

    /// <summary>
    ///     从数据点中提取分类标签数组。
    /// </summary>
    /// <returns>分类标签数组，若无分类则返回 null。</returns>
    private string[]? get_categories()
    {
        if (_x_scale_type != ScaleType.Category) return null;

        var categories = new string[_data_points.Count];

        for (var i = 0; i < _data_points.Count; i++)
            categories[i] = _data_points[i].category ?? _data_points[i].x.ToString("G");

        return categories;
    }

    /// <summary>
    ///     渲染不支持的图形类型提示文本。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="feature">不支持的特性描述。</param>
    private static void render_unsupported(IRenderCanvas canvas, string feature)
    {
        canvas.set_fill_color(PlotColor.from_hex("#999999"));
        canvas.set_font(new PlotFont("sans-serif", 14f));
        canvas.fill_text($"[暂不支持: {feature}]", canvas.width / 2.0f - 80, canvas.height / 2.0f);
    }

    #endregion
}