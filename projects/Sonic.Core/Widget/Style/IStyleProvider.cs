namespace Core.Widget.Style;

/// <summary>
///     IStyleProvider 接口
/// </summary>
public interface IStyleProvider
{
    /// <summary>
    ///     获取指定键的样式
    /// </summary>
    IStyle? get_style(string key);
}