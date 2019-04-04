using Oak.Typescript.Lexer;

namespace Oak.Typescript.Tests;


/// <summary>

///     TsLexer 词法分析器单元测试


/// </summary>
public sealed class TsLexerTests
{
    #region 边界测试

    [Fact]
    public void Tokenize_EmptyInput_ShouldReturnOnlyEof()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("");

        Assert.Single(tokens);
        Assert.Equal(TsTokenType.Eof, tokens[0].type);
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_ShouldReturnOnlyEof()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("   \t  \n  ");

        Assert.Single(tokens);
        Assert.Equal(TsTokenType.Eof, tokens[0].type);
    }

    #endregion

    #region 标识符测试

    [Fact]
    public void Tokenize_SimpleIdentifier_ShouldReturnIdentifier()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("hello");

        Assert.Equal(TsTokenType.Identifier, tokens[0].type);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_DollarIdentifier_ShouldReturnIdentifier()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("$myVar");

        Assert.Equal(TsTokenType.Identifier, tokens[0].type);
        Assert.Equal("$myVar", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_UnderscoreIdentifier_ShouldReturnIdentifier()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("_private");

        Assert.Equal(TsTokenType.Identifier, tokens[0].type);
        Assert.Equal("_private", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_AlphanumericIdentifier_ShouldReturnIdentifier()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("var123");

        Assert.Equal(TsTokenType.Identifier, tokens[0].type);
        Assert.Equal("var123", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_DollarOnly_ShouldReturnIdentifier()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("$");

        Assert.Equal(TsTokenType.Identifier, tokens[0].type);
        Assert.Equal("$", tokens[0].Value);
    }

    #endregion

    #region 关键字测试

    [Fact]
    public void Tokenize_FunctionKeyword_ShouldReturnKeyword()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("function");

        Assert.Equal(TsTokenType.Keyword, tokens[0].type);
        Assert.Equal("function", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_ClassKeyword_ShouldReturnKeyword()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("class");

        Assert.Equal(TsTokenType.Keyword, tokens[0].type);
        Assert.Equal("class", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_ConstKeyword_ShouldReturnKeyword()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("const");

        Assert.Equal(TsTokenType.Keyword, tokens[0].type);
        Assert.Equal("const", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_LetKeyword_ShouldReturnKeyword()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("let");

        Assert.Equal(TsTokenType.Keyword, tokens[0].type);
        Assert.Equal("let", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_VarKeyword_ShouldReturnKeyword()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("var");

        Assert.Equal(TsTokenType.Keyword, tokens[0].type);
        Assert.Equal("var", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_TrueLiteral_ShouldReturnLiteral()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("true");

        Assert.Equal(TsTokenType.Literal, tokens[0].type);
        Assert.Equal("true", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_FalseLiteral_ShouldReturnLiteral()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("false");

        Assert.Equal(TsTokenType.Literal, tokens[0].type);
        Assert.Equal("false", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_NullLiteral_ShouldReturnLiteral()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("null");

        Assert.Equal(TsTokenType.Literal, tokens[0].type);
        Assert.Equal("null", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_MultipleKeywords_ShouldAllBeRecognized()
    {
        var lexer = new TsLexer();
        var keywords = new[] { "if", "else", "for", "while", "do", "switch", "case",
            "break", "continue", "return", "try", "catch", "finally", "throw",
            "new", "typeof", "instanceof", "async", "await", "yield",
            "import", "export", "interface", "type", "enum", "namespace" };

        foreach (var kw in keywords)
        {
            var tokens = lexer.Tokenize(kw);
            Assert.Equal(TsTokenType.Keyword, tokens[0].type);
            Assert.Equal(kw, tokens[0].Value);
        }
    }

    #endregion

    #region 数字测试

    [Fact]
    public void Tokenize_DecimalInteger_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("42");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Zero_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("0", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_HexInteger_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0xFF");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("0xFF", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_HexLowercase_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0x1a2b");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("0x1a2b", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_BinaryInteger_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0b1010");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("0b1010", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_BinaryUppercase_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0B1100");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("0b1100", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_OctalInteger_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0o777");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("0o777", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_OctalUppercase_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0O123");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("0o123", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Float_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("3.14");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("3.14", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_ScientificNotation_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("1e10");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("1e10", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_ScientificNotationWithPlus_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("1.5e+3");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("1.5e+3", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_ScientificNotationWithMinus_ShouldReturnNumber()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("2e-5");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("2e-5", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_BigIntSuffix_ShouldReturnBigInt()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("123n");

        Assert.Equal(TsTokenType.BigInt, tokens[0].type);
        Assert.Equal("123n", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_HexBigInt_ShouldReturnBigInt()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0xFFn");

        Assert.Equal(TsTokenType.BigInt, tokens[0].type);
        Assert.Equal("0xFFn", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_BinaryBigInt_ShouldReturnBigInt()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0b1010n");

        Assert.Equal(TsTokenType.BigInt, tokens[0].type);
        Assert.Equal("0b1010n", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_OctalBigInt_ShouldReturnBigInt()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("0o777n");

        Assert.Equal(TsTokenType.BigInt, tokens[0].type);
        Assert.Equal("0o777n", tokens[0].Value);
    }

    #endregion

    #region 字符串测试

    [Fact]
    public void Tokenize_DoubleQuotedString_ShouldReturnString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("\"hello\"");

        Assert.Equal(TsTokenType.String, tokens[0].type);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_SingleQuotedString_ShouldReturnString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("'hello'");

        Assert.Equal(TsTokenType.String, tokens[0].type);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_EmptyString_ShouldReturnString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("\"\"");

        Assert.Equal(TsTokenType.String, tokens[0].type);
        Assert.Equal("", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StringWithNewlineEscape_ShouldReturnString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("\"hello\\nworld\"");

        Assert.Equal(TsTokenType.String, tokens[0].type);
        Assert.Equal("hello\nworld", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StringWithTabEscape_ShouldReturnString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("\"tab\\there\"");

        Assert.Equal(TsTokenType.String, tokens[0].type);
        Assert.Equal("tab\there", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StringWithBackslashEscape_ShouldReturnString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("\"path\\\\file\"");

        Assert.Equal(TsTokenType.String, tokens[0].type);
        Assert.Equal("path\\file", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StringWithQuoteEscape_ShouldReturnString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("\"say \\\"hi\\\"\"");

        Assert.Equal(TsTokenType.String, tokens[0].type);
        Assert.Equal("say \"hi\"", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_TemplateString_ShouldReturnTemplateString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("`hello`");

        Assert.Equal(TsTokenType.TemplateString, tokens[0].type);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_TemplateStringWithEscape_ShouldReturnTemplateString()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("`line1\\nline2`");

        Assert.Equal(TsTokenType.TemplateString, tokens[0].type);
        Assert.Equal("line1\nline2", tokens[0].Value);
    }

    #endregion

    #region 运算符测试

    [Fact]
    public void Tokenize_AssignmentOperator_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("=");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("=", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StrictEquality_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("===");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("===", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StrictInequality_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("!==");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("!==", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_ArrowOperator_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("=>");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("=>", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_NullishCoalescing_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("??");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("??", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_OptionalChaining_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("?.");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("?.", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_LogicalAnd_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("&&");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("&&", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_LogicalOr_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("||");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal("||", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_UnsignedRightShift_ShouldReturnOperator()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize(">>>");

        Assert.Equal(TsTokenType.Operator, tokens[0].type);
        Assert.Equal(">>>", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_CompoundAssignmentOperators_ShouldAllBeRecognized()
    {
        var lexer = new TsLexer();
        var operators = new[] { "+=", "-=", "*=", "/=", "%=", "**=",
            "&=", "|=", "^=", "<<=", ">>=", ">>>=", "??=" };

        foreach (var op in operators)
        {
            var tokens = lexer.Tokenize(op);
            Assert.Equal(TsTokenType.Operator, tokens[0].type);
            Assert.Equal(op, tokens[0].Value);
        }
    }

    [Fact]
    public void Tokenize_Colon_ShouldReturnPunctuation()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize(":");

        Assert.Equal(TsTokenType.Punctuation, tokens[0].type);
        Assert.Equal(":", tokens[0].Value);
    }

    #endregion

    #region 分隔符测试

    [Fact]
    public void Tokenize_Semicolon_ShouldReturnDelimiter()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize(";");

        Assert.Equal(TsTokenType.Delimiter, tokens[0].type);
        Assert.Equal(";", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Parentheses_ShouldReturnDelimiters()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("()");

        Assert.Equal(TsTokenType.Delimiter, tokens[0].type);
        Assert.Equal("(", tokens[0].Value);
        Assert.Equal(TsTokenType.Delimiter, tokens[1].type);
        Assert.Equal(")", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_Braces_ShouldReturnDelimiters()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("{}");

        Assert.Equal(TsTokenType.Delimiter, tokens[0].type);
        Assert.Equal("{", tokens[0].Value);
        Assert.Equal(TsTokenType.Delimiter, tokens[1].type);
        Assert.Equal("}", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_Brackets_ShouldReturnDelimiters()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("[]");

        Assert.Equal(TsTokenType.Delimiter, tokens[0].type);
        Assert.Equal("[", tokens[0].Value);
        Assert.Equal(TsTokenType.Delimiter, tokens[1].type);
        Assert.Equal("]", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_Comma_ShouldReturnDelimiter()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize(",");

        Assert.Equal(TsTokenType.Delimiter, tokens[0].type);
        Assert.Equal(",", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Dot_ShouldReturnDelimiter()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize(".");

        Assert.Equal(TsTokenType.Delimiter, tokens[0].type);
        Assert.Equal(".", tokens[0].Value);
    }

    #endregion

    #region 注释测试

    [Fact]
    public void Tokenize_LineComment_ShouldBeSkipped()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("// this is a comment\n42");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_BlockComment_ShouldBeSkipped()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("/* comment */ 42");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_NestedBlockComment_ShouldBeSkipped()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("/* outer /* inner */ outer */ 42");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_MultilineBlockComment_ShouldBeSkipped()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("/* line1\nline2\nline3 */ 42");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Value);
    }

    #endregion

    #region Hashbang 注释测试

    [Fact]
    public void Tokenize_HashbangComment_ShouldBeSkipped()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("#!/usr/bin/env node\n42");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_HashbangAtNonStart_ShouldNotBeHashbang()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("42 #!");

        Assert.Equal(TsTokenType.Number, tokens[0].type);
        Assert.Equal("42", tokens[0].Value);
    }

    #endregion

    #region 位置追踪测试

    [Fact]
    public void Tokenize_FirstToken_ShouldStartAtLine1Column1()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("hello");

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
    }

    [Fact]
    public void Tokenize_SecondLine_ShouldTrackLineNumbers()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("hello\nworld");

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal("hello", tokens[0].Value);
        Assert.Equal(2, tokens[1].Line);
        Assert.Equal("world", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_ColumnTracking_ShouldResetAfterNewline()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("ab\ncd");

        Assert.Equal(1, tokens[0].Column);
        Assert.Equal(1, tokens[1].Column);
    }

    [Fact]
    public void Tokenize_ColumnIncrement_ShouldTrackCorrectly()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("a b");

        Assert.Equal(1, tokens[0].Column);
        Assert.Equal(3, tokens[1].Column);
    }

    [Fact]
    public void Tokenize_EofPosition_ShouldBeAfterLastToken()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("hello");

        var eof = tokens[^1];
        Assert.Equal(TsTokenType.Eof, eof.type);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void Tokenize_VariableDeclaration_ShouldReturnCorrectTokens()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("const x = 42;");

        Assert.Equal(TsTokenType.Keyword, tokens[0].type);
        Assert.Equal("const", tokens[0].Value);
        Assert.Equal(TsTokenType.Identifier, tokens[1].type);
        Assert.Equal("x", tokens[1].Value);
        Assert.Equal(TsTokenType.Operator, tokens[2].type);
        Assert.Equal("=", tokens[2].Value);
        Assert.Equal(TsTokenType.Number, tokens[3].type);
        Assert.Equal("42", tokens[3].Value);
        Assert.Equal(TsTokenType.Delimiter, tokens[4].type);
        Assert.Equal(";", tokens[4].Value);
    }

    [Fact]
    public void Tokenize_FunctionDeclaration_ShouldReturnCorrectTokens()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("function add(a, b) { return a + b; }");

        Assert.Equal(TsTokenType.Keyword, tokens[0].type);
        Assert.Equal("function", tokens[0].Value);
        Assert.Equal(TsTokenType.Identifier, tokens[1].type);
        Assert.Equal("add", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_ArrowFunction_ShouldReturnCorrectTokens()
    {
        var lexer = new TsLexer();
        var tokens = lexer.Tokenize("(x) => x + 1");

        Assert.Equal(TsTokenType.Delimiter, tokens[0].type);
        Assert.Equal("(", tokens[0].Value);
        Assert.Equal(TsTokenType.Identifier, tokens[1].type);
        Assert.Equal("x", tokens[1].Value);
        Assert.Equal(TsTokenType.Delimiter, tokens[2].type);
        Assert.Equal(")", tokens[2].Value);
        Assert.Equal(TsTokenType.Operator, tokens[3].type);
        Assert.Equal("=>", tokens[3].Value);
    }

    #endregion
}
