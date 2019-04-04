using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;


/// <summary>

///     C 语言语法解析器


/// </summary>
public sealed class CParser : ParserBase<IReadOnlyList<CToken>, CAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<CToken> _tokens = [];

    public CParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public override CAstNode parse(IReadOnlyList<CToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var declarations = new List<CAstNode>();

        while (!is_at_end())
        {
            skip_new_lines();

            if (is_at_end())
            {
                break;
            }

            var decl = parse_external_declaration();
            if (decl is not null)
            {
                declarations.Add(decl);
            }
        }

        return new CTranslationUnit(declarations);
    }

    #region Type Specifier

    private CAstNode parse_type_specifier()
    {
        var startToken = peek();
        var isConst = false;
        var isPointer = false;

        if (match(CNodeKind.keyword, "const"))
        {
            isConst = true;
        }

        string typeName;

        if (check(CNodeKind.keyword, "struct") || check(CNodeKind.keyword, "union") ||
            check(CNodeKind.keyword, "enum"))
        {
            var kw = advance().text;
            if (check(CNodeKind.identifier))
            {
                typeName = $"{kw} {advance().text}";
            }
            else
            {
                typeName = kw;
            }
        }
        else if (check(CNodeKind.keyword) || check(CNodeKind.identifier))
        {
            typeName = advance().text;
        }
        else
        {
            throw new ParseException("期望类型说明符");
        }

        while (match(CNodeKind.@operator, "*")) isPointer = true;

        if (match(CNodeKind.keyword, "const"))
        {
            isConst = true;
        }

        return new CTypeNode(typeName, isPointer, isConst, default);
    }

    #endregion

    private void synchronize()
    {
        advance();

        while (!is_at_end())
        {
            if (previous().kind == CNodeKind.delimiter && previous().text == ";")
            {
                return;
            }

            if (check(CNodeKind.keyword))
            {
                var value = peek().text;
                if (value is "int" or "char" or "float" or "double" or "void" or "struct" or "if" or "while" or "for"
                    or "return" or "typedef")
                {
                    return;
                }
            }

            advance();
        }
    }

    private CToken peek_next()
    {
        return _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];
    }

    #region Token Access

    private bool is_at_end()
    {
        return peek().kind == CNodeKind.eof;
    }

    private CToken peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private CToken previous()
    {
        return _tokens[_current - 1];
    }

    private CToken advance()
    {
        if (!is_at_end())
        {
            _current++;
        }

        return previous();
    }

    private bool check(NodeKind type)
    {
        return !is_at_end() && peek().kind == type;
    }

    private bool check(NodeKind type, string value)
    {
        return !is_at_end() && peek().kind == type && peek().text == value;
    }

    private bool match(NodeKind type)
    {
        if (check(type))
        {
            advance();
            return true;
        }

        return false;
    }

    private bool match(NodeKind type, string value)
    {
        if (check(type, value))
        {
            advance();
            return true;
        }

        return false;
    }

    private CToken consume(NodeKind type, string errorCode, string message)
    {
        if (check(type))
        {
            return advance();
        }

        var token = peek();
        _diagnostics?.report_error(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void skip_new_lines()
    {
        while (match(CNodeKind.new_line))
        {
        }
    }

    #endregion

    #region External Declarations

    private CAstNode? parse_external_declaration()
    {
        try
        {
            skip_new_lines();

            if (is_at_end())
            {
                return null;
            }

            if (check(CNodeKind.keyword, "typedef"))
            {
                return parse_typedef();
            }

            if (check(CNodeKind.keyword, "struct") || check(CNodeKind.keyword, "union"))
            {
                return parse_struct_or_union();
            }

            if (check(CNodeKind.keyword, "enum"))
            {
                return parse_enum();
            }

            return parse_declaration_or_function();
        }
        catch (ParseException)
        {
            synchronize();
            return null;
        }
    }

    private CAstNode parse_typedef()
    {
        var startToken = advance();
        var type = parse_type_specifier();
        var name = consume(CNodeKind.identifier, "OC2001", "期望类型别名名称").text;
        consume(CNodeKind.delimiter, "OC2002", "期望 ';'");

        return new CTypedef(type, name, default);
    }

    private CAstNode parse_struct_or_union()
    {
        var startToken = advance();
        string? name = null;

        if (check(CNodeKind.identifier))
        {
            name = advance().text;
        }

        if (match(CNodeKind.delimiter, "{"))
        {
            var fields = new List<CVarDecl>();

            while (!check(CNodeKind.delimiter, "}") && !is_at_end())
            {
                skip_new_lines();
                if (check(CNodeKind.delimiter, "}"))
                {
                    break;
                }

                var fieldType = parse_type_specifier();
                var fieldName = consume(CNodeKind.identifier, "OC2003", "期望字段名").text;
                CAstNode? init = null;

                if (match(CNodeKind.@operator, "="))
                {
                    init = parse_expression();
                }

                consume(CNodeKind.delimiter, "OC2004", "期望 ';'");
                fields.Add(new CVarDecl(fieldType, fieldName, init));
            }

            consume(CNodeKind.delimiter, "OC2005", "期望 '}'");

            if (name is not null)
            {
                consume(CNodeKind.delimiter, "OC2006", "期望 ';'");
            }

            return new CStructDef(name, fields, default);
        }

        if (name is not null)
        {
            consume(CNodeKind.delimiter, "OC2007", "期望 ';'");
            return new CStructDef(name, [], default);
        }

        throw new ParseException("结构体定义语法错误");
    }

    private CAstNode parse_enum()
    {
        var startToken = advance();
        string? name = null;

        if (check(CNodeKind.identifier))
        {
            name = advance().text;
        }

        if (match(CNodeKind.delimiter, "{"))
        {
            while (!check(CNodeKind.delimiter, "}") && !is_at_end())
            {
                skip_new_lines();
                if (check(CNodeKind.delimiter, "}"))
                {
                    break;
                }

                consume(CNodeKind.identifier, "OC2008", "期望枚举常量名");

                if (match(CNodeKind.@operator, "="))
                {
                    parse_expression();
                }

                if (!check(CNodeKind.delimiter, "}"))
                {
                    consume(CNodeKind.delimiter, "OC2009", "期望 ','");
                }
            }

            consume(CNodeKind.delimiter, "OC2010", "期望 '}'");
        }

        consume(CNodeKind.delimiter, "OC2011", "期望 ';'");
        return new CStructDef(name, [], default);
    }

    private CAstNode parse_declaration_or_function()
    {
        var startToken = peek();
        var type = parse_type_specifier();
        var name = consume(CNodeKind.identifier, "OC2012", "期望标识符").text;

        if (check(CNodeKind.delimiter, "("))
        {
            return parse_function_definition(type, name, default);
        }

        CAstNode? init = null;

        if (match(CNodeKind.@operator, "="))
        {
            init = parse_initializer();
        }

        consume(CNodeKind.delimiter, "OC2013", "期望 ';'");
        return new CVarDecl(type, name, init, default);
    }

    private CAstNode parse_function_definition(CAstNode returnType, string name, CToken startToken)
    {
        consume(CNodeKind.delimiter, "OC2014", "期望 '('");
        var parameters = new List<CParamDecl>();

        if (!check(CNodeKind.delimiter, ")"))
        {
            do
            {
                skip_new_lines();
                if (check(CNodeKind.delimiter, ")"))
                {
                    break;
                }

                if (check(CNodeKind.keyword, "void"))
                {
                    advance();
                    if (check(CNodeKind.delimiter, ")"))
                    {
                        break;
                    }
                }

                var paramType = parse_type_specifier();
                var paramName = consume(CNodeKind.identifier, "OC2015", "期望参数名").text;
                parameters.Add(new CParamDecl(paramType, paramName));
            } while (match(CNodeKind.delimiter, ","));
        }

        consume(CNodeKind.delimiter, "OC2016", "期望 ')'");
        var body = parse_compound_statement();

        return new CFunctionDef(returnType, name, parameters, body, default);
    }

    #endregion

    #region Statements

    private CAstNode parse_statement()
    {
        skip_new_lines();

        if (check(CNodeKind.delimiter, "{"))
        {
            return parse_compound_statement();
        }

        if (check(CNodeKind.keyword, "if"))
        {
            return parse_if_stmt();
        }

        if (check(CNodeKind.keyword, "while"))
        {
            return parse_while_stmt();
        }

        if (check(CNodeKind.keyword, "do"))
        {
            return parse_do_while_stmt();
        }

        if (check(CNodeKind.keyword, "for"))
        {
            return parse_for_stmt();
        }

        if (check(CNodeKind.keyword, "return"))
        {
            return parse_return_stmt();
        }

        if (check(CNodeKind.keyword, "break"))
        {
            return parse_break_stmt();
        }

        if (check(CNodeKind.keyword, "continue"))
        {
            return parse_continue_stmt();
        }

        if (check(CNodeKind.keyword, "switch"))
        {
            return parse_switch_stmt();
        }

        if (check(CNodeKind.keyword, "goto"))
        {
            return parse_goto_stmt();
        }

        if (check(CNodeKind.identifier) && peek_next().kind == CNodeKind.delimiter && peek_next().text == ":")
        {
            return parse_label_stmt();
        }

        return parse_expr_or_decl_stmt();
    }

    private CAstNode parse_compound_statement()
    {
        var startToken = consume(CNodeKind.delimiter, "OC2017", "期望 '{'");
        var statements = new List<CAstNode>();

        while (!check(CNodeKind.delimiter, "}") && !is_at_end())
        {
            skip_new_lines();
            if (check(CNodeKind.delimiter, "}"))
            {
                break;
            }

            var stmt = parse_statement();
            statements.Add(stmt);
        }

        consume(CNodeKind.delimiter, "OC2018", "期望 '}'");
        return new CCompound(statements, default);
    }

    private CAstNode parse_if_stmt()
    {
        var startToken = advance();
        consume(CNodeKind.delimiter, "OC2019", "期望 '('");
        var condition = parse_expression();
        consume(CNodeKind.delimiter, "OC2020", "期望 ')'");
        var thenBody = parse_statement();
        CAstNode? elseBody = null;

        if (match(CNodeKind.keyword, "else"))
        {
            elseBody = parse_statement();
        }

        return new CIf(condition, thenBody, elseBody, default);
    }

    private CAstNode parse_while_stmt()
    {
        var startToken = advance();
        consume(CNodeKind.delimiter, "OC2021", "期望 '('");
        var condition = parse_expression();
        consume(CNodeKind.delimiter, "OC2022", "期望 ')'");
        var body = parse_statement();

        return new CWhile(condition, body, default);
    }

    private CAstNode parse_do_while_stmt()
    {
        var startToken = advance();
        var body = parse_statement();
        consume(CNodeKind.keyword, "OC2023", "期望 'while'");
        consume(CNodeKind.delimiter, "OC2024", "期望 '('");
        var condition = parse_expression();
        consume(CNodeKind.delimiter, "OC2025", "期望 ')'");
        consume(CNodeKind.delimiter, "OC2026", "期望 ';'");

        return new CDoWhile(body, condition, default);
    }

    private CAstNode parse_for_stmt()
    {
        var startToken = advance();
        consume(CNodeKind.delimiter, "OC2027", "期望 '('");
        CAstNode? init = null;

        if (!check(CNodeKind.delimiter, ";"))
        {
            init = parse_expr_or_decl_stmt();
        }
        else
        {
            consume(CNodeKind.delimiter, "OC2028", "期望 ';'");
        }

        CAstNode? condition = null;
        if (!check(CNodeKind.delimiter, ";"))
        {
            condition = parse_expression();
        }

        consume(CNodeKind.delimiter, "OC2029", "期望 ';'");

        CAstNode? increment = null;
        if (!check(CNodeKind.delimiter, ")"))
        {
            increment = parse_expression();
        }

        consume(CNodeKind.delimiter, "OC2030", "期望 ')'");

        var body = parse_statement();
        return new CFor(init, condition, increment, body, default);
    }

    private CAstNode parse_return_stmt()
    {
        var startToken = advance();
        CAstNode? value = null;

        if (!check(CNodeKind.delimiter, ";"))
        {
            value = parse_expression();
        }

        consume(CNodeKind.delimiter, "OC2031", "期望 ';'");
        return new CReturn(value, default);
    }

    private CAstNode parse_break_stmt()
    {
        var startToken = advance();
        consume(CNodeKind.delimiter, "OC2032", "期望 ';'");
        return new CBreak();
    }

    private CAstNode parse_continue_stmt()
    {
        var startToken = advance();
        consume(CNodeKind.delimiter, "OC2033", "期望 ';'");
        return new CContinue();
    }

    private CAstNode parse_goto_stmt()
    {
        var startToken = advance();
        var label = consume(CNodeKind.identifier, "OC2034", "期望标签名").text;
        consume(CNodeKind.delimiter, "OC2035", "期望 ';'");
        return new CGoto(label, default);
    }

    private CAstNode parse_label_stmt()
    {
        var name = advance().text;
        consume(CNodeKind.delimiter, "OC2036", "期望 ':'");
        var stmt = parse_statement();
        return new CLabel(name, stmt);
    }

    private CAstNode parse_switch_stmt()
    {
        var startToken = advance();
        consume(CNodeKind.delimiter, "OC2037", "期望 '('");
        var expr = parse_expression();
        consume(CNodeKind.delimiter, "OC2038", "期望 ')'");
        consume(CNodeKind.delimiter, "OC2039", "期望 '{'");

        var cases = new List<CAstNode>();

        while (!check(CNodeKind.delimiter, "}") && !is_at_end())
        {
            skip_new_lines();
            if (check(CNodeKind.delimiter, "}"))
            {
                break;
            }

            if (match(CNodeKind.keyword, "case"))
            {
                var value = parse_expression();
                consume(CNodeKind.delimiter, "OC2040", "期望 ':'");
                var body = new List<CAstNode>();

                while (!check(CNodeKind.delimiter, "}") && !check(CNodeKind.keyword, "case") &&
                       !check(CNodeKind.keyword, "default") && !is_at_end()) body.Add(parse_statement());

                cases.Add(new CCase(value, body));
            }
            else if (match(CNodeKind.keyword, "default"))
            {
                consume(CNodeKind.delimiter, "OC2041", "期望 ':'");
                var body = new List<CAstNode>();

                while (!check(CNodeKind.delimiter, "}") && !check(CNodeKind.keyword, "case") &&
                       !check(CNodeKind.keyword, "default") && !is_at_end()) body.Add(parse_statement());

                cases.Add(new CCase(null, body));
            }
            else
            {
                throw new ParseException("Switch 语句中期望 case 或 default");
            }
        }

        consume(CNodeKind.delimiter, "OC2042", "期望 '}'");
        return new CSwitch(expr, cases, default);
    }

    private CAstNode parse_expr_or_decl_stmt()
    {
        var startToken = peek();

        if (is_type_specifier(peek()))
        {
            var type = parse_type_specifier();
            var name = consume(CNodeKind.identifier, "OC2043", "期望变量名").text;
            CAstNode? init = null;

            if (match(CNodeKind.@operator, "="))
            {
                init = parse_initializer();
            }

            consume(CNodeKind.delimiter, "OC2044", "期望 ';'");
            return new CVarDecl(type, name, init, default);
        }

        var expr = parse_expression();
        consume(CNodeKind.delimiter, "OC2045", "期望 ';'");
        return new CExprStmt(expr, default);
    }

    private bool is_type_specifier(CToken token)
    {
        if (token.kind != CNodeKind.keyword)
        {
            return false;
        }

        return token.text is "int" or "char" or "float" or "double" or "void" or "short" or "long"
            or "signed" or "unsigned" or "const" or "struct" or "union" or "enum" or "static"
            or "extern" or "auto" or "register" or "volatile" or "inline" or "restrict"
            or "_Bool" or "_Complex" or "_Imaginary" or "typedef";
    }

    private CAstNode parse_initializer()
    {
        if (check(CNodeKind.delimiter, "{"))
        {
            return parse_init_list();
        }

        return parse_expression();
    }

    private CAstNode parse_init_list()
    {
        var startToken = consume(CNodeKind.delimiter, "OC2046", "期望 '{'");
        var elements = new List<CAstNode>();

        if (!check(CNodeKind.delimiter, "}"))
        {
            do
            {
                skip_new_lines();
                if (check(CNodeKind.delimiter, "}"))
                {
                    break;
                }

                elements.Add(parse_initializer());
            } while (match(CNodeKind.delimiter, ","));
        }

        consume(CNodeKind.delimiter, "OC2047", "期望 '}'");
        return new CInitList(elements, default);
    }

    #endregion

    #region Expressions

    private CAstNode parse_expression()
    {
        return parse_assignment();
    }

    private CAstNode parse_assignment()
    {
        var left = parse_conditional();

        if (check(CNodeKind.@operator) &&
            peek().text is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "&=" or "|=" or "^=" or "<<=" or ">>=")
        {
            var op = advance().text;
            var right = parse_assignment();
            return new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_conditional()
    {
        var condition = parse_or();

        if (match(CNodeKind.@operator, "?"))
        {
            var thenExpr = parse_expression();
            consume(CNodeKind.delimiter, "OC2048", "期望 ':'");
            var elseExpr = parse_conditional();
            return new CTernaryOp(condition, thenExpr, elseExpr);
        }

        return condition;
    }

    private CAstNode parse_or()
    {
        var left = parse_and();

        while (match(CNodeKind.@operator, "||"))
        {
            var op = previous().text;
            var right = parse_and();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_and()
    {
        var left = parse_bit_or();

        while (match(CNodeKind.@operator, "&&"))
        {
            var op = previous().text;
            var right = parse_bit_or();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_bit_or()
    {
        var left = parse_bit_xor();

        while (match(CNodeKind.@operator, "|"))
        {
            var op = previous().text;
            var right = parse_bit_xor();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_bit_xor()
    {
        var left = parse_bit_and();

        while (match(CNodeKind.@operator, "^"))
        {
            var op = previous().text;
            var right = parse_bit_and();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_bit_and()
    {
        var left = parse_equality();

        while (match(CNodeKind.@operator, "&"))
        {
            var op = previous().text;
            var right = parse_equality();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_equality()
    {
        var left = parse_relational();

        while (check(CNodeKind.@operator) && peek().text is "==" or "!=")
        {
            var op = advance().text;
            var right = parse_relational();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_relational()
    {
        var left = parse_shift();

        while (check(CNodeKind.@operator) && peek().text is "<" or ">" or "<=" or ">=")
        {
            var op = advance().text;
            var right = parse_shift();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_shift()
    {
        var left = parse_additive();

        while (check(CNodeKind.@operator) && peek().text is "<<" or ">>")
        {
            var op = advance().text;
            var right = parse_additive();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_additive()
    {
        var left = parse_multiplicative();

        while (check(CNodeKind.@operator) && peek().text is "+" or "-")
        {
            var op = advance().text;
            var right = parse_multiplicative();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_multiplicative()
    {
        var left = parse_cast();

        while (check(CNodeKind.@operator) && peek().text is "*" or "/" or "%")
        {
            var op = advance().text;
            var right = parse_cast();
            left = new CBinaryOp(left, op, right);
        }

        return left;
    }

    private CAstNode parse_cast()
    {
        if (check(CNodeKind.delimiter, "("))
        {
            var saved = _current;
            advance();

            if (is_type_specifier(peek()))
            {
                var type = parse_type_specifier();
                consume(CNodeKind.delimiter, "OC2049", "期望 ')'");
                var expr = parse_cast();
                return new CCast(type, expr);
            }

            _current = saved;
        }

        return parse_unary();
    }

    private CAstNode parse_unary()
    {
        if (check(CNodeKind.@operator) && peek().text is "+" or "-" or "!" or "~" or "*" or "&" or "++" or "--")
        {
            var op = advance().text;
            var operand = parse_unary();
            return new CUnaryOp(op, operand);
        }

        if (match(CNodeKind.keyword, "sizeof"))
        {
            if (match(CNodeKind.delimiter, "("))
            {
                if (is_type_specifier(peek()))
                {
                    var type = parse_type_specifier();
                    consume(CNodeKind.delimiter, "OC2050", "期望 ')'");
                    return new CSizeOf(type);
                }

                var expr = parse_expression();
                consume(CNodeKind.delimiter, "OC2051", "期望 ')'");
                return new CSizeOf(expr);
            }

            return new CSizeOf(parse_unary());
        }

        return parse_postfix();
    }

    private CAstNode parse_postfix()
    {
        var expr = parse_primary();

        while (true)
            if (match(CNodeKind.delimiter, "["))
            {
                var index = parse_expression();
                consume(CNodeKind.delimiter, "OC2052", "期望 ']'");
                expr = new CSubscript(expr, index);
            }
            else if (match(CNodeKind.delimiter, "("))
            {
                var args = new List<CAstNode>();

                if (!check(CNodeKind.delimiter, ")"))
                {
                    do
                    {
                        args.Add(parse_expression());
                    } while (match(CNodeKind.delimiter, ","));
                }

                consume(CNodeKind.delimiter, "OC2053", "期望 ')'");
                expr = new CCall(expr, args);
            }
            else if (match(CNodeKind.@operator, "."))
            {
                var member = consume(CNodeKind.identifier, "OC2054", "期望成员名").text;
                expr = new CMemberAccess(expr, member, false);
            }
            else if (match(CNodeKind.@operator, "->"))
            {
                var member = consume(CNodeKind.identifier, "OC2055", "期望成员名").text;
                expr = new CMemberAccess(expr, member, true);
            }
            else if (check(CNodeKind.@operator) && peek().text is "++" or "--")
            {
                var op = advance().text;
                expr = new CUnaryOp(op, expr);
            }
            else
            {
                break;
            }

        return expr;
    }

    private CAstNode parse_primary()
    {
        if (match(CNodeKind.number))
        {
            var token = previous();
            return new CLiteral("number", token.text, default);
        }

        if (match(CNodeKind.@string))
        {
            var token = previous();
            return new CLiteral("string", token.text, default);
        }

        if (match(CNodeKind.@char))
        {
            var token = previous();
            return new CLiteral("char", token.text, default);
        }

        if (check(CNodeKind.identifier))
        {
            var token = advance();
            return new CIdentifier(token.text, default);
        }

        if (match(CNodeKind.delimiter, "("))
        {
            var expr = parse_expression();
            consume(CNodeKind.delimiter, "OC2056", "期望 ')'");
            return expr;
        }

        var errorToken = peek();
        _diagnostics?.report_error(
            string.Empty,
            default,
            "OC2057",
            $"意外的标记 '{errorToken.text}'");

        throw new ParseException($"意外的标记 '{errorToken.text}'");
    }

    #endregion
}
