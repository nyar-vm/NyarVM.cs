namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     代码块
/// </summary>
public sealed record CodeBlock : NotedownBlock
{
    /// <summary>
    ///     创建代码块
    /// </summary>
    public CodeBlock(Attr attr, string text)
    {
        this.attr = attr;
        this.text = text;
    }

    /// <summary>
    ///     创建代码块（带语言标识）
    /// </summary>
    public CodeBlock(string language, string text)
        : this(new Attr(string.Empty, [language]), text)
    {
    }

    /// <inheritdoc />
    public override NotedownBlockType block_type => NotedownBlockType.code_block;

    /// <summary>
    ///     属性（class 中通常包含语言标识）
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     代码文本
    /// </summary>
    public string text { get; init; }

    /// <summary>
    ///     语言标识（取自 Attr.Classes 第一个元素）
    /// </summary>
    public string language => attr.classes.Count > 0 ? attr.classes[0] : string.Empty;
}