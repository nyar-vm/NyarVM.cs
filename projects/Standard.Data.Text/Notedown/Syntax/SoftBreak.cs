namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     软换行
/// </summary>
public sealed record SoftBreak : NotedownInline
{
    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.soft_break;
}