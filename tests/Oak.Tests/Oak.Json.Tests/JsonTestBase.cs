using Oak.Diagnostics;
using Oak.Json;
using Oak.Testing;

namespace Oak.Json.Tests;

public abstract class JsonTestBase : TestBase
{
    protected IReadOnlyList<JsonToken> TokenizeWithTimeout(JsonLexer lexer, string source)
    {
        return ExecuteWithTimeout(() => lexer.Tokenize(source), "词法分析器");
    }

    protected (JsonValue? Value, DiagnosticSink Diagnostics) ParseWithTimeout(string source)
    {
        var diagnostics = new DiagnosticSink();
        var value = ExecuteWithTimeout(() =>
        {
            var parser = new JsonParser();
            var result = parser.Parse(source, diagnostics);
            return result.Value;
        }, "语法分析器");
        return (value, diagnostics);
    }
}
