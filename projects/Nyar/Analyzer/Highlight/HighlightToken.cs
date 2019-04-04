using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Highlight;

public readonly struct HighlightToken
{
    public HighlightKind kind { get; }
    public TextSpan span { get; }
    public string? modifier { get; }

    public HighlightToken(HighlightKind kind, TextSpan span, string? modifier = null)
    {
        this.kind = kind;
        this.span = span;
        this.modifier = modifier;
    }

    public override string ToString()
    {
        return modifier is not null
            ? $"{kind}:{modifier} @ {span}"
            : $"{kind} @ {span}";
    }
}