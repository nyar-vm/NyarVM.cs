namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     引用目标，描述一个引用指向什么
/// </summary>
public readonly record struct ReferenceTarget
{
    /// <summary>
    ///     创建引用目标
    /// </summary>
    public ReferenceTarget(ReferenceKind kind, string targetId, string? title = null)
    {
        this.kind = kind;
        target_id = targetId;
        this.title = title;
    }

    /// <summary>
    ///     引用类型
    /// </summary>
    public ReferenceKind kind { get; init; }

    /// <summary>
    ///     目标标识符（URL、锚点 ID、引用键 等）
    /// </summary>
    public string target_id { get; init; }

    /// <summary>
    ///     目标标题（如有）
    /// </summary>
    public string? title { get; init; }

    /// <summary>
    ///     创建外部 URL 引用
    /// </summary>
    public static ReferenceTarget url(string url, string? title = null)
    {
        return new ReferenceTarget(ReferenceKind.external_url, url, title);
    }

    /// <summary>
    ///     创建内部锚点引用
    /// </summary>
    public static ReferenceTarget anchor(string anchorId)
    {
        return new ReferenceTarget(ReferenceKind.internal_anchor, anchorId);
    }

    /// <summary>
    ///     创建文献引用
    /// </summary>
    public static ReferenceTarget bib_key(string key)
    {
        return new ReferenceTarget(ReferenceKind.citation, key);
    }

    /// <summary>
    ///     创建脚注引用
    /// </summary>
    public static ReferenceTarget footnote(string id)
    {
        return new ReferenceTarget(ReferenceKind.footnote, id);
    }

    /// <summary>
    ///     创建图片引用
    /// </summary>
    public static ReferenceTarget img(string url, string? title = null)
    {
        return new ReferenceTarget(ReferenceKind.image, url, title);
    }
}