namespace Std.Data.Text.Python.Tests;

public class PythonLexerTests
{
    [Fact]
    public void TokenizeToGreen_BasicSyntax_ReturnsCorrectNodes()
    {
        // 准备测试代码
        var code = "def hello():\n    print('Hello, world!')\n\nhello()";
        var lexer = new PythonLexer();

        // 执行词法分析
        var greenNodes = lexer.TokenizeToGreen(code);

        // 验证 token        Assert.NotEmpty(greenNodes);
        
        // 检查第一个节点是否是关键字 "def"
        var firstNode = greenNodes[0];
        Assert.Equal(4, firstNode.Width);
        Assert.Equal("def", firstNode.Text);
    }

    [Fact]
    public void TokenizeToGreen_Expressions_ReturnsCorrectNodes()
    {
        // 准备测试代码
        var code = "x = 1 + 2 * 3";
        var lexer = new PythonLexer();

        // 执行词法分析
        var greenNodes = lexer.TokenizeToGreen(code);

        // 验证 token        Assert.NotEmpty(greenNodes);
    }

    [Fact]
    public void Tokenize_StillWorks_ReturnsCorrectTokens()
    {
        // 准备测试代码
        var code = "def test():\n    pass";
        var lexer = new PythonLexer();

        // 执行词法分析
        var tokens = lexer.Tokenize(code);

        // 验证 token 数量和类型        Assert.NotEmpty(tokens);
    }
}

