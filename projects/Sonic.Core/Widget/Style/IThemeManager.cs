namespace Core.Widget.Style;

/// <summary>
///     IThemeManager 接口
/// </summary>
public interface IThemeManager
{
    /// <summary>
    ///     当前主题名称
    /// </summary>
    string current_theme { get; }

    /// <summary>
    ///     应用指定主题
    /// </summary>
    void apply_theme(string theme_name);
}