using Oak.Diagnostics;

namespace Oak.Javap.Tests;

public class JvpLexerTests
{
    #region 边界测试

    [Fact]
    public void Tokenize_EmptyInput_ShouldReturnOnlyEof()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("");

        Assert.Single(tokens);
        Assert.Equal(JvpTokenType.Eof, tokens[0].type);
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ShouldReturnOnlyEof()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("  \t\n  ");

        Assert.Single(tokens);
        Assert.Equal(JvpTokenType.Eof, tokens[0].type);
    }

    [Fact]
    public void Tokenize_UnknownCharacter_ShouldNotCrash()
    {
        var lexer = new JvpLexer();
        var diagnostics = new DiagnosticSink();
        var tokens = lexer.Tokenize("@", diagnostics);

        Assert.NotNull(tokens);
    }

    [Fact]
    public void Tokenize_LineTracking_ShouldBeCorrect()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("iconst_0\nistore_1");

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(2, tokens[1].Line);
    }

    [Fact]
    public void Tokenize_ColumnTracking_ShouldStartAtOne()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("iconst_0");

        Assert.Equal(1, tokens[0].Column);
    }

    #endregion

    #region 标点符号测试

    [Fact]
    public void Tokenize_OpenBrace_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("{");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
        Assert.Equal("{", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_CloseBrace_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("}");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
    }

    [Fact]
    public void Tokenize_OpenParen_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("(");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
    }

    [Fact]
    public void Tokenize_CloseParen_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize(")");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Colon_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize(":");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Semicolon_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize(";");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Comma_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize(",");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Dot_ShouldReturnPunctuation()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize(".");

        Assert.Equal(JvpTokenType.Punctuation, tokens[0].type);
    }

    #endregion

    #region 常量池引用测试

    [Fact]
    public void Tokenize_ConstantPoolRef_ShouldReturnConstantPoolRef()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("#1");

        Assert.Equal(JvpTokenType.ConstantPoolRef, tokens[0].type);
        Assert.Equal("#1", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_LargeConstantPoolRef_ShouldReturnConstantPoolRef()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("#999");

        Assert.Equal(JvpTokenType.ConstantPoolRef, tokens[0].type);
    }

    #endregion

    #region 访问修饰符测试

    [Fact]
    public void Tokenize_Public_ShouldReturnAccessModifier()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("public");

        Assert.Equal(JvpTokenType.AccessModifier, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Static_ShouldReturnAccessModifier()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("static");

        Assert.Equal(JvpTokenType.AccessModifier, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Final_ShouldReturnAccessModifier()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("final");

        Assert.Equal(JvpTokenType.AccessModifier, tokens[0].type);
    }

    #endregion

    #region 类型关键字测试

    [Fact]
    public void Tokenize_Class_ShouldReturnTypeKeyword()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("class");

        Assert.Equal(JvpTokenType.TypeKeyword, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Interface_ShouldReturnTypeKeyword()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("interface");

        Assert.Equal(JvpTokenType.TypeKeyword, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Enum_ShouldReturnTypeKeyword()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("enum");

        Assert.Equal(JvpTokenType.TypeKeyword, tokens[0].type);
    }

    #endregion

    #region 操作码测试

    [Fact]
    public void Tokenize_Iconst_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("iconst_0");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Istore_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("istore_1");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Ireturn_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("ireturn");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Return_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("return");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Iload_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("iload_0");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Invokevirtual_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("invokevirtual");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_New_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("new");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Dup_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("dup");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Iadd_ShouldReturnOpcode()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("iadd");

        Assert.Equal(JvpTokenType.Opcode, tokens[0].type);
    }

    #endregion

    #region 数字测试

    [Fact]
    public void Tokenize_PositiveInteger_ShouldReturnNumber()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("42");

        Assert.Equal(JvpTokenType.Number, tokens[0].type);
    }

    [Fact]
    public void Tokenize_NegativeInteger_ShouldReturnNumber()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("-3");

        Assert.Equal(JvpTokenType.Number, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Zero_ShouldReturnNumber()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("0");

        Assert.Equal(JvpTokenType.Number, tokens[0].type);
    }

    #endregion

    #region 注释测试

    [Fact]
    public void Tokenize_Comment_ShouldReturnComment()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("// a comment\n");

        Assert.Equal(JvpTokenType.Comment, tokens[0].type);
    }

    #endregion

    #region 特殊关键字测试

    [Fact]
    public void Tokenize_Compiled_ShouldReturnHeaderKeyword()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("Compiled");

        Assert.Equal(JvpTokenType.HeaderKeyword, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Code_ShouldReturnSectionMarker()
    {
        var lexer = new JvpLexer();
        var tokens = lexer.Tokenize("Code");

        Assert.Equal(JvpTokenType.SectionMarker, tokens[0].type);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void Tokenize_SimpleClass_ShouldReturnCorrectTokens()
    {
        var lexer = new JvpLexer();
        var source = "Compiled from \"Test.java\"\npublic class Test {\n  public Test();\n    Code:\n       0: aload_0\n       1: invokespecial #1\n       4: return\n}";
        var tokens = lexer.Tokenize(source);

        Assert.NotEmpty(tokens);
        var types = tokens.Select(t => t.type).Distinct().ToList();
        Assert.Contains(JvpTokenType.HeaderKeyword, types);
        Assert.Contains(JvpTokenType.AccessModifier, types);
        Assert.Contains(JvpTokenType.TypeKeyword, types);
        Assert.Contains(JvpTokenType.Punctuation, types);
        Assert.Contains(JvpTokenType.Identifier, types);
        Assert.Contains(JvpTokenType.SectionMarker, types);
        Assert.Contains(JvpTokenType.Number, types);
        Assert.Contains(JvpTokenType.Opcode, types);
        Assert.Contains(JvpTokenType.ConstantPoolRef, types);
    }

    [Fact]
    public void Tokenize_OpcodeWithOffset_ShouldParseCorrectly()
    {
        var lexer = new JvpLexer();
        var source = "      0: iconst_0\n      1: istore_1\n      2: return";
        var tokens = lexer.Tokenize(source);

        Assert.Contains(tokens, t => t.Value == "0");
        Assert.Contains(tokens, t => t.Value == "iconst_0");
        Assert.Contains(tokens, t => t.Value == "1");
        Assert.Contains(tokens, t => t.Value == "return");
    }

    [Fact]
    public void Tokenize_TokenCount_ShouldBeReasonable()
    {
        var lexer = new JvpLexer();
        var source = "public class Test {\n  public Test();\n    Code:\n       0: return\n}";
        var tokens = lexer.Tokenize(source);

        Assert.True(tokens.Count >= 15);
    }

    #endregion
}
