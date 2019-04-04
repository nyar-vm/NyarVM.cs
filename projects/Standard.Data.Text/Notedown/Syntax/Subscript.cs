namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     下标
/// </summary>
public sealed record Subscript : NotedownInline
{
    /// <summary>
    ///     创建下标
    /// </summary>
    public Subscript(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.subscript;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}