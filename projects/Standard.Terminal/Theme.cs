namespace Std.Terminal;

/// <summary>
///     UI 主题，定义所有控件的颜色、边框样式等视觉属性
/// </summary>
public sealed class TuiTheme
{
    /// <summary>
    ///     暗色主题预设
    /// </summary>
    public static readonly TuiTheme dark = new()
    {
        surface = new RgbColor(20, 20, 20),
        surface_secondary = new RgbColor(40, 40, 40),
        text = RgbColor.white,
        text_secondary = new RgbColor(150, 150, 150),
        text_disabled = RgbColor.gray,
        border = new RgbColor(80, 80, 80),
        focus = new RgbColor(0, 150, 255),
        selected = new RgbColor(0, 100, 200),
        primary = new RgbColor(0, 120, 220),
        secondary = new RgbColor(80, 80, 80),
        success = new RgbColor(0, 170, 70),
        danger = new RgbColor(220, 50, 50),
        warning = new RgbColor(230, 180, 0),
        info = new RgbColor(0, 160, 210)
    };

    /// <summary>
    ///     亮色主题预设
    /// </summary>
    public static readonly TuiTheme light = new()
    {
        surface = new RgbColor(245, 245, 245),
        surface_secondary = new RgbColor(220, 220, 220),
        text = new RgbColor(20, 20, 20),
        text_secondary = new RgbColor(120, 120, 120),
        text_disabled = new RgbColor(170, 170, 170),
        border = new RgbColor(180, 180, 180),
        focus = new RgbColor(0, 100, 200),
        selected = new RgbColor(200, 230, 255),
        primary = new RgbColor(0, 100, 200),
        secondary = new RgbColor(180, 180, 180),
        success = new RgbColor(0, 150, 60),
        danger = new RgbColor(200, 40, 40),
        warning = new RgbColor(200, 160, 0),
        info = new RgbColor(0, 140, 190)
    };

    /// <summary>
    ///     主背景色
    /// </summary>
    public RgbColor surface { get; init; }

    /// <summary>
    ///     次背景色（如选中项、卡片内层）
    /// </summary>
    public RgbColor surface_secondary { get; init; }

    /// <summary>
    ///     主文本色
    /// </summary>
    public RgbColor text { get; init; }

    /// <summary>
    ///     次文本色（如提示、占位符）
    /// </summary>
    public RgbColor text_secondary { get; init; }

    /// <summary>
    ///     禁用文本色
    /// </summary>
    public RgbColor text_disabled { get; init; }

    /// <summary>
    ///     主边框色
    /// </summary>
    public RgbColor border { get; init; }

    /// <summary>
    ///     焦点/高亮色
    /// </summary>
    public RgbColor focus { get; init; }

    /// <summary>
    ///     选中项背景色
    /// </summary>
    public RgbColor selected { get; init; }

    /// <summary>
    ///     主要操作色
    /// </summary>
    public RgbColor primary { get; init; }

    /// <summary>
    ///     次要操作色
    /// </summary>
    public RgbColor secondary { get; init; }

    /// <summary>
    ///     成功色
    /// </summary>
    public RgbColor success { get; init; }

    /// <summary>
    ///     危险色
    /// </summary>
    public RgbColor danger { get; init; }

    /// <summary>
    ///     警告色
    /// </summary>
    public RgbColor warning { get; init; }

    /// <summary>
    ///     信息色
    /// </summary>
    public RgbColor info { get; init; }
}