namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     加粗
/// </summary>
public sealed record Strong : NotedownInline
{
    /// <summary>
    ///     创建加粗
    /// </summary>
    public Strong(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.strong;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}