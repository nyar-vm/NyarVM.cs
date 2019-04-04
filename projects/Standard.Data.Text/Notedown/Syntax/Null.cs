namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     空块
/// </summary>
public sealed record Null : NotedownBlock
{
    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.@null;
}