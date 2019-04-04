using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Typescript.AST;
using Std.Data.Text.Typescript.Lexer;

namespace Std.Data.Text.Typescript.Parsing;


/// <summary>
///     TypeScript 语法分析器
/// </summary>
public sealed class TsParser : IParser<IReadOnlyList<TsToken>, TsAstNode>
{
    private readonly string _file_path = string.Empty;
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<TsToken> _tokens = [];

    
/// <summary>
///     创建 TypeScript 语法分析器
/// </summary>
    public TsParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    
/// <summary>
///     解析词法单元序列
/// </summary>
    public TsAstNode parse(IReadOnlyList<TsToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var declarations = new List<TsAstNode>();

        while (!is_at_end())
        {
            var decl = parse_declaration();
            if (decl is not null)
            {
                declarations.Add(decl);
            }
        }

        return new TsCompilationUnit(declarations);
    }

    #region Token Access

    private bool is_at_end()
    {
        return peek().type == TsTokenType.eof;
    }

    private TsToken peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private TsToken peek_next()
    {
        var index = _current + 1;
        return index < _tokens.Count ? _tokens[index] : _tokens[^1];
    }

    private TsToken previous()
    {
        return _tokens[_current - 1];
    }

    private TsToken advance()
    {
        if (!is_at_end())
        {
            _current++;
        }

        return previous();
    }

    private bool check(TsTokenType type)
    {
        return !is_at_end() && peek().type == type;
    }

    private bool check(TsTokenType type, string value)
    {
        return !is_at_end() && peek().type == type && peek().value == value;
    }

    private bool match(TsTokenType type)
    {
        if (check(type))
        {
            advance();
            return true;
        }

        return false;
    }

    private bool match(TsTokenType type, string value)
    {
        if (check(type, value))
        {
            advance();
            return true;
        }

        return false;
    }

    private TsToken consume(TsTokenType type, string errorCode, string message)
    {
        if (check(type))
        {
            return advance();
        }

        var token = peek();
        _diagnostics?.report_error(
            _file_path,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private TsToken consume_keyword(string keyword, string errorCode, string message)
    {
        if (check(TsTokenType.keyword, keyword))
        {
            return advance();
        }

        var token = peek();
        _diagnostics?.report_error(
            _file_path,
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
            if (previous().type == TsTokenType.delimiter && previous().value == ";")
            {
                return;
            }

            if (peek().type == TsTokenType.keyword)
            {
                switch (peek().value)
                {
                    case "const":
                    case "let":
                    case "var":
                    case "function":
                    case "class":
                    case "interface":
                    case "type":
                    case "import":
                    case "export":
                    case "if":
                    case "for":
                    case "while":
                    case "do":
                    case "switch":
                    case "try":
                    case "return":
                    case "throw":
                    case "break":
                    case "continue":
                    case "enum":
                    case "namespace":
                        return;
                }
            }

            if (peek().type == TsTokenType.delimiter && peek().value == "}")
            {
                return;
            }

            advance();
        }
    }

    #endregion

    #region Declarations

    private TsAstNode? parse_declaration()
    {
        try
        {
            if (check(TsTokenType.keyword, "import"))
            {
                return parse_import_decl();
            }

            if (check(TsTokenType.keyword, "export"))
            {
                return parse_export_decl();
            }

            if (check(TsTokenType.keyword, "const") || check(TsTokenType.keyword, "let") ||
                check(TsTokenType.keyword, "var"))
            {
                return parse_variable_decl();
            }

            if (check(TsTokenType.keyword, "function"))
            {
                return parse_function_decl();
            }

            if (check(TsTokenType.keyword, "class"))
            {
                return parse_class_decl();
            }

            if (check(TsTokenType.keyword, "interface"))
            {
                return parse_interface_decl();
            }

            if (check(TsTokenType.keyword, "type"))
            {
                return parse_type_alias_decl();
            }

            if (check(TsTokenType.keyword, "enum"))
            {
                return parse_enum_decl();
            }

            if (check(TsTokenType.keyword, "namespace"))
            {
                return parse_namespace_decl();
            }

            return parse_statement();
        }
        catch (ParseException)
        {
            synchronize();
            return null;
        }
    }

    private TsImportDecl parse_import_decl()
    {
        consume_keyword("import", "OAK2001", "期望 'import' 关键字");

        string? alias = null;
        var isTypeOnly = false;

        if (match(TsTokenType.keyword, "type"))
        {
            isTypeOnly = true;
        }

        string modulePath;

        if (match(TsTokenType.delimiter, "{"))
        {
            while (!check(TsTokenType.delimiter, "}") && !is_at_end())
            {
                advance();

                if (match(TsTokenType.delimiter, ","))
                {
                }
            }

            consume(TsTokenType.delimiter, "OAK2002", "期望 '}'");
            consume_keyword("from", "OAK2003", "期望 'from' 关键字");
            modulePath = consume(TsTokenType.@string, "OAK2004", "期望模块路径字符串").value;
        }
        else if (match(TsTokenType.@operator, "*"))
        {
            consume_keyword("as", "OAK2005", "期望 'as' 关键字");
            alias = consume(TsTokenType.identifier, "OAK2006", "期望别名").value;
            consume_keyword("from", "OAK2007", "期望 'from' 关键字");
            modulePath = consume(TsTokenType.@string, "OAK2008", "期望模块路径字符串").value;
        }
        else
        {
            alias = consume(TsTokenType.identifier, "OAK2009", "期望导入名").value;
            consume_keyword("from", "OAK2010", "期望 'from' 关键字");
            modulePath = consume(TsTokenType.@string, "OAK2011", "期望模块路径字符串").value;
        }

        match(TsTokenType.delimiter, ";");

        return new TsImportDecl(modulePath, alias, isTypeOnly);
    }

    private TsExportDecl parse_export_decl()
    {
        consume_keyword("export", "OAK2012", "期望 'export' 关键字");
        var decl = parse_declaration();

        if (decl is null)
        {
            throw new ParseException("导出后应为声明");
        }

        var name = decl switch
        {
            TsVariableDecl v => v.name,
            TsFunctionDecl f => f.name,
            TsClassDecl c => c.name,
            TsInterfaceDecl i => i.name,
            TsTypeAliasDecl t => t.name,
            TsEnumDecl e => e.name,
            TsNamespaceDecl n => n.name,
            _ => "default"
        };

        return new TsExportDecl(name, decl);
    }

    private TsVariableDecl parse_variable_decl()
    {
        var isConst = match(TsTokenType.keyword, "const");

        if (!isConst)
        {
            if (check(TsTokenType.keyword, "let"))
            {
                advance();
            }
            else if (check(TsTokenType.keyword, "var"))
            {
                advance();
            }
        }

        var name = consume(TsTokenType.identifier, "OAK2013", "期望变量名").value;

        TsAstNode? typeAnnotation = null;
        if (match(TsTokenType.punctuation, ":"))
        {
            typeAnnotation = parse_type_annotation();
        }

        TsAstNode? initializer = null;
        if (match(TsTokenType.@operator, "="))
        {
            initializer = parse_assignment();
        }

        match(TsTokenType.delimiter, ";");

        return new TsVariableDecl(name, typeAnnotation, initializer, isConst);
    }

    private TsFunctionDecl parse_function_decl()
    {
        var isAsync = match(TsTokenType.keyword, "async");

        consume_keyword("function", "OAK2014", "期望 'function' 关键字");

        var isGenerator = match(TsTokenType.@operator, "*");

        var name = consume(TsTokenType.identifier, "OAK2015", "期望函数名").value;

        consume(TsTokenType.delimiter, "OAK2016", "期望 '('");
        var parameters = parse_parameters();
        consume(TsTokenType.delimiter, "OAK2017", "期望 ')'");

        TsAstNode? returnType = null;
        if (match(TsTokenType.punctuation, ":"))
        {
            returnType = parse_type_annotation();
        }

        var body = parse_block_stmt();

        return new TsFunctionDecl(name, parameters, returnType, body, isAsync, isGenerator);
    }

    private TsClassDecl parse_class_decl()
    {
        consume_keyword("class", "OAK2018", "期望 'class' 关键字");
        var name = consume(TsTokenType.identifier, "OAK2019", "期望类名").value;

        consume(TsTokenType.delimiter, "OAK2020", "期望 '{'");

        var members = new List<TsAstNode>();

        while (!check(TsTokenType.delimiter, "}") && !is_at_end())
        {
            var member = parse_class_member();
            if (member is not null)
            {
                members.Add(member);
            }
        }

        consume(TsTokenType.delimiter, "OAK2021", "期望 '}'");

        return new TsClassDecl(name, members);
    }

    private TsAstNode? parse_class_member()
    {
        try
        {
            if (check(TsTokenType.keyword, "get") || check(TsTokenType.keyword, "set"))
            {
                advance();
            }

            var name = consume(TsTokenType.identifier, "OAK2022", "期望成员名").value;

            if (check(TsTokenType.delimiter, "("))
            {
                return parse_method_decl(name);
            }

            TsAstNode? typeAnnotation = null;
            if (match(TsTokenType.punctuation, ":"))
            {
                typeAnnotation = parse_type_annotation();
            }

            TsAstNode? initializer = null;
            if (match(TsTokenType.@operator, "="))
            {
                initializer = parse_expression();
            }

            match(TsTokenType.delimiter, ";");

            return new TsVariableDecl(name, typeAnnotation, initializer, false);
        }
        catch (ParseException)
        {
            synchronize();
            return null;
        }
    }

    private TsFunctionDecl parse_method_decl(string name)
    {
        consume(TsTokenType.delimiter, "OAK2023", "期望 '('");
        var parameters = parse_parameters();
        consume(TsTokenType.delimiter, "OAK2024", "期望 ')'");

        TsAstNode? returnType = null;
        if (match(TsTokenType.punctuation, ":"))
        {
            returnType = parse_type_annotation();
        }

        var body = parse_block_stmt();

        return new TsFunctionDecl(name, parameters, returnType, body, false, false);
    }

    private TsInterfaceDecl parse_interface_decl()
    {
        consume_keyword("interface", "OAK2025", "期望 'interface' 关键字");
        var name = consume(TsTokenType.identifier, "OAK2026", "期望接口名").value;

        consume(TsTokenType.delimiter, "OAK2027", "期望 '{'");

        var members = new List<TsAstNode>();

        while (!check(TsTokenType.delimiter, "}") && !is_at_end())
        {
            var memberName = consume(TsTokenType.identifier, "OAK2028", "期望属性名").value;

            match(TsTokenType.@operator, "?");

            consume(TsTokenType.punctuation, "OAK2029", "期望 ':'");
            var memberType = parse_type_annotation();

            match(TsTokenType.delimiter, ";");

            members.Add(new TsVariableDecl(memberName, memberType, null, false));
        }

        consume(TsTokenType.delimiter, "OAK2030", "期望 '}'");

        return new TsInterfaceDecl(name, members);
    }

    private TsTypeAliasDecl parse_type_alias_decl()
    {
        consume_keyword("type", "OAK2031", "期望 'type' 关键字");
        var name = consume(TsTokenType.identifier, "OAK2032", "期望类型别名").value;

        consume(TsTokenType.@operator, "OAK2033", "期望 '='");
        var type = parse_type_annotation();
        match(TsTokenType.delimiter, ";");

        return new TsTypeAliasDecl(name, type);
    }

    private TsEnumDecl parse_enum_decl()
    {
        consume_keyword("enum", "OAK2070", "期望 'enum' 关键字");

        var isConst = false;
        if (check(TsTokenType.keyword, "const"))
        {
            advance();
            isConst = true;
        }

        var name = consume(TsTokenType.identifier, "OAK2071", "期望枚举名").value;

        consume(TsTokenType.delimiter, "OAK2072", "期望 '{'");

        var members = new List<TsEnumMember>();

        while (!check(TsTokenType.delimiter, "}") && !is_at_end())
        {
            var memberName = consume(TsTokenType.identifier, "OAK2073", "期望枚举成员名").value;

            TsAstNode? initializer = null;
            if (match(TsTokenType.@operator, "="))
            {
                initializer = parse_assignment();
            }

            members.Add(new TsEnumMember(memberName, initializer));

            if (!match(TsTokenType.delimiter, ","))
            {
                break;
            }
        }

        consume(TsTokenType.delimiter, "OAK2074", "期望 '}'");

        return new TsEnumDecl(name, members, isConst);
    }

    private TsNamespaceDecl parse_namespace_decl()
    {
        consume_keyword("namespace", "OAK2075", "期望 'namespace' 关键字");
        var name = consume(TsTokenType.identifier, "OAK2076", "期望命名空间名").value;

        consume(TsTokenType.delimiter, "OAK2077", "期望 '{'");

        var members = new List<TsAstNode>();

        while (!check(TsTokenType.delimiter, "}") && !is_at_end())
        {
            var decl = parse_declaration();
            if (decl is not null)
            {
                members.Add(decl);
            }
        }

        consume(TsTokenType.delimiter, "OAK2078", "期望 '}'");

        return new TsNamespaceDecl(name, members);
    }

    #endregion

    #region Parameters

    private IReadOnlyList<TsParameter> parse_parameters()
    {
        var parameters = new List<TsParameter>();

        if (!check(TsTokenType.delimiter, ")"))
        {
            parameters.Add(parse_parameter());

            while (match(TsTokenType.delimiter, ",")) parameters.Add(parse_parameter());
        }

        return parameters;
    }

    private TsParameter parse_parameter()
    {
        if (match(TsTokenType.@operator, "..."))
        {
            var restName = consume(TsTokenType.identifier, "OAK2034", "期望参数名").value;

            TsAstNode? restTypeAnnotation = null;
            if (match(TsTokenType.punctuation, ":"))
            {
                restTypeAnnotation = parse_type_annotation();
            }

            return new TsParameter($"...{restName}", restTypeAnnotation, null);
        }

        var name = consume(TsTokenType.identifier, "OAK2034", "期望参数名").value;

        TsAstNode? typeAnnotation = null;
        if (match(TsTokenType.punctuation, ":"))
        {
            typeAnnotation = parse_type_annotation();
        }

        TsAstNode? defaultValue = null;
        if (match(TsTokenType.@operator, "="))
        {
            defaultValue = parse_assignment();
        }

        return new TsParameter(name, typeAnnotation, defaultValue);
    }

    #endregion

    #region Types

    private TsAstNode parse_type_annotation()
    {
        var types = new List<TsAstNode> { parse_single_type() };

        while (match(TsTokenType.@operator, "|")) types.Add(parse_single_type());

        if (types.Count == 1)
        {
            return new TsTypeAnnotation(types[0]);
        }

        return new TsTypeAnnotation(new TsUnionType(types));
    }

    private TsAstNode parse_single_type()
    {
        if (check(TsTokenType.keyword))
        {
            var name = advance().value;
            return new TsPrimitiveType(name);
        }

        if (check(TsTokenType.identifier))
        {
            var name = advance().value;
            return new TsPrimitiveType(name);
        }

        if (match(TsTokenType.delimiter, "["))
        {
            consume(TsTokenType.delimiter, "OAK2035", "期望 ']'");
            return new TsArrayType(new TsPrimitiveType("any"));
        }

        if (match(TsTokenType.delimiter, "{"))
        {
            while (!check(TsTokenType.delimiter, "}") && !is_at_end()) advance();

            consume(TsTokenType.delimiter, "OAK2036", "期望 '}'");
            return new TsPrimitiveType("object");
        }

        return new TsPrimitiveType("any");
    }

    #endregion

    #region Statements

    private TsAstNode parse_statement()
    {
        if (check(TsTokenType.keyword, "if"))
        {
            return parse_if_stmt();
        }

        if (check(TsTokenType.keyword, "for"))
        {
            return parse_for_stmt();
        }

        if (check(TsTokenType.keyword, "while"))
        {
            return parse_while_stmt();
        }

        if (check(TsTokenType.keyword, "do"))
        {
            return parse_do_while_stmt();
        }

        if (check(TsTokenType.keyword, "switch"))
        {
            return parse_switch_stmt();
        }

        if (check(TsTokenType.keyword, "try"))
        {
            return parse_try_stmt();
        }

        if (check(TsTokenType.keyword, "return"))
        {
            return parse_return_stmt();
        }

        if (check(TsTokenType.keyword, "throw"))
        {
            return parse_throw_stmt();
        }

        if (check(TsTokenType.keyword, "break"))
        {
            return parse_break_stmt();
        }

        if (check(TsTokenType.keyword, "continue"))
        {
            return parse_continue_stmt();
        }

        if (check(TsTokenType.keyword, "debugger"))
        {
            return parse_debugger_stmt();
        }

        if (check(TsTokenType.delimiter, "{"))
        {
            return parse_block_stmt();
        }

        if (check(TsTokenType.delimiter, ";"))
        {
            return parse_empty_stmt();
        }

        if (check(TsTokenType.identifier) && peek_next().type == TsTokenType.punctuation &&
            peek_next().value == ":")
        {
            return parse_labeled_stmt();
        }

        return parse_expr_stmt();
    }

    private TsBlockStmt parse_block_stmt()
    {
        consume(TsTokenType.delimiter, "OAK2037", "期望 '{'");

        var statements = new List<TsAstNode>();

        while (!check(TsTokenType.delimiter, "}") && !is_at_end())
        {
            var stmt = parse_declaration();

            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        consume(TsTokenType.delimiter, "OAK2038", "期望 '}'");

        return new TsBlockStmt(statements);
    }

    private TsIfStmt parse_if_stmt()
    {
        consume_keyword("if", "OAK2039", "期望 'if' 关键字");
        consume(TsTokenType.delimiter, "OAK2040", "期望 '('");
        var condition = parse_expression();
        consume(TsTokenType.delimiter, "OAK2041", "期望 ')'");
        var thenBlock = parse_statement();

        TsAstNode? elseBlock = null;
        if (match(TsTokenType.keyword, "else"))
        {
            elseBlock = parse_statement();
        }

        return new TsIfStmt(condition, thenBlock, elseBlock);
    }

    private TsAstNode parse_for_stmt()
    {
        consume_keyword("for", "OAK2042", "期望 'for' 关键字");

        var isAwait = match(TsTokenType.keyword, "await");

        consume(TsTokenType.delimiter, "OAK2043", "期望 '('");

        if (check(TsTokenType.keyword, "const") || check(TsTokenType.keyword, "let") ||
            check(TsTokenType.keyword, "var"))
        {
            var left = parse_variable_decl();

            if (check(TsTokenType.keyword, "in"))
            {
                advance();
                var right = parse_expression();
                consume(TsTokenType.delimiter, "OAK2080", "期望 ')'");
                var body = parse_statement();
                return new TsForInStmt(left, right, body);
            }

            if (check(TsTokenType.keyword, "of"))
            {
                advance();
                var right = parse_expression();
                consume(TsTokenType.delimiter, "OAK2081", "期望 ')'");
                var body = parse_statement();
                return new TsForOfStmt(left, right, body, isAwait);
            }

            var init = left;

            TsAstNode? condition = null;
            if (!check(TsTokenType.delimiter, ";"))
            {
                condition = parse_expression();
            }

            match(TsTokenType.delimiter, ";");

            TsAstNode? increment = null;
            if (!check(TsTokenType.delimiter, ")"))
            {
                increment = parse_expression();
            }

            consume(TsTokenType.delimiter, "OAK2044", "期望 ')'");
            var forBody = parse_statement();

            return new TsForStmt(init, condition, increment, forBody);
        }

        TsAstNode? forInit = null;
        if (!check(TsTokenType.delimiter, ";"))
        {
            forInit = parse_expression();
            match(TsTokenType.delimiter, ";");
        }
        else
        {
            match(TsTokenType.delimiter, ";");
        }

        TsAstNode? forCondition = null;
        if (!check(TsTokenType.delimiter, ";"))
        {
            forCondition = parse_expression();
        }

        match(TsTokenType.delimiter, ";");

        TsAstNode? forIncrement = null;
        if (!check(TsTokenType.delimiter, ")"))
        {
            forIncrement = parse_expression();
        }

        consume(TsTokenType.delimiter, "OAK2044", "期望 ')'");
        var forBody2 = parse_statement();

        return new TsForStmt(forInit, forCondition, forIncrement, forBody2);
    }

    private TsWhileStmt parse_while_stmt()
    {
        consume_keyword("while", "OAK2045", "期望 'while' 关键字");
        consume(TsTokenType.delimiter, "OAK2046", "期望 '('");
        var condition = parse_expression();
        consume(TsTokenType.delimiter, "OAK2047", "期望 ')'");
        var body = parse_statement();

        return new TsWhileStmt(condition, body);
    }

    private TsDoWhileStmt parse_do_while_stmt()
    {
        consume_keyword("do", "OAK2082", "期望 'do' 关键字");
        var body = parse_statement();
        consume_keyword("while", "OAK2083", "期望 'while' 关键字");
        consume(TsTokenType.delimiter, "OAK2084", "期望 '('");
        var condition = parse_expression();
        consume(TsTokenType.delimiter, "OAK2085", "期望 ')'");
        match(TsTokenType.delimiter, ";");

        return new TsDoWhileStmt(body, condition);
    }

    private TsSwitchStmt parse_switch_stmt()
    {
        consume_keyword("switch", "OAK2086", "期望 'switch' 关键字");
        consume(TsTokenType.delimiter, "OAK2087", "期望 '('");
        var expression = parse_expression();
        consume(TsTokenType.delimiter, "OAK2088", "期望 ')'");
        consume(TsTokenType.delimiter, "OAK2089", "期望 '{'");

        var cases = new List<TsSwitchCase>();

        while (!check(TsTokenType.delimiter, "}") && !is_at_end())
        {
            if (check(TsTokenType.keyword, "case"))
            {
                advance();
                var test = parse_expression();
                consume(TsTokenType.punctuation, "OAK2090", "期望 ':'");

                var statements = new List<TsAstNode>();
                while (!check(TsTokenType.keyword, "case") && !check(TsTokenType.keyword, "default") &&
                       !check(TsTokenType.delimiter, "}") && !is_at_end())
                {
                    var stmt = parse_declaration();
                    if (stmt is not null)
                    {
                        statements.Add(stmt);
                    }
                }

                cases.Add(new TsSwitchCase(test, statements));
            }
            else if (check(TsTokenType.keyword, "default"))
            {
                advance();
                consume(TsTokenType.punctuation, "OAK2091", "期望 ':'");

                var statements = new List<TsAstNode>();
                while (!check(TsTokenType.keyword, "case") && !check(TsTokenType.keyword, "default") &&
                       !check(TsTokenType.delimiter, "}") && !is_at_end())
                {
                    var stmt = parse_declaration();
                    if (stmt is not null)
                    {
                        statements.Add(stmt);
                    }
                }

                cases.Add(new TsSwitchCase(null, statements));
            }
            else
            {
                break;
            }
        }

        consume(TsTokenType.delimiter, "OAK2092", "期望 '}'");

        return new TsSwitchStmt(expression, cases);
    }

    private TsTryStmt parse_try_stmt()
    {
        consume_keyword("try", "OAK2093", "期望 'try' 关键字");
        var block = parse_block_stmt();

        TsCatchClause? catchClause = null;
        if (match(TsTokenType.keyword, "catch"))
        {
            string? paramName = null;
            TsAstNode? paramType = null;

            if (match(TsTokenType.delimiter, "("))
            {
                paramName = consume(TsTokenType.identifier, "OAK2094", "期望 catch 参数名").value;

                if (match(TsTokenType.punctuation, ":"))
                {
                    paramType = parse_type_annotation();
                }

                consume(TsTokenType.delimiter, "OAK2095", "期望 ')'");
            }

            var catchBlock = parse_block_stmt();
            catchClause = new TsCatchClause(paramName, paramType, catchBlock);
        }

        TsAstNode? finallyBlock = null;
        if (match(TsTokenType.keyword, "finally"))
        {
            finallyBlock = parse_block_stmt();
        }

        return new TsTryStmt(block, catchClause, finallyBlock);
    }

    private TsReturnStmt parse_return_stmt()
    {
        consume_keyword("return", "OAK2048", "期望 'return' 关键字");

        TsAstNode? value = null;
        if (!check(TsTokenType.delimiter, ";") && !check(TsTokenType.delimiter, "}") &&
            !is_at_end())
        {
            value = parse_expression();
        }

        match(TsTokenType.delimiter, ";");

        return new TsReturnStmt(value);
    }

    private TsThrowStmt parse_throw_stmt()
    {
        consume_keyword("throw", "OAK2096", "期望 'throw' 关键字");
        var value = parse_expression();
        match(TsTokenType.delimiter, ";");

        return new TsThrowStmt(value);
    }

    private TsBreakStmt parse_break_stmt()
    {
        consume_keyword("break", "OAK2097", "期望 'break' 关键字");

        string? label = null;
        if (check(TsTokenType.identifier) && !check(TsTokenType.delimiter, ";"))
        {
            label = advance().value;
        }

        match(TsTokenType.delimiter, ";");

        return new TsBreakStmt(label);
    }

    private TsContinueStmt parse_continue_stmt()
    {
        consume_keyword("continue", "OAK2098", "期望 'continue' 关键字");

        string? label = null;
        if (check(TsTokenType.identifier) && !check(TsTokenType.delimiter, ";"))
        {
            label = advance().value;
        }

        match(TsTokenType.delimiter, ";");

        return new TsContinueStmt(label);
    }

    private TsDebuggerStmt parse_debugger_stmt()
    {
        consume_keyword("debugger", "OAK2099", "期望 'debugger' 关键字");
        match(TsTokenType.delimiter, ";");

        return new TsDebuggerStmt();
    }

    private TsEmptyStmt parse_empty_stmt()
    {
        consume(TsTokenType.delimiter, "OAK2100", "期望 ';'");
        return new TsEmptyStmt();
    }

    private TsLabeledStmt parse_labeled_stmt()
    {
        var label = consume(TsTokenType.identifier, "OAK2101", "期望标签名").value;
        consume(TsTokenType.punctuation, "OAK2102", "期望 ':'");
        var statement = parse_statement();

        return new TsLabeledStmt(label, statement);
    }

    private TsExprStmt parse_expr_stmt()
    {
        var expr = parse_expression();
        match(TsTokenType.delimiter, ";");
        return new TsExprStmt(expr);
    }

    #endregion

    #region Expressions

    private TsAstNode parse_expression()
    {
        return parse_assignment();
    }

    private TsAstNode parse_assignment()
    {
        var expr = parse_ternary();

        if (check(TsTokenType.@operator, "=") || check(TsTokenType.@operator, "+=") ||
            check(TsTokenType.@operator, "-=") || check(TsTokenType.@operator, "*=") ||
            check(TsTokenType.@operator, "/=") || check(TsTokenType.@operator, "%=") ||
            check(TsTokenType.@operator, "&=") || check(TsTokenType.@operator, "|=") ||
            check(TsTokenType.@operator, "^=") || check(TsTokenType.@operator, "<<=") ||
            check(TsTokenType.@operator, ">>=") || check(TsTokenType.@operator, ">>>="))
        {
            var op = advance().value;
            var right = parse_assignment();
            return new TsAssignmentExpr(expr, op, right);
        }

        return expr;
    }

    private TsAstNode parse_ternary()
    {
        var expr = parse_or();

        if (match(TsTokenType.@operator, "?"))
        {
            var thenBranch = parse_expression();
            consume(TsTokenType.punctuation, "OAK2049", "期望 ':'");
            var elseBranch = parse_expression();
            return new TsConditionalExpr(expr, thenBranch, elseBranch);
        }

        return expr;
    }

    private TsAstNode parse_or()
    {
        var left = parse_and();

        while (match(TsTokenType.@operator, "||"))
        {
            var op = previous().value;
            var right = parse_and();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_and()
    {
        var left = parse_bitwise_or();

        while (match(TsTokenType.@operator, "&&"))
        {
            var op = previous().value;
            var right = parse_bitwise_or();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_bitwise_or()
    {
        var left = parse_bitwise_xor();

        while (check(TsTokenType.@operator, "|") && !check(TsTokenType.@operator, "||"))
        {
            var op = advance().value;
            var right = parse_bitwise_xor();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_bitwise_xor()
    {
        var left = parse_bitwise_and();

        while (match(TsTokenType.@operator, "^"))
        {
            var op = previous().value;
            var right = parse_bitwise_and();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_bitwise_and()
    {
        var left = parse_equality();

        while (check(TsTokenType.@operator, "&") && !check(TsTokenType.@operator, "&&"))
        {
            var op = advance().value;
            var right = parse_equality();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_equality()
    {
        var left = parse_relational();

        while (check(TsTokenType.@operator, "==") || check(TsTokenType.@operator, "!=") ||
               check(TsTokenType.@operator, "===") || check(TsTokenType.@operator, "!=="))
        {
            var op = advance().value;
            var right = parse_relational();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_relational()
    {
        var left = parse_shift();

        while (check(TsTokenType.@operator, "<") || check(TsTokenType.@operator, ">") ||
               check(TsTokenType.@operator, "<=") || check(TsTokenType.@operator, ">=") ||
               check(TsTokenType.keyword, "as"))
        {
            var op = advance().value;
            var right = parse_shift();
            left = new TsBinaryExpr(left, op, right);
        }

        if (check(TsTokenType.keyword, "instanceof"))
        {
            advance();
            var right = parse_shift();
            left = new TsInstanceofExpr(left, right);

            while (check(TsTokenType.@operator, "<") || check(TsTokenType.@operator, ">") ||
                   check(TsTokenType.@operator, "<=") || check(TsTokenType.@operator, ">=") ||
                   check(TsTokenType.keyword, "instanceof") || check(TsTokenType.keyword, "as"))
            {
                if (check(TsTokenType.keyword, "instanceof"))
                {
                    advance();
                    right = parse_shift();
                    left = new TsInstanceofExpr(left, right);
                }
                else
                {
                    var op = advance().value;
                    var nextRight = parse_shift();
                    left = new TsBinaryExpr(left, op, nextRight);
                }
            }
        }

        if (check(TsTokenType.keyword, "in"))
        {
            advance();
            var right = parse_shift();
            left = new TsBinaryExpr(left, "in", right);
        }

        return left;
    }

    private TsAstNode parse_shift()
    {
        var left = parse_additive();

        while (check(TsTokenType.@operator, "<<") || check(TsTokenType.@operator, ">>") ||
               check(TsTokenType.@operator, ">>>"))
        {
            var op = advance().value;
            var right = parse_additive();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_additive()
    {
        var left = parse_multiplicative();

        while (check(TsTokenType.@operator, "+") || check(TsTokenType.@operator, "-"))
        {
            var op = advance().value;
            var right = parse_multiplicative();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_multiplicative()
    {
        var left = parse_exponentiation();

        while (check(TsTokenType.@operator, "*") || check(TsTokenType.@operator, "/") ||
               check(TsTokenType.@operator, "%"))
        {
            var op = advance().value;
            var right = parse_exponentiation();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_exponentiation()
    {
        var left = parse_unary();

        if (check(TsTokenType.@operator, "**"))
        {
            var op = advance().value;
            var right = parse_exponentiation();
            left = new TsBinaryExpr(left, op, right);
        }

        return left;
    }

    private TsAstNode parse_unary()
    {
        if (check(TsTokenType.@operator, "-") || check(TsTokenType.@operator, "+") ||
            check(TsTokenType.@operator, "!") || check(TsTokenType.@operator, "~") ||
            check(TsTokenType.@operator, "++") || check(TsTokenType.@operator, "--"))
        {
            var op = advance().value;
            var operand = parse_unary();
            return new TsUnaryExpr(op, operand, true);
        }

        if (check(TsTokenType.keyword, "typeof"))
        {
            advance();
            var operand = parse_unary();
            return new TsTypeofExpr(operand);
        }

        if (check(TsTokenType.keyword, "void") || check(TsTokenType.keyword, "delete"))
        {
            var op = advance().value;
            var operand = parse_unary();
            return new TsUnaryExpr(op, operand, true);
        }

        if (check(TsTokenType.keyword, "await"))
        {
            advance();
            var operand = parse_unary();
            return new TsUnaryExpr("await", operand, true);
        }

        if (check(TsTokenType.keyword, "yield"))
        {
            return parse_yield_expr();
        }

        return parse_postfix();
    }

    private TsYieldExpr parse_yield_expr()
    {
        consume_keyword("yield", "OAK2103", "期望 'yield' 关键字");

        var isDelegate = match(TsTokenType.@operator, "*");

        TsAstNode? value = null;
        if (!check(TsTokenType.delimiter, ";") && !check(TsTokenType.delimiter, "}") && !is_at_end())
        {
            value = parse_assignment();
        }

        return new TsYieldExpr(value, isDelegate);
    }

    private TsAstNode parse_postfix()
    {
        var expr = parse_call_member();

        if (check(TsTokenType.@operator, "++") || check(TsTokenType.@operator, "--"))
        {
            var op = advance().value;
            return new TsUnaryExpr(op, expr, false);
        }

        return expr;
    }

    private TsAstNode parse_call_member()
    {
        var expr = parse_primary();

        while (true)
            if (check(TsTokenType.delimiter, "("))
            {
                expr = parse_call_expr(expr);
            }
            else if (match(TsTokenType.delimiter, "."))
            {
                var memberName = consume(TsTokenType.identifier, "OAK2050", "期望成员名").value;
                expr = new TsPropertyAccess(expr, memberName);
            }
            else if (check(TsTokenType.@operator, "?."))
            {
                advance();
                var memberName = consume(TsTokenType.identifier, "OAK2051", "期望成员名").value;
                expr = new TsPropertyAccess(expr, memberName);
            }
            else if (check(TsTokenType.delimiter, "["))
            {
                advance();
                var index = parse_expression();
                consume(TsTokenType.delimiter, "OAK2052", "期望 ']'");
                expr = new TsElementAccess(expr, index);
            }
            else
            {
                break;
            }

        return expr;
    }

    private TsAstNode parse_call_expr(TsAstNode callee)
    {
        consume(TsTokenType.delimiter, "OAK2053", "期望 '('");
        var args = new List<TsAstNode>();

        if (!check(TsTokenType.delimiter, ")"))
        {
            args.Add(parse_argument());

            while (match(TsTokenType.delimiter, ",")) args.Add(parse_argument());
        }

        consume(TsTokenType.delimiter, "OAK2054", "期望 ')'");
        return new TsCallExpr(callee, args);
    }

    private TsAstNode parse_argument()
    {
        if (match(TsTokenType.@operator, "..."))
        {
            return new TsSpreadElement(parse_assignment());
        }

        return parse_assignment();
    }

    private TsAstNode parse_primary()
    {
        if (check(TsTokenType.number))
        {
            advance();
            return new TsLiteral("number", previous().value);
        }

        if (check(TsTokenType.big_int))
        {
            advance();
            return new TsLiteral("bigint", previous().value);
        }

        if (check(TsTokenType.@string))
        {
            advance();
            return new TsLiteral("string", previous().value);
        }

        if (check(TsTokenType.template_string))
        {
            advance();
            return new TsLiteral("template", previous().value);
        }

        if (check(TsTokenType.literal))
        {
            advance();
            return new TsLiteral(previous().value, previous().value);
        }

        if (check(TsTokenType.keyword, "this"))
        {
            advance();
            return new TsThisExpr();
        }

        if (check(TsTokenType.keyword, "super"))
        {
            advance();
            return new TsSuperExpr();
        }

        if (check(TsTokenType.keyword, "function"))
        {
            return parse_function_expr();
        }

        if (check(TsTokenType.keyword, "new"))
        {
            return parse_new_expr();
        }

        if (check(TsTokenType.identifier))
        {
            var token = advance();

            if (check(TsTokenType.@operator, "=>"))
            {
                return parse_arrow_function([new TsParameter(previous().value, null, null)], false);
            }

            return new TsIdentifier(previous().value);
        }

        if (check(TsTokenType.delimiter, "("))
        {
            return parse_paren_or_arrow_function();
        }

        if (check(TsTokenType.delimiter, "["))
        {
            return parse_array_literal();
        }

        if (check(TsTokenType.delimiter, "{"))
        {
            return parse_object_literal();
        }

        var errorToken = peek();
        _diagnostics?.report_error(
            _file_path,
            default,
            "OAK2060",
            $"意外的标记 '{errorToken.value}'");

        throw new ParseException($"意外的标记 '{errorToken.value}'");
    }

    private TsAstNode parse_paren_or_arrow_function()
    {
        var savedCurrent = _current;

        try
        {
            var parameters = try_parse_arrow_parameters();
            if (parameters is not null && check(TsTokenType.@operator, "=>"))
            {
                return parse_arrow_function(parameters, false);
            }
        }
        catch (ParseException)
        {
        }

        _current = savedCurrent;

        advance();
        var expr = parse_expression();
        consume(TsTokenType.delimiter, "OAK2059", "期望 ')'");

        if (check(TsTokenType.@operator, "=>"))
        {
            return parse_arrow_function([new TsParameter("it", null, null)], false);
        }

        return expr;
    }

    private IReadOnlyList<TsParameter>? try_parse_arrow_parameters()
    {
        if (!check(TsTokenType.delimiter, "("))
        {
            return null;
        }

        advance();

        var parameters = new List<TsParameter>();

        if (!check(TsTokenType.delimiter, ")"))
        {
            var param = try_parse_single_arrow_parameter();
            if (param is null)
            {
                return null;
            }

            parameters.Add(param);

            while (match(TsTokenType.delimiter, ","))
            {
                param = try_parse_single_arrow_parameter();
                if (param is null)
                {
                    return null;
                }

                parameters.Add(param);
            }
        }

        if (!check(TsTokenType.delimiter, ")"))
        {
            return null;
        }

        advance();

        return parameters;
    }

    private TsParameter? try_parse_single_arrow_parameter()
    {
        if (match(TsTokenType.@operator, "..."))
        {
            if (!check(TsTokenType.identifier))
            {
                return null;
            }

            var name = advance().value;
            return new TsParameter($"...{name}", null, null);
        }

        if (!check(TsTokenType.identifier))
        {
            return null;
        }

        var paramName = advance().value;

        TsAstNode? typeAnnotation = null;
        if (check(TsTokenType.punctuation, ":"))
        {
            advance();
            typeAnnotation = parse_type_annotation();
        }

        TsAstNode? defaultValue = null;
        if (match(TsTokenType.@operator, "="))
        {
            defaultValue = parse_assignment();
        }

        return new TsParameter(paramName, typeAnnotation, defaultValue);
    }

    private TsNewExpr parse_new_expr()
    {
        consume_keyword("new", "OAK2055", "期望 'new' 关键字");
        var callee = parse_call_member();

        if (callee is TsCallExpr callExpr)
        {
            return new TsNewExpr(callExpr.callee, callExpr.arguments);
        }

        return new TsNewExpr(callee, []);
    }

    private TsFunctionExpr parse_function_expr()
    {
        consume_keyword("function", "OAK2104", "期望 'function' 关键字");

        var isGenerator = match(TsTokenType.@operator, "*");

        string? name = null;
        if (check(TsTokenType.identifier))
        {
            name = advance().value;
        }

        consume(TsTokenType.delimiter, "OAK2105", "期望 '('");
        var parameters = parse_parameters();
        consume(TsTokenType.delimiter, "OAK2106", "期望 ')'");

        TsAstNode? returnType = null;
        if (match(TsTokenType.punctuation, ":"))
        {
            returnType = parse_type_annotation();
        }

        var body = parse_block_stmt();

        return new TsFunctionExpr(name, parameters, returnType, body, false, isGenerator);
    }

    private TsArrayLiteral parse_array_literal()
    {
        consume(TsTokenType.delimiter, "OAK2061", "期望 '['");
        var elements = new List<TsAstNode>();

        if (!check(TsTokenType.delimiter, "]"))
        {
            elements.Add(parse_array_element());

            while (match(TsTokenType.delimiter, ","))
            {
                if (check(TsTokenType.delimiter, "]"))
                {
                    break;
                }

                elements.Add(parse_array_element());
            }
        }

        consume(TsTokenType.delimiter, "OAK2062", "期望 ']'");
        return new TsArrayLiteral(elements);
    }

    private TsAstNode parse_array_element()
    {
        if (match(TsTokenType.@operator, "..."))
        {
            return new TsSpreadElement(parse_assignment());
        }

        return parse_assignment();
    }

    private TsObjectLiteral parse_object_literal()
    {
        consume(TsTokenType.delimiter, "OAK2063", "期望 '{'");
        var properties = new List<TsProperty>();

        if (!check(TsTokenType.delimiter, "}"))
        {
            properties.Add(parse_property());

            while (match(TsTokenType.delimiter, ","))
            {
                if (check(TsTokenType.delimiter, "}"))
                {
                    break;
                }

                properties.Add(parse_property());
            }
        }

        consume(TsTokenType.delimiter, "OAK2064", "期望 '}'");
        return new TsObjectLiteral(properties);
    }

    private TsProperty parse_property()
    {
        var key = consume(TsTokenType.identifier, "OAK2065", "期望属性名").value;
        consume(TsTokenType.punctuation, "OAK2066", "期望 ':'");
        var value = parse_assignment();
        return new TsProperty(key, value);
    }

    private TsArrowFunctionExpr parse_arrow_function(IReadOnlyList<TsParameter> parameters, bool isAsync)
    {
        consume(TsTokenType.@operator, "OAK2067", "期望 '=>'");
        return parse_arrow_function_body(parameters, isAsync);
    }

    private TsArrowFunctionExpr parse_arrow_function_body(IReadOnlyList<TsParameter> parameters, bool isAsync)
    {
        TsAstNode? returnType = null;

        if (match(TsTokenType.punctuation, ":"))
        {
            returnType = parse_type_annotation();
        }

        TsAstNode body;
        if (check(TsTokenType.delimiter, "{"))
        {
            body = parse_block_stmt();
        }
        else
        {
            body = parse_assignment();
        }

        return new TsArrowFunctionExpr(parameters, returnType, body, isAsync);
    }

    #endregion
}