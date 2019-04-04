using System;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     柱状图渲染器，支持垂直柱状图和水平条形图。
/// </summary>
internal static class BarRenderer
{
    /// <summary>
    ///     渲染垂直柱状图。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="data_points">数据点列表。</param>
    /// <param name="plot_left">绘图区域左边界。</param>
    /// <param name="plot_top">绘图区域上边界。</param>
    /// <param name="plot_right">绘图区域右边界。</param>
    /// <param name="plot_bottom">绘图区域下边界。</param>
    /// <param name="y_min">Y 轴数据最小值。</param>
    /// <param name="y_max">Y 轴数据最大值。</param>
    /// <param name="fill_color">默认填充颜色。</param>
    /// <param name="stroke_color">描边颜色。</param>
    /// <param name="palette">颜色调色板，用于分类着色。</param>
    /// <param name="show_labels">是否显示柱顶数值标签。</param>
    public static void render_vertical(
        IRenderCanvas canvas,
        DataPoint[] data_points,
        float plot_left, float plot_top, float plot_right, float plot_bottom,
        double y_min, double y_max,
        PlotColor fill_color, PlotColor stroke_color,
        ColorPalette? palette, bool show_labels)
    {
        if (data_points.Length == 0) return;

        var plot_width = plot_right - plot_left;
        var plot_height = plot_bottom - plot_top;
        var y_range = y_max - y_min;

        if (Math.Abs(y_range) < double.Epsilon) return;

        var bar_width = plot_width / data_points.Length;
        var bar_padding = bar_width * 0.15f;
        var actual_bar_width = bar_width - bar_padding * 2;

        var baseline_y = plot_bottom - (float)((0 - y_min) / y_range) * plot_height;

        if (baseline_y < plot_top) baseline_y = plot_top;

        if (baseline_y > plot_bottom) baseline_y = plot_bottom;

        canvas.set_line_width(1.0f);

        for (var i = 0; i < data_points.Length; i++)
        {
            var pt = data_points[i];
            var bar_x = plot_left + bar_width * i + bar_padding;
            var bar_top = plot_bottom - (float)((pt.y - y_min) / y_range) * plot_height;

            var color = palette != null && pt.category != null
                ? palette.get_color(pt.category)
                : fill_color;

            canvas.set_fill_color(color);
            canvas.fill_rect(bar_x, bar_top, actual_bar_width, baseline_y - bar_top);

            canvas.set_stroke_color(stroke_color);
            canvas.stroke_rect(bar_x, bar_top, actual_bar_width, baseline_y - bar_top);

            if (show_labels)
            {
                canvas.set_fill_color(PlotColor.from_hex("#333333"));
                canvas.set_font(new PlotFont("sans-serif", 10f));
                var label = pt.y.ToString("G4");
                canvas.fill_text(label, bar_x + actual_bar_width / 2 - 12, bar_top - 14);
            }
        }
    }

    /// <summary>
    ///     渲染水平条形图。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="data_points">数据点列表。</param>
    /// <param name="plot_left">绘图区域左边界。</param>
    /// <param name="plot_top">绘图区域上边界。</param>
    /// <param name="plot_right">绘图区域右边界。</param>
    /// <param name="plot_bottom">绘图区域下边界。</param>
    /// <param name="x_min">X 轴数据最小值。</param>
    /// <param name="x_max">X 轴数据最大值。</param>
    /// <param name="fill_color">默认填充颜色。</param>
    /// <param name="stroke_color">描边颜色。</param>
    /// <param name="palette">颜色调色板，用于分类着色。</param>
    /// <param name="show_labels">是否显示条尾数值标签。</param>
    public static void render_horizontal(
        IRenderCanvas canvas,
        DataPoint[] data_points,
        float plot_left, float plot_top, float plot_right, float plot_bottom,
        double x_min, double x_max,
        PlotColor fill_color, PlotColor stroke_color,
        ColorPalette? palette, bool show_labels)
    {
        if (data_points.Length == 0) return;

        var plot_width = plot_right - plot_left;
        var plot_height = plot_bottom - plot_top;
        var x_range = x_max - x_min;

        if (Math.Abs(x_range) < double.Epsilon) return;

        var bar_height = plot_height / data_points.Length;
        var bar_padding = bar_height * 0.15f;
        var actual_bar_height = bar_height - bar_padding * 2;

        var baseline_x = plot_left + (float)((0 - x_min) / x_range) * plot_width;

        if (baseline_x < plot_left) baseline_x = plot_left;

        if (baseline_x > plot_right) baseline_x = plot_right;

        canvas.set_line_width(1.0f);

        for (var i = 0; i < data_points.Length; i++)
        {
            var pt = data_points[i];
            var bar_y = plot_top + bar_height * i + bar_padding;
            var bar_right = plot_left + (float)((pt.x - x_min) / x_range) * plot_width;

            var color = palette != null && pt.category != null
                ? palette.get_color(pt.category)
                : fill_color;

            canvas.set_fill_color(color);
            canvas.fill_rect(baseline_x, bar_y, bar_right - baseline_x, actual_bar_height);

            canvas.set_stroke_color(stroke_color);
            canvas.stroke_rect(baseline_x, bar_y, bar_right - baseline_x, actual_bar_height);

            if (show_labels)
            {
                canvas.set_fill_color(PlotColor.from_hex("#333333"));
                canvas.set_font(new PlotFont("sans-serif", 10f));
                var label = pt.x.ToString("G4");
                canvas.fill_text(label, bar_right + 4, bar_y + actual_bar_height / 2 - 5);
            }
        }
    }
}