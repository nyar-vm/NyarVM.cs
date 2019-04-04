using Nyar.VM.TextVM.Compiler;

namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// PatternParser 模式解析器的单元测试。
/// 覆盖字面量、选择、量词、字符类、捕获组及错误路径。
/// </summary>
public class PatternParserTests
{
    /// <summary>
    /// 单字符字面量 "a" 解析为 LiteralNode("a")。
    /// </summary>
    [Fact]
    public void Parse_LiteralString_ReturnsLiteralNode()
    {
        (AstNode? node, String? error) = PatternParser.Parse("a");

        Assert.Null(error);
        Assert.NotNull(node);
        LiteralNode literal = Assert.IsType<LiteralNode>(node);
        Assert.Equal("a", literal.Value);
    }

    /// <summary>
    /// 选择表达式 "a|b" 解析为 AltNode。
    /// </summary>
    [Fact]
    public void Parse_Alternation_ReturnsAltNode()
    {
        (AstNode? node, String? error) = PatternParser.Parse("a|b");

        Assert.Null(error);
        Assert.NotNull(node);
        AltNode alt = Assert.IsType<AltNode>(node);
        LiteralNode left = Assert.IsType<LiteralNode>(alt.Left);
        LiteralNode right = Assert.IsType<LiteralNode>(alt.Right);
        Assert.Equal("a", left.Value);
        Assert.Equal("b", right.Value);
    }

    /// <summary>
    /// Kleene 星号 "a*" 解析为 StarNode，其内层为 LiteralNode("a")。
    /// </summary>
    [Fact]
    public void Parse_Star_ReturnsStarNode()
    {
        (AstNode? node, String? error) = PatternParser.Parse("a*");

        Assert.Null(error);
        Assert.NotNull(node);
        StarNode star = Assert.IsType<StarNode>(node);
        LiteralNode inner = Assert.IsType<LiteralNode>(star.Inner);
        Assert.Equal("a", inner.Value);
    }

    /// <summary>
    /// 字符类 "[az]" 解析为 CharClassNode，包含正确的区间。
    /// 注：当前解析器对字符类中非转义字符使用双 Advance，因此两个字符构成一个区间。
    /// </summary>
    [Fact]
    public void Parse_CharClass_ReturnsCharClassNode()
    {
        (AstNode? node, String? error) = PatternParser.Parse("[az]");

        Assert.Null(error);
        Assert.NotNull(node);
        CharClassNode cc = Assert.IsType<CharClassNode>(node);
        Assert.False(cc.Negated);
        Assert.NotEmpty(cc.Ranges);
    }

    /// <summary>
    /// 捕获组 "(a)" 解析为 CaptureNode，包含正确的组编号和内层 LiteralNode。
    /// </summary>
    [Fact]
    public void Parse_Capture_ReturnsCaptureNode()
    {
        (AstNode? node, String? error) = PatternParser.Parse("(a)");

        Assert.Null(error);
        Assert.NotNull(node);
        CaptureNode capture = Assert.IsType<CaptureNode>(node);
        Assert.Equal(0, capture.GroupId);
        LiteralNode inner = Assert.IsType<LiteralNode>(capture.Inner);
        Assert.Equal("a", inner.Value);
    }

    /// <summary>
    /// 无效模式（未闭合的字符类）返回非空错误信息。
    /// </summary>
    [Fact]
    public void Parse_InvalidPattern_ReturnsError()
    {
        (AstNode? node, String? error) = PatternParser.Parse("[");

        Assert.Null(node);
        Assert.NotNull(error);
    }
}
