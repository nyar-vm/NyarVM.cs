using System;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     坐标轴渲染器，负责绘制坐标轴线、刻度标签和网格线。
/// </summary>
internal static class AxisRenderer
{
    #region X 轴渲染

    /// <summary>
    ///     渲染 X 轴（底部），支持分类标签和数值刻度两种模式。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="plot_left">绘图区域左边界。</param>
    /// <param name="plot_right">绘图区域右边界。</param>
    /// <param name="plot_bottom">绘图区域下边界。</param>
    /// <param name="scale_type">X 轴刻度类型。</param>
    /// <param name="x_min">X 轴数据最小值。</param>
    /// <param name="x_max">X 轴数据最大值。</param>
    /// <param name="ticks">X 轴刻度值数组。</param>
    /// <param name="categories">分类标签数组，当刻度类型为 Category 时使用。</param>
    public static void render_x_axis(
        IRenderCanvas canvas,
        float plot_left, float plot_right, float plot_bottom,
        ScaleType scale_type,
        double x_min, double x_max,
        double[] ticks,
        string[]? categories)
    {
        canvas.set_stroke_color(_axis_color);
        canvas.set_line_width(1.0f);
        canvas.set_line_dash([]);

        canvas.begin_path();
        canvas.move_to(plot_left, plot_bottom);
        canvas.line_to(plot_right, plot_bottom);
        canvas.stroke();

        canvas.set_font(_label_font);
        canvas.set_fill_color(_label_color);

        var plot_width = plot_right - plot_left;

        if (scale_type == ScaleType.Category && categories != null)
        {
            var bar_width = plot_width / categories.Length;

            for (var i = 0; i < categories.Length; i++)
            {
                var tick_x = plot_left + bar_width * (i + 0.5f);

                canvas.begin_path();
                canvas.move_to(tick_x, plot_bottom);
                canvas.line_to(tick_x, plot_bottom + 5);
                canvas.stroke();

                canvas.fill_text(categories[i], tick_x - 15, plot_bottom + 8);
            }
        }
        else
        {
            var range = x_max - x_min;

            if (Math.Abs(range) < double.Epsilon) return;

            for (var i = 0; i < ticks.Length; i++)
            {
                var tick_x = plot_left + (float)((ticks[i] - x_min) / range) * plot_width;

                canvas.begin_path();
                canvas.move_to(tick_x, plot_bottom);
                canvas.line_to(tick_x, plot_bottom + 5);
                canvas.stroke();

                var label = format_tick_value(ticks[i]);
                canvas.fill_text(label, tick_x - 12, plot_bottom + 8);
            }
        }
    }

    #endregion

    #region Y 轴渲染

    /// <summary>
    ///     渲染 Y 轴（左侧），绘制数值刻度线和标签。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="plot_left">绘图区域左边界。</param>
    /// <param name="plot_top">绘图区域上边界。</param>
    /// <param name="plot_bottom">绘图区域下边界。</param>
    /// <param name="y_min">Y 轴数据最小值。</param>
    /// <param name="y_max">Y 轴数据最大值。</param>
    /// <param name="ticks">Y 轴刻度值数组。</param>
    public static void render_y_axis(
        IRenderCanvas canvas,
        float plot_left, float plot_top, float plot_bottom,
        double y_min, double y_max,
        double[] ticks)
    {
        canvas.set_stroke_color(_axis_color);
        canvas.set_line_width(1.0f);
        canvas.set_line_dash([]);

        canvas.begin_path();
        canvas.move_to(plot_left, plot_top);
        canvas.line_to(plot_left, plot_bottom);
        canvas.stroke();

        canvas.set_font(_label_font);
        canvas.set_fill_color(_label_color);

        var range = y_max - y_min;

        if (Math.Abs(range) < double.Epsilon) return;

        var plot_height = plot_bottom - plot_top;

        for (var i = 0; i < ticks.Length; i++)
        {
            var tick_y = plot_bottom - (float)((ticks[i] - y_min) / range) * plot_height;

            canvas.begin_path();
            canvas.move_to(plot_left - 5, tick_y);
            canvas.line_to(plot_left, tick_y);
            canvas.stroke();

            var label = format_tick_value(ticks[i]);
            canvas.fill_text(label, plot_left - 35, tick_y + 4);
        }
    }

    #endregion

    #region 网格线渲染

    /// <summary>
    ///     渲染网格线，在 Y 轴刻度位置绘制水平虚线。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="plot_left">绘图区域左边界。</param>
    /// <param name="plot_right">绘图区域右边界。</param>
    /// <param name="plot_top">绘图区域上边界。</param>
    /// <param name="plot_bottom">绘图区域下边界。</param>
    /// <param name="y_min">Y 轴数据最小值。</param>
    /// <param name="y_max">Y 轴数据最大值。</param>
    /// <param name="ticks">Y 轴刻度值数组。</param>
    public static void render_grid(
        IRenderCanvas canvas,
        float plot_left, float plot_right,
        float plot_top, float plot_bottom,
        double y_min, double y_max,
        double[] ticks)
    {
        canvas.set_stroke_color(_grid_color);
        canvas.set_line_width(0.5f);
        canvas.set_line_dash([3f, 3f]);

        var range = y_max - y_min;

        if (Math.Abs(range) < double.Epsilon) return;

        var plot_height = plot_bottom - plot_top;

        for (var i = 0; i < ticks.Length; i++)
        {
            var tick_y = plot_bottom - (float)((ticks[i] - y_min) / range) * plot_height;

            if (tick_y < plot_top || tick_y > plot_bottom) continue;

            canvas.begin_path();
            canvas.move_to(plot_left, tick_y);
            canvas.line_to(plot_right, tick_y);
            canvas.stroke();
        }

        canvas.set_line_dash([]);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     格式化刻度值为人类可读的字符串。
    /// </summary>
    /// <param name="value">刻度值。</param>
    /// <returns>格式化后的字符串。</returns>
    private static string format_tick_value(double value)
    {
        if (Math.Abs(value) < double.Epsilon) return "0";

        if (Math.Abs(value - Math.Round(value)) < 1e-9) return ((long)Math.Round(value)).ToString();

        return value.ToString("G4");
    }

    #endregion

    #region 常量

    /// <summary>
    ///     坐标轴线颜色。
    /// </summary>
    private static readonly PlotColor _axis_color = PlotColor.from_hex("#666666");

    /// <summary>
    ///     网格线颜色。
    /// </summary>
    private static readonly PlotColor _grid_color = PlotColor.from_hex("#E0E0E0");

    /// <summary>
    ///     刻度标签颜色。
    /// </summary>
    private static readonly PlotColor _label_color = PlotColor.from_hex("#333333");

    /// <summary>
    ///     刻度标签字体。
    /// </summary>
    private static readonly PlotFont _label_font = new("sans-serif", 11f);

    #endregion
}