namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     行内代码
/// </summary>
public sealed record Code : NotedownInline
{
    /// <summary>
    ///     创建行内代码
    /// </summary>
    public Code(Attr attr, string text)
    {
        this.attr = attr;
        this.text = text;
    }

    /// <summary>
    ///     创建行内代码（无属性）
    /// </summary>
    public Code(string text)
        : this(Attr.empty, text)
    {
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.code;

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     代码文本
    /// </summary>
    public string text { get; init; }
}