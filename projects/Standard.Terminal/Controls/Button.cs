using Std.Command;

namespace Std.Terminal.Controls;

/// <summary>
///     按钮控件，支持点击事件
/// </summary>
public sealed class Button : View
{
    private Action? _on_click;

    /// <summary>
    ///     创建按钮
    /// </summary>
    /// <param name="text">按钮文本</param>
    public Button(string text)
    {
        this.text = text;
        width = text.Length + 4;
        height = 3;
        tab_stop = true;
    }

    /// <summary>
    ///     按钮文本
    /// </summary>
    public LocalizableString text { get; set; }

    /// <summary>
    ///     按钮样式预设
    /// </summary>
    public ButtonStyleType style_type { get; set; } = ButtonStyleType.primary;

    /// <summary>
    ///     设置点击事件
    /// </summary>
    /// <param name="handler">点击处理</param>
    public Button OnClick(Action handler)
    {
        _on_click = handler;
        return this;
    }

    /// <summary>
    ///     设置按钮样式
    /// </summary>
    /// <param name="style">样式预设</param>
    public Button with_style(ButtonStyleType style)
    {
        style_type = style;
        return this;
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var displayText = text.default_value;
        var (fg, bg, focusFg, focusBg) = get_button_colors(style_type);

        var textFg = is_focused ? focusFg : fg;
        var borderFg = is_focused ? RgbColor.Yellow : fg;

        if (!enabled)
        {
            textFg = RgbColor.Gray;
            borderFg = RgbColor.Gray;
            bg = RgbColor.Black;
        }

        ctx.fill_rect(0, 0, width, height, bg);
        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, bg);

        var textY = height / 2;
        ctx.draw_centered_text(textY, displayText, textFg, bg);
    }

    private static (RgbColor fg, RgbColor bg, RgbColor focusFg, RgbColor focusBg) get_button_colors(
        ButtonStyleType style)
    {
        return style switch
        {
            ButtonStyleType.primary => (White: RgbColor.White, new RgbColor(0, 100, 200), RgbColor.White,
                new RgbColor(0, 140, 255)),
            ButtonStyleType.secondary => (White: RgbColor.White, new RgbColor(80, 80, 80), RgbColor.White,
                new RgbColor(120, 120, 120)),
            ButtonStyleType.success => (White: RgbColor.White, new RgbColor(0, 150, 50), RgbColor.White,
                new RgbColor(0, 200, 70)),
            ButtonStyleType.danger => (White: RgbColor.White, new RgbColor(200, 40, 40), RgbColor.White,
                new RgbColor(255, 60, 60)),
            ButtonStyleType.warning => (Black: RgbColor.Black, new RgbColor(220, 180, 0), RgbColor.Black,
                new RgbColor(255, 210, 0)),
            ButtonStyleType.info => (White: RgbColor.White, new RgbColor(0, 150, 200), RgbColor.White,
                new RgbColor(0, 190, 255)),
            ButtonStyleType.link => (new RgbColor(0, 150, 255), Black: RgbColor.Black, new RgbColor(100, 200, 255),
                RgbColor.Black),
            _ => (White: RgbColor.White, new RgbColor(0, 100, 200), RgbColor.White, new RgbColor(0, 140, 255))
        };
    }

    internal void click()
    {
        if (enabled) _on_click?.Invoke();
    }
}