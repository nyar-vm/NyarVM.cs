namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     引用（单/双引号）
/// </summary>
public sealed record Quoted : NotedownInline
{
    /// <summary>
    ///     创建引用
    /// </summary>
    public Quoted(QuoteType quoteType, IReadOnlyList<NotedownInline> inlines)
    {
        quote_type = quoteType;
        this.inlines = inlines;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.quoted;

    /// <summary>
    ///     引用类型
    /// </summary>
    public QuoteType quote_type { get; init; }

    /// <summary>
    ///     行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> inlines { get; init; }
}