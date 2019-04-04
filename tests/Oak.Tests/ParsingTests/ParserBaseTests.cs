using Oak.Diagnostics;
using Oak.Parsing;

namespace Oak.Core.Tests.ParsingTests;

using Oak.Testing;

public sealed class TestParser : ParserBase<string, int>
{
    public TestParser() : base() { }

    public override int Parse(string input)
    {
        return input.Length;
    }
}

public class ParserBaseTests : TestBase
{
    [Fact]
    public void ParserBase_Parse()
    {
        var parser = new TestParser();
        var result = ExecuteWithTimeout(() => parser.Parse("hello"), "语法分析");
        Assert.Equal(5, result);
    }

    [Fact]
    public void ParseResult_Ok()
    {
        var result = ParseResult<int>.Ok(42);
        Assert.True(result.Success);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ParseResult_Fail()
    {
        var diagnostics = new List<DiagnosticMessage>
        {
            new(DiagnosticLevel.Error, "E001", "test error")
        };
        var result = ParseResult<int>.Fail(diagnostics);
        Assert.False(result.Success);
        Assert.Single(result.Diagnostics);
    }
}
