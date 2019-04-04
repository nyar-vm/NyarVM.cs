using Oak.Diagnostics;

namespace Oak.Json.Tests;

public class JsonLexerTests : JsonTestBase
{
    private readonly JsonLexer _lexer = new();

    [Fact]
    public void EmptyInput_ProducesOnlyEof()
    {
        var tokens = TokenizeWithTimeout(_lexer, "");
        Assert.Single(tokens);
        Assert.Equal(JsonTokenType.EndOfFile, tokens[0].type);
    }

    [Fact]
    public void LeftBrace()
    {
        var tokens = TokenizeWithTimeout(_lexer, "{");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(JsonTokenType.LeftBrace, tokens[0].type);
        Assert.Equal("{", tokens[0].Text);
    }

    [Fact]
    public void RightBrace()
    {
        var tokens = TokenizeWithTimeout(_lexer, "}");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(JsonTokenType.RightBrace, tokens[0].type);
    }

    [Fact]
    public void String_Simple()
    {
        var tokens = TokenizeWithTimeout(_lexer, "\"hello\"");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(JsonTokenType.String, tokens[0].type);
        Assert.Equal("hello", tokens[0].Text);
    }

    [Fact]
    public void Number_Integer()
    {
        var tokens = TokenizeWithTimeout(_lexer, "42");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(JsonTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void TrueKeyword()
    {
        var tokens = TokenizeWithTimeout(_lexer, "true");
        Assert.Contains(tokens, t => t.type == JsonTokenType.True);
    }

    [Fact]
    public void FalseKeyword()
    {
        var tokens = TokenizeWithTimeout(_lexer, "false");
        Assert.Contains(tokens, t => t.type == JsonTokenType.False);
    }

    [Fact]
    public void NullKeyword()
    {
        var tokens = TokenizeWithTimeout(_lexer, "null");
        Assert.Contains(tokens, t => t.type == JsonTokenType.Null);
    }

    [Fact]
    public void SimpleObject()
    {
        var tokens = TokenizeWithTimeout(_lexer, "{\"a\": 1}");
        Assert.Equal(6, tokens.Count);
        Assert.Equal(JsonTokenType.LeftBrace, tokens[0].type);
        Assert.Equal(JsonTokenType.String, tokens[1].type);
        Assert.Equal("a", tokens[1].Text);
        Assert.Equal(JsonTokenType.Colon, tokens[2].type);
        Assert.Equal(JsonTokenType.Number, tokens[3].type);
        Assert.Equal(JsonTokenType.RightBrace, tokens[4].type);
        Assert.Equal(JsonTokenType.EndOfFile, tokens[5].type);
    }

    [Fact]
    public void SimpleArray()
    {
        var tokens = TokenizeWithTimeout(_lexer, "[1, 2, 3]");
        Assert.Equal(8, tokens.Count);
        Assert.Equal(JsonTokenType.LeftBracket, tokens[0].type);
        Assert.Equal(JsonTokenType.Number, tokens[1].type);
        Assert.Equal(JsonTokenType.Comma, tokens[2].type);
        Assert.Equal(JsonTokenType.RightBracket, tokens[6].type);
        Assert.Equal(JsonTokenType.EndOfFile, tokens[7].type);
    }

    [Fact]
    public void InvalidCharacter_ProducesError()
    {
        var diagnostics = new DiagnosticSink();
        var tokens = _lexer.Tokenize("@", diagnostics);
        Assert.True(diagnostics.HasErrors);
    }
}
