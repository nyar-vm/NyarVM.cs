namespace Core.Terminal;

/// <summary>
///     终端文本样式，组合前景色、背景色与文本装饰
/// </summary>
public sealed class Style
{
    /// <summary>
    ///     前景色
    /// </summary>
    public Color foreground { get; set; } = Color.@default;

    /// <summary>
    ///     背景色
    /// </summary>
    public Color background { get; set; } = Color.@default;

    /// <summary>
    ///     文本装饰
    /// </summary>
    public TextDecoration decoration { get; set; } = TextDecoration.none;
}