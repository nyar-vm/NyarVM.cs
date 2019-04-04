using System;
using System.Text;

namespace Plotter.Rendering;

/// <summary>
///     基于 SVG 的渲染画布实现，将绘图指令转换为 SVG XML 文档。
/// </summary>
public sealed class SvgRenderCanvas : IRenderCanvas
{
    #region 字段

    /// <summary>
    ///     SVG 文档构建器。
    /// </summary>
    private readonly StringBuilder _svg_builder;

    /// <summary>
    ///     画布宽度。
    /// </summary>
    private int _width;

    /// <summary>
    ///     画布高度。
    /// </summary>
    private int _height;

    /// <summary>
    ///     当前填充颜色。
    /// </summary>
    private PlotColor _fill_color;

    /// <summary>
    ///     当前描边颜色。
    /// </summary>
    private PlotColor _stroke_color;

    /// <summary>
    ///     当前线宽。
    /// </summary>
    private float _line_width = 1.0f;

    /// <summary>
    ///     当前虚线模式。
    /// </summary>
    private float[] _line_dash = [];

    /// <summary>
    ///     当前字体。
    /// </summary>
    private PlotFont _font;

    /// <summary>
    ///     当前路径数据（SVG path d 属性内容）。
    /// </summary>
    private readonly StringBuilder _path_data;

    /// <summary>
    ///     是否正在渲染。
    /// </summary>
    private bool _rendering;

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化 <see cref="SvgRenderCanvas" /> 类的新实例，默认尺寸为 800×600。
    /// </summary>
    public SvgRenderCanvas()
        : this(800, 600)
    {
    }

    /// <summary>
    ///     初始化 <see cref="SvgRenderCanvas" /> 类的新实例，指定画布尺寸。
    /// </summary>
    /// <param name="width">画布宽度。</param>
    /// <param name="height">画布高度。</param>
    public SvgRenderCanvas(int width, int height)
    {
        _svg_builder = new StringBuilder();
        _path_data = new StringBuilder();
        _width = width;
        _height = height;
        _fill_color = new PlotColor(0, 0, 0);
        _stroke_color = new PlotColor(0, 0, 0);
        _font = new PlotFont("sans-serif", 12f);
        _rendering = false;
    }

    #endregion

    #region 画布管理

    /// <inheritdoc />
    public int width => _width;

    /// <inheritdoc />
    public int height => _height;

    /// <inheritdoc />
    public void set_size(int width, int height)
    {
        _width = width;
        _height = height;
    }

    /// <inheritdoc />
    public void begin_render()
    {
        _rendering = true;
        _svg_builder.Clear();
        _svg_builder.Append(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{_width}\" height=\"{_height}\" viewBox=\"0 0 {_width} {_height}\">");
        _svg_builder.AppendLine();
    }

    /// <inheritdoc />
    public void end_render()
    {
        _svg_builder.AppendLine("</svg>");
        _rendering = false;
    }

    /// <inheritdoc />
    public void clear()
    {
        _svg_builder.Clear();
        _path_data.Clear();
        _rendering = false;
    }

    #endregion

    #region 样式设置

    /// <inheritdoc />
    public void set_fill_color(PlotColor color)
    {
        _fill_color = color;
    }

    /// <inheritdoc />
    public void set_stroke_color(PlotColor color)
    {
        _stroke_color = color;
    }

    /// <inheritdoc />
    public void set_line_width(float width)
    {
        _line_width = width;
    }

    /// <inheritdoc />
    public void set_line_dash(float[] pattern)
    {
        _line_dash = pattern ?? [];
    }

    /// <inheritdoc />
    public void set_font(PlotFont font)
    {
        _font = font;
    }

    #endregion

    #region 图形绘制

    /// <inheritdoc />
    public void fill_rect(float x, float y, float w, float h)
    {
        _svg_builder.Append(
            $"  <rect x=\"{format_float(x)}\" y=\"{format_float(y)}\" width=\"{format_float(w)}\" height=\"{format_float(h)}\"");
        append_fill_attribute();
        append_common_attributes(false);
        _svg_builder.AppendLine(" />");
    }

    /// <inheritdoc />
    public void stroke_rect(float x, float y, float w, float h)
    {
        _svg_builder.Append(
            $"  <rect x=\"{format_float(x)}\" y=\"{format_float(y)}\" width=\"{format_float(w)}\" height=\"{format_float(h)}\"");
        append_stroke_attribute();
        append_common_attributes(true);
        _svg_builder.AppendLine(" />");
    }

    /// <inheritdoc />
    public void fill_circle(float cx, float cy, float r)
    {
        _svg_builder.Append(
            $"  <circle cx=\"{format_float(cx)}\" cy=\"{format_float(cy)}\" r=\"{format_float(r)}\"");
        append_fill_attribute();
        append_common_attributes(false);
        _svg_builder.AppendLine(" />");
    }

    /// <inheritdoc />
    public void stroke_circle(float cx, float cy, float r)
    {
        _svg_builder.Append(
            $"  <circle cx=\"{format_float(cx)}\" cy=\"{format_float(cy)}\" r=\"{format_float(r)}\"");
        append_stroke_attribute();
        append_common_attributes(true);
        _svg_builder.AppendLine(" />");
    }

    /// <inheritdoc />
    public void fill_ellipse(float cx, float cy, float rx, float ry)
    {
        _svg_builder.Append(
            $"  <ellipse cx=\"{format_float(cx)}\" cy=\"{format_float(cy)}\" rx=\"{format_float(rx)}\" ry=\"{format_float(ry)}\"");
        append_fill_attribute();
        append_common_attributes(false);
        _svg_builder.AppendLine(" />");
    }

    /// <inheritdoc />
    public void stroke_ellipse(float cx, float cy, float rx, float ry)
    {
        _svg_builder.Append(
            $"  <ellipse cx=\"{format_float(cx)}\" cy=\"{format_float(cy)}\" rx=\"{format_float(rx)}\" ry=\"{format_float(ry)}\"");
        append_stroke_attribute();
        append_common_attributes(true);
        _svg_builder.AppendLine(" />");
    }

    #endregion

    #region 路径绘制

    /// <inheritdoc />
    public void begin_path()
    {
        _path_data.Clear();
    }

    /// <inheritdoc />
    public void move_to(float x, float y)
    {
        if (_path_data.Length > 0) _path_data.Append(' ');

        _path_data.Append($"M {format_float(x)} {format_float(y)}");
    }

    /// <inheritdoc />
    public void line_to(float x, float y)
    {
        if (_path_data.Length > 0) _path_data.Append(' ');

        _path_data.Append($"L {format_float(x)} {format_float(y)}");
    }

    /// <inheritdoc />
    public void arc(float cx, float cy, float r, float start_angle, float end_angle)
    {
        var cos_start = (float)Math.Cos(start_angle);
        var sin_start = (float)Math.Sin(start_angle);
        var cos_end = (float)Math.Cos(end_angle);
        var sin_end = (float)Math.Sin(end_angle);

        var start_x = cx + r * cos_start;
        var start_y = cy + r * sin_start;
        var end_x = cx + r * cos_end;
        var end_y = cy + r * sin_end;

        if (_path_data.Length > 0) _path_data.Append(' ');

        _path_data.Append($"L {format_float(start_x)} {format_float(start_y)}");

        var angle_diff = end_angle - start_angle;
        if (angle_diff < 0) angle_diff += 2.0f * (float)Math.PI;

        var large_arc_flag = angle_diff > (float)Math.PI ? 1 : 0;

        _path_data.Append(
            $" A {format_float(r)} {format_float(r)} 0 {large_arc_flag} 1 {format_float(end_x)} {format_float(end_y)}");
    }

    /// <inheritdoc />
    public void close_path()
    {
        if (_path_data.Length > 0) _path_data.Append(' ');

        _path_data.Append('Z');
    }

    /// <inheritdoc />
    public void fill()
    {
        if (_path_data.Length == 0) return;

        _svg_builder.Append($"  <path d=\"{_path_data}\"");
        append_fill_attribute();
        append_common_attributes(false);
        _svg_builder.AppendLine(" />");
    }

    /// <inheritdoc />
    public void stroke()
    {
        if (_path_data.Length == 0) return;

        _svg_builder.Append($"  <path d=\"{_path_data}\"");
        append_stroke_attribute();
        append_common_attributes(true);
        _svg_builder.AppendLine(" />");
    }

    #endregion

    #region 文本绘制

    /// <inheritdoc />
    public void fill_text(string text, float x, float y)
    {
        var escaped = escape_xml(text);
        _svg_builder.Append($"  <text x=\"{format_float(x)}\" y=\"{format_float(y)}\"");
        append_fill_attribute();
        _svg_builder.Append($" style=\"{_font.to_svg_style()}\"");
        _svg_builder.AppendLine($">{escaped}</text>");
    }

    /// <inheritdoc />
    public void stroke_text(string text, float x, float y)
    {
        var escaped = escape_xml(text);
        _svg_builder.Append($"  <text x=\"{format_float(x)}\" y=\"{format_float(y)}\"");
        append_stroke_attribute();
        _svg_builder.Append($" style=\"{_font.to_svg_style()}\"");
        _svg_builder.AppendLine($">{escaped}</text>");
    }

    #endregion

    #region 导出

    /// <summary>
    ///     导出为 PNG 格式字节数组。SVG 画布不支持此操作。
    /// </summary>
    /// <returns>此方法始终抛出 <see cref="NotSupportedException" />。</returns>
    /// <exception cref="NotSupportedException">SVG 画布不支持 PNG 导出。</exception>
    public byte[] to_png_bytes()
    {
        throw new NotSupportedException("PNG 导出需要 BitmapRenderCanvas");
    }

    /// <summary>
    ///     导出为 SVG 格式字符串。
    /// </summary>
    /// <returns>完整的 SVG 文档字符串。</returns>
    public string to_svg_string()
    {
        return _svg_builder.ToString();
    }

    /// <summary>
    ///     获取底层原生渲染上下文对象。
    /// </summary>
    /// <returns><see cref="StringBuilder" /> 实例，包含正在构建的 SVG 文档。</returns>
    public object get_native_context()
    {
        return _svg_builder;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     将浮点数格式化为 SVG 属性值字符串，去除末尾无意义的零。
    /// </summary>
    /// <param name="value">浮点数值。</param>
    /// <returns>格式化后的字符串。</returns>
    private static string format_float(float value)
    {
        return value.ToString("G");
    }

    /// <summary>
    ///     追加填充颜色属性到当前元素。
    /// </summary>
    private void append_fill_attribute()
    {
        _svg_builder.Append($" fill=\"{_fill_color.to_svg_string()}\"");
    }

    /// <summary>
    ///     追加描边颜色属性到当前元素。
    /// </summary>
    private void append_stroke_attribute()
    {
        _svg_builder.Append($" stroke=\"{_stroke_color.to_svg_string()}\"");
    }

    /// <summary>
    ///     追加通用样式属性（描边宽度、虚线模式等）。
    /// </summary>
    /// <param name="is_stroke">是否为描边操作。</param>
    private void append_common_attributes(bool is_stroke)
    {
        if (is_stroke)
        {
            if (Math.Abs(_line_width - 1.0f) > float.Epsilon)
                _svg_builder.Append($" stroke-width=\"{format_float(_line_width)}\"");
        }
        else
        {
            _svg_builder.Append(" stroke=\"none\"");
        }

        if (_line_dash.Length > 0)
        {
            var dash_parts = new string[_line_dash.Length];
            for (var i = 0; i < _line_dash.Length; i++) dash_parts[i] = format_float(_line_dash[i]);

            _svg_builder.Append($" stroke-dasharray=\"{string.Join(",", dash_parts)}\"");
        }
    }

    /// <summary>
    ///     对文本内容进行 XML 转义，替换特殊字符。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <returns>转义后的安全文本。</returns>
    private static string escape_xml(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
            switch (ch)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&apos;");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }

        return sb.ToString();
    }

    #endregion
}