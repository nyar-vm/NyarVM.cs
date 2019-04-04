namespace Nyar.Language.WebStyle;

/// <summary>
///     Web 样式资产：单个样式输入源
/// </summary>
public sealed class WebStyleAsset
{
    /// <summary>
    ///     资产名称（用于注释标识和来源追踪）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     样式内容文本
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    ///     样式来源类型
    /// </summary>
    public WebStyleAssetKind Kind { get; init; } = WebStyleAssetKind.Css;

    /// <summary>
    ///     资产内容是否为空或仅包含空白字符
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Content);

    /// <summary>
    ///     创建原始 CSS 资产
    /// </summary>
    /// <param name="name">来源名称</param>
    /// <param name="content">CSS 文本</param>
    public static WebStyleAsset css(string name, string content)
    {
        return new WebStyleAsset
        {
            Name = name,
            Content = content,
            Kind = WebStyleAssetKind.Css
        };
    }

    /// <summary>
    ///     创建 SCSS 资产
    /// </summary>
    /// <param name="name">来源名称</param>
    /// <param name="content">SCSS 源代码</param>
    public static WebStyleAsset scss(string name, string content)
    {
        return new WebStyleAsset
        {
            Name = name,
            Content = content,
            Kind = WebStyleAssetKind.Scss
        };
    }

    /// <summary>
    ///     创建 Tailwind 资产
    /// </summary>
    /// <param name="name">来源名称</param>
    /// <param name="content">Tailwind 源码或类名文本</param>
    public static WebStyleAsset tailwind(string name, string content)
    {
        return new WebStyleAsset
        {
            Name = name,
            Content = content,
            Kind = WebStyleAssetKind.Tailwind
        };
    }
}