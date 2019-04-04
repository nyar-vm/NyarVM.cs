namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     分割线
/// </summary>
public sealed record HorizontalRule : NotedownBlock
{
    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.horizontal_rule;
}