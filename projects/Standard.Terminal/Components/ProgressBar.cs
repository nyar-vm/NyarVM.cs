using System.Text;
using Std.Math;

namespace Std.Terminal.Components;

/// <summary>
///     进度条组件，支持自定义宽度和样式字符。
/// </summary>
public sealed class ProgressBar : IRenderable
{
    /// <summary>
    ///     获取或设置当前进度值（0.0 到 1.0）。
    /// </summary>
    public double value { get; set; }

    /// <summary>
    ///     获取或设置填充字符。
    /// </summary>
    public char fill_char { get; set; } = '#';

    /// <summary>
    ///     获取或设置空白字符。
    /// </summary>
    public char empty_char { get; set; } = '-';

    /// <summary>
    ///     获取或设置是否显示百分比文本。
    /// </summary>
    public bool show_percentage { get; set; } = true;

    /// <summary>
    ///     获取或设置进度条左括号字符。
    /// </summary>
    public char left_bracket { get; set; } = '[';

    /// <summary>
    ///     获取或设置进度条右括号字符。
    /// </summary>
    public char right_bracket { get; set; } = ']';

    /// <summary>
    ///     获取布局的期望宽度。
    /// </summary>
    public int desired_width { get; set; } = 40;

    /// <summary>
    ///     获取布局的期望高度。
    /// </summary>
    public int desired_height => 1;

    /// <summary>
    ///     在指定区域内渲染此进度条。
    /// </summary>
    /// <param name="renderer">终端渲染器。</param>
    /// <param name="region">渲染区域。</param>
    public void render(AnsiTerminalRenderer renderer, Rectangle region)
    {
        var width = SonicMath.min(desired_width, region.width);
        var barWidth = show_percentage ? width - 7 : width - 2;
        if (barWidth < 1) barWidth = 1;

        var filled = (int)(barWidth * SonicMath.clamp(value, 0.0, 1.0));
        var empty = barWidth - filled;

        var sb = new StringBuilder();
        sb.Append(left_bracket);
        sb.Append(new string(fill_char, filled));
        sb.Append(new string(empty_char, empty));
        sb.Append(right_bracket);

        if (show_percentage) sb.Append($" {value:P0}");

        renderer.console.set_cursor_position(region.x, region.y);
        renderer.write(AnsiString.plain(sb.ToString()).ansi_sequence);
    }
}