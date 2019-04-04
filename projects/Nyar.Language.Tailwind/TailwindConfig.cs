namespace Nyar.Language.Tailwind;

/// <summary>
///     Tailwind CSS 配置：定义主题（颜色、间距、断点等）、变体和插件
/// </summary>
public sealed class TailwindConfig
{
    /// <summary>
    ///     主题配置：颜色、字号、间距等设计 Token
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Theme { get; init; } = new();

    /// <summary>
    ///     启用的变体（如 "hover"、"focus"、"dark"）
    /// </summary>
    public List<string> Variants { get; init; } = [];

    /// <summary>
    ///     启用的插件名称列表
    /// </summary>
    public List<string> Plugins { get; init; } = [];

    /// <summary>
    ///     是否启用 JIT 模式
    /// </summary>
    public bool JitMode { get; init; } = true;
}