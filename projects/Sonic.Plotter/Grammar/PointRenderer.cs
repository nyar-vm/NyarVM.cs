using System;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     散点图渲染器，绘制数据点圆形标记，支持大小和颜色映射。
/// </summary>
internal static class PointRenderer
{
    /// <summary>
    ///     渲染散点图数据点。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="data_points">数据点列表。</param>
    /// <param name="plot_left">绘图区域左边界。</param>
    /// <param name="plot_top">绘图区域上边界。</param>
    /// <param name="plot_right">绘图区域右边界。</param>
    /// <param name="plot_bottom">绘图区域下边界。</param>
    /// <param name="x_min">X 轴数据最小值。</param>
    /// <param name="x_max">X 轴数据最大值。</param>
    /// <param name="y_min">Y 轴数据最小值。</param>
    /// <param name="y_max">Y 轴数据最大值。</param>
    /// <param name="fill_color">默认填充颜色。</param>
    /// <param name="stroke_color">默认描边颜色。</param>
    /// <param name="palette">颜色调色板，用于分类着色。</param>
    /// <param name="base_size">基础点半径（像素）。</param>
    public static void render(
        IRenderCanvas canvas,
        DataPoint[] data_points,
        float plot_left, float plot_top, float plot_right, float plot_bottom,
        double x_min, double x_max, double y_min, double y_max,
        PlotColor fill_color, PlotColor stroke_color,
        ColorPalette? palette, float base_size)
    {
        var plot_width = plot_right - plot_left;
        var plot_height = plot_bottom - plot_top;
        var x_range = x_max - x_min;
        var y_range = y_max - y_min;

        if (Math.Abs(x_range) < double.Epsilon || Math.Abs(y_range) < double.Epsilon) return;

        canvas.set_line_width(1.0f);

        for (var i = 0; i < data_points.Length; i++)
        {
            var pt = data_points[i];
            var px = plot_left + (float)((pt.x - x_min) / x_range) * plot_width;
            var py = plot_bottom - (float)((pt.y - y_min) / y_range) * plot_height;

            var color = palette != null && pt.category != null
                ? palette.get_color(pt.category)
                : fill_color;

            var radius = (float)(base_size * Math.Sqrt(pt.size));

            canvas.set_fill_color(color);
            canvas.fill_circle(px, py, radius);

            canvas.set_stroke_color(stroke_color);
            canvas.stroke_circle(px, py, radius);
        }
    }
}