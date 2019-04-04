using Oak.Testing;

namespace Oak.Sql.Tests;

public abstract class SqlTestBase : TestBase
{
    protected virtual int ParseTimeoutMs => 5000;

    protected ParseResult<SqlNode> ParseWithTimeout(string source, DiagnosticSink? diagnostics = null)
    {
        var parser = new SqlParser();
        return ExecuteWithTimeout(() => parser.Parse(source, diagnostics), "SQL 语法分析器");
    }

    protected IReadOnlyList<SqlToken> TokenizeWithTimeout(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new SqlLexer();
        return ExecuteWithTimeout(() => lexer.Tokenize(source, diagnostics), "SQL 词法分析器");
    }

    protected static void AssertParseSuccess(ParseResult<SqlNode> result)
    {
        Assert.True(result.Success, $"解析失败：{string.Join(", ", result.Diagnostics.Select(d => d.Message))}");
        Assert.NotNull(result.Value);
    }

    protected static void AssertParseFailure(ParseResult<SqlNode> result)
    {
        Assert.False(result.Success);
    }
}
