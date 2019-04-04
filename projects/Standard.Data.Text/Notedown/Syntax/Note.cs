namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     脚注
/// </summary>
public sealed record Note : NotedownInline
{
    /// <summary>
    ///     创建脚注
    /// </summary>
    public Note(IReadOnlyList<NotedownBlock> blocks)
    {
        this.blocks = blocks;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.note;

    /// <summary>
    ///     脚注内容（块级）
    /// </summary>
    public IReadOnlyList<NotedownBlock> blocks { get; init; }
}