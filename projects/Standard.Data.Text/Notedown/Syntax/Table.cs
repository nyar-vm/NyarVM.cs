namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     表格块
/// </summary>
public sealed record Table : NotedownBlock
{
    /// <summary>
    ///     创建表格
    /// </summary>
    public Table(
        Attr attr,
        Caption? caption,
        IReadOnlyList<ColSpec> colSpecs,
        TableHead head,
        IReadOnlyList<TableBody> bodies,
        TableFoot foot)
    {
        this.attr = attr;
        this.caption = caption;
        col_specs = colSpecs;
        this.head = head;
        this.bodies = bodies;
        this.foot = foot;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.table;

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     标题
    /// </summary>
    public Caption? caption { get; init; }

    /// <summary>
    ///     列规格
    /// </summary>
    public IReadOnlyList<ColSpec> col_specs { get; init; }

    /// <summary>
    ///     表头
    /// </summary>
    public TableHead head { get; init; }

    /// <summary>
    ///     表体列表
    /// </summary>
    public IReadOnlyList<TableBody> bodies { get; init; }

    /// <summary>
    ///     表脚
    /// </summary>
    public TableFoot foot { get; init; }
}