namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     行块（诗歌、地址等），每行是一组行内内容
/// </summary>
public sealed record LineBlock : NotedownBlock
{
    /// <summary>
    ///     创建行块
    /// </summary>
    public LineBlock(IReadOnlyList<IReadOnlyList<NotedownInline>> lines)
    {
        this.lines = lines;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.line_block;

    /// <summary>
    ///     每行的行内内容
    /// </summary>
    public IReadOnlyList<IReadOnlyList<NotedownInline>> lines { get; init; }
}