namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     表格标题
/// </summary>
public sealed record Caption
{
    /// <summary>
    ///     创建表格标题
    /// </summary>
    public Caption(IReadOnlyList<NotedownInline> inlines, CaptionPosition position = CaptionPosition.bottom)
    {
        this.inlines = inlines;
        this.position = position;
    }

    /// <summary>
    ///     位置（上/下）
    /// </summary>
    public CaptionPosition position { get; init; }

    /// <summary>
    ///     标题内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}