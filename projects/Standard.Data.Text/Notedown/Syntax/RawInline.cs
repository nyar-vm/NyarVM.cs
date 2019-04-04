namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     原始行内格式
/// </summary>
public sealed record RawInline : NotedownInline
{
    /// <summary>
    ///     创建原始行内格式
    /// </summary>
    public RawInline(string format, string text)
    {
        this.format = format;
        this.text = text;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.raw_inline;

    /// <summary>
    ///     格式名（如 "html", "latex"）
    /// </summary>
    public string format { get; init; }

    /// <summary>
    ///     原始内容
    /// </summary>
    public string text { get; init; }
}