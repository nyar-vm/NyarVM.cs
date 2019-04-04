using Oak.Testing;

namespace Oak.Protobuf.Tests;

public abstract class ProtoTestBase : TestBase
{
    protected virtual int ParseTimeoutMs => 5000;

    protected ParseResult<ProtoFile> ParseWithTimeout(string source, DiagnosticSink? diagnostics = null)
    {
        var parser = new ProtoParser();
        return ExecuteWithTimeout(() => parser.Parse(source, diagnostics), "Protobuf 语法分析器");
    }

    protected IReadOnlyList<ProtoToken> TokenizeWithTimeout(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new ProtoLexer();
        return ExecuteWithTimeout(() => lexer.Tokenize(source, diagnostics), "Protobuf 词法分析器");
    }

    protected static void AssertParseSuccess(ParseResult<ProtoFile> result)
    {
        Assert.True(result.Success, $"解析失败：{string.Join(", ", result.Diagnostics.Select(d => d.Message))}");
        Assert.NotNull(result.Value);
    }
}
