namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     标题块
/// </summary>
public sealed record Header : NotedownBlock
{
    /// <summary>
    ///     创建标题块
    /// </summary>
    public Header(int level, Attr attr, IReadOnlyList<NotedownInline> inlines)
    {
        this.level = level;
        this.attr = attr;
        this.inlines = inlines;
    }

    /// <summary>
    ///     创建标题块（无属性）
    /// </summary>
    public Header(int level, IReadOnlyList<NotedownInline> inlines)
        : this(level, Attr.empty, inlines)
    {
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.header;

    /// <summary>
    ///     标题级别（1-6）
    /// </summary>
    public int level { get; init; }

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     标题行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}