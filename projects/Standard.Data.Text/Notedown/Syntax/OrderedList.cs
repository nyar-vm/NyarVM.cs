namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     有序列表
/// </summary>
public sealed record OrderedList : NotedownBlock
{
    /// <summary>
    ///     创建有序列表
    /// </summary>
    public OrderedList(ListAttributes listAttributes, IReadOnlyList<IReadOnlyList<NotedownBlock>> items)
    {
        list_attributes = listAttributes;
        this.items = items;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.ordered_list;

    /// <summary>
    ///     列表编号属性
    /// </summary>
    public ListAttributes list_attributes { get; init; }

    /// <summary>
    ///     列表项，每项是一组块的列表
    /// </summary>
    public IReadOnlyList<IReadOnlyList<NotedownBlock>> items { get; init; }
}