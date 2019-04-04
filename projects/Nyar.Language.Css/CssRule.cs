namespace Nyar.Language.Css;

/// <summary>
///     CSS 规则：选择器 + 一组声明
/// </summary>
public sealed class CssRule
{
    /// <summary>
    ///     选择器字符串（如 ".my-class"、"#my-id"、"div > span"）
    /// </summary>
    public string Selector { get; init; } = string.Empty;

    /// <summary>
    ///     声明列表
    /// </summary>
    public List<CssDeclaration> Declarations { get; init; } = [];
}