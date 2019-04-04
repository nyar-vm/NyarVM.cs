namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     纯文本块（非顶级）
/// </summary>
public sealed record Plain : NotedownBlock
{
    /// <summary>
    ///     创建纯文本块
    /// </summary>
    public Plain(IReadOnlyList<NotedownInline> inlines)
    {
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.plain;

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}