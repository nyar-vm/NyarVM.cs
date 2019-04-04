using System.Text.Json;

namespace Std.Terminal;

/// <summary>
///     全局主题管理器
/// </summary>
public static class ThemeManager
{
    private static TuiTheme? _current;

    /// <summary>
    ///     当前活动主题
    /// </summary>
    public static TuiTheme current
    {
        get => _current ?? TuiTheme.dark;
        set => _current = value;
    }

    /// <summary>
    ///     从文件加载主题
    /// </summary>
    /// <param name="filePath">JSON 主题文件路径</param>
    public static TuiTheme load_from_file(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var theme = JsonSerializer.Deserialize<TuiTheme>(json)
                    ?? TuiTheme.dark;
        current = theme;
        return theme;
    }

    /// <summary>
    ///     保存当前主题到文件
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    public static void save_to_file(string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(current, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    ///     获取所有预设主题
    /// </summary>
    public static IReadOnlyList<TuiTheme> get_presets()
    {
        return new List<TuiTheme> { TuiTheme.dark, TuiTheme.light };
    }
}