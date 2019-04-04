namespace Std.Data.Text.GraphQL;

public readonly struct GqlToken
{
    public GqlTokenType type { get; }
    public string text { get; }
    public int line { get; }
    public int column { get; }

    public GqlToken(GqlTokenType type, string text, int line, int column)
    {
        this.type = type;
        this.text = text;
        this.line = line;
        this.column = column;
    }

    public override string ToString()
    {
        return $"{type}('{text}') @ {line}:{column}";
    }
}