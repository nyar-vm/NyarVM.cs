using Nyar.Language.Css;

namespace Nyar.Language.Tailwind;

/// <summary>
///     Tailwind 样式表：从工具类配置生成的 CSS。
///     包含源配置和编译后的 CSS 样式表。
/// </summary>
public sealed class TailwindStylesheet
{
    /// <summary>
    ///     Tailwind 配置
    /// </summary>
    public TailwindConfig Config { get; init; } = new();

    /// <summary>
    ///     从配置生成的工具类集合
    /// </summary>
    public List<TailwindUtility> Utilities { get; init; } = [];

    /// <summary>
    ///     编译后的目标 CSS 样式表
    /// </summary>
    public CssStylesheet CompiledCss { get; init; } = new();

    /// <summary>
    ///     样式表是否为空
    /// </summary>
    public bool IsEmpty => Utilities.Count == 0 && CompiledCss.IsEmpty;
}