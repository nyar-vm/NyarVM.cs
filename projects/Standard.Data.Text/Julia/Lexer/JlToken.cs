using Std.Data.Text.Syntax;

namespace Std.Data.Text.Julia.Lexer;

public sealed record JlToken(JlTokenType type, string value, int line, int column)
{
    public TextSpan to_source_span()
    {
        return default;
    }
}
