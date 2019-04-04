namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     定义列表
/// </summary>
public sealed record DefinitionList : NotedownBlock
{
    /// <summary>
    ///     创建定义列表
    /// </summary>
    public DefinitionList(IReadOnlyList<DefinitionItem> items)
    {
        this.items = items;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.definition_list;

    /// <summary>
    ///     定义列表项
    /// </summary>
    public IReadOnlyList<DefinitionItem> items { get; init; }
}