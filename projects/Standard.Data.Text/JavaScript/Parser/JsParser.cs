using Std.Data.Text.Diagnostics;
using Std.Data.Text.Typescript.AST;
using Std.Data.Text.Typescript.Lexer;
using Std.Data.Text.Typescript.Parsing;

namespace Std.Data.Text.JavaScript.Parser;

public sealed class JsParser
{
    private readonly TsParser _ts_parser = new();

    public JsParser(DiagnosticSink? diagnostics = null)
    {
        _ts_parser = new TsParser(diagnostics);
    }

    public TsAstNode parse(IReadOnlyList<TsToken> tokens)
    {
        return _ts_parser.parse(tokens);
    }
}

