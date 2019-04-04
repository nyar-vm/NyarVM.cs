namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     删除线
/// </summary>
public sealed record Strikeout : NotedownInline
{
    /// <summary>
    ///     创建删除线
    /// </summary>
    public Strikeout(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.strikeout;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}