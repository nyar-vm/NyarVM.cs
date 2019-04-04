using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.Lexer;

public sealed record TsToken(TsTokenType type, string value, int line, int column)
{
    public TextSpan to_source_span()
    {
        return default;
    }
}

