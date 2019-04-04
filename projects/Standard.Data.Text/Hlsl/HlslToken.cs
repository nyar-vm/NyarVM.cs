namespace Std.Data.Text.Hlsl;

public readonly struct HlslToken
{
    public HlslTokenType Type { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }

    public HlslToken(HlslTokenType type, string text, int line, int column)
    {
        Type = type;
        Text = text;
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"{Type}('{Text}') @ {Line}:{Column}";
    }
}
