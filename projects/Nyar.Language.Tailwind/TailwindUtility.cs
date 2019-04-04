namespace Nyar.Language.Tailwind;

/// <summary>
///     Tailwind 原子化工具类定义
///     将类名（如 "text-red-500"、"p-4"）映射到 CSS 声明
/// </summary>
public sealed class TailwindUtility
{
    /// <summary>
    ///     工具类名称（如 "text-red-500"）
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    ///     生成的 CSS 属性-值对列表
    /// </summary>
    public List<string> CssDeclarations { get; init; } = [];

    /// <summary>
    ///     适用的变体（如 ["hover", "focus"]）
    /// </summary>
    public List<string> ApplicableVariants { get; init; } = [];
}