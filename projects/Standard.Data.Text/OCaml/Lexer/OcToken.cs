namespace Std.Data.Text.OCaml.Lexer;

public sealed record OcToken(OcTokenType Type, string Value, int Line, int Column)
{
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
