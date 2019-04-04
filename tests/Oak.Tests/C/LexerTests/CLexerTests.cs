using Oak.Syntax;

namespace Oak.C.Tests.LexerTests;

public class CLexerTests
{
    private readonly CLexer _lexer = new();

    [Fact]
    public void Tokenize_Keywords_ShouldReturnKeywordTokens()
    {
        var tokens = _lexer.TokenizeAsCTokens("int void return");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.Keyword, tokens[0].Kind);
        Assert.Equal("int", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Identifier_ShouldReturnIdentifierToken()
    {
        var tokens = _lexer.TokenizeAsCTokens("myVariable");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.Identifier, tokens[0].Kind);
        Assert.Equal("myVariable", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Number_ShouldReturnNumberToken()
    {
        var tokens = _lexer.TokenizeAsCTokens("42");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.Number, tokens[0].Kind);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_String_ShouldReturnStringToken()
    {
        var tokens = _lexer.TokenizeAsCTokens("\"hello\"");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.String, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Operators_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.TokenizeAsCTokens("+ - * / = == !=");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.Operator, tokens[0].Kind);
        Assert.Equal("+", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Delimiters_ShouldReturnDelimiterTokens()
    {
        var tokens = _lexer.TokenizeAsCTokens("( ) { } [ ] ; , .");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.Delimiter, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Preprocessor_ShouldReturnPreprocessorToken()
    {
        var tokens = _lexer.TokenizeAsCTokens("#include <stdio.h>");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.Preprocessor, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Comment_ShouldBeSkipped()
    {
        var tokens = _lexer.TokenizeAsCTokens("// comment\nint");

        var intToken = tokens.FirstOrDefault(t => t.Kind == CNodeKind.Keyword && t.Text == "int");
        Assert.NotNull(intToken);
    }

    [Fact]
    public void Tokenize_BlockComment_ShouldReturnCommentToken()
    {
        var tokens = _lexer.TokenizeAsCTokens("/* block comment */\nint");

        Assert.True(tokens.Count >= 2);
        Assert.Equal(CNodeKind.Comment, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.TokenizeAsCTokens("");

        Assert.NotEmpty(tokens);
        Assert.Equal(CNodeKind.Eof, tokens[^1].Kind);
    }
}
