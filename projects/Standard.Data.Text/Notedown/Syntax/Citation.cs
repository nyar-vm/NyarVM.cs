namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     引用文献条目
/// </summary>
public readonly record struct Citation
{
    /// <summary>
    ///     创建引用文献条目
    /// </summary>
    public Citation(
        string id,
        IReadOnlyList<NotedownInline>? prefix = null,
        IReadOnlyList<NotedownInline>? suffix = null,
        CitationMode mode = CitationMode.normal,
        int noteNum = 0,
        int hash = 0)
    {
        this.id = id;
        this.prefix = prefix ?? [];
        this.suffix = suffix ?? [];
        this.mode = mode;
        note_num = noteNum;
        this.hash = hash;
    }

    /// <summary>
    ///     引用 ID
    /// </summary>
    public string id { get; init; }

    /// <summary>
    ///     前缀
    /// </summary>
    public IReadOnlyList<NotedownInline> prefix { get; init; }

    /// <summary>
    ///     后缀
    /// </summary>
    public IReadOnlyList<NotedownInline> suffix { get; init; }

    /// <summary>
    ///     引用模式
    /// </summary>
    public CitationMode mode { get; init; }

    /// <summary>
    ///     附注页码
    /// </summary>
    public int note_num { get; init; }

    /// <summary>
    ///     哈希
    /// </summary>
    public int hash { get; init; }
}