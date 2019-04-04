namespace Std.Data.Text.CSharp.Oak.CSharp.Tests.LexerTests;

public class CsLexerTests
{
    private readonly CsLexer _lexer = new();

    [Fact]
    public void Tokenize_Keywords_ShouldReturnKeywordTokens()
    {
        var tokens = _lexer.TokenizeAsCsTokens("int void return class");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Keyword, tokens[0].Kind);
        Assert.Equal("int", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Identifier_ShouldReturnIdentifierToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("myVariable");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Identifier, tokens[0].Kind);
        Assert.Equal("myVariable", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Number_ShouldReturnNumberToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("42");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Number, tokens[0].Kind);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_FloatNumber_ShouldReturnNumberToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("3.14f");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Number, tokens[0].Kind);
        Assert.Equal("3.14f", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_String_ShouldReturnStringToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("\"hello world\"");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.String, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_VerbatimString_ShouldReturnStringToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("@\"hello \"\"world\"\"\"");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.String, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Char_ShouldReturnCharToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("'a'");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Char, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Operators_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.TokenizeAsCsTokens("+ - * / = == != => ??");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Operator, tokens[0].Kind);
        Assert.Equal("+", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Delimiters_ShouldReturnDelimiterTokens()
    {
        var tokens = _lexer.TokenizeAsCsTokens("( ) { } [ ] ; , .");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Delimiter, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Comment_ShouldBeSkipped()
    {
        var tokens = _lexer.TokenizeAsCsTokens("// comment\nint");

        var intToken = tokens.FirstOrDefault(t => t.Kind == CsNodeKind.Keyword && t.Text == "int");
        Assert.NotNull(intToken);
    }

    [Fact]
    public void Tokenize_BlockComment_ShouldReturnCommentToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("/* block comment */\nint");

        Assert.True(tokens.Count >= 2);
        Assert.Equal(CsNodeKind.Comment, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_HexNumber_ShouldReturnNumberToken()
    {
        var tokens = _lexer.TokenizeAsCsTokens("0xFF");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Number, tokens[0].Kind);
        Assert.Equal("0xFF", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.TokenizeAsCsTokens("");

        Assert.NotEmpty(tokens);
        Assert.Equal(CsNodeKind.Eof, tokens[^1].Kind);
    }
}
