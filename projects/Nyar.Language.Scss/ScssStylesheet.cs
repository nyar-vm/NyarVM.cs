using Nyar.Language.Css;

namespace Nyar.Language.Scss;

/// <summary>
///     SCSS 样式表：聚合 SCSS 变量、Mixin、嵌套规则和普通 CSS 规则。
///     编译后输出为标准 CSS。
/// </summary>
public sealed class ScssStylesheet
{
    /// <summary>
    ///     SCSS 变量集合
    /// </summary>
    public List<ScssVariable> Variables { get; init; } = [];

    /// <summary>
    ///     SCSS Mixin 集合
    /// </summary>
    public List<ScssMixin> Mixins { get; init; } = [];

    /// <summary>
    ///     编译后的目标 CSS 样式表
    /// </summary>
    public CssStylesheet CompiledCss { get; init; } = new();

    /// <summary>
    ///     样式表是否为空
    /// </summary>
    public bool IsEmpty => Variables.Count == 0 && Mixins.Count == 0 && CompiledCss.IsEmpty;
}