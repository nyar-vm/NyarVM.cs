namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     无序列表（含任务列表）
/// </summary>
public sealed record BulletList : NotedownBlock
{
    /// <summary>
    ///     创建无序列表
    /// </summary>
    public BulletList(IReadOnlyList<IReadOnlyList<NotedownBlock>> items)
    {
        this.items = items;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.bullet_list;

    /// <summary>
    ///     列表项，每项是一组块的列表
    /// </summary>
    public IReadOnlyList<IReadOnlyList<NotedownBlock>> items { get; init; }
}