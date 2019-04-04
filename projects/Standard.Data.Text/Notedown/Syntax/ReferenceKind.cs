namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     引用目标类型
/// </summary>
public enum ReferenceKind
{
    /// <summary>外部 URL 引用</summary>
    external_url,

    /// <summary>内部锚点引用（如 #header-id）</summary>
    internal_anchor,

    /// <summary>文献引用（如 @bibkey）</summary>
    citation,

    /// <summary>脚注引用</summary>
    footnote,

    /// <summary>图片引用</summary>
    image
}