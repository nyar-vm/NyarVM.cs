namespace Std.Data.Text.Regex.Oak.Regex.Tests;


/// <summary>

///     RegexParser 单元测试


/// </summary>
public class RegexParserTests
{
    
/// <summary>
    
///     测试空模式解析为 RegexAstEmpty
    

/// </summary>
    [Fact]
    public void Parse_EmptyPattern_ReturnsEmpty()
    {
        var parser = new RegexParser("");
        var result = parser.Parse();

        Assert.IsType<RegexAstEmpty>(result);
    }

    
/// <summary>
    
///     测试单个字面量字符解析
    

/// </summary>
    [Fact]
    public void Parse_SingleLiteral_ReturnsLiteral()
    {
        var parser = new RegexParser("a");
        var result = parser.Parse();

        var literal = Assert.IsType<RegexAstLiteral>(result);
        Assert.Equal('a', literal.Char);
    }

    
/// <summary>
    
///     测试连接序列解析为 RegexAstConcat
    

/// </summary>
    [Fact]
    public void Parse_Concatenation_ReturnsConcat()
    {
        var parser = new RegexParser("ab");
        var result = parser.Parse();

        var concat = Assert.IsType<RegexAstConcat>(result);
        Assert.Equal(2, concat.Children.Count);
        var left = Assert.IsType<RegexAstLiteral>(concat.Children[0]);
        var right = Assert.IsType<RegexAstLiteral>(concat.Children[1]);
        Assert.Equal('a', left.Char);
        Assert.Equal('b', right.Char);
    }

    
/// <summary>
    
///     测试 Kleene 星号量词解析
    

/// </summary>
    [Fact]
    public void Parse_StarQuantifier_ReturnsStar()
    {
        var parser = new RegexParser("a*");
        var result = parser.Parse();

        var star = Assert.IsType<RegexAstStar>(result);
        var child = Assert.IsType<RegexAstLiteral>(star.Child);
        Assert.Equal('a', child.Char);
    }

    
/// <summary>
    
///     测试加号量词解析
    

/// </summary>
    [Fact]
    public void Parse_PlusQuantifier_ReturnsPlus()
    {
        var parser = new RegexParser("a+");
        var result = parser.Parse();

        var plus = Assert.IsType<RegexAstPlus>(result);
        var child = Assert.IsType<RegexAstLiteral>(plus.Child);
        Assert.Equal('a', child.Char);
    }

    
/// <summary>
    
///     测试问号量词解析
    

/// </summary>
    [Fact]
    public void Parse_QuestionQuantifier_ReturnsQuestion()
    {
        var parser = new RegexParser("a?");
        var result = parser.Parse();

        var question = Assert.IsType<RegexAstQuestion>(result);
        var child = Assert.IsType<RegexAstLiteral>(question.Child);
        Assert.Equal('a', child.Char);
    }

    
/// <summary>
    
///     测试选择（交替）解析
    

/// </summary>
    [Fact]
    public void Parse_Alternation_ReturnsAlt()
    {
        var parser = new RegexParser("a|b");
        var result = parser.Parse();

        var alt = Assert.IsType<RegexAstAlt>(result);
        var left = Assert.IsType<RegexAstLiteral>(alt.Left);
        var right = Assert.IsType<RegexAstLiteral>(alt.Right);
        Assert.Equal('a', left.Char);
        Assert.Equal('b', right.Char);
    }

    
/// <summary>
    
///     测试字符类解析
    

/// </summary>
    [Fact]
    public void Parse_CharClass_ReturnsCharClass()
    {
        var parser = new RegexParser("[abc]");
        var result = parser.Parse();

        var charClass = Assert.IsType<RegexAstCharClass>(result);
        Assert.Equal("abc", charClass.Chars);
    }

    
/// <summary>
    
///     测试括号分组解析，括号仅用于分组不产生额外节点
    

/// </summary>
    [Fact]
    public void Parse_ParenthesizedGroup_StripsParens()
    {
        var parser = new RegexParser("(a)");
        var result = parser.Parse();

        var literal = Assert.IsType<RegexAstLiteral>(result);
        Assert.Equal('a', literal.Char);
    }

    
/// <summary>
    
///     测试复杂嵌套模式：(ab|c)*
    

/// </summary>
    [Fact]
    public void Parse_ComplexGroupWithStar_ReturnsStarOfAlt()
    {
        var parser = new RegexParser("(ab|c)*");
        var result = parser.Parse();

        var star = Assert.IsType<RegexAstStar>(result);
        var alt = Assert.IsType<RegexAstAlt>(star.Child);

        var concat = Assert.IsType<RegexAstConcat>(alt.Left);
        Assert.Equal(2, concat.Children.Count);
        var leftA = Assert.IsType<RegexAstLiteral>(concat.Children[0]);
        var leftB = Assert.IsType<RegexAstLiteral>(concat.Children[1]);
        Assert.Equal('a', leftA.Char);
        Assert.Equal('b', leftB.Char);

        var right = Assert.IsType<RegexAstLiteral>(alt.Right);
        Assert.Equal('c', right.Char);
    }

    
/// <summary>
    
///     测试转义特殊字符，转义后的特殊字符变为字面量
    

/// </summary>
    [Fact]
    public void Parse_EscapedSpecialChar_ReturnsLiteral()
    {
        var parser = new RegexParser("\\*");
        var result = parser.Parse();

        var literal = Assert.IsType<RegexAstLiteral>(result);
        Assert.Equal('*', literal.Char);
    }

    
/// <summary>
    
///     测试点号（任意字符）解析为字符类
    

/// </summary>
    [Fact]
    public void Parse_Dot_ReturnsCharClass()
    {
        var parser = new RegexParser(".");
        var result = parser.Parse();

        var charClass = Assert.IsType<RegexAstCharClass>(result);
        Assert.Equal(".", charClass.Chars);
    }

    
/// <summary>
    
///     测试复杂模式：a(b|c)*d
    

/// </summary>
    [Fact]
    public void Parse_ComplexPattern_ReturnsCorrectStructure()
    {
        var parser = new RegexParser("a(b|c)*d");
        var result = parser.Parse();

        var concat = Assert.IsType<RegexAstConcat>(result);
        Assert.Equal(3, concat.Children.Count);

        var first = Assert.IsType<RegexAstLiteral>(concat.Children[0]);
        Assert.Equal('a', first.Char);

        var star = Assert.IsType<RegexAstStar>(concat.Children[1]);
        var alt = Assert.IsType<RegexAstAlt>(star.Child);
        var altLeft = Assert.IsType<RegexAstLiteral>(alt.Left);
        var altRight = Assert.IsType<RegexAstLiteral>(alt.Right);
        Assert.Equal('b', altLeft.Char);
        Assert.Equal('c', altRight.Char);

        var third = Assert.IsType<RegexAstLiteral>(concat.Children[2]);
        Assert.Equal('d', third.Char);
    }

    
/// <summary>
    
///     测试转义字母保持原样
    

/// </summary>
    [Fact]
    public void Parse_EscapedNormalChar_ReturnsLiteral()
    {
        var parser = new RegexParser("\\a");
        var result = parser.Parse();

        var literal = Assert.IsType<RegexAstLiteral>(result);
        Assert.Equal('a', literal.Char);
    }

    
/// <summary>
    
///     测试多字符交替，交替是左结合的
    

/// </summary>
    [Fact]
    public void Parse_MultipleAlternation_ReturnsNestedAlt()
    {
        var parser = new RegexParser("a|b|c");
        var result = parser.Parse();

        var alt = Assert.IsType<RegexAstAlt>(result);

        var leftAlt = Assert.IsType<RegexAstAlt>(alt.Left);
        var leftA = Assert.IsType<RegexAstLiteral>(leftAlt.Left);
        var leftB = Assert.IsType<RegexAstLiteral>(leftAlt.Right);
        Assert.Equal('a', leftA.Char);
        Assert.Equal('b', leftB.Char);

        var right = Assert.IsType<RegexAstLiteral>(alt.Right);
        Assert.Equal('c', right.Char);
    }
}
