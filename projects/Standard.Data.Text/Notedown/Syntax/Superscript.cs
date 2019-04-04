namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     上标
/// </summary>
public sealed record Superscript : NotedownInline
{
    /// <summary>
    ///     创建上标
    /// </summary>
    public Superscript(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.superscript;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}