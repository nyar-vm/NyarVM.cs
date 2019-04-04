namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 根文档节点，对齐 pandoc Pandoc
///     包含块级内容、元数据以及双向引用+锚点映射
///     引用映射在构造时自动构建，无需手动调用 WithReferenceMap
/// </summary>
public sealed record NotedownDocument
{
    /// <summary>
    ///     创建文档
    /// </summary>
    public NotedownDocument(Meta meta, IReadOnlyList<NotedownBlock> blocks)
    {
        this.meta = meta;
        this.blocks = blocks;
        reference_map = ReferenceMap.build(this);
    }

    /// <summary>
    ///     创建文档（无元数据）
    /// </summary>
    public NotedownDocument(IReadOnlyList<NotedownBlock> blocks)
        : this(Meta.empty, blocks)
    {
    }

    /// <summary>
    ///     文档元数据
    /// </summary>
    public Meta meta { get; init; }

    /// <summary>
    ///     块级内容
    /// </summary>
    public IReadOnlyList<NotedownBlock> blocks { get; init; }

    /// <summary>
    ///     双向引用+锚点映射（正向引用、反向引用、锚点注册表）
    ///     构造时自动构建。若通过 `with { Blocks = ... }` 修改了块内容，
    ///     请调用 WithReferenceMap() 重建
    /// </summary>
    public ReferenceMap reference_map { get; init; }

    /// <summary>
    ///     空文档
    /// </summary>
    public static NotedownDocument empty { get; } = new([]);

    /// <summary>
    ///     重建引用映射（在通过 with 修改 Blocks 后使用）
    /// </summary>
    public NotedownDocument with_reference_map()
    {
        return this with { reference_map = ReferenceMap.build(this) };
    }
}