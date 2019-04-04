using Oak.Lexing;
using Oak.Syntax;
using Oak.Testing;

namespace Oak.Core.Tests.LexingTests;

public sealed class TestLexer : LexerBase
{
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        Source = new StringSource(source);
        Reset();

        var nodes = new List<GreenLeafNode>();
        while (!IsAtEnd())
        {
            if (char.IsWhiteSpace(Peek()))
            {
                SkipWhitespace();
                continue;
            }

            var c = Advance();
            nodes.Add(new GreenLeafNode(1, c.ToString().Length, c.ToString()));
        }

        return nodes;
    }
}

public class LexerBaseTests : TestBase
{
    [Fact]
    public void LexerBase_TokenizeFromString()
    {
        var lexer = new TestLexer();
        var nodes = ExecuteWithTimeout(() => lexer.Tokenize("a b"), "词法分析");
        Assert.Equal(2, nodes.Count);
        Assert.Equal("a", nodes[0].text);
        Assert.Equal("b", nodes[1].text);
    }

    [Fact]
    public void LexerBase_TokenizeFromISource()
    {
        var source = new StringSource("x y");
        var lexer = new TestLexer();
        var nodes = ExecuteWithTimeout(() => lexer.Tokenize(source), "词法分析");
        Assert.Equal(2, nodes.Count);
    }

    [Fact]
    public void LexerBase_PositionTracking()
    {
        var lexer = new TestLexer();
        var nodes = ExecuteWithTimeout(() => lexer.Tokenize("a\nb"), "词法分析");
        Assert.Equal(2, nodes.Count);
        Assert.Equal(1, nodes[0].width);
        Assert.Equal(1, nodes[1].width);
    }

    [Fact]
    public void LexerBase_SkipsWhitespace()
    {
        var lexer = new TestLexer();
        var nodes = ExecuteWithTimeout(() => lexer.Tokenize("  a  b  "), "词法分析");
        Assert.Equal(2, nodes.Count);
        Assert.Equal("a", nodes[0].text);
        Assert.Equal("b", nodes[1].text);
    }
}
