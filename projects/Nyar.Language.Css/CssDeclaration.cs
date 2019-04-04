namespace Nyar.Language.Css;

/// <summary>
///     CSS 声明：单个属性-值对
/// </summary>
public sealed class CssDeclaration
{
    /// <summary>
    ///     属性名（如 "color"、"font-size"）
    /// </summary>
    public string Property { get; init; } = string.Empty;

    /// <summary>
    ///     属性值（如 "red"、"16px"）
    /// </summary>
    public string Value { get; init; } = string.Empty;
}