using Std.Data.Text.OCaml.AST;
using Std.Data.Text.OCaml.Lexer;

namespace Std.Data.Text.OCaml.Parser;

public sealed class OcParser : IParser<IReadOnlyList<OcToken>, OcAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<OcToken> _tokens = [];

    public OcParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public OcAstNode Parse(IReadOnlyList<OcToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var declarations = new List<OcAstNode>();

        while (!IsAtEnd())
        {
            Match(OcTokenType.Operator, ";;");

            if (IsAtEnd())
            {
                break;
            }

            var decl = ParseDeclaration();
            if (decl is not null)
            {
                declarations.Add(decl);
            }
        }

        return new OcModule("Main", declarations);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().type == OcTokenType.Eof;
    }

    private OcToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private OcToken Previous()
    {
        return _tokens[_current - 1];
    }

    private OcToken Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool Check(OcTokenType type)
    {
        return !IsAtEnd() && Peek().type == type;
    }

    private bool Check(OcTokenType type, string value)
    {
        return !IsAtEnd() && Peek().type == type && Peek().Value == value;
    }

    private bool Match(OcTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(OcTokenType type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private OcToken Consume(OcTokenType type, string errorCode, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private OcToken ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(OcTokenType.Keyword, keyword))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().type == OcTokenType.Operator && Previous().Value == ";;")
            {
                return;
            }

            if (Peek().type == OcTokenType.Keyword)
            {
                var value = Peek().Value;
                if (value is "let" or "type" or "module" or "open" or "exception"
                    or "class" or "val" or "external" or "include")
                {
                    return;
                }
            }

            Advance();
        }
    }

    #endregion

    #region Declarations

    private OcAstNode? ParseDeclaration()
    {
        try
        {
            if (Check(OcTokenType.Keyword, "open"))
            {
                return ParseOpenDecl();
            }

            if (Check(OcTokenType.Keyword, "let"))
            {
                return ParseLetDecl();
            }

            if (Check(OcTokenType.Keyword, "type"))
            {
                return ParseTypeDecl();
            }

            if (Check(OcTokenType.Keyword, "module"))
            {
                return ParseModuleDecl();
            }

            if (Check(OcTokenType.Keyword, "exception"))
            {
                return ParseExceptionDecl();
            }

            if (Check(OcTokenType.Keyword, "val"))
            {
                return ParseValDecl();
            }

            if (Check(OcTokenType.Keyword, "external"))
            {
                return ParseExternalDecl();
            }

            return ParseExpression();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private OcOpen ParseOpenDecl()
    {
        ConsumeKeyword("open", "OCM2001", "期望 'open' 关键字");
        var modulePath = ParseModulePath();
        return new OcOpen(modulePath);
    }

    private string ParseModulePath()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(Consume(OcTokenType.ModuleName, "OCM2002", "期望模块名").Value);

        while (Match(OcTokenType.Delimiter, "."))
            sb.Append('.').Append(Consume(OcTokenType.Identifier, "OCM2003", "期望标识符").Value);

        return sb.ToString();
    }

    private OcAstNode ParseLetDecl()
    {
        ConsumeKeyword("let", "OCM2004", "期望 'let' 关键字");
        var isRec = Match(OcTokenType.Keyword, "rec");

        var pattern = ParsePattern();

        var parameters = new List<OcAstNode>();
        while (!Check(OcTokenType.Operator, "=") && !Check(OcTokenType.Keyword, "in") && !IsAtEnd())
            parameters.Add(ParsePattern());

        OcAstNode? typeConstraint = null;
        if (Match(OcTokenType.Operator, ":"))
        {
            typeConstraint = ParseType();
        }

        Consume(OcTokenType.Operator, "OCM2005", "期望 '='");
        var body = ParseExpression();

        if (Match(OcTokenType.Keyword, "in"))
        {
            var inExpr = ParseExpression();
            return new OcLetExpr(isRec, pattern, body, inExpr);
        }

        return new OcLetBinding(isRec, pattern, parameters, body);
    }

    private OcTypeDecl ParseTypeDecl()
    {
        ConsumeKeyword("type", "OCM2006", "期望 'type' 关键字");
        var isRecursive = Match(OcTokenType.Keyword, "and");

        var typeParams = new List<string>();
        while (Check(OcTokenType.Identifier) && char.IsLower(Peek().Value[0]))
            typeParams.Add(Advance().Value);

        var name = Consume(OcTokenType.Identifier, "OCM2007", "期望类型名").Value;

        var isPrivate = false;
        if (Check(OcTokenType.Keyword, "private"))
        {
            isPrivate = Match(OcTokenType.Keyword, "private");
        }

        Consume(OcTokenType.Operator, "OCM2008", "期望 '='");

        var constructors = new List<OcAstNode>();
        constructors.Add(ParseVariantConstructor());

        while (Match(OcTokenType.Operator, "|")) constructors.Add(ParseVariantConstructor());

        return new OcTypeDecl(name, typeParams, constructors, isPrivate, isRecursive);
    }

    private OcVariantConstructor ParseVariantConstructor()
    {
        var name = Consume(OcTokenType.Identifier, "OCM2009", "期望构造器名").Value;

        var arguments = new List<OcAstNode>();
        if (Match(OcTokenType.Operator, "of"))
        {
            arguments.Add(ParseType());

            while (Match(OcTokenType.Operator, "*")) arguments.Add(ParseType());
        }

        return new OcVariantConstructor(name, arguments);
    }

    private OcModule ParseModuleDecl()
    {
        ConsumeKeyword("module", "OCM2010", "期望 'module' 关键字");
        var name = Consume(OcTokenType.ModuleName, "OCM2011", "期望模块名").Value;

        if (Match(OcTokenType.Operator, ":"))
        {
            while (!Check(OcTokenType.Operator, "=") && !IsAtEnd()) Advance();
        }

        if (Match(OcTokenType.Operator, "="))
        {
            if (Match(OcTokenType.Keyword, "struct"))
            {
                var declarations = new List<OcAstNode>();

                while (!Check(OcTokenType.Keyword, "end") && !IsAtEnd())
                {
                    Match(OcTokenType.Operator, ";;");
                    var decl = ParseDeclaration();
                    if (decl is not null)
                    {
                        declarations.Add(decl);
                    }
                }

                ConsumeKeyword("end", "OCM2012", "期望 'end'");
                return new OcModule(name, declarations);
            }
        }

        return new OcModule(name, []);
    }

    private OcExceptionDecl ParseExceptionDecl()
    {
        ConsumeKeyword("exception", "OCM2013", "期望 'exception' 关键字");
        var name = Consume(OcTokenType.Identifier, "OCM2014", "期望异常名").Value;

        var arguments = new List<OcAstNode>();
        if (Match(OcTokenType.Operator, "of"))
        {
            arguments.Add(ParseType());

            while (Match(OcTokenType.Operator, "*")) arguments.Add(ParseType());
        }

        return new OcExceptionDecl(name, arguments);
    }

    private OcValBinding ParseValDecl()
    {
        ConsumeKeyword("val", "OCM2015", "期望 'val' 关键字");
        var pattern = ParsePattern();

        if (Match(OcTokenType.Operator, ":"))
        {
            var type = ParseType();
            return new OcValBinding(pattern, type);
        }

        return new OcValBinding(pattern, new OcUnit());
    }

    private OcAstNode ParseExternalDecl()
    {
        ConsumeKeyword("external", "OCM2016", "期望 'external' 关键字");
        var name = Consume(OcTokenType.Identifier, "OCM2017", "期望外部函数名").Value;

        Consume(OcTokenType.Operator, "OCM2018", "期望 ':'");
        var type = ParseType();

        Consume(OcTokenType.Operator, "OCM2019", "期望 '='");

        while (!Check(OcTokenType.Operator, ";;") && !IsAtEnd()) Advance();

        return new OcValBinding(new OcVarPattern(name), type);
    }

    #endregion

    #region Types

    private OcAstNode ParseType()
    {
        return ParseFunctionType();
    }

    private OcAstNode ParseFunctionType()
    {
        var left = ParseTupleType();

        while (Match(OcTokenType.Operator, "->"))
        {
            var right = ParseTupleType();
            left = new OcFunctionType(left, right);
        }

        return left;
    }

    private OcAstNode ParseTupleType()
    {
        var left = ParseAppType();

        if (Match(OcTokenType.Operator, "*"))
        {
            var elements = new List<OcAstNode> { left };
            elements.Add(ParseAppType());

            while (Match(OcTokenType.Operator, "*")) elements.Add(ParseAppType());

            return new OcTupleType(elements);
        }

        return left;
    }

    private OcAstNode ParseAppType()
    {
        var type = ParseAtomicType();

        while (Check(OcTokenType.Identifier) || Check(OcTokenType.Delimiter, "(") ||
               Check(OcTokenType.Delimiter, "["))
        {
            var arg = ParseAtomicType();
            type = new OcTypeApp(type, [arg]);
        }

        return type;
    }

    private OcAstNode ParseAtomicType()
    {
        if (Check(OcTokenType.Delimiter, "("))
        {
            Advance();
            if (Match(OcTokenType.Delimiter, ")"))
            {
                return new OcTupleType([]);
            }

            var type = ParseType();

            if (Check(OcTokenType.Delimiter, ","))
            {
                var types = new List<OcAstNode> { type };
                while (Match(OcTokenType.Delimiter, ",")) types.Add(ParseType());
                Consume(OcTokenType.Delimiter, "OCM2020", "期望 ')'");
                return new OcTupleType(types);
            }

            Consume(OcTokenType.Delimiter, "OCM2021", "期望 ')'");
            return type;
        }

        if (Match(OcTokenType.Delimiter, "["))
        {
            var elementType = ParseType();
            Consume(OcTokenType.Delimiter, "OCM2022", "期望 ']'");
            return new OcListType(elementType);
        }

        if (Check(OcTokenType.Identifier))
        {
            var name = Advance().Value;
            if (char.IsLower(name[0]))
            {
                return new OcTypeVar(name);
            }

            return new OcTypeCon(name);
        }

        if (Check(OcTokenType.ModuleName))
        {
            var name = Advance().Value;
            return new OcTypeCon(name);
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            "OCM2023",
            $"期望类型，遇到 '{errorToken.Value}'");

        throw new ParseException($"期望类型，遇到 '{errorToken.Value}'");
    }

    #endregion

    #region Expressions

    private OcAstNode ParseExpression()
    {
        return ParseSequence();
    }

    private OcAstNode ParseSequence()
    {
        var expr = ParseIfLetExpr();

        while (Match(OcTokenType.Operator, ";"))
        {
            var right = ParseIfLetExpr();
            expr = new OcSequence(expr, right);
        }

        return expr;
    }

    private OcAstNode ParseIfLetExpr()
    {
        if (Check(OcTokenType.Keyword, "if"))
        {
            return ParseIfExpr();
        }

        if (Check(OcTokenType.Keyword, "let"))
        {
            return ParseLetExpr();
        }

        if (Check(OcTokenType.Keyword, "fun"))
        {
            return ParseFunExpr();
        }

        if (Check(OcTokenType.Keyword, "function"))
        {
            return ParseFunctionExpr();
        }

        if (Check(OcTokenType.Keyword, "match"))
        {
            return ParseMatchExpr();
        }

        if (Check(OcTokenType.Keyword, "try"))
        {
            return ParseTryExpr();
        }

        if (Check(OcTokenType.Keyword, "for"))
        {
            return ParseForExpr();
        }

        if (Check(OcTokenType.Keyword, "while"))
        {
            return ParseWhileExpr();
        }

        return ParseInfixExpr();
    }

    private OcIfExpr ParseIfExpr()
    {
        ConsumeKeyword("if", "OCM2024", "期望 'if' 关键字");
        var condition = ParseExpression();
        ConsumeKeyword("then", "OCM2025", "期望 'then' 关键字");
        var thenBranch = ParseExpression();

        OcAstNode elseBranch = new OcUnit();
        if (Match(OcTokenType.Keyword, "else"))
        {
            elseBranch = ParseExpression();
        }

        return new OcIfExpr(condition, thenBranch, elseBranch);
    }

    private OcLetExpr ParseLetExpr()
    {
        ConsumeKeyword("let", "OCM2026", "期望 'let' 关键字");
        var isRec = Match(OcTokenType.Keyword, "rec");

        var pattern = ParsePattern();

        OcAstNode? typeConstraint = null;
        if (Match(OcTokenType.Operator, ":"))
        {
            typeConstraint = ParseType();
        }

        Consume(OcTokenType.Operator, "OCM2027", "期望 '='");
        var body = ParseExpression();

        ConsumeKeyword("in", "OCM2028", "期望 'in' 关键字");
        var inExpr = ParseExpression();

        return new OcLetExpr(isRec, pattern, body, inExpr);
    }

    private OcFunExpr ParseFunExpr()
    {
        ConsumeKeyword("fun", "OCM2029", "期望 'fun' 关键字");

        var parameters = new List<OcAstNode>();
        while (!Check(OcTokenType.Operator, "->") && !IsAtEnd())
            parameters.Add(ParsePattern());

        Consume(OcTokenType.Operator, "OCM2030", "期望 '->'");
        var body = ParseExpression();

        return new OcFunExpr(parameters, body);
    }

    private OcFunExpr ParseFunctionExpr()
    {
        ConsumeKeyword("function", "OCM2031", "期望 'function' 关键字");

        var cases = new List<OcMatchCase>();

        while (Match(OcTokenType.Operator, "|"))
        {
            var pattern = ParsePattern();
            OcAstNode? guard = null;
            if (Match(OcTokenType.Keyword, "when"))
            {
                guard = ParseExpression();
            }

            Consume(OcTokenType.Operator, "OCM2032", "期望 '->'");
            var body = ParseExpression();
            cases.Add(new OcMatchCase(pattern, guard, body));
        }

        return new OcFunExpr([new OcWildCardPattern()], new OcMatchExpr(new OcUnit(), cases));
    }

    private OcMatchExpr ParseMatchExpr()
    {
        ConsumeKeyword("match", "OCM2033", "期望 'match' 关键字");
        var scrutinee = ParseExpression();
        ConsumeKeyword("with", "OCM2034", "期望 'with' 关键字");

        var cases = new List<OcMatchCase>();

        Match(OcTokenType.Operator, "|");

        while (!IsAtEnd())
        {
            var pattern = ParsePattern();
            OcAstNode? guard = null;
            if (Match(OcTokenType.Keyword, "when"))
            {
                guard = ParseExpression();
            }

            Consume(OcTokenType.Operator, "OCM2035", "期望 '->'");
            var body = ParseExpression();
            cases.Add(new OcMatchCase(pattern, guard, body));

            if (!Match(OcTokenType.Operator, "|"))
            {
                break;
            }
        }

        return new OcMatchExpr(scrutinee, cases);
    }

    private OcTryExpr ParseTryExpr()
    {
        ConsumeKeyword("try", "OCM2036", "期望 'try' 关键字");
        var expression = ParseExpression();
        ConsumeKeyword("with", "OCM2037", "期望 'with' 关键字");

        var cases = new List<OcMatchCase>();

        Match(OcTokenType.Operator, "|");

        while (!IsAtEnd())
        {
            var pattern = ParsePattern();
            OcAstNode? guard = null;
            if (Match(OcTokenType.Keyword, "when"))
            {
                guard = ParseExpression();
            }

            Consume(OcTokenType.Operator, "OCM2038", "期望 '->'");
            var body = ParseExpression();
            cases.Add(new OcMatchCase(pattern, guard, body));

            if (!Match(OcTokenType.Operator, "|"))
            {
                break;
            }
        }

        return new OcTryExpr(expression, cases);
    }

    private OcForExpr ParseForExpr()
    {
        ConsumeKeyword("for", "OCM2039", "期望 'for' 关键字");
        var iterator = Consume(OcTokenType.Identifier, "OCM2040", "期望迭代变量").Value;
        Consume(OcTokenType.Operator, "OCM2041", "期望 '='");
        var start = ParseExpression();

        var isDownto = false;
        if (Match(OcTokenType.Keyword, "downto"))
        {
            isDownto = true;
        }
        else
        {
            ConsumeKeyword("to", "OCM2042", "期望 'to' 或 'downto'");
        }

        var end = ParseExpression();
        ConsumeKeyword("do", "OCM2043", "期望 'do' 关键字");
        var body = ParseExpression();
        ConsumeKeyword("done", "OCM2044", "期望 'done' 关键字");

        return new OcForExpr(iterator, start, end, isDownto, body);
    }

    private OcWhileExpr ParseWhileExpr()
    {
        ConsumeKeyword("while", "OCM2045", "期望 'while' 关键字");
        var condition = ParseExpression();
        ConsumeKeyword("do", "OCM2046", "期望 'do' 关键字");
        var body = ParseExpression();
        ConsumeKeyword("done", "OCM2047", "期望 'done' 关键字");

        return new OcWhileExpr(condition, body);
    }

    private OcAstNode ParseInfixExpr()
    {
        var left = ParseApplication();

        while (Check(OcTokenType.Operator) &&
               Peek().Value is not "=" and not ";;" and not "->" and not "<-" and not "|")
        {
            var op = Advance().Value;
            var right = ParseApplication();
            left = new OcBinaryOp(left, op, right);
        }

        return left;
    }

    private OcAstNode ParseApplication()
    {
        var expr = ParseAtomicExpression();

        while (Check(OcTokenType.Identifier) || Check(OcTokenType.Delimiter, "(") ||
               Check(OcTokenType.Delimiter, "[") || Check(OcTokenType.Delimiter, "{") ||
               Check(OcTokenType.Char) || Check(OcTokenType.Number) ||
               Check(OcTokenType.String) || Check(OcTokenType.Keyword, "true") ||
               Check(OcTokenType.Keyword, "false") || Check(OcTokenType.ModuleName))
        {
            var arg = ParseAtomicExpression();
            expr = new OcApplication(expr, [arg]);
        }

        return expr;
    }

    private OcAstNode ParseAtomicExpression()
    {
        if (Check(OcTokenType.Number))
        {
            var token = Advance();
            return new OcLiteral("number", Peek().Value);
        }

        if (Check(OcTokenType.String))
        {
            var token = Advance();
            return new OcLiteral("string", Peek().Value);
        }

        if (Check(OcTokenType.Char))
        {
            var token = Advance();
            return new OcLiteral("char", Peek().Value);
        }

        if (Check(OcTokenType.Keyword, "true"))
        {
            Advance();
            return new OcLiteral("bool", "true");
        }

        if (Check(OcTokenType.Keyword, "false"))
        {
            Advance();
            return new OcLiteral("bool", "false");
        }

        if (Check(OcTokenType.Identifier))
        {
            var token = Advance();
            return new OcIdentifier(Peek().Value);
        }

        if (Check(OcTokenType.ModuleName))
        {
            var module = Advance().Value;
            if (Match(OcTokenType.Delimiter, "."))
            {
                var name = Consume(OcTokenType.Identifier, "OCM2048", "期望标识符").Value;
                return new OcQualified(module, name);
            }

            return new OcIdentifier(module);
        }

        if (Check(OcTokenType.Delimiter, "("))
        {
            Advance();

            if (Match(OcTokenType.Delimiter, ")"))
            {
                return new OcUnit();
            }

            var expr = ParseExpression();

            if (Check(OcTokenType.Delimiter, ","))
            {
                var elements = new List<OcAstNode> { expr };
                while (Match(OcTokenType.Delimiter, ",")) elements.Add(ParseExpression());
                Consume(OcTokenType.Delimiter, "OCM2049", "期望 ')'");
                return new OcTuple(elements);
            }

            Consume(OcTokenType.Delimiter, "OCM2050", "期望 ')'");
            return expr;
        }

        if (Match(OcTokenType.Delimiter, "["))
        {
            var elements = new List<OcAstNode>();

            if (!Check(OcTokenType.Delimiter, "]"))
            {
                elements.Add(ParseExpression());

                while (Match(OcTokenType.Delimiter, ";")) elements.Add(ParseExpression());
            }

            Consume(OcTokenType.Delimiter, "OCM2051", "期望 ']'");
            return new OcList(elements);
        }

        if (Match(OcTokenType.Delimiter, "[|"))
        {
            var elements = new List<OcAstNode>();

            if (!Check(OcTokenType.Delimiter, "|]"))
            {
                elements.Add(ParseExpression());

                while (Match(OcTokenType.Delimiter, ";")) elements.Add(ParseExpression());
            }

            Consume(OcTokenType.Delimiter, "OCM2052", "期望 '|]'");
            return new OcArray(elements);
        }

        if (Check(OcTokenType.Keyword, "begin"))
        {
            Advance();
            var expr = ParseExpression();
            ConsumeKeyword("end", "OCM2053", "期望 'end'");
            return expr;
        }

        if (Match(OcTokenType.Operator, "!"))
        {
            var operand = ParseAtomicExpression();
            return new OcUnaryOp("!", operand);
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            "OCM2054",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    #endregion

    #region Patterns

    private OcAstNode ParsePattern()
    {
        if (Check(OcTokenType.Identifier) && char.IsUpper(Peek().Value[0]))
        {
            var name = Advance().Value;
            var arguments = new List<OcAstNode>();

            while (Check(OcTokenType.Identifier) || Check(OcTokenType.Delimiter, "(") ||
                   Check(OcTokenType.Delimiter, "[") || Check(OcTokenType.Char) ||
                   Check(OcTokenType.Number) || Check(OcTokenType.String) ||
                   Check(OcTokenType.Keyword, "true") || Check(OcTokenType.Keyword, "false"))
                arguments.Add(ParsePattern());

            return new OcConstructorPattern(name, arguments);
        }

        if (Check(OcTokenType.Identifier))
        {
            var name = Advance().Value;
            return new OcVarPattern(name);
        }

        if (Check(OcTokenType.Number))
        {
            var token = Advance();
            return new OcLiteral("number", Peek().Value);
        }

        if (Check(OcTokenType.String))
        {
            var token = Advance();
            return new OcLiteral("string", Peek().Value);
        }

        if (Check(OcTokenType.Char))
        {
            var token = Advance();
            return new OcLiteral("char", Peek().Value);
        }

        if (Check(OcTokenType.Keyword, "true"))
        {
            Advance();
            return new OcLiteral("bool", "true");
        }

        if (Check(OcTokenType.Keyword, "false"))
        {
            Advance();
            return new OcLiteral("bool", "false");
        }

        if (Match(OcTokenType.Delimiter, "("))
        {
            if (Match(OcTokenType.Delimiter, ")"))
            {
                return new OcUnit();
            }

            var pattern = ParsePattern();

            if (Check(OcTokenType.Delimiter, ","))
            {
                var elements = new List<OcAstNode> { pattern };
                while (Match(OcTokenType.Delimiter, ",")) elements.Add(ParsePattern());
                Consume(OcTokenType.Delimiter, "OCM2055", "期望 ')'");
                return new OcTuplePattern(elements);
            }

            Consume(OcTokenType.Delimiter, "OCM2056", "期望 ')'");
            return pattern;
        }

        if (Match(OcTokenType.Delimiter, "["))
        {
            var elements = new List<OcAstNode>();
            if (!Check(OcTokenType.Delimiter, "]"))
            {
                elements.Add(ParsePattern());

                while (Match(OcTokenType.Delimiter, ";")) elements.Add(ParsePattern());
            }

            Consume(OcTokenType.Delimiter, "OCM2057", "期望 ']'");
            return new OcListPattern(elements);
        }

        if (Match(OcTokenType.Operator, "_"))
        {
            return new OcWildCardPattern();
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            "OCM2058",
            $"期望模式，遇到 '{errorToken.Value}'");

        throw new ParseException($"期望模式，遇到 '{errorToken.Value}'");
    }

    #endregion
}
