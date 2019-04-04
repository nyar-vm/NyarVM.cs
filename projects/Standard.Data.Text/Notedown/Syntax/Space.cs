namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     空格
/// </summary>
public sealed record Space : NotedownInline
{
    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.space;
}