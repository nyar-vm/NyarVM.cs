namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     原始格式块
/// </summary>
public sealed record RawBlock : NotedownBlock
{
    /// <summary>
    ///     创建原始格式块
    /// </summary>
    public RawBlock(string format, string text)
    {
        this.format = format;
        this.text = text;
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.raw_block;

    /// <summary>
    ///     格式名（如 "html", "latex"）
    /// </summary>
    public string format { get; init; }

    /// <summary>
    ///     原始内容
    /// </summary>
    public string text { get; init; }
}