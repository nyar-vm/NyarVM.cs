using Std.Data.Text.Diagnostics;
using Std.Data.Text.Julia.AST;
using Std.Data.Text.Julia.Lexer;
using Std.Data.Text.Parsing;

namespace Std.Data.Text.Julia.Parser;

public sealed class JlParser
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<JlToken> _tokens = [];

    public JlParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     解析 Token 列表为 AST
    /// </summary>
    public JlAstNode parse(IReadOnlyList<JlToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var statements = new List<JlAstNode>();

        while (!is_at_end())
        {
            var stmt = parse_statement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return new JlBlock(statements);
    }

    #region Token Access

    private bool is_at_end()
    {
        return peek().type == JlTokenType.eof;
    }

    private JlToken peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private JlToken previous()
    {
        return _tokens[_current - 1];
    }

    private JlToken advance()
    {
        if (!is_at_end())
        {
            _current++;
        }

        return previous();
    }

    private bool check(JlTokenType type)
    {
        return !is_at_end() && peek().type == type;
    }

    private bool check(JlTokenType type, string value)
    {
        return !is_at_end() && peek().type == type && peek().value == value;
    }

    private bool match(JlTokenType type)
    {
        if (check(type))
        {
            advance();
            return true;
        }

        return false;
    }

    private bool match(JlTokenType type, string value)
    {
        if (check(type, value))
        {
            advance();
            return true;
        }

        return false;
    }

    private JlToken consume(JlTokenType type, string errorCode, string message)
    {
        if (check(type))
        {
            return advance();
        }

        var token = peek();
        _diagnostics?.add_error(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private JlToken consume_keyword(string keyword, string errorCode, string message)
    {
        if (check(JlTokenType.keyword, keyword))
        {
            return advance();
        }

        var token = peek();
        _diagnostics?.add_error(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void synchronize()
    {
        advance();

        while (!is_at_end())
        {
            if (peek().type == JlTokenType.keyword)
            {
                var value = peek().value;
                if (value is "function" or "macro" or "struct" or "module"
                    or "if" or "for" or "while" or "try" or "begin"
                    or "import" or "using" or "export")
                {
                    return;
                }
            }

            advance();
        }
    }

    #endregion

    #region Statements

    private JlAstNode? parse_statement()
    {
        try
        {
            if (check(JlTokenType.keyword, "module"))
            {
                return parse_module();
            }

            if (check(JlTokenType.keyword, "import") || check(JlTokenType.keyword, "using"))
            {
                return parse_import();
            }

            if (check(JlTokenType.keyword, "export"))
            {
                return parse_export();
            }

            if (check(JlTokenType.keyword, "function"))
            {
                return parse_function_def();
            }

            if (check(JlTokenType.keyword, "macro"))
            {
                return parse_macro_def();
            }

            if (check(JlTokenType.keyword, "struct") || check(JlTokenType.keyword, "abstract"))
            {
                return parse_struct_def();
            }

            if (check(JlTokenType.keyword, "if"))
            {
                return parse_if_expr();
            }

            if (check(JlTokenType.keyword, "for"))
            {
                return parse_for_expr();
            }

            if (check(JlTokenType.keyword, "while"))
            {
                return parse_while_expr();
            }

            if (check(JlTokenType.keyword, "try"))
            {
                return parse_try_expr();
            }

            if (check(JlTokenType.keyword, "let"))
            {
                return parse_let_expr();
            }

            if (check(JlTokenType.keyword, "return"))
            {
                return parse_return_expr();
            }

            if (check(JlTokenType.keyword, "break"))
            {
                advance();
                return new JlBreakExpr();
            }

            if (check(JlTokenType.keyword, "continue"))
            {
                advance();
                return new JlContinueExpr();
            }

            if (check(JlTokenType.keyword, "begin"))
            {
                return parse_block();
            }

            return parse_expr_statement();
        }
        catch (ParseException)
        {
            synchronize();
            return null;
        }
    }

    private JlModule parse_module()
    {
        consume_keyword("module", "JLA2001", "期望 'module' 关键字");
        var name = consume(JlTokenType.identifier, "JLA2002", "期望模块名").value;

        var statements = new List<JlAstNode>();

        while (!check(JlTokenType.keyword, "end") && !is_at_end())
        {
            var stmt = parse_statement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        consume_keyword("end", "JLA2003", "期望 'end'");

        return new JlModule(name, [], [], statements);
    }

    private JlImport parse_import()
    {
        var isUsing = match(JlTokenType.keyword, "using");
        if (!isUsing)
        {
            consume_keyword("import", "JLA2004", "期望 'import' 或 'using'");
        }

        var moduleBuilder = new System.Text.StringBuilder();
        moduleBuilder.Append(consume(JlTokenType.identifier, "JLA2005", "期望模块名").value);

        while (match(JlTokenType.@operator, "."))
            moduleBuilder.Append('.').Append(consume(JlTokenType.identifier, "JLA2006", "期望标识符").value);

        var moduleName = moduleBuilder.ToString();

        var names = new List<string>();
        if (match(JlTokenType.@operator, ":"))
        {
            names.Add(consume(JlTokenType.identifier, "JLA2007", "期望导入名").value);
            while (match(JlTokenType.delimiter, ","))
                names.Add(consume(JlTokenType.identifier, "JLA2008", "期望导入名").value);
        }

        return new JlImport(isUsing, moduleName, names);
    }

    private JlExport parse_export()
    {
        consume_keyword("export", "JLA2009", "期望 'export' 关键字");

        var names = new List<string>();
        names.Add(consume(JlTokenType.identifier, "JLA2010", "期望导出名").value);
        while (match(JlTokenType.delimiter, ","))
            names.Add(consume(JlTokenType.identifier, "JLA2011", "期望导出名").value);

        return new JlExport(names);
    }

    private JlFunctionDef parse_function_def()
    {
        consume_keyword("function", "JLA2012", "期望 'function' 关键字");

        var name = consume(JlTokenType.identifier, "JLA2013", "期望函数名").value;

        consume(JlTokenType.delimiter, "JLA2014", "期望 '('");
        var parameters = parse_parameters();
        consume(JlTokenType.delimiter, "JLA2015", "期望 ')'");

        JlAstNode? returnType = null;
        if (match(JlTokenType.@operator, "::"))
        {
            returnType = parse_type();
        }

        var whereParams = new List<string>();
        if (match(JlTokenType.keyword, "where"))
        {
            whereParams.Add(consume(JlTokenType.identifier, "JLA2016", "期望类型参数").value);
            while (match(JlTokenType.delimiter, ","))
                whereParams.Add(consume(JlTokenType.identifier, "JLA2017", "期望类型参数").value);
        }

        var body = parse_block_body();

        return new JlFunctionDef(name, parameters, whereParams, returnType, body, false);
    }

    private JlMacroDef parse_macro_def()
    {
        consume_keyword("macro", "JLA2018", "期望 'macro' 关键字");

        var name = consume(JlTokenType.identifier, "JLA2019", "期望宏名").value;

        consume(JlTokenType.delimiter, "JLA2020", "期望 '('");
        var parameters = parse_parameters();
        consume(JlTokenType.delimiter, "JLA2021", "期望 ')'");

        var body = parse_block_body();

        return new JlMacroDef(name, parameters, body);
    }

    private JlAstNode parse_struct_def()
    {
        var isAbstract = match(JlTokenType.keyword, "abstract");

        if (isAbstract)
        {
            consume_keyword("type", "JLA2022", "期望 'type' 关键字");
            var name = consume(JlTokenType.identifier, "JLA2023", "期望类型名").value;

            var typeParams = new List<string>();
            if (match(JlTokenType.delimiter, "{"))
            {
                typeParams.Add(consume(JlTokenType.identifier, "JLA2024", "期望类型参数").value);
                while (match(JlTokenType.delimiter, ","))
                    typeParams.Add(consume(JlTokenType.identifier, "JLA2025", "期望类型参数").value);
                consume(JlTokenType.delimiter, "JLA2026", "期望 '}'");
            }

            JlAstNode? superType = null;
            if (match(JlTokenType.@operator, "<:"))
            {
                superType = parse_type();
            }

            consume_keyword("end", "JLA2027", "期望 'end'");

            return new JlTypeDef(name, typeParams, superType);
        }

        var isMutable = match(JlTokenType.keyword, "mutable");
        if (isMutable)
        {
            consume_keyword("struct", "JLA2028", "期望 'struct' 关键字");
        }
        else
        {
            consume_keyword("struct", "JLA2029", "期望 'struct' 关键字");
        }

        var structName = consume(JlTokenType.identifier, "JLA2030", "期望结构体名").value;

        var structTypeParams = new List<string>();
        if (match(JlTokenType.delimiter, "{"))
        {
            structTypeParams.Add(consume(JlTokenType.identifier, "JLA2031", "期望类型参数").value);
            while (match(JlTokenType.delimiter, ","))
                structTypeParams.Add(consume(JlTokenType.identifier, "JLA2032", "期望类型参数").value);
            consume(JlTokenType.delimiter, "JLA2033", "期望 '}'");
        }

        JlAstNode? structSuperType = null;
        if (match(JlTokenType.@operator, "<:"))
        {
            structSuperType = parse_type();
        }

        var fields = new List<JlAstNode>();

        while (!check(JlTokenType.keyword, "end") && !is_at_end())
        {
            var fieldName = consume(JlTokenType.identifier, "JLA2034", "期望字段名").value;

            JlAstNode? fieldType = null;
            if (match(JlTokenType.@operator, "::"))
            {
                fieldType = parse_type();
            }

            fields.Add(new JlField(fieldName, fieldType));
        }

        consume_keyword("end", "JLA2035", "期望 'end'");

        return new JlStructDef(structName, structTypeParams, fields, isMutable, false);
    }

    private IReadOnlyList<JlAstNode> parse_parameters()
    {
        var parameters = new List<JlAstNode>();

        while (!check(JlTokenType.delimiter, ")") && !is_at_end())
        {
            var isVarargs = match(JlTokenType.@operator, "...");

            var name = consume(JlTokenType.identifier, "JLA2036", "期望参数名").value;

            JlAstNode? typeAnnotation = null;
            if (match(JlTokenType.@operator, "::"))
            {
                typeAnnotation = parse_type();
            }

            JlAstNode? defaultValue = null;
            if (match(JlTokenType.@operator, "="))
            {
                defaultValue = parse_expression();
            }

            parameters.Add(new JlParameter(name, typeAnnotation, defaultValue, isVarargs));

            if (!match(JlTokenType.delimiter, ","))
            {
                break;
            }
        }

        return parameters;
    }

    private JlBlock parse_block_body()
    {
        var statements = new List<JlAstNode>();

        while (!check(JlTokenType.keyword, "end") && !is_at_end())
        {
            var stmt = parse_statement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        consume_keyword("end", "JLA2037", "期望 'end'");

        return new JlBlock(statements);
    }

    private JlIfExpr parse_if_expr()
    {
        consume_keyword("if", "JLA2038", "期望 'if' 关键字");
        var condition = parse_expression();

        var thenBranch = parse_block_or_statement();

        var elseIfBranches = new List<(JlAstNode Condition, JlAstNode Body)>();

        while (check(JlTokenType.keyword, "elseif"))
        {
            advance();
            var elseIfCond = parse_expression();
            var elseIfBody = parse_block_or_statement();
            elseIfBranches.Add((elseIfCond, elseIfBody));
        }

        JlAstNode? elseBranch = null;
        if (match(JlTokenType.keyword, "else"))
        {
            elseBranch = parse_block_or_statement();
        }

        consume_keyword("end", "JLA2039", "期望 'end'");

        return new JlIfExpr(condition, thenBranch, elseIfBranches, elseBranch);
    }

    private JlForExpr parse_for_expr()
    {
        consume_keyword("for", "JLA2040", "期望 'for' 关键字");

        var iterators = new List<(JlAstNode Iterator, JlAstNode Iterable)>();
        iterators.Add(parse_iterator());

        while (match(JlTokenType.delimiter, ",")) iterators.Add(parse_iterator());

        var body = parse_block_or_statement();

        consume_keyword("end", "JLA2041", "期望 'end'");

        return new JlForExpr(iterators, body);
    }

    private (JlAstNode Iterator, JlAstNode Iterable) parse_iterator()
    {
        var iterator = parse_expression();
        consume_keyword("in", "JLA2042", "期望 'in' 关键字");
        var iterable = parse_expression();

        return (iterator, iterable);
    }

    private JlWhileExpr parse_while_expr()
    {
        consume_keyword("while", "JLA2043", "期望 'while' 关键字");
        var condition = parse_expression();
        var body = parse_block_or_statement();

        consume_keyword("end", "JLA2044", "期望 'end'");

        return new JlWhileExpr(condition, body);
    }

    private JlTryExpr parse_try_expr()
    {
        consume_keyword("try", "JLA2045", "期望 'try' 关键字");
        var body = parse_block_or_statement();

        var catchClauses = new List<(JlAstNode Pattern, JlAstNode Body)>();

        if (match(JlTokenType.keyword, "catch"))
        {
            JlAstNode pattern = new JlIdentifier("_");
            if (check(JlTokenType.identifier))
            {
                pattern = new JlIdentifier(advance().value);
            }

            var catchBody = parse_block_or_statement();
            catchClauses.Add((pattern, catchBody));
        }

        JlAstNode? finallyBody = null;
        if (match(JlTokenType.keyword, "finally"))
        {
            finallyBody = parse_block_or_statement();
        }

        consume_keyword("end", "JLA2046", "期望 'end'");

        return new JlTryExpr(body, catchClauses, finallyBody);
    }

    private JlLetExpr parse_let_expr()
    {
        consume_keyword("let", "JLA2047", "期望 'let' 关键字");

        var bindings = new List<JlAstNode>();
        while (!check(JlTokenType.keyword, "end") && !is_at_end() && !check(JlTokenType.delimiter, ";"))
        {
            var name = consume(JlTokenType.identifier, "JLA2048", "期望绑定名").value;

            JlAstNode? typeAnnotation = null;
            if (match(JlTokenType.@operator, "::"))
            {
                typeAnnotation = parse_type();
            }

            JlAstNode? value = null;
            if (match(JlTokenType.@operator, "="))
            {
                value = parse_expression();
            }

            bindings.Add(new JlAssignment(new JlIdentifier(name), value ?? new JlUnit()));

            if (!match(JlTokenType.delimiter, ","))
            {
                break;
            }
        }

        match(JlTokenType.delimiter, ";");

        var body = parse_block_or_statement();

        consume_keyword("end", "JLA2049", "期望 'end'");

        return new JlLetExpr(bindings, body);
    }

    private JlReturnExpr parse_return_expr()
    {
        consume_keyword("return", "JLA2050", "期望 'return' 关键字");

        JlAstNode? value = null;
        if (!check(JlTokenType.keyword, "end") && !check(JlTokenType.delimiter, ";") && !is_at_end())
        {
            value = parse_expression();
        }

        return new JlReturnExpr(value);
    }

    private JlBlock parse_block()
    {
        consume_keyword("begin", "JLA2051", "期望 'begin' 关键字");

        var statements = new List<JlAstNode>();

        while (!check(JlTokenType.keyword, "end") && !is_at_end())
        {
            var stmt = parse_statement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        consume_keyword("end", "JLA2052", "期望 'end'");

        return new JlBlock(statements);
    }

    private JlAstNode parse_block_or_statement()
    {
        if (check(JlTokenType.keyword, "begin"))
        {
            return parse_block();
        }

        var statements = new List<JlAstNode>();

        while (!check(JlTokenType.keyword, "end") && !check(JlTokenType.keyword, "elseif") &&
               !check(JlTokenType.keyword, "else") && !check(JlTokenType.keyword, "catch") &&
               !check(JlTokenType.keyword, "finally") && !is_at_end())
        {
            var stmt = parse_statement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }

            match(JlTokenType.delimiter, ";");
        }

        if (statements.Count == 1)
        {
            return statements[0];
        }

        return new JlBlock(statements);
    }

    private JlAstNode parse_expr_statement()
    {
        var expr = parse_expression();

        if (match(JlTokenType.@operator, "="))
        {
            var right = parse_expression();
            return new JlAssignment(expr, right);
        }

        var compoundOps = new[] { "+=", "-=", "*=", "/=", "//=", "^=", "%=" };
        foreach (var op in compoundOps)
        {
            if (match(JlTokenType.@operator, op))
            {
                var right = parse_expression();
                return new JlCompoundAssignment(expr, op, right);
            }
        }

        return expr;
    }

    #endregion

    #region Expressions

    private JlAstNode parse_expression()
    {
        return parse_assignment();
    }

    private JlAstNode parse_assignment()
    {
        var expr = parse_ternary();

        if (check(JlTokenType.@operator, "="))
        {
            advance();
            var right = parse_assignment();
            return new JlAssignment(expr, right);
        }

        return expr;
    }

    private JlAstNode parse_ternary()
    {
        var expr = parse_or();

        if (match(JlTokenType.@operator, "?"))
        {
            var thenBranch = parse_expression();
            consume(JlTokenType.punctuation, "JLA2053", "期望 ':'");
            var elseBranch = parse_ternary();
            return new JlTernary(expr, thenBranch, elseBranch);
        }

        return expr;
    }

    private JlAstNode parse_or()
    {
        var left = parse_and();

        while (match(JlTokenType.@operator, "||"))
        {
            var right = parse_and();
            left = new JlBinaryOp(left, "||", right);
        }

        return left;
    }

    private JlAstNode parse_and()
    {
        var left = parse_comparison();

        while (match(JlTokenType.@operator, "&&"))
        {
            var right = parse_comparison();
            left = new JlBinaryOp(left, "&&", right);
        }

        return left;
    }

    private JlAstNode parse_comparison()
    {
        var left = parse_pipe();

        while (check(JlTokenType.@operator) &&
               peek().value is "==" or "!=" or "<" or ">" or "<=" or ">=" or "===" or "!==" or "isa")
        {
            var op = advance().value;
            var right = parse_pipe();
            left = new JlBinaryOp(left, op, right);
        }

        return left;
    }

    private JlAstNode parse_pipe()
    {
        var left = parse_range();

        while (check(JlTokenType.@operator, "|>") || check(JlTokenType.@operator, "<|"))
        {
            var isReverse = advance().value == "<|";
            var right = parse_range();
            left = new JlPipe(left, right, isReverse);
        }

        return left;
    }

    private JlAstNode parse_range()
    {
        var left = parse_additive();

        if (check(JlTokenType.@operator, "..") || check(JlTokenType.@operator, ":"))
        {
            advance();
            var end = parse_additive();
            return new JlRange(left, null, end);
        }

        return left;
    }

    private JlAstNode parse_additive()
    {
        var left = parse_multiplicative();

        while (check(JlTokenType.@operator) && peek().value is "+" or "-")
        {
            var op = advance().value;
            var right = parse_multiplicative();
            left = new JlBinaryOp(left, op, right);
        }

        return left;
    }

    private JlAstNode parse_multiplicative()
    {
        var left = parse_unary();

        while (check(JlTokenType.@operator) && peek().value is "*" or "/" or "//" or "%" or "\\" or "÷")
        {
            var op = advance().value;
            var right = parse_unary();
            left = new JlBinaryOp(left, op, right);
        }

        return left;
    }

    private JlAstNode parse_unary()
    {
        if (check(JlTokenType.@operator, "-") || check(JlTokenType.@operator, "!"))
        {
            var op = advance().value;
            var operand = parse_unary();
            return new JlUnaryOp(op, operand, true);
        }

        return parse_power();
    }

    private JlAstNode parse_power()
    {
        var left = parse_postfix();

        if (match(JlTokenType.@operator, "^"))
        {
            var right = parse_unary();
            left = new JlBinaryOp(left, "^", right);
        }

        return left;
    }

    private JlAstNode parse_postfix()
    {
        var expr = parse_primary();

        while (true)
        {
            if (check(JlTokenType.delimiter, "("))
            {
                advance();
                var args = new List<JlAstNode>();
                var kwargs = new List<JlAstNode>();

                if (!check(JlTokenType.delimiter, ")"))
                {
                    args.Add(parse_expression());
                    while (match(JlTokenType.delimiter, ",")) args.Add(parse_expression());
                }

                consume(JlTokenType.delimiter, "JLA2054", "期望 ')'");

                expr = new JlCall(expr, args, kwargs);
            }
            else if (check(JlTokenType.delimiter, "["))
            {
                advance();
                var indices = new List<JlAstNode>();

                if (!check(JlTokenType.delimiter, "]"))
                {
                    indices.Add(parse_expression());
                    while (match(JlTokenType.delimiter, ",")) indices.Add(parse_expression());
                }

                consume(JlTokenType.delimiter, "JLA2055", "期望 ']'");
                expr = new JlIndex(expr, indices);
            }
            else if (match(JlTokenType.@operator, "."))
            {
                var field = consume(JlTokenType.identifier, "JLA2056", "期望字段名").value;
                expr = new JlFieldAccess(expr, field);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private JlAstNode parse_primary()
    {
        if (check(JlTokenType.number))
        {
            var token = advance();
            return new JlLiteral("number", token.value);
        }

        if (check(JlTokenType.@string))
        {
            var token = advance();
            return new JlLiteral("string", token.value);
        }

        if (check(JlTokenType.@char))
        {
            var token = advance();
            return new JlLiteral("char", token.value);
        }

        if (check(JlTokenType.keyword, "true"))
        {
            advance();
            return new JlLiteral("bool", "true");
        }

        if (check(JlTokenType.keyword, "false"))
        {
            advance();
            return new JlLiteral("bool", "false");
        }

        if (check(JlTokenType.keyword, "nothing"))
        {
            advance();
            return new JlUnit();
        }

        if (check(JlTokenType.identifier))
        {
            var token = advance();
            return new JlIdentifier(token.value);
        }

        if (check(JlTokenType.macro_name))
        {
            var token = advance();
            return new JlMacroCall(token.value, []);
        }

        if (check(JlTokenType.symbol))
        {
            var token = advance();
            return new JlSymbol(token.value);
        }

        if (check(JlTokenType.command_type))
        {
            var token = advance();
            return new JlCommand(token.value);
        }

        if (check(JlTokenType.delimiter, "("))
        {
            advance();

            if (match(JlTokenType.delimiter, ")"))
            {
                return new JlTuple([]);
            }

            var expr = parse_expression();

            if (check(JlTokenType.delimiter, ","))
            {
                var elements = new List<JlAstNode> { expr };
                while (match(JlTokenType.delimiter, ",")) elements.Add(parse_expression());
                consume(JlTokenType.delimiter, "JLA2057", "期望 ')'");
                return new JlTuple(elements);
            }

            consume(JlTokenType.delimiter, "JLA2058", "期望 ')'");
            return expr;
        }

        if (check(JlTokenType.delimiter, "["))
        {
            advance();

            if (match(JlTokenType.delimiter, "]"))
            {
                return new JlArray([]);
            }

            var elements = new List<JlAstNode>();
            elements.Add(parse_expression());
            while (match(JlTokenType.delimiter, ",")) elements.Add(parse_expression());

            consume(JlTokenType.delimiter, "JLA2059", "期望 ']'");
            return new JlArray(elements);
        }

        if (check(JlTokenType.@operator, "->"))
        {
            advance();
            var parameters = new List<JlAstNode>();
            var body = parse_expression();
            return new JlLambda(parameters, body);
        }

        var errorToken = peek();
        _diagnostics?.add_error(
            string.Empty,
            default,
            "JLA2060",
            $"意外的标记 '{errorToken.value}'");

        throw new ParseException($"意外的标记 '{errorToken.value}'");
    }

    #endregion

    #region Types

    private JlAstNode parse_type()
    {
        if (check(JlTokenType.delimiter, "{"))
        {
            advance();
            var args = new List<JlAstNode>();
            args.Add(parse_type());
            while (match(JlTokenType.delimiter, ",")) args.Add(parse_type());
            consume(JlTokenType.delimiter, "JLA2061", "期望 '}'");

            if (check(JlTokenType.identifier))
            {
                var name = advance().value;
                return new JlTypeApp(new JlTypeCon(name), args);
            }

            return new JlTupleType(args);
        }

        if (check(JlTokenType.identifier))
        {
            var name = advance().value;

            if (char.IsLower(name[0]))
            {
                return new JlTypeVar(name);
            }

            if (check(JlTokenType.delimiter, "{"))
            {
                advance();
                var args = new List<JlAstNode>();
                args.Add(parse_type());
                while (match(JlTokenType.delimiter, ",")) args.Add(parse_type());
                consume(JlTokenType.delimiter, "JLA2062", "期望 '}'");
                return new JlTypeApp(new JlTypeCon(name), args);
            }

            return new JlTypeCon(name);
        }

        if (check(JlTokenType.delimiter, "("))
        {
            advance();
            var type = parse_type();
            consume(JlTokenType.delimiter, "JLA2063", "期望 ')'");
            return type;
        }

        return new JlTypeCon("Any");
    }

    #endregion
}
