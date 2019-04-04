namespace Std.Data.Text.Erlang.Lexer;

public sealed record ErToken(ErTokenType Type, string Value, int Line, int Column)
{
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
