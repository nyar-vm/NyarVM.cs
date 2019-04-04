using Oak.Diagnostics;
using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public sealed class TestLanguage : Language
{
    public override string name => "Test";
    public bool FeatureEnabled { get; init; }
}

public sealed class TestContext : ISyntaxContext
{
}

public class LanguageAndParseContextTests
{
    private const int TimeoutMs = 5000;

    private static ParseContext<TestLanguage, TestContext> CreateContext(string text)
    {
        return new ParseContext<TestLanguage, TestContext>(
            new StringSource(text), new TestLanguage(), new TestContext(), new DiagnosticSink());
    }

    [Fact]
    public void Language_Name()
    {
        var lang = new TestLanguage();
        Assert.Equal("Test", lang.name);
    }

    [Fact]
    public void Language_ToString()
    {
        var lang = new TestLanguage();
        Assert.Equal("Test", lang.ToString());
    }

    [Fact]
    public void ParseContext_BasicOperations()
    {
        var parseCtx = CreateContext("abc");
        Assert.Equal(0, parseCtx.position);
        Assert.Equal('a', parseCtx.current);
        parseCtx.advance();
        Assert.Equal(1, parseCtx.position);
        Assert.Equal('b', parseCtx.current);
    }

    [Fact]
    public void ParseContext_Peek()
    {
        var parseCtx = CreateContext("abc");
        Assert.Equal('a', parseCtx.peek(0));
        Assert.Equal('b', parseCtx.peek(1));
        Assert.Equal('c', parseCtx.peek(2));
        Assert.Equal('\0', parseCtx.peek(3));
    }

    [Fact]
    public void ParseContext_GetSpanFrom()
    {
        var parseCtx = CreateContext("abc");
        parseCtx.advance();
        parseCtx.advance();
        var span = parseCtx.get_span_from(0);
        Assert.Equal(default(TextSpan), span);
    }

    [Fact]
    public void ParseContext_GetText()
    {
        var parseCtx = CreateContext("abc");
        var text = parseCtx.get_text(default(TextSpan));
        Assert.Equal("ab", text);
    }

    [Fact]
    public void ParseContext_SkipToChar()
    {
        var parseCtx = CreateContext("abc;def");
        parseCtx.skip_to(';');
        Assert.Equal(3, parseCtx.position);
        Assert.Equal(';', parseCtx.current);
    }

    [Fact]
    public void ParseContext_SkipToChar_NotFound()
    {
        var parseCtx = CreateContext("abcdef");
        parseCtx.skip_to(';');
        Assert.Equal(6, parseCtx.position);
    }

    [Fact]
    public void ParseContext_SkipToPredicate()
    {
        var parseCtx = CreateContext("abc1def");
        parseCtx.skip_to(char.IsDigit);
        Assert.Equal(3, parseCtx.position);
        Assert.Equal('1', parseCtx.current);
    }

    [Fact]
    public void ParseContext_SkipToPredicate_NotFound()
    {
        var parseCtx = CreateContext("abcdef");
        parseCtx.skip_to(char.IsDigit);
        Assert.Equal(6, parseCtx.position);
    }

    [Fact]
    public void ParseContext_ExpectChar_Success()
    {
        var parseCtx = CreateContext("abc");
        var result = parseCtx.expect('a', "TEST001", "expected 'a'");
        Assert.True(result);
        Assert.Empty(parseCtx.diagnostics.Messages);
    }

    [Fact]
    public void ParseContext_ExpectChar_Failure()
    {
        var parseCtx = CreateContext("abc");
        var result = parseCtx.expect('x', "TEST001", "expected 'x'");
        Assert.False(result);
        Assert.Single(parseCtx.diagnostics.Messages);
        Assert.Equal(DiagnosticLevel.Error, parseCtx.diagnostics.Messages[0].Level);
    }

    [Fact]
    public void ParseContext_ExpectPredicate_Success()
    {
        var parseCtx = CreateContext("abc");
        var result = parseCtx.expect(char.IsLetter, "TEST002", "expected letter");
        Assert.True(result);
        Assert.Empty(parseCtx.diagnostics.Messages);
    }

    [Fact]
    public void ParseContext_ExpectPredicate_Failure()
    {
        var parseCtx = CreateContext("123");
        var result = parseCtx.expect(char.IsLetter, "TEST002", "expected letter");
        Assert.False(result);
        Assert.Single(parseCtx.diagnostics.Messages);
    }

    [Fact]
    public void ParseContext_Recover_Success()
    {
        var parseCtx = CreateContext("abc;def");
        parseCtx.recover((ref ParseContext<TestLanguage, TestContext> p) =>
        {
            p.advance();
            p.advance();
        }, "should not fail");
        Assert.Empty(parseCtx.diagnostics.Errors);
    }

    [Fact]
    public void ParseContext_Recover_Failure()
    {
        var parseCtx = CreateContext("abc;def");
        parseCtx.recover((ref ParseContext<TestLanguage, TestContext> p) =>
        {
            p.diagnostics.AddError("", default(TextSpan), "TEST_ERR", "forced error");
        }, "recovery test");
        Assert.True(parseCtx.diagnostics.Errors.Count >= 2);
    }
}
