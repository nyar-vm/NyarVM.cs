namespace Std.Terminal.Controls;

/// <summary>
///     分隔线
/// </summary>
public sealed class Separator : View
{
    /// <summary>
    ///     创建分隔线（默认为水平方向）
    /// </summary>
    public Separator() : this(Orientation.horizontal)
    {
        width = 10;
        height = 1;
    }

    /// <summary>
    ///     创建指定方向的分隔线
    /// </summary>
    /// <param name="orientation">方向</param>
    public Separator(Orientation orientation)
    {
        this.orientation = orientation;
        if (orientation == Orientation.horizontal)
        {
            width = 10;
            height = 1;
        }
        else
        {
            width = 1;
            height = 10;
        }
    }

    /// <summary>
    ///     分隔线方向
    /// </summary>
    public Orientation orientation { get; }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var lineChar = orientation == Orientation.horizontal ? '─' : '│';
        var len = orientation == Orientation.horizontal ? width : height;

        for (var i = 0; i < len; i++)
            if (orientation == Orientation.horizontal)
                ctx.draw_text(i, 0, lineChar.ToString(), RgbColor.Gray, ctx.default_background);
            else
                ctx.draw_text(0, i, lineChar.ToString(), RgbColor.Gray, ctx.default_background);
    }
}