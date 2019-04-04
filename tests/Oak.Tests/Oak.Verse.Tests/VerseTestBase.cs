using Oak.Syntax;
using Oak.Testing;

namespace Oak.Verse.Tests;

public abstract class VerseTestBase : TestBase
{
    protected CompilationUnit ParseWithTimeout(string source, DiagnosticSink? diagnostics = null)
    {
        var parser = new VerseParser(diagnostics);
        return ExecuteWithTimeout(() => parser.Parse(source), "Verse 语法分析器");
    }

    protected IReadOnlyList<GreenLeafNode> TokenizeWithTimeout(string source)
    {
        var lexer = new VerseLexer();
        return ExecuteWithTimeout(() => lexer.Tokenize(source), "Verse 词法分析器");
    }

    protected static void AssertTokenKind(IReadOnlyList<GreenLeafNode> tokens, int index, NodeKind expectedKind)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedKind, tokens[index].kind);
    }

    protected static void AssertTokenValue(IReadOnlyList<GreenLeafNode> tokens, int index, string expectedValue)
    {
        Assert.True(index < tokens.Count, $"索引 {index} 超出范围（共 {tokens.Count} 个词法单元）");
        Assert.Equal(expectedValue, tokens[index].text);
    }

    protected static void AssertEndsWithEof(IReadOnlyList<GreenLeafNode> tokens)
    {
        Assert.NotEmpty(tokens);
        Assert.Equal(VerseNodeKind.Eof, tokens[^1].kind);
    }
}
