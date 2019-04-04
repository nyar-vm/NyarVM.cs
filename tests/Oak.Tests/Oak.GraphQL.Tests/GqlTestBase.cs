using Oak.Testing;

namespace Oak.GraphQL.Tests;

public abstract class GqlTestBase : TestBase
{
    protected virtual int ParseTimeoutMs => 5000;

    protected ParseResult<GqlSchema> ParseWithTimeout(string source, DiagnosticSink? diagnostics = null)
    {
        var parser = new GqlParser();
        return ExecuteWithTimeout(() => parser.Parse(source, diagnostics), "GraphQL 语法分析器");
    }

    protected IReadOnlyList<GqlToken> TokenizeWithTimeout(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new GqlLexer();
        return ExecuteWithTimeout(() => lexer.Tokenize(source, diagnostics), "GraphQL 词法分析器");
    }

    protected static void AssertParseSuccess(ParseResult<GqlSchema> result)
    {
        Assert.True(result.Success, $"解析失败：{string.Join(", ", result.Diagnostics.Select(d => d.Message))}");
        Assert.NotNull(result.Value);
    }
}
