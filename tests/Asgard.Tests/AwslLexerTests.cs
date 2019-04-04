using Xunit;

namespace VOA.ToolChain.Tests;

/// <summary>
///     AWSL 词法分析器 Tokenize 测试
/// </summary>
public class AwslLexerTests
{
    private readonly AwslLexer _lexer = new();

    #region 字面量测试

    /// <summary>
    ///     布尔和 null 字面量
    /// </summary>
    [Fact]
    public void Tokenize_Literals_ShouldReturnLiteralTokens()
    {
        var tokens = _lexer.Tokenize("true false null");

        Assert.All(tokens.Take(3), t => Assert.Equal(AwslNodeKind.literal, t.Kind));
        Assert.Equal("true", tokens[0].Text);
        Assert.Equal("false", tokens[1].Text);
        Assert.Equal("null", tokens[2].Text);
    }

    #endregion

    #region 分隔符测试

    /// <summary>
    ///     括号和大括号
    /// </summary>
    [Fact]
    public void Tokenize_Delimiters_ShouldReturnDelimiterTokens()
    {
        var tokens = _lexer.Tokenize("( ) [ ] { } ,");

        Assert.All(tokens.Take(7), t => Assert.Equal(AwslNodeKind.delimiter, t.Kind));
        Assert.Equal("(", tokens[0].Text);
        Assert.Equal(")", tokens[1].Text);
        Assert.Equal("[", tokens[2].Text);
        Assert.Equal("]", tokens[3].Text);
        Assert.Equal("{", tokens[4].Text);
        Assert.Equal("}", tokens[5].Text);
        Assert.Equal(",", tokens[6].Text);
    }

    #endregion

    #region 微函数声明测试

    /// <summary>
    ///     micro 函数声明 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_MicroFunction_ShouldTokenizeCorrectly()
    {
        var tokens = _lexer.Tokenize("micro increment(count: i32): i32 { return count + 1 }");

        Assert.Equal(AwslNodeKind.keyword, tokens[0].Kind);
        Assert.Equal("micro", tokens[0].Text);
        Assert.Equal(AwslNodeKind.identifier, tokens[1].Kind);
        Assert.Equal("increment", tokens[1].Text);
    }

    #endregion

    #region 基础 Token 测试

    /// <summary>
    ///     空源码应只返回 Eof
    /// </summary>
    [Fact]
    public void Tokenize_EmptySource_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("");

        Assert.Single(tokens);
        Assert.Equal(AwslNodeKind.eof, tokens[0].Kind);
    }

    /// <summary>
    ///     纯空白源码应只返回 Eof
    /// </summary>
    [Fact]
    public void Tokenize_WhitespaceOnly_ShouldReturnEof()
    {
        var tokens = _lexer.Tokenize("   \t  \n  \r\n  ");

        Assert.Single(tokens);
        Assert.Equal(AwslNodeKind.eof, tokens[0].Kind);
    }

    /// <summary>
    ///     关键字 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_Keywords_ShouldReturnKeywordTokens()
    {
        var tokens = _lexer.Tokenize("let const micro if else for foreach in return import export");

        Assert.Equal(11, tokens.Count(t => t.Kind == AwslNodeKind.keyword));
        Assert.Equal("let", tokens[0].Text);
        Assert.Equal("const", tokens[1].Text);
        Assert.Equal("micro", tokens[2].Text);
    }

    /// <summary>
    ///     标识符 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_Identifier_ShouldReturnIdentifierToken()
    {
        var tokens = _lexer.Tokenize("myVariable");

        Assert.Equal(AwslNodeKind.identifier, tokens[0].Kind);
        Assert.Equal("myVariable", tokens[0].Text);
    }

    /// <summary>
    ///     标签名作为标识符 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_TagNames_ShouldReturnIdentifierTokens()
    {
        var tokens = _lexer.Tokenize("VStack Text Button HStack");

        Assert.All(tokens.Take(4), t => Assert.Equal(AwslNodeKind.identifier, t.Kind));
        Assert.Equal("VStack", tokens[0].Text);
        Assert.Equal("Text", tokens[1].Text);
        Assert.Equal("Button", tokens[2].Text);
        Assert.Equal("HStack", tokens[3].Text);
    }

    /// <summary>
    ///     带连字符的标识符
    /// </summary>
    [Fact]
    public void Tokenize_IdentifierWithHyphen_ShouldReturnSingleIdentifier()
    {
        var tokens = _lexer.Tokenize("my-variable");

        Assert.Equal(AwslNodeKind.identifier, tokens[0].Kind);
        Assert.Equal("my-variable", tokens[0].Text);
    }

    #endregion

    #region 字符串字面量测试

    /// <summary>
    ///     双引号字符串
    /// </summary>
    [Fact]
    public void Tokenize_DoubleQuotedString_ShouldReturnStringToken()
    {
        var tokens = _lexer.Tokenize("\"hello world\"");

        Assert.Equal(AwslNodeKind.@string, tokens[0].Kind);
        Assert.Equal("hello world", tokens[0].Text);
    }

    /// <summary>
    ///     单引号字符串
    /// </summary>
    [Fact]
    public void Tokenize_SingleQuotedString_ShouldReturnStringToken()
    {
        var tokens = _lexer.Tokenize("'hello world'");

        Assert.Equal(AwslNodeKind.@string, tokens[0].Kind);
        Assert.Equal("hello world", tokens[0].Text);
    }

    /// <summary>
    ///     反引号模板字符串
    /// </summary>
    [Fact]
    public void Tokenize_TemplateLiteral_ShouldReturnStringToken()
    {
        var tokens = _lexer.Tokenize("`template literal`");

        Assert.Equal(AwslNodeKind.@string, tokens[0].Kind);
        Assert.Equal("template literal", tokens[0].Text);
    }

    /// <summary>
    ///     带转义字符的字符串
    /// </summary>
    [Fact]
    public void Tokenize_StringWithEscape_ShouldHandleEscapes()
    {
        var tokens = _lexer.Tokenize("\"hello\\nworld\\t!\"");

        Assert.Equal(AwslNodeKind.@string, tokens[0].Kind);
        Assert.Equal("hello\nworld\t!", tokens[0].Text);
    }

    #endregion

    #region 数字字面量测试

    /// <summary>
    ///     整数
    /// </summary>
    [Fact]
    public void Tokenize_Integer_ShouldReturnNumberToken()
    {
        var tokens = _lexer.Tokenize("42");

        Assert.Equal(AwslNodeKind.number, tokens[0].Kind);
        Assert.Equal("42", tokens[0].Text);
    }

    /// <summary>
    ///     浮点数
    /// </summary>
    [Fact]
    public void Tokenize_Float_ShouldReturnNumberToken()
    {
        var tokens = _lexer.Tokenize("3.14");

        Assert.Equal(AwslNodeKind.number, tokens[0].Kind);
        Assert.Equal("3.14", tokens[0].Text);
    }

    /// <summary>
    ///     十六进制数字
    /// </summary>
    [Fact]
    public void Tokenize_HexNumber_ShouldReturnNumberToken()
    {
        var tokens = _lexer.Tokenize("0xFF");

        Assert.Equal(AwslNodeKind.number, tokens[0].Kind);
        Assert.Equal("0xFF", tokens[0].Text);
    }

    #endregion

    #region 运算符测试

    /// <summary>
    ///     基本运算符
    /// </summary>
    [Fact]
    public void Tokenize_BasicOperators_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.Tokenize("+ - * / % = !");

        Assert.All(tokens.Take(6), t => Assert.Equal(AwslNodeKind.@operator, t.Kind));
    }

    /// <summary>
    ///     比较运算符
    /// </summary>
    [Fact]
    public void Tokenize_ComparisonOperators_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.Tokenize("== != < > <= >=");

        Assert.All(tokens.Take(6), t => Assert.Equal(AwslNodeKind.@operator, t.Kind));
        Assert.Equal("==", tokens[0].Text);
        Assert.Equal("!=", tokens[1].Text);
        Assert.Equal("<", tokens[2].Text);
        Assert.Equal(">", tokens[3].Text);
        Assert.Equal("<=", tokens[4].Text);
        Assert.Equal(">=", tokens[5].Text);
    }

    /// <summary>
    ///     复合赋值运算符
    /// </summary>
    [Fact]
    public void Tokenize_CompoundAssignmentOperators_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.Tokenize("+= -= *= /= %=");

        Assert.All(tokens.Take(5), t => Assert.Equal(AwslNodeKind.@operator, t.Kind));
    }

    /// <summary>
    ///     逻辑运算符
    /// </summary>
    [Fact]
    public void Tokenize_LogicalOperators_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.Tokenize("&& || !");

        Assert.All(tokens.Take(3), t => Assert.Equal(AwslNodeKind.@operator, t.Kind));
    }

    /// <summary>
    ///     箭头和 Lambda 运算符
    /// </summary>
    [Fact]
    public void Tokenize_ArrowOperators_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.Tokenize("=> ->");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("=>", tokens[0].Text);
        Assert.Equal(AwslNodeKind.@operator, tokens[1].Kind);
        Assert.Equal("->", tokens[1].Text);
    }

    /// <summary>
    ///     自增自减运算符
    /// </summary>
    [Fact]
    public void Tokenize_IncrementDecrement_ShouldReturnOperatorTokens()
    {
        var tokens = _lexer.Tokenize("++ --");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("++", tokens[0].Text);
        Assert.Equal(AwslNodeKind.@operator, tokens[1].Kind);
        Assert.Equal("--", tokens[1].Text);
    }

    /// <summary>
    ///     </运算符（标签关闭起始符）
    /// </summary>
    [Fact]
    public void Tokenize_TagCloseStart_ShouldReturnOperatorToken()
    {
        var tokens = _lexer.Tokenize("</");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("</", tokens[0].Text);
    }

    /// <summary>
    ///     /> 运算符（自闭合标签）
    /// </summary>
    [Fact]
    public void Tokenize_SelfCloseTag_ShouldReturnOperatorToken()
    {
        var tokens = _lexer.Tokenize("/>");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("/>", tokens[0].Text);
    }

    #endregion

    #region 标点符号测试

    /// <summary>
    ///     冒号和双冒号
    /// </summary>
    [Fact]
    public void Tokenize_ColonAndDoubleColon_ShouldReturnPunctuationTokens()
    {
        var tokens = _lexer.Tokenize(": ::");

        Assert.Equal(AwslNodeKind.punctuation, tokens[0].Kind);
        Assert.Equal(":", tokens[0].Text);
        Assert.Equal(AwslNodeKind.punctuation, tokens[1].Kind);
        Assert.Equal("::", tokens[1].Text);
    }

    /// <summary>
    ///     分号
    /// </summary>
    [Fact]
    public void Tokenize_Semicolon_ShouldReturnPunctuationToken()
    {
        var tokens = _lexer.Tokenize(";");

        Assert.Equal(AwslNodeKind.punctuation, tokens[0].Kind);
        Assert.Equal(";", tokens[0].Text);
    }

    #endregion

    #region 注释测试

    /// <summary>
    ///     HTML 注释应被跳过
    /// </summary>
    [Fact]
    public void Tokenize_HtmlComment_ShouldBeSkipped()
    {
        var tokens = _lexer.Tokenize("<!-- comment -->let");

        Assert.Equal(AwslNodeKind.keyword, tokens[0].Kind);
        Assert.Equal("let", tokens[0].Text);
    }

    /// <summary>
    ///     行注释应被跳过
    /// </summary>
    [Fact]
    public void Tokenize_LineComment_ShouldBeSkipped()
    {
        var tokens = _lexer.Tokenize("// this is a comment\nlet");

        Assert.Equal(AwslNodeKind.keyword, tokens[0].Kind);
        Assert.Equal("let", tokens[0].Text);
    }

    /// <summary>
    ///     块注释应被跳过
    /// </summary>
    [Fact]
    public void Tokenize_BlockComment_ShouldBeSkipped()
    {
        var tokens = _lexer.Tokenize("/* this is a block comment */let");

        Assert.Equal(AwslNodeKind.keyword, tokens[0].Kind);
        Assert.Equal("let", tokens[0].Text);
    }

    /// <summary>
    ///     嵌套块注释应被正确跳过
    /// </summary>
    [Fact]
    public void Tokenize_NestedBlockComment_ShouldBeSkipped()
    {
        var tokens = _lexer.Tokenize("/* outer /* inner */ outer */let");

        Assert.Equal(AwslNodeKind.keyword, tokens[0].Kind);
        Assert.Equal("let", tokens[0].Text);
    }

    #endregion

    #region @ 前缀测试

    /// <summary>
    ///     @click 事件绑定前缀
    /// </summary>
    [Fact]
    public void Tokenize_AtClick_ShouldReturnAtPrefixToken()
    {
        var tokens = _lexer.Tokenize("@click");

        Assert.Equal(AwslNodeKind.at_prefix, tokens[0].Kind);
        Assert.Equal("click", tokens[0].Text);
    }

    /// <summary>
    ///     @bind 响应式绑定前缀
    /// </summary>
    [Fact]
    public void Tokenize_AtBind_ShouldReturnAtPrefixToken()
    {
        var tokens = _lexer.Tokenize("@bind");

        Assert.Equal(AwslNodeKind.at_prefix, tokens[0].Kind);
        Assert.Equal("bind", tokens[0].Text);
    }

    /// <summary>
    ///     @input 事件前缀
    /// </summary>
    [Fact]
    public void Tokenize_AtInput_ShouldReturnAtPrefixToken()
    {
        var tokens = _lexer.Tokenize("@input");

        Assert.Equal(AwslNodeKind.at_prefix, tokens[0].Kind);
        Assert.Equal("input", tokens[0].Text);
    }

    /// <summary>
    ///     单独的 @ 字符作运算符
    /// </summary>
    [Fact]
    public void Tokenize_StandaloneAt_ShouldReturnOperatorToken()
    {
        var tokens = _lexer.Tokenize("@ ");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("@", tokens[0].Text);
    }

    #endregion

    #region 模板块测试

    /// <summary>
    ///     完整 template 块 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_TemplateBlock_ShouldTokenizeCorrectly()
    {
        var source = "<template><VStack><Text>Hello</Text></VStack></template>";
        var tokens = _lexer.Tokenize(source);

        var kinds = tokens.Select(t => t.Kind).ToList();
        Assert.Contains(AwslNodeKind.@operator, kinds);
        Assert.Contains(AwslNodeKind.identifier, kinds);
        Assert.Contains(AwslNodeKind.eof, kinds);
    }

    /// <summary>
    ///     Widget 块 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_WidgetBlock_ShouldTokenizeCorrectly()
    {
        var source = @"
<widget>
    <VStack>
        <Text>{label}: {count}</Text>
    </VStack>
</widget>";

        var tokens = _lexer.Tokenize(source);

        Assert.NotEmpty(tokens);
        Assert.Contains(tokens, t => t.Text == "VStack");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.delimiter && t.Text == "{");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.delimiter && t.Text == "}");
    }

    /// <summary>
    ///     Script 块 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_ScriptBlock_ShouldTokenizePropertyDeclarations()
    {
        var source = @"
<script>
    let name: string = ""Alice""
    let age: number = 30
</script>";

        var tokens = _lexer.Tokenize(source);

        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.keyword && t.Text == "let");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "name");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "age");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.@string && t.Text == "Alice");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.number && t.Text == "30");
    }

    /// <summary>
    ///     Style 块 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_StyleBlock_ShouldTokenizeCssClasses()
    {
        var source = @"
<style>
    .container {
        padding: 10px;
    }
</style>";

        var tokens = _lexer.Tokenize(source);

        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "container");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.number && t.Text == "10");
    }

    #endregion

    #region 完整组件测试

    /// <summary>
    ///     完整单文件组件 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_FullComponent_ShouldTokenizeAllSections()
    {
        var source = @"
<script>
    let count: i32 = 0
    let label: string = ""Click me""
</script>

<widget>
    <VStack>
        <Text>{label}: {count}</Text>
        <Button onClick={handleClick}>{label}</Button>
    </VStack>
</widget>

<style>
    .container { padding: 10px; }
</style>";

        var tokens = _lexer.Tokenize(source);

        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.keyword && t.Text == "let");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.keyword && t.Text == "widget");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "count");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "label");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "VStack");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "Text");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "Button");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.number && t.Text == "0");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.@string && t.Text == "Click me");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.eof);
    }

    /// <summary>
    ///     使用 const 进行只读声明
    /// </summary>
    [Fact]
    public void Tokenize_ConstDeclaration_ShouldReturnKeywordToken()
    {
        var tokens = _lexer.Tokenize("const max: i32 = 100");

        Assert.Equal(AwslNodeKind.keyword, tokens[0].Kind);
        Assert.Equal("const", tokens[0].Text);
    }

    /// <summary>
    ///     import 语句 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_ImportStatement_ShouldTokenizeCorrectly()
    {
        var tokens = _lexer.Tokenize("import { Button } from \"ui\"");

        Assert.Equal(AwslNodeKind.keyword, tokens[0].Kind);
        Assert.Equal("import", tokens[0].Text);
        Assert.Equal(AwslNodeKind.identifier, tokens[2].Kind);
        Assert.Equal("Button", tokens[2].Text);
    }

    /// <summary>
    ///     loop 指令 Token 化
    /// </summary>
    [Fact]
    public void Tokenize_LoopDirective_ShouldTokenizeCorrectly()
    {
        var tokens = _lexer.Tokenize("<loop item in {items}>");

        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.identifier && t.Text == "loop");
        Assert.Contains(tokens, t => t.Kind == AwslNodeKind.keyword && t.Text == "in");
    }

    #endregion

    #region 回归测试

    /// <summary>
    ///     source 中以 < 开头时不会被误判为 HTML 注释
    /// </summary>
    [Fact]
    public void Tokenize_TagOpenNotComment_ShouldReturnTagOpen()
    {
        var tokens = _lexer.Tokenize("<VStack>");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("<", tokens[0].Text);
        Assert.Equal(AwslNodeKind.identifier, tokens[1].Kind);
        Assert.Equal("VStack", tokens[1].Text);
        Assert.Equal(AwslNodeKind.@operator, tokens[2].Kind);
        Assert.Equal(">", tokens[2].Text);
    }

    /// <summary>
    ///     source 中 ! 开头的不是 HTML 注释（单字 !）
    /// </summary>
    [Fact]
    public void Tokenize_Bang_ShouldNotBeConfusedWithHtmlComment()
    {
        var tokens = _lexer.Tokenize("!flag");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("!", tokens[0].Text);
    }

    /// <summary>
    ///     解析以 @ 与非标识符开头的 Token（如 `@""`）
    /// </summary>
    [Fact]
    public void Tokenize_AtWithString_ShouldTokenizeCorrectly()
    {
        var tokens = _lexer.Tokenize("@\"hello\"");

        Assert.Equal(AwslNodeKind.@operator, tokens[0].Kind);
        Assert.Equal("@", tokens[0].Text);
        Assert.Equal(AwslNodeKind.@string, tokens[1].Kind);
        Assert.Equal("hello", tokens[1].Text);
    }

    #endregion
}