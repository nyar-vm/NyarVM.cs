namespace Oak.Verse.Tests;

public class VerseLexerTests : VerseTestBase
{
    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = TokenizeWithTimeout("");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Eof, tokens[^1].kind);
    }

    [Fact]
    public void Tokenize_Keywords_ShouldReturnKeywordTokens()
    {
        var tokens = TokenizeWithTimeout("scene label jump call return");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Keyword, tokens[0].kind);
        Assert.Equal("scene", tokens[0].text);
    }

    [Fact]
    public void Tokenize_Identifier_ShouldReturnIdentifierToken()
    {
        var tokens = TokenizeWithTimeout("myVariable");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Identifier, tokens[0].kind);
        Assert.Equal("myVariable", tokens[0].text);
    }

    [Fact]
    public void Tokenize_Number_ShouldReturnNumberToken()
    {
        var tokens = TokenizeWithTimeout("42");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Number, tokens[0].kind);
        Assert.Equal("42", tokens[0].text);
    }

    [Fact]
    public void Tokenize_String_ShouldReturnStringToken()
    {
        var tokens = TokenizeWithTimeout("\"hello world\"");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.String, tokens[0].kind);
    }

    [Fact]
    public void Tokenize_Literals_ShouldReturnLiteralTokens()
    {
        var tokens = TokenizeWithTimeout("true false null");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Literal, tokens[0].kind);
        Assert.Equal("true", tokens[0].text);
    }

    [Fact]
    public void Tokenize_Operators_ShouldReturnOperatorTokens()
    {
        var tokens = TokenizeWithTimeout("== != + - =");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Operator, tokens[0].kind);
        Assert.Equal("==", tokens[0].text);
    }

    [Fact]
    public void Tokenize_Delimiters_ShouldReturnDelimiterTokens()
    {
        var tokens = TokenizeWithTimeout("( ) { }");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Delimiter, tokens[0].kind);
    }

    [Fact]
    public void Tokenize_LineComment_ShouldBeSkipped()
    {
        var tokens = TokenizeWithTimeout("# comment\nscene");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Keyword, tokens[0].kind);
        Assert.Equal("scene", tokens[0].text);
    }

    [Fact]
    public void Tokenize_DoubleSlashComment_ShouldBeSkipped()
    {
        var tokens = TokenizeWithTimeout("// comment\nlabel");

        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Keyword, tokens[0].kind);
        Assert.Equal("label", tokens[0].text);
    }
}
