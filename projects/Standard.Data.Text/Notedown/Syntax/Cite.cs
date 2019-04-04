namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     文献引用
/// </summary>
public sealed record Cite : NotedownInline
{
    /// <summary>
    ///     创建文献引用
    /// </summary>
    public Cite(IReadOnlyList<Citation> citations, IReadOnlyList<NotedownInline> inlines)
    {
        this.citations = citations;
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.cite;

    /// <summary>
    ///     引用列表
    /// </summary>
    public IReadOnlyList<Citation> citations { get; init; }

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}