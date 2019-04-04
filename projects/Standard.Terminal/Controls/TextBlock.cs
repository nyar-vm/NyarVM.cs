namespace Std.Terminal.Controls;

/// <summary>
///     文本块，显示静态或可更新的文本
/// </summary>
public sealed class TextBlock : View
{
    private string _text;

    /// <summary>
    ///     创建文本块
    /// </summary>
    /// <param name="text">显示文本</param>
    public TextBlock(string text)
    {
        _text = text;
        width = text.Length;
        height = 1;
    }

    /// <summary>
    ///     显示文本
    /// </summary>
    public string text
    {
        get => _text;
        set
        {
            _text = value;
            width = value.Length;
            height = 1;
        }
    }

    /// <summary>
    ///     文本样式
    /// </summary>
    public TextBlockStyle style_type { get; set; } = TextBlockStyle.body;

    /// <summary>
    ///     设置文本样式预设
    /// </summary>
    /// <param name="style">样式预设</param>
    public TextBlock style(TextBlockStyle style)
    {
        style_type = style;
        return this;
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var fg = get_text_color(style_type);
        ctx.draw_text(0, 0, _text, fg, ctx.default_background);
    }

    private static RgbColor get_text_color(TextBlockStyle style)
    {
        return style switch
        {
            TextBlockStyle.title => new RgbColor(255, 255, 100),
            TextBlockStyle.body => RgbColor.White,
            TextBlockStyle.highlight => new RgbColor(100, 200, 255),
            TextBlockStyle.error => new RgbColor(255, 80, 80),
            TextBlockStyle.header => new RgbColor(200, 200, 200),
            _ => RgbColor.White
        };
    }
}