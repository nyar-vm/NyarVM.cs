using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Python.AST;
using Std.Data.Text.Python.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Python.Parser;


/// <summary>
///     Python 语法解析器，基于 Oak.Core 的 ParserBase 实现
/// </summary>
public sealed class PythonParser : ParserBase<IReadOnlyList<GreenLeafNode>, PyAstNode>
{
    private IReadOnlyList<GreenLeafNode> _tokens = [];
    private int _current;


/// <summary>
///     创建 Python 语法解析器
/// </summary>
    public PythonParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
    }


/// <summary>
///     解析词法单元序列
/// </summary>
    public override PyAstNode parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var statements = new List<PyAstNode>();

        while (!is_at_end())
        {
            skip_new_lines();

            if (is_at_end())
            {
                break;
            }

            var stmt = parse_statement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return new PyModule(statements);
    }

    #region Token Access

    private bool is_at_end()
    {
        return peek().kind == PythonNodeKind.eof;
    }

    private GreenLeafNode peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private GreenLeafNode previous()
    {
        return _tokens[_current - 1];
    }

    private GreenLeafNode advance()
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

    private GreenLeafNode consume(NodeKind type, string errorCode, string message)
    {
        if (check(type))
        {
            return advance();
        }

        _diagnostics?.report_error(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private GreenLeafNode consume_keyword(string keyword, string errorCode, string message)
    {
        if (check(PythonNodeKind.keyword, keyword))
        {
            return advance();
        }

        _diagnostics?.report_error(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void skip_new_lines()
    {
        while (match(PythonNodeKind.new_line))
        {
        }
    }

    private void expect_indent()
    {
        consume(PythonNodeKind.indent, "NPY2001", "期望缩进");
    }

    private void expect_dedent()
    {
        consume(PythonNodeKind.dedent, "NPY2002", "期望反缩进");
    }

    #endregion

    #region Statements

    private PyAstNode? parse_statement()
    {
        try
        {
            skip_new_lines();

            if (check(PythonNodeKind.keyword, "def"))
            {
                return parse_function_def();
            }

            if (check(PythonNodeKind.keyword, "class"))
            {
                return parse_class_def();
            }

            if (check(PythonNodeKind.keyword, "if"))
            {
                return parse_if_stmt();
            }

            if (check(PythonNodeKind.keyword, "while"))
            {
                return parse_while_stmt();
            }

            if (check(PythonNodeKind.keyword, "for"))
            {
                return parse_for_stmt();
            }

            if (check(PythonNodeKind.keyword, "return"))
            {
                return parse_return_stmt();
            }

            if (check(PythonNodeKind.keyword, "yield"))
            {
                return parse_yield_stmt();
            }

            if (check(PythonNodeKind.keyword, "break"))
            {
                advance();
                return new PyBreak(Span: default);
            }

            if (check(PythonNodeKind.keyword, "continue"))
            {
                advance();
                return new PyContinue(Span: default);
            }

            if (check(PythonNodeKind.keyword, "pass"))
            {
                advance();
                return new PyPass();
            }

            if (check(PythonNodeKind.keyword, "try"))
            {
                return parse_try_stmt();
            }

            if (check(PythonNodeKind.keyword, "raise"))
            {
                return parse_raise_stmt();
            }

            if (check(PythonNodeKind.keyword, "import"))
            {
                return parse_import_stmt();
            }

            if (check(PythonNodeKind.keyword, "from"))
            {
                return parse_from_import_stmt();
            }

            return parse_expr_stmt();
        }
        catch (ParseException)
        {
            synchronize();
            return null;
        }
    }

    private void synchronize()
    {
        advance();

        while (!is_at_end())
        {
            if (previous().kind == PythonNodeKind.new_line)
            {
                return;
            }

            if (check(PythonNodeKind.keyword))
            {
                var value = peek().text;
                if (value is "def" or "class" or "if" or "while" or "for" or "return"
                    or "try" or "except" or "finally" or "raise" or "import" or "from"
                    or "break" or "continue" or "yield")
                {
                    return;
                }
            }

            advance();
        }
    }

    private PyFunctionDef parse_function_def()
    {
        consume_keyword("def", "NPY2003", "期望 'def' 关键字");

        var name = consume(PythonNodeKind.identifier, "NPY2004", "期望函数名").text;

        consume(PythonNodeKind.delimiter, "NPY2005", "期望 '('");

        var parameters = new List<string>();

        if (!check(PythonNodeKind.delimiter, ")"))
        {
            parameters.Add(consume(PythonNodeKind.identifier, "NPY2006", "期望参数名").text ?? "");

            while (match(PythonNodeKind.delimiter, ","))
            {
                parameters.Add(consume(PythonNodeKind.identifier, "NPY2007", "期望参数名").text ?? "");
            }
        }

        consume(PythonNodeKind.delimiter, "NPY2008", "期望 ')'");
        consume(PythonNodeKind.delimiter, "NPY2009", "期望 ':'");

        var body = parse_suite();

        return new PyFunctionDef(name ?? "", parameters, body, Span: default);
    }

    private PyClassDef parse_class_def()
    {
        consume_keyword("class", "NPY2010", "期望 'class' 关键字");

        var name = consume(PythonNodeKind.identifier, "NPY2011", "期望类名").text;

        consume(PythonNodeKind.delimiter, "NPY2012", "期望 ':'");

        var body = parse_suite();

        return new PyClassDef(name ?? "", body, Span: default);
    }

    private PyIf parse_if_stmt()
    {
        consume_keyword("if", "NPY2013", "期望 'if' 关键字");

        var condition = parse_expression();

        consume(PythonNodeKind.delimiter, "NPY2014", "期望 ':'");

        var thenBody = parse_suite();

        List<PyAstNode>? elseBody = null;

        skip_new_lines();

        if (match(PythonNodeKind.keyword, "else"))
        {
            consume(PythonNodeKind.delimiter, "NPY2015", "期望 ':'");
            elseBody = [.. parse_suite()];
        }
        else if (check(PythonNodeKind.keyword, "elif"))
        {
            elseBody = [parse_if_stmt()];
        }

        return new PyIf(condition, thenBody, elseBody, Span: default);
    }

    private PyWhile parse_while_stmt()
    {
        consume_keyword("while", "NPY2016", "期望 'while' 关键字");

        var condition = parse_expression();

        consume(PythonNodeKind.delimiter, "NPY2017", "期望 ':'");

        var body = parse_suite();

        return new PyWhile(condition, body, Span: default);
    }

    private PyFor parse_for_stmt()
    {
        consume_keyword("for", "NPY2018", "期望 'for' 关键字");

        var iterator = consume(PythonNodeKind.identifier, "NPY2019", "期望迭代变量").text;

        consume_keyword("in", "NPY2020", "期望 'in' 关键字");

        var iterable = parse_expression();

        consume(PythonNodeKind.delimiter, "NPY2021", "期望 ':'");

        var body = parse_suite();

        return new PyFor(iterator ?? "", iterable, body, Span: default);
    }

    private PyReturn parse_return_stmt()
    {
        consume_keyword("return", "NPY2022", "期望 'return' 关键字");

        PyAstNode? value = null;

        if (!check(PythonNodeKind.new_line) && !check(PythonNodeKind.dedent) && !is_at_end())
        {
            value = parse_expression();
        }

        return new PyReturn(value, Span: default);
    }

    private PyAstNode parse_expr_stmt()
    {
        var expr = parse_expression();

        if (match(PythonNodeKind.@operator, "="))
        {
            var value = parse_expression();
            return new PyAssign(expr, value);
        }

        var augAssignOps = new[] { "+=", "-=", "*=", "/=", "//=", "%=", "@=", "&=", "|=", "^=", ">>=", "<<=", "**=" };
        foreach (var augOp in augAssignOps)
        {
            if (match(PythonNodeKind.@operator, augOp))
            {
                var value = parse_expression();
                return new PyAugAssign(expr, augOp.TrimEnd('='), value);
            }
        }

        return new PyExprStmt(expr);
    }

    private PyYield parse_yield_stmt()
    {
        consume_keyword("yield", "NPY2023", "期望 'yield' 关键字");

        PyAstNode? value = null;

        if (!check(PythonNodeKind.new_line) && !check(PythonNodeKind.dedent) && !is_at_end())
        {
            value = parse_expression();
        }

        return new PyYield(value, Span: default);
    }

    private PyTry parse_try_stmt()
    {
        consume_keyword("try", "NPY2024", "期望 'try' 关键字");

        consume(PythonNodeKind.delimiter, "NPY2025", "期望 ':'");

        var body = parse_suite();

        var handlers = new List<PyExceptClause>();

        while (match(PythonNodeKind.keyword, "except"))
        {
            PyAstNode? exceptionType = null;
            string? exceptName = null;

            if (!check(PythonNodeKind.delimiter, ":"))
            {
                exceptionType = parse_expression();

                if (match(PythonNodeKind.keyword, "as"))
                {
                    exceptName = consume(PythonNodeKind.identifier, "NPY2026", "期望异常变量名").text;
                }
            }

            consume(PythonNodeKind.delimiter, "NPY2027", "期望 ':'");
            var handlerBody = parse_suite();

            handlers.Add(new PyExceptClause(exceptionType, exceptName, handlerBody));
        }

        IReadOnlyList<PyAstNode>? elseBody = null;

        skip_new_lines();

        if (match(PythonNodeKind.keyword, "else"))
        {
            consume(PythonNodeKind.delimiter, "NPY2028", "期望 ':'");
            elseBody = [.. parse_suite()];
        }

        IReadOnlyList<PyAstNode>? finallyBody = null;

        skip_new_lines();

        if (match(PythonNodeKind.keyword, "finally"))
        {
            consume(PythonNodeKind.delimiter, "NPY2029", "期望 ':'");
            finallyBody = [.. parse_suite()];
        }

        return new PyTry(body, handlers, elseBody, finallyBody, Span: default);
    }

    private PyRaise parse_raise_stmt()
    {
        consume_keyword("raise", "NPY2030", "期望 'raise' 关键字");

        PyAstNode? exception = null;

        if (!check(PythonNodeKind.new_line) && !check(PythonNodeKind.dedent) && !is_at_end())
        {
            exception = parse_expression();
        }

        return new PyRaise(exception, Span: default);
    }

    private PyImport parse_import_stmt()
    {
        consume_keyword("import", "NPY2031", "期望 'import' 关键字");

        var items = new List<PyImportItem> { parse_import_item() };

        while (match(PythonNodeKind.delimiter, ","))
        {
            items.Add(parse_import_item());
        }

        return new PyImport(items, Span: default);
    }

    private PyFromImport parse_from_import_stmt()
    {
        consume_keyword("from", "NPY2032", "期望 'from' 关键字");

        var moduleBuilder = new System.Text.StringBuilder();
        moduleBuilder.Append(consume(PythonNodeKind.identifier, "NPY2033", "期望模块名").text);

        while (match(PythonNodeKind.delimiter, "."))
        {
            moduleBuilder.Append('.');

            if (check(PythonNodeKind.identifier))
            {
                moduleBuilder.Append(advance().text);
            }
        }

        var module = moduleBuilder.ToString();

        consume_keyword("import", "NPY2034", "期望 'import' 关键字");

        var items = new List<PyImportItem> { parse_import_item() };

        while (match(PythonNodeKind.delimiter, ","))
        {
            items.Add(parse_import_item());
        }

        return new PyFromImport(module, items, Span: default);
    }

    private PyImportItem parse_import_item()
    {
        var name = consume(PythonNodeKind.identifier, "NPY2036", "期望标识符").text;

        string? alias = null;

        if (match(PythonNodeKind.keyword, "as"))
        {
            alias = consume(PythonNodeKind.identifier, "NPY2037", "期望别名").text;
        }

        return new PyImportItem(name ?? "", alias);
    }

    private IReadOnlyList<PyAstNode> parse_suite()
    {
        var statements = new List<PyAstNode>();

        if (check(PythonNodeKind.new_line))
        {
            advance();
            expect_indent();

            while (!check(PythonNodeKind.dedent) && !is_at_end())
            {
                skip_new_lines();

                if (check(PythonNodeKind.dedent) || is_at_end())
                {
                    break;
                }

                var stmt = parse_statement();
                if (stmt is not null)
                {
                    statements.Add(stmt);
                }
            }

            expect_dedent();
        }
        else
        {
            var stmt = parse_statement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return statements;
    }

    #endregion

    #region Expressions

    private PyAstNode parse_expression()
    {
        return parse_or();
    }

    private PyAstNode parse_or()
    {
        var left = parse_and();

        while (match(PythonNodeKind.keyword, "or"))
        {
            var op = previous().text;
            var right = parse_and();
            left = new PyBinaryOp(left, op ?? "", right);
        }

        return left;
    }

    private PyAstNode parse_and()
    {
        var left = parse_not();

        while (match(PythonNodeKind.keyword, "and"))
        {
            var op = previous().text;
            var right = parse_not();
            left = new PyBinaryOp(left, op ?? "", right);
        }

        return left;
    }

    private PyAstNode parse_not()
    {
        if (match(PythonNodeKind.keyword, "not"))
        {
            var operand = parse_not();
            return new PyUnaryOp("not", operand);
        }

        return parse_comparison();
    }

    private PyAstNode parse_comparison()
    {
        var left = parse_addition();

        while (check(PythonNodeKind.@operator) &&
               (peek().text is "==" or "!=" or "<" or ">" or "<=" or ">="))
        {
            var op = advance().text;
            var right = parse_addition();
            left = new PyBinaryOp(left, op ?? "", right);
        }

        return left;
    }

    private PyAstNode parse_addition()
    {
        var left = parse_multiplication();

        while (check(PythonNodeKind.@operator) && (peek().text is "+" or "-"))
        {
            var op = advance().text;
            var right = parse_multiplication();
            left = new PyBinaryOp(left, op ?? "", right);
        }

        return left;
    }

    private PyAstNode parse_multiplication()
    {
        var left = parse_unary();

        while (check(PythonNodeKind.@operator) &&
               (peek().text is "*" or "/" or "//" or "%" or "@"))
        {
            var op = advance().text;
            var right = parse_unary();
            left = new PyBinaryOp(left, op ?? "", right);
        }

        return left;
    }

    private PyAstNode parse_unary()
    {
        if (check(PythonNodeKind.@operator) && (peek().text is "+" or "-" or "~"))
        {
            var op = advance().text;
            var operand = parse_unary();
            return new PyUnaryOp(op ?? "", operand);
        }

        return parse_power();
    }

    private PyAstNode parse_power()
    {
        var left = parse_postfix();

        if (match(PythonNodeKind.@operator, "**"))
        {
            var right = parse_unary();
            left = new PyBinaryOp(left, "**", right);
        }

        return left;
    }

    private PyAstNode parse_postfix()
    {
        var expr = parse_primary();

        while (true)
        {
            if (match(PythonNodeKind.delimiter, "."))
            {
                var name = consume(PythonNodeKind.identifier, "NPY2030", "期望属性名").text;
                expr = new PyAttribute(expr, name ?? "");
            }
            else if (match(PythonNodeKind.delimiter, "("))
            {
                var args = new List<PyAstNode>();

                if (!check(PythonNodeKind.delimiter, ")"))
                {
                    args.Add(parse_expression());

                    while (match(PythonNodeKind.delimiter, ","))
                    {
                        args.Add(parse_expression());
                    }
                }

                consume(PythonNodeKind.delimiter, "NPY2031", "期望 ')'");
                expr = new PyCall(expr, args);
            }
            else if (match(PythonNodeKind.delimiter, "["))
            {
                var index = parse_expression();
                consume(PythonNodeKind.delimiter, "NPY2032", "期望 ']'");
                expr = new PySubscript(expr, index);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private PyAstNode parse_primary()
    {
        if (match(PythonNodeKind.number))
        {
            var token = previous();
            return new PyLiteral("number", token.text ?? "", Span: default);
        }

        if (match(PythonNodeKind.@string))
        {
            var token = previous();
            return new PyLiteral("string", token.text ?? "", Span: default);
        }

        if (check(PythonNodeKind.keyword))
        {
            if (peek().text is "True" or "False" or "None")
            {
                var token = advance();
                return new PyLiteral(token.text?.ToLowerInvariant() ?? "", token.text ?? "", Span: default);
            }
        }

        if (match(PythonNodeKind.identifier))
        {
            var token = previous();
            return new PyIdentifier(token.text ?? "", Span: default);
        }

        if (match(PythonNodeKind.delimiter, "("))
        {
            var expr = parse_expression();
            consume(PythonNodeKind.delimiter, "NPY2033", "期望 ')'");
            return expr;
        }

        if (match(PythonNodeKind.delimiter, "["))
        {
            var elements = new List<PyAstNode>();

            if (!check(PythonNodeKind.delimiter, "]"))
            {
                elements.Add(parse_expression());

                while (match(PythonNodeKind.delimiter, ","))
                {
                    elements.Add(parse_expression());
                }
            }

            consume(PythonNodeKind.delimiter, "NPY2034", "期望 ']'");
            return new PyList(elements);
        }

        var errorToken = peek();
        _diagnostics?.report_error(
            string.Empty,
            default,
            "NPY2035",
            $"意外的标记 '{errorToken.text}'");

        throw new ParseException($"意外的标记 '{errorToken.text}'");
    }

    #endregion
}
