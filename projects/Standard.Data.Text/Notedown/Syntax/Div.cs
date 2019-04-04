namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     通用块容器
/// </summary>
public sealed record Div : NotedownBlock
{
    /// <summary>
    ///     创建通用块容器
    /// </summary>
    public Div(Attr attr, IReadOnlyList<NotedownBlock> children)
    {
        this.attr = attr;
        this.children = children;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.div;

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     子块列表
    /// </summary>
    public IReadOnlyList<NotedownBlock> children { get; init; }
}