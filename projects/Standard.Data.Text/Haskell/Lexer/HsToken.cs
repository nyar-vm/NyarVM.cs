namespace Std.Data.Text.Haskell.Lexer;

public sealed record HsToken(HsTokenType Type, string Value, int Line, int Column)
{
    public TextSpan ToSourceSpan()
    {
        return default;
    }
}
