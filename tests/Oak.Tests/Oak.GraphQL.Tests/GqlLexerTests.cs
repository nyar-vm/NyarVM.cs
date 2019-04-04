namespace Oak.GraphQL.Tests;

public class GqlLexerTests : GqlTestBase
{
    [Fact]
    public void Tokenize_Keywords_ShouldReturnKeywordTokens()
    {
        var tokens = TokenizeWithTimeout("type input interface enum union scalar schema directive");

        Assert.True(tokens.Count >= 9);
        Assert.Equal(GqlTokenType.type, tokens[0].type);
        Assert.Equal(GqlTokenType.Input, tokens[1].type);
        Assert.Equal(GqlTokenType.Interface, tokens[2].type);
        Assert.Equal(GqlTokenType.Enum, tokens[3].type);
        Assert.Equal(GqlTokenType.Union, tokens[4].type);
        Assert.Equal(GqlTokenType.Scalar, tokens[5].type);
        Assert.Equal(GqlTokenType.Schema, tokens[6].type);
        Assert.Equal(GqlTokenType.Directive, tokens[7].type);
    }

    [Fact]
    public void Tokenize_Name_ShouldReturnNameToken()
    {
        var tokens = TokenizeWithTimeout("User");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(GqlTokenType.Name, tokens[0].type);
        Assert.Equal("User", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_IntValue_ShouldReturnIntToken()
    {
        var tokens = TokenizeWithTimeout("42");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(GqlTokenType.IntValue, tokens[0].type);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_FloatValue_ShouldReturnFloatToken()
    {
        var tokens = TokenizeWithTimeout("3.14");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(GqlTokenType.FloatValue, tokens[0].type);
    }

    [Fact]
    public void Tokenize_StringValue_ShouldReturnStringToken()
    {
        var tokens = TokenizeWithTimeout("\"hello\"");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(GqlTokenType.StringValue, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Punctuation_ShouldReturnPunctuationTokens()
    {
        var tokens = TokenizeWithTimeout("{ } ( ) [ ] ! : @");

        Assert.True(tokens.Count >= 9);
        Assert.Equal(GqlTokenType.LeftBrace, tokens[0].type);
        Assert.Equal(GqlTokenType.RightBrace, tokens[1].type);
        Assert.Equal(GqlTokenType.LeftParen, tokens[2].type);
        Assert.Equal(GqlTokenType.RightParen, tokens[3].type);
        Assert.Equal(GqlTokenType.LeftBracket, tokens[4].type);
        Assert.Equal(GqlTokenType.RightBracket, tokens[5].type);
        Assert.Equal(GqlTokenType.Exclamation, tokens[6].type);
        Assert.Equal(GqlTokenType.Colon, tokens[7].type);
        Assert.Equal(GqlTokenType.At, tokens[8].type);
    }

    [Fact]
    public void Tokenize_Spread_ShouldReturnSpreadToken()
    {
        var tokens = TokenizeWithTimeout("...");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(GqlTokenType.Spread, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Comment_ShouldBeSkipped()
    {
        var tokens = TokenizeWithTimeout("# comment\ntype");

        Assert.True(tokens.Count >= 2);
        Assert.Equal(GqlTokenType.type, tokens[0].type);
    }

    [Fact]
    public void Tokenize_BooleanLiterals_ShouldReturnCorrectTokens()
    {
        var tokens = TokenizeWithTimeout("true false");

        Assert.Equal(3, tokens.Count);
        Assert.Equal(GqlTokenType.True, tokens[0].type);
        Assert.Equal(GqlTokenType.False, tokens[1].type);
    }
}
