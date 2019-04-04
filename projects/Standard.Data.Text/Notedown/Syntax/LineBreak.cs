namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     硬换行
/// </summary>
public sealed record LineBreak : NotedownInline
{
    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.line_break;
}