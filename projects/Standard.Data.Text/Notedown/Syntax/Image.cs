namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     图片
/// </summary>
public sealed record Image : NotedownInline
{
    /// <summary>
    ///     创建图片
    /// </summary>
    public Image(Attr attr, IReadOnlyList<NotedownInline> inlines, Target target)
    {
        this.attr = attr;
        this.inlines = inlines;
        this.target = target;
    }

    /// <summary>
    ///     创建图片（无属性）
    /// </summary>
    public Image(IReadOnlyList<NotedownInline> inlines, Target target)
        : this(Attr.empty, inlines, target)
    {
    }

    /// <summary>
    ///     创建图片（仅 URL + 替代文本）
    /// </summary>
    public Image(string altText, string url, string? title = null)
        : this(Attr.empty, [new Str(altText)], new Target(url, title))
    {
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.image;

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     替代文本
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }

    /// <summary>
    ///     图片目标
    /// </summary>
    public Target target { get; init; }
}