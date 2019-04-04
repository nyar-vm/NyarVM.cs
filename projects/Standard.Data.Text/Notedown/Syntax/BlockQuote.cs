namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     引用块
/// </summary>
public sealed record BlockQuote : NotedownBlock
{
    /// <summary>
    ///     创建引用块
    /// </summary>
    public BlockQuote(IReadOnlyList<NotedownBlock> children)
    {
        this.children = children;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.block_quote;

    /// <summary>
    ///     子块列表
    /// </summary>
    public IReadOnlyList<NotedownBlock> children { get; init; }
}