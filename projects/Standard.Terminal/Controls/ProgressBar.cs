namespace Std.Terminal.Controls;

/// <summary>
///     进度条控件，显示完成百分比
/// </summary>
public sealed class ProgressBar : View
{
    private double _value;

    /// <summary>
    ///     创建进度条
    /// </summary>
    public ProgressBar()
    {
        width = 40;
        height = 1;
    }

    /// <summary>
    ///     进度值（0-100）
    /// </summary>
    public double value
    {
        get => _value;
        set => _value = System.Math.Clamp(value, 0, 100);
    }

    /// <summary>
    ///     进度条样式
    /// </summary>
    public ProgressBarStyle style { get; set; } = ProgressBarStyle.block;

    /// <summary>
    ///     是否显示百分比文本
    /// </summary>
    public bool show_percentage { get; set; } = true;

    /// <summary>
    ///     进度条颜色
    /// </summary>
    public RgbColor? progress_color { get; set; }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var theme = ThemeManager.current;

        ctx.fill_rect(0, 0, width, height, theme.surface);

        var barWidth = show_percentage ? width - 6 : width;
        if (barWidth <= 0) return;

        var progressColor = progress_color ?? theme.success;
        var ratio = _value / 100.0;
        var filledChars = (int)System.Math.Round(ratio * barWidth);

        var filledStr = get_filled_string(filledChars, ratio);
        var emptyStr = get_empty_string(barWidth - filledChars);

        ctx.draw_text(0, 0, filledStr, progressColor, theme.surface);
        ctx.draw_text(filledChars, 0, emptyStr, theme.text_secondary, theme.surface);

        if (show_percentage)
        {
            var percentText = $"{_value,3:F0}%";
            ctx.draw_text(barWidth + 1, 0, percentText, theme.text, theme.surface);
        }
    }

    private string get_filled_string(int count, double ratio)
    {
        if (count <= 0) return string.Empty;

        return style switch
        {
            ProgressBarStyle.block => new string('█', count),
            ProgressBarStyle.smooth => ratio >= 1.0
                ? new string('━', count)
                : new string('━', System.Math.Max(0, count - 1)) + "╸",
            ProgressBarStyle.dash => new string('=', count),
            _ => new string('█', count)
        };
    }

    private static string get_empty_string(int count)
    {
        return count > 0 ? new string(' ', count) : string.Empty;
    }
}