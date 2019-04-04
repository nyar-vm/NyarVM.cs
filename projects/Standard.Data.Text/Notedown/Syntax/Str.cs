namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     纯文本
/// </summary>
public sealed record Str : NotedownInline
{
    /// <summary>
    ///     创建纯文本
    /// </summary>
    public Str(string text)
    {
        this.text = text;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.str;

    /// <summary>
    ///     文本内容
    /// </summary>
    public string text { get; init; }
}