namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     段落块（顶级）
/// </summary>
public sealed record Para : NotedownBlock
{
    /// <summary>
    ///     创建段落块
    /// </summary>
    public Para(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.para;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}