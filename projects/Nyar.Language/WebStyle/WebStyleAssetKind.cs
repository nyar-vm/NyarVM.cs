namespace Nyar.Language.WebStyle;

/// <summary>
///     Web 样式资产的来源类型
/// </summary>
public enum WebStyleAssetKind
{
    /// <summary>
    ///     原始 CSS 文本，无需编译
    /// </summary>
    Css,

    /// <summary>
    ///     SCSS 源代码，需编译为 CSS
    /// </summary>
    Scss,

    /// <summary>
    ///     Tailwind 源码或类名集合，需扫描生成 CSS
    /// </summary>
    Tailwind,
}