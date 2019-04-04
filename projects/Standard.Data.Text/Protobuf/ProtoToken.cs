namespace Std.Data.Text.Protobuf;

public readonly struct ProtoToken
{
    public ProtoTokenType type { get; }
    public string text { get; }
    public int line { get; }
    public int column { get; }

    public ProtoToken(ProtoTokenType type, string text, int line, int column)
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