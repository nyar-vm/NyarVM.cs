namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     通用行内容器
/// </summary>
public sealed record Span : NotedownInline
{
    /// <summary>
    ///     创建通用行内容器
    /// </summary>
    public Span(Attr attr, IReadOnlyList<NotedownInline> inlines)
    {
        this.attr = attr;
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.span;

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}