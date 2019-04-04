namespace Nyar.Language.WebStyle;

/// <summary>
///     Web 样式 Bundle：统一的样式合并输出模型
/// </summary>
public sealed class WebStyleBundle
{
    /// <summary>
    ///     Bundle 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     合并后的 CSS 文本
    /// </summary>
    public string Css { get; init; } = string.Empty;

    /// <summary>
    ///     参与合并的资产名称列表
    /// </summary>
    public IReadOnlyList<string> Sources { get; init; } = [];

    /// <summary>
    ///     CSS 内容是否为空或仅包含空白字符
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Css);
}