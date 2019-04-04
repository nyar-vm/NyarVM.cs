namespace Oak.Protobuf.Tests;

public class ProtoLexerTests : ProtoTestBase
{
    [Fact]
    public void Tokenize_Keywords_ShouldReturnKeywordTokens()
    {
        var tokens = TokenizeWithTimeout("syntax package import message enum service rpc returns");

        Assert.True(tokens.Count >= 9);
        Assert.Equal(ProtoTokenType.Syntax, tokens[0].type);
        Assert.Equal(ProtoTokenType.Package, tokens[1].type);
        Assert.Equal(ProtoTokenType.Import, tokens[2].type);
        Assert.Equal(ProtoTokenType.Message, tokens[3].type);
        Assert.Equal(ProtoTokenType.Enum, tokens[4].type);
        Assert.Equal(ProtoTokenType.Service, tokens[5].type);
        Assert.Equal(ProtoTokenType.Rpc, tokens[6].type);
        Assert.Equal(ProtoTokenType.Returns, tokens[7].type);
    }

    [Fact]
    public void Tokenize_Identifier_ShouldReturnIdentifierToken()
    {
        var tokens = TokenizeWithTimeout("UserMessage");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(ProtoTokenType.Identifier, tokens[0].type);
        Assert.Equal("UserMessage", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_IntLiteral_ShouldReturnIntToken()
    {
        var tokens = TokenizeWithTimeout("42");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(ProtoTokenType.IntLiteral, tokens[0].type);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_StringLiteral_ShouldReturnStringToken()
    {
        var tokens = TokenizeWithTimeout("\"proto3\"");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(ProtoTokenType.StringLiteral, tokens[0].type);
        Assert.Equal("proto3", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_BoolLiteral_ShouldReturnBoolToken()
    {
        var tokens = TokenizeWithTimeout("true false");

        Assert.Equal(3, tokens.Count);
        Assert.Equal(ProtoTokenType.BoolLiteral, tokens[0].type);
        Assert.Equal(ProtoTokenType.BoolLiteral, tokens[1].type);
    }

    [Fact]
    public void Tokenize_Punctuation_ShouldReturnPunctuationTokens()
    {
        var tokens = TokenizeWithTimeout("{ } ( ) [ ] ; : , . =");

        Assert.True(tokens.Count >= 11);
        Assert.Equal(ProtoTokenType.LeftBrace, tokens[0].type);
        Assert.Equal(ProtoTokenType.RightBrace, tokens[1].type);
        Assert.Equal(ProtoTokenType.LeftParen, tokens[2].type);
        Assert.Equal(ProtoTokenType.RightParen, tokens[3].type);
        Assert.Equal(ProtoTokenType.LeftBracket, tokens[4].type);
        Assert.Equal(ProtoTokenType.RightBracket, tokens[5].type);
    }

    [Fact]
    public void Tokenize_LineComment_ShouldBeSkipped()
    {
        var tokens = TokenizeWithTimeout("// comment\nmessage");

        Assert.True(tokens.Count >= 2);
        Assert.Equal(ProtoTokenType.Message, tokens[0].type);
    }

    [Fact]
    public void Tokenize_BlockComment_ShouldBeSkipped()
    {
        var tokens = TokenizeWithTimeout("/* comment */ message");

        Assert.True(tokens.Count >= 2);
        Assert.Equal(ProtoTokenType.Message, tokens[0].type);
    }

    [Fact]
    public void Tokenize_FieldModifiers_ShouldReturnKeywordTokens()
    {
        var tokens = TokenizeWithTimeout("repeated optional required oneof map reserved");

        Assert.True(tokens.Count >= 7);
        Assert.Equal(ProtoTokenType.Repeated, tokens[0].type);
        Assert.Equal(ProtoTokenType.Optional, tokens[1].type);
        Assert.Equal(ProtoTokenType.Required, tokens[2].type);
        Assert.Equal(ProtoTokenType.Oneof, tokens[3].type);
        Assert.Equal(ProtoTokenType.Map, tokens[4].type);
        Assert.Equal(ProtoTokenType.Reserved, tokens[5].type);
    }
}
