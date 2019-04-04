namespace Std.Terminal;

/// <summary>
///     终端主题，定义命名样式映射，用于控制台输出的样式查找
/// </summary>
public sealed class Theme
{
    private readonly Dictionary<string, ConsoleStyle> _styles = new();

    /// <summary>
    ///     创建指定名称的主题
    /// </summary>
    /// <param name="name">主题名称</param>
    public Theme(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     主题名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     获取或设置指定键的样式
    /// </summary>
    /// <param name="key">样式键名</param>
    /// <returns>对应的样式</returns>
    public ConsoleStyle this[string key]
    {
        get => _styles.GetValueOrDefault(key, ConsoleStyle.@default);
        set => _styles[key] = value;
    }

    /// <summary>
    ///     暗色主题预设
    /// </summary>
    public static Theme dark { get; } = create_dark_theme();

    /// <summary>
    ///     亮色主题预设
    /// </summary>
    public static Theme light { get; } = create_light_theme();

    /// <summary>
    ///     获取指定键的样式
    /// </summary>
    /// <param name="key">样式键名</param>
    /// <returns>对应的样式，若不存在则返回默认样式</returns>
    public ConsoleStyle get_style(string key)
    {
        return _styles.GetValueOrDefault(key, ConsoleStyle.@default);
    }

    private static Theme create_dark_theme()
    {
        var theme = new Theme("Dark");
        theme["heading"] = new ConsoleStyle { foreground = ConsoleColor.Cyan, bold = true };
        theme["body"] = new ConsoleStyle { foreground = ConsoleColor.Gray };
        theme["error"] = new ConsoleStyle { foreground = ConsoleColor.Red, bold = true };
        theme["warning"] = new ConsoleStyle { foreground = ConsoleColor.Yellow };
        theme["success"] = new ConsoleStyle { foreground = ConsoleColor.Green };
        theme["highlight"] = new ConsoleStyle { foreground = ConsoleColor.White, bold = true };
        theme["dim"] = new ConsoleStyle { foreground = ConsoleColor.DarkGray };
        theme["link"] = new ConsoleStyle { foreground = ConsoleColor.Blue, underline = true };
        return theme;
    }

    private static Theme create_light_theme()
    {
        var theme = new Theme("Light");
        theme["heading"] = new ConsoleStyle { foreground = ConsoleColor.DarkCyan, bold = true };
        theme["body"] = ConsoleStyle.@default;
        theme["error"] = new ConsoleStyle { foreground = ConsoleColor.DarkRed, bold = true };
        theme["warning"] = new ConsoleStyle { foreground = ConsoleColor.DarkYellow };
        theme["success"] = new ConsoleStyle { foreground = ConsoleColor.DarkGreen };
        theme["highlight"] = new ConsoleStyle { foreground = ConsoleColor.Black, bold = true };
        theme["dim"] = new ConsoleStyle { foreground = ConsoleColor.Gray };
        theme["link"] = new ConsoleStyle { foreground = ConsoleColor.DarkBlue, underline = true };
        return theme;
    }
}