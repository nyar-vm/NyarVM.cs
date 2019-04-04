namespace Std.Data.Text.Xml;

/// <summary>
///     XML 语言配置
/// </summary>
public sealed class XmlLanguageConfig
{
    /// <summary>
    ///     是否美化输出（缩进格式化）
    /// </summary>
    public bool pretty_print { get; init; } = true;

    /// <summary>
    ///     根元素名称（格式化时使用）
    /// </summary>
    public string root_element_name { get; init; } = "document";

    /// <summary>
    ///     默认配置实例
    /// </summary>
    public static XmlLanguageConfig @default { get; } = new();
}