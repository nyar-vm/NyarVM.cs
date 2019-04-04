using Core.Graphic.Canvas;

namespace Std.Canvas;

/// <summary>
///     终端画布实现，提供基于文本的绘图操作。
/// </summary>
public sealed class TerminalCanvas : ICanvas
{
    private readonly char[] _buffer;
    private readonly StringBuilder _output;

    /// <summary>
    ///     初始化 <see cref="TerminalCanvas" /> 的新实例。
    /// </summary>
    /// <param name="width">画布宽度。</param>
    /// <param name="height">画布高度。</param>
    public TerminalCanvas(int width, int height)
    {
        this.width = width;
        this.height = height;
        _buffer = new char[width * height];
        _output = new StringBuilder();
        clear();
    }

    /// <summary>
    ///     获取画布宽度。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取画布高度。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     清空画布内容。
    /// </summary>
    public void clear()
    {
        Array.Fill(_buffer, ' ');
        _output.Clear();
    }

    /// <summary>
    ///     在画布上绘制一条直线。
    /// </summary>
    /// <param name="x1">起始点水平坐标。</param>
    /// <param name="y1">起始点垂直坐标。</param>
    /// <param name="x2">终止点水平坐标。</param>
    /// <param name="y2">终止点垂直坐标。</param>
    /// <param name="ch">绘制字符，默认为 '-'。</param>
    public void draw_line(int x1, int y1, int x2, int y2, char ch = '-')
    {
        var dx = System.Math.Abs(x2 - x1);
        var dy = System.Math.Abs(y2 - y1);
        var sx = x1 < x2 ? 1 : -1;
        var sy = y1 < y2 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            set_pixel(x1, y1, ch);

            if (x1 == x2 && y1 == y2) break;

            var e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }

    /// <summary>
    ///     在画布上绘制一个矩形边框。
    /// </summary>
    /// <param name="x">左上角水平坐标。</param>
    /// <param name="y">左上角垂直坐标。</param>
    /// <param name="rect_width">矩形宽度。</param>
    /// <param name="rect_height">矩形高度。</param>
    /// <param name="ch">绘制字符，默认为 '#'。</param>
    public void draw_rect(int x, int y, int rectWidth, int rectHeight, char ch = '#')
    {
        draw_line(x, y, x + rectWidth - 1, y, ch);
        draw_line(x, y + rectHeight - 1, x + rectWidth - 1, y + rectHeight - 1, ch);
        draw_line(x, y, x, y + rectHeight - 1, ch);
        draw_line(x + rectWidth - 1, y, x + rectWidth - 1, y + rectHeight - 1, ch);
    }

    /// <summary>
    ///     在画布上绘制文本。
    /// </summary>
    /// <param name="x">起始水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    /// <param name="text">要绘制的文本。</param>
    public void draw_text(int x, int y, string text)
    {
        for (var i = 0; i < text.Length; i++) set_pixel(x + i, y, text[i]);
    }

    /// <summary>
    ///     将画布内容渲染为字符串并输出。
    /// </summary>
    /// <returns>画布内容的字符串表示。</returns>
    public string render()
    {
        _output.Clear();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++) _output.Append(_buffer[y * width + x]);

            if (y < height - 1) _output.AppendLine();
        }

        return _output.ToString();
    }

    /// <summary>
    ///     设置指定位置的像素字符。
    /// </summary>
    /// <param name="x">水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    /// <param name="ch">字符。</param>
    private void set_pixel(int x, int y, char ch)
    {
        if (x >= 0 && x < width && y >= 0 && y < height) _buffer[y * width + x] = ch;
    }
}