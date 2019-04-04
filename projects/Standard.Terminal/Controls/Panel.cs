namespace Std.Terminal.Controls;

/// <summary>
///     面板，带边框和标题的容器
/// </summary>
public sealed class Panel : View
{
    /// <summary>
    ///     创建面板
    /// </summary>
    /// <param name="content">面板内容</param>
    public Panel(View content)
    {
        this.content = content;
        width = 10;
        height = 5;
    }

    /// <summary>
    ///     面板标题
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    ///     面板内容
    /// </summary>
    public View content { get; set; }

    /// <summary>
    ///     边框样式
    /// </summary>
    public BorderStyle border_style { get; set; } = BorderStyle.single;

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.White;

        if (!enabled) borderFg = RgbColor.Gray;

        ctx.draw_border(0, 0, width, height, border_style, borderFg, ctx.default_background);

        if (!string.IsNullOrEmpty(title)) ctx.draw_text(2, 0, $" {title} ", borderFg, ctx.default_background);

        var innerWidth = width - 2;
        var innerHeight = height - 2;

        if (innerWidth > 0 && innerHeight > 0)
        {
            content.x = 1;
            content.y = 1;
            content.width = innerWidth;
            content.height = innerHeight;
            content.render(ctx.create_child(1, 1, innerWidth, innerHeight));
        }
    }
}