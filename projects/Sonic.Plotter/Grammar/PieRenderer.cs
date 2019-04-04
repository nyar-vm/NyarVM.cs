using System;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     饼图渲染器，绘制饼图扇区、标签，支持甜甜圈模式。
/// </summary>
internal static class PieRenderer
{
    /// <summary>
    ///     渲染饼图。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    /// <param name="data_points">数据点列表，每个点代表一个扇区。</param>
    /// <param name="center_x">饼图中心 X 坐标。</param>
    /// <param name="center_y">饼图中心 Y 坐标。</param>
    /// <param name="radius">饼图半径。</param>
    /// <param name="palette">颜色调色板。</param>
    /// <param name="stroke_color">扇区描边颜色。</param>
    /// <param name="inner_radius">内圆半径，大于 0 时为甜甜圈模式。</param>
    /// <param name="show_labels">是否显示扇区标签。</param>
    public static void render(
        IRenderCanvas canvas,
        DataPoint[] data_points,
        float center_x, float center_y, float radius,
        ColorPalette palette,
        PlotColor stroke_color,
        float inner_radius,
        bool show_labels)
    {
        if (data_points.Length == 0) return;

        var total = 0.0;

        for (var i = 0; i < data_points.Length; i++) total += Math.Abs(data_points[i].y);

        if (total < double.Epsilon) return;

        canvas.set_line_width(1.0f);
        canvas.set_stroke_color(stroke_color);

        var start_angle = (float)(-Math.PI / 2.0);

        for (var i = 0; i < data_points.Length; i++)
        {
            var value = Math.Abs(data_points[i].y);
            var sweep = (float)(2.0 * Math.PI * value / total);
            var end_angle = start_angle + sweep;

            var category = data_points[i].category ?? $"Slice {i}";
            var color = palette.get_color(category);

            canvas.set_fill_color(color);
            canvas.begin_path();
            canvas.move_to(center_x, center_y);
            canvas.arc(center_x, center_y, radius, start_angle, end_angle);
            canvas.close_path();
            canvas.fill();
            canvas.stroke();

            if (inner_radius > 0)
            {
                canvas.set_fill_color(PlotColor.white);
                canvas.begin_path();
                canvas.arc(center_x, center_y, inner_radius, 0, (float)(2.0 * Math.PI));
                canvas.close_path();
                canvas.fill();
            }

            if (show_labels)
            {
                var mid_angle = start_angle + sweep / 2;
                var label_radius = radius + 20;
                var label_x = center_x + (float)Math.Cos(mid_angle) * label_radius;
                var label_y = center_y + (float)Math.Sin(mid_angle) * label_radius;

                var percentage = value / total * 100.0;
                var label_text = $"{category} {percentage:F1}%";

                canvas.set_fill_color(PlotColor.from_hex("#333333"));
                canvas.set_font(new PlotFont("sans-serif", 11f));
                canvas.fill_text(label_text, label_x - 20, label_y - 5);
            }

            start_angle = end_angle;
        }
    }
}