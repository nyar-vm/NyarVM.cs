namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     斜体
/// </summary>
public sealed record Emph : NotedownInline
{
    /// <summary>
    ///     创建斜体
    /// </summary>
    public Emph(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.emph;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}