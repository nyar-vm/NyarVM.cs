namespace Std.Data.Text.Tailwind;

/// <summary>
///     Tailwind 配置
/// </summary>
public sealed class TailwindConfig
{
    public static TailwindConfig @default { get; } = new();


    /// <summary>
    ///     内容扫描路径模式
    /// </summary>
    public List<string> content { get; init; } = ["**/*.v", "**/*.html", "**/*.js"];


    /// <summary>
    ///     是否生成 Preflight（CSS Reset）
    /// </summary>
    public bool preflight { get; init; } = true;
}