namespace Std.Terminal;

/// <summary>
///     内置终端主题集合，提供默认、暗色和亮色三种主题
/// </summary>
public static class Themes
{
    /// <summary>
    ///     获取默认主题
    /// </summary>
    public static Theme default_theme { get; } = create_default_theme();

    /// <summary>
    ///     获取暗色主题
    /// </summary>
    public static Theme dark_theme { get; } = Theme.dark;

    /// <summary>
    ///     获取亮色主题
    /// </summary>
    public static Theme light_theme { get; } = Theme.light;

    private static Theme create_default_theme()
    {
        var theme = new Theme("Default");
        theme["heading"] = new ConsoleStyle { foreground = ConsoleColor.Cyan, bold = true };
        theme["body"] = ConsoleStyle.@default;
        theme["error"] = new ConsoleStyle { foreground = ConsoleColor.Red };
        theme["warning"] = new ConsoleStyle { foreground = ConsoleColor.Yellow };
        theme["success"] = new ConsoleStyle { foreground = ConsoleColor.Green };
        theme["highlight"] = new ConsoleStyle { foreground = ConsoleColor.White, bold = true };
        theme["dim"] = new ConsoleStyle { foreground = ConsoleColor.DarkGray };
        theme["link"] = new ConsoleStyle { foreground = ConsoleColor.Blue, underline = true };
        return theme;
    }
}