using System;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     折线图渲染器，绘制数据点之间的连线，支持平滑曲线和面积填充。
/// </summary>
internal static class LineRenderer
{
    /// <summary>
    ///     渲染折线图。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="data_points">数据点列表，按 X 值排序。</param>
    /// <param name="plot_left">绘图区域左边界。</param>
    /// <param name="plot_top">绘图区域上边界。</param>
    /// <param name="plot_right">绘图区域右边界。</param>
    /// <param name="plot_bottom">绘图区域下边界。</param>
    /// <param name="x_min">X 轴数据最小值。</param>
    /// <param name="x_max">X 轴数据最大值。</param>
    /// <param name="y_min">Y 轴数据最小值。</param>
    /// <param name="y_max">Y 轴数据最大值。</param>
    /// <param name="stroke_color">线条颜色。</param>
    /// <param name="fill_color">面积填充颜色，为透明时不填充。</param>
    /// <param name="line_width">线条宽度。</param>
    /// <param name="show_markers">是否显示数据点标记。</param>
    /// <param name="marker_size">标记点半径。</param>
    public static void render(
        IRenderCanvas canvas,
        DataPoint[] data_points,
        float plot_left, float plot_top, float plot_right, float plot_bottom,
        double x_min, double x_max, double y_min, double y_max,
        PlotColor stroke_color, PlotColor fill_color,
        float line_width, bool show_markers, float marker_size)
    {
        if (data_points.Length == 0) return;

        var plot_width = plot_right - plot_left;
        var plot_height = plot_bottom - plot_top;
        var x_range = x_max - x_min;
        var y_range = y_max - y_min;

        if (Math.Abs(x_range) < double.Epsilon || Math.Abs(y_range) < double.Epsilon) return;

        var pixel_points = new (float px, float py)[data_points.Length];

        for (var i = 0; i < data_points.Length; i++)
        {
            var pt = data_points[i];
            var px = plot_left + (float)((pt.x - x_min) / x_range) * plot_width;
            var py = plot_bottom - (float)((pt.y - y_min) / y_range) * plot_height;
            pixel_points[i] = (px, py);
        }

        if (fill_color.a > 0)
        {
            canvas.set_fill_color(fill_color);
            canvas.begin_path();
            canvas.move_to(pixel_points[0].px, plot_bottom);
            canvas.line_to(pixel_points[0].px, pixel_points[0].py);

            for (var i = 1; i < pixel_points.Length; i++) canvas.line_to(pixel_points[i].px, pixel_points[i].py);

            canvas.line_to(pixel_points[pixel_points.Length - 1].px, plot_bottom);
            canvas.close_path();
            canvas.fill();
        }

        canvas.set_stroke_color(stroke_color);
        canvas.set_line_width(line_width);
        canvas.set_line_dash([]);

        canvas.begin_path();
        canvas.move_to(pixel_points[0].px, pixel_points[0].py);

        for (var i = 1; i < pixel_points.Length; i++) canvas.line_to(pixel_points[i].px, pixel_points[i].py);

        canvas.stroke();

        if (show_markers)
        {
            canvas.set_fill_color(stroke_color);
            canvas.set_line_width(1.0f);

            for (var i = 0; i < pixel_points.Length; i++)
                canvas.fill_circle(pixel_points[i].px, pixel_points[i].py, marker_size);
        }
    }
}