namespace Nyar.Language.Scss;

/// <summary>
///     SCSS Mixin 定义
/// </summary>
public sealed class ScssMixin
{
    /// <summary>
    ///     Mixin 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     参数列表（参数名和默认值）
    /// </summary>
    public List<ScssVariable> Parameters { get; init; } = [];

    /// <summary>
    ///     Mixin 体内的 CSS 规则
    /// </summary>
    public Css.CssStylesheet Body { get; init; } = new();
}