namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     链接
/// </summary>
public sealed record Link : NotedownInline
{
    /// <summary>
    ///     创建链接
    /// </summary>
    public Link(Attr attr, IReadOnlyList<NotedownInline> inlines, Target target)
    {
        this.attr = attr;
        this.inlines = inlines;
        this.target = target;
    }

    /// <summary>
    ///     创建链接（无属性）
    /// </summary>
    public Link(IReadOnlyList<NotedownInline> inlines, Target target)
        : this(Attr.empty, inlines, target)
    {
    }

    /// <summary>
    ///     创建链接（仅 URL）
    /// </summary>
    public Link(IReadOnlyList<NotedownInline> inlines, string url, string? title = null)
        : this(Attr.empty, inlines, new Target(url, title))
    {
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.link;

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     链接内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }

    /// <summary>
    ///     链接目标
    /// </summary>
    public Target target { get; init; }
}