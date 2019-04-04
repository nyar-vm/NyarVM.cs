using Std.Data.Text.Typescript.Lexer;

namespace Std.Data.Text.JavaScript.Lexer;

public sealed class JsLexer
{
    private readonly TsLexer _ts_lexer = new();

    public IReadOnlyList<TsToken> tokenize(string source)
    {
        return _ts_lexer.tokenize(source);
    }
}

