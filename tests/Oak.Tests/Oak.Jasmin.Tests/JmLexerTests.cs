using Oak.Diagnostics;

namespace Oak.Jasmin.Tests;

public class JmLexerTests
{
    #region 边界测试

    [Fact]
    public void Tokenize_EmptyInput_ShouldReturnOnlyEof()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("");

        Assert.Single(tokens);
        Assert.Equal(JmTokenType.Eof, tokens[0].type);
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ShouldReturnOnlyEof()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("  \t\n ");

        Assert.Single(tokens);
        Assert.Equal(JmTokenType.Eof, tokens[0].type);
    }

    [Fact]
    public void Tokenize_UnknownCharacter_ShouldNotCrash()
    {
        var lexer = new JmLexer();
        var diagnostics = new DiagnosticSink();
        var tokens = lexer.Tokenize("@", diagnostics);

        Assert.NotNull(tokens);
    }

    [Fact]
    public void Tokenize_LineTracking_ShouldStartAtOne()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("push\npop");

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(2, tokens[1].Line);
    }

    [Fact]
    public void Tokenize_ColumnTracking_ShouldStartAtOne()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("push");

        Assert.Equal(1, tokens[0].Column);
    }

    #endregion

    #region 指令测试

    [Fact]
    public void Tokenize_ClassDirective_ShouldReturnDirective()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize(".class");

        Assert.Equal(JmTokenType.Directive, tokens[0].type);
        Assert.Equal("class", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_SuperDirective_ShouldReturnDirective()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize(".super");

        Assert.Equal(JmTokenType.Directive, tokens[0].type);
        Assert.Equal("super", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_MethodDirective_ShouldReturnDirective()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize(".method");

        Assert.Equal(JmTokenType.Directive, tokens[0].type);
        Assert.Equal("method", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_EndDirective_ShouldReturnDirective()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize(".end");

        Assert.Equal(JmTokenType.Directive, tokens[0].type);
    }

    [Fact]
    public void Tokenize_LimitDirective_ShouldReturnDirective()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize(".limit");

        Assert.Equal(JmTokenType.Directive, tokens[0].type);
    }

    [Fact]
    public void Tokenize_AllDirectives_ShouldBeRecognized()
    {
        var lexer = new JmLexer();
        var directives = new[] { ".class", ".super", ".method", ".end", ".limit",
            ".line", ".var", ".throws", ".catch", ".source", ".version", ".field",
            ".implements", ".interface", ".attribute", ".debug" };

        foreach (var d in directives)
        {
            var tokens = lexer.Tokenize(d);
            Assert.Equal(JmTokenType.Directive, tokens[0].type);
        }
    }

    #endregion

    #region 访问修饰符测试

    [Fact]
    public void Tokenize_Public_ShouldReturnAccessModifier()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("public");

        Assert.Equal(JmTokenType.AccessModifier, tokens[0].type);
        Assert.Equal("public", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Private_ShouldReturnAccessModifier()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("private");

        Assert.Equal(JmTokenType.AccessModifier, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Static_ShouldReturnAccessModifier()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("static");

        Assert.Equal(JmTokenType.AccessModifier, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Final_ShouldReturnAccessModifier()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("final");

        Assert.Equal(JmTokenType.AccessModifier, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Abstract_ShouldReturnAccessModifier()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("abstract");

        Assert.Equal(JmTokenType.AccessModifier, tokens[0].type);
    }

    #endregion

    #region 操作码测试

    [Fact]
    public void Tokenize_Aload_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("aload");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Iload_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("iload");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Istore_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("istore");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Invokevirtual_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("invokevirtual");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Return_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("return");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Getstatic_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("getstatic");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Ldc_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("ldc");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_New_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("new");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Dup_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("dup");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Iadd_ShouldReturnOpcode()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("iadd");

        Assert.Equal(JmTokenType.Opcode, tokens[0].type);
    }

    #endregion

    #region 描述符测试

    [Fact]
    public void Tokenize_ObjectDescriptor_ShouldReturnDescriptor()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("Ljava/lang/Object;");

        Assert.Equal(JmTokenType.Descriptor, tokens[0].type);
    }

    [Fact]
    public void Tokenize_ArrayDescriptor_ShouldReturnDescriptor()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("[I");

        Assert.Equal(JmTokenType.Descriptor, tokens[0].type);
    }

    [Fact]
    public void Tokenize_VoidDescriptor_ShouldReturnDescriptor()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("V");

        Assert.Equal(JmTokenType.Descriptor, tokens[0].type);
    }

    [Fact]
    public void Tokenize_IntDescriptor_ShouldReturnDescriptor()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("I");

        Assert.Equal(JmTokenType.Descriptor, tokens[0].type);
    }

    #endregion

    #region 标签测试

    [Fact]
    public void Tokenize_LabelSuffix_ShouldReturnLabel()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("loop_start:");

        Assert.Equal(JmTokenType.Label, tokens[0].type);
        Assert.Equal("loop_start", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_SimpleLabel_ShouldReturnLabel()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("Label1:");

        Assert.Equal(JmTokenType.Label, tokens[0].type);
        Assert.Equal("Label1", tokens[0].Value);
    }

    #endregion

    #region 数字测试

    [Fact]
    public void Tokenize_PositiveInteger_ShouldReturnNumber()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("100");

        Assert.Equal(JmTokenType.Number, tokens[0].type);
        Assert.Equal("100", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_NegativeInteger_ShouldReturnNumber()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("-5");

        Assert.Equal(JmTokenType.Number, tokens[0].type);
    }

    [Fact]
    public void Tokenize_Zero_ShouldReturnNumber()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("0");

        Assert.Equal(JmTokenType.Number, tokens[0].type);
    }

    #endregion

    #region 字符串测试

    [Fact]
    public void Tokenize_StringLiteral_ShouldReturnStringLiteral()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("\"hello\"");

        Assert.Equal(JmTokenType.StringLiteral, tokens[0].type);
    }

    [Fact]
    public void Tokenize_StringWithEscape_ShouldReturnStringLiteral()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("\"hello\\nworld\"");

        Assert.Equal(JmTokenType.StringLiteral, tokens[0].type);
    }

    #endregion

    #region 注释测试

    [Fact]
    public void Tokenize_Comment_ShouldReturnComment()
    {
        var lexer = new JmLexer();
        var tokens = lexer.Tokenize("; this is a comment\n");

        Assert.Equal(JmTokenType.Comment, tokens[0].type);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void Tokenize_MethodDefinition_ShouldReturnCorrectTokens()
    {
        var lexer = new JmLexer();
        var source = ".method public static main([Ljava/lang/String;)V\n  .limit stack 2\n  return\n.end method";
        var tokens = lexer.Tokenize(source);

        Assert.NotEmpty(tokens);
        var types = tokens.Select(t => t.type).Distinct().ToList();
        Assert.Contains(JmTokenType.Directive, types);
        Assert.Contains(JmTokenType.AccessModifier, types);
        Assert.Contains(JmTokenType.Identifier, types);
        Assert.Contains(JmTokenType.Descriptor, types);
        Assert.Contains(JmTokenType.Number, types);
        Assert.Contains(JmTokenType.Opcode, types);
    }

    [Fact]
    public void Tokenize_ClassDefinition_ShouldReturnCorrectTokens()
    {
        var lexer = new JmLexer();
        var source = ".class public HelloWorld\n.super java/lang/Object\n\n.method public <init>()V\n  aload_0\n  invokespecial java/lang/Object/<init>()V\n  return\n.end method";
        var tokens = lexer.Tokenize(source);

        Assert.NotEmpty(tokens);
        Assert.Contains(tokens, t => t.type == JmTokenType.Directive && t.Value == "class");
        Assert.Contains(tokens, t => t.type == JmTokenType.Directive && t.Value == "super");
        Assert.Contains(tokens, t => t.Value == "HelloWorld");
        Assert.Contains(tokens, t => t.Value == "java/lang/Object");
    }

    [Fact]
    public void Tokenize_TokenCount_ShouldBeReasonable()
    {
        var lexer = new JmLexer();
        var source = ".class public Test\n.super java/lang/Object\n.method test()I\n  iconst_0\n  ireturn\n.end method";
        var tokens = lexer.Tokenize(source);

        Assert.True(tokens.Count >= 20);
    }

    #endregion
}
