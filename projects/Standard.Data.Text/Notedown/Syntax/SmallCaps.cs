namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     小大写
/// </summary>
public sealed record SmallCaps : NotedownInline
{
    /// <summary>
    ///     创建小大写
    /// </summary>
    public SmallCaps(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.small_caps;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}