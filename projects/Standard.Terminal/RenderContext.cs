namespace Std.Terminal;

/// <summary>
///     渲染上下文，控件通过此对象将内容绘制到屏幕缓冲区
/// </summary>
public sealed class RenderContext
{
    private readonly ScreenBuffer _buffer;
    private readonly int _offset_x;
    private readonly int _offset_y;

    internal RenderContext(ScreenBuffer buffer, int width, int height, int offsetX, int offsetY)
    {
        _buffer = buffer;
        this.width = width;
        this.height = height;
        _offset_x = offsetX;
        _offset_y = offsetY;
    }

    /// <summary>
    ///     可用宽度
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     可用高度
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     默认前景色
    /// </summary>
    public RgbColor default_foreground { get; set; } = RgbColor.white;

    /// <summary>
    ///     默认背景色
    /// </summary>
    public RgbColor default_background { get; set; } = RgbColor.black;

    /// <summary>
    ///     在相对坐标绘制纯文本
    /// </summary>
    /// <param name="x">相对 X 坐标</param>
    /// <param name="y">相对 Y 坐标</param>
    /// <param name="text">文本内容</param>
    public void draw_text(int x, int y, string text)
    {
        draw_text(x, y, text, default_foreground, default_background);
    }

    /// <summary>
    ///     在相对坐标绘制带颜色的纯文本
    /// </summary>
    /// <param name="x">相对 X 坐标</param>
    /// <param name="y">相对 Y 坐标</param>
    /// <param name="text">文本内容</param>
    /// <param name="foreground">前景色</param>
    /// <param name="background">背景色</param>
    public void draw_text(int x, int y, string text, RgbColor foreground, RgbColor background)
    {
        _buffer.set_string(_offset_x + x, _offset_y + y, text, foreground, background);
    }

    /// <summary>
    ///     在指定高度绘制水平居中文本
    /// </summary>
    /// <param name="y">Y 坐标</param>
    /// <param name="text">文本内容</param>
    public void draw_centered_text(int y, string text)
    {
        draw_centered_text(y, text, default_foreground, default_background);
    }

    /// <summary>
    ///     在指定高度绘制水平居中带颜色的文本
    /// </summary>
    /// <param name="y">Y 坐标</param>
    /// <param name="text">文本内容</param>
    /// <param name="foreground">前景色</param>
    /// <param name="background">背景色</param>
    public void draw_centered_text(int y, string text, RgbColor foreground, RgbColor background)
    {
        var x = (width - text.Length) / 2;
        if (x < 0) x = 0;

        draw_text(x, y, text, foreground, background);
    }

    /// <summary>
    ///     填充指定矩形区域为某颜色
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="background">背景色</param>
    public void fill_rect(int x, int y, int width, int height, RgbColor background)
    {
        for (var row = 0; row < height; row++)
        for (var col = 0; col < width; col++)
            _buffer.set_char(_offset_x + x + col, _offset_y + y + row, ' ', default_foreground, background);
    }

    /// <summary>
    ///     绘制边框
    /// </summary>
    /// <param name="x">左上角 X</param>
    /// <param name="y">左上角 Y</param>
    /// <param name="width">边框宽度</param>
    /// <param name="height">边框高度</param>
    /// <param name="style">边框样式</param>
    public void draw_border(int x, int y, int width, int height, BorderStyle style)
    {
        draw_border(x, y, width, height, style, default_foreground, default_background);
    }

    /// <summary>
    ///     绘制带颜色的边框
    /// </summary>
    /// <param name="x">左上角 X</param>
    /// <param name="y">左上角 Y</param>
    /// <param name="width">边框宽度</param>
    /// <param name="height">边框高度</param>
    /// <param name="style">边框样式</param>
    /// <param name="foreground">前景色</param>
    /// <param name="background">背景色</param>
    public void draw_border(int x, int y, int width, int height, BorderStyle style, RgbColor foreground,
        RgbColor background)
    {
        var chars = BorderChars.get(style);

        _buffer.set_char(_offset_x + x, _offset_y + y, chars.top_left, foreground, background);
        _buffer.set_char(_offset_x + x + width - 1, _offset_y + y, chars.top_right, foreground, background);
        _buffer.set_char(_offset_x + x, _offset_y + y + height - 1, chars.bottom_left, foreground, background);
        _buffer.set_char(_offset_x + x + width - 1, _offset_y + y + height - 1, chars.bottom_right, foreground,
            background);

        for (var i = 1; i < width - 1; i++)
        {
            _buffer.set_char(_offset_x + x + i, _offset_y + y, chars.horizontal, foreground, background);
            _buffer.set_char(_offset_x + x + i, _offset_y + y + height - 1, chars.horizontal, foreground, background);
        }

        for (var i = 1; i < height - 1; i++)
        {
            _buffer.set_char(_offset_x + x, _offset_y + y + i, chars.vertical, foreground, background);
            _buffer.set_char(_offset_x + x + width - 1, _offset_y + y + i, chars.vertical, foreground, background);
        }
    }

    /// <summary>
    ///     创建子上下文，用于渲染子控件
    /// </summary>
    /// <param name="x">相对 X 偏移</param>
    /// <param name="y">相对 Y 偏移</param>
    /// <param name="width">子上下文宽度</param>
    /// <param name="height">子上下文高度</param>
    /// <returns>新的渲染上下文</returns>
    public RenderContext create_child(int x, int y, int width, int height)
    {
        return new RenderContext(_buffer, width, height, _offset_x + x, _offset_y + y)
        {
            default_foreground = default_foreground,
            default_background = default_background
        };
    }
}