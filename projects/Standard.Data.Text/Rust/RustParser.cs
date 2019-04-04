using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Rust;


/// <summary>
///     Rust 语言语法解析器
/// </summary>
public sealed class RustParser : Std.Data.Text.Parsing.ParserBase<IReadOnlyList<RustToken>, RustAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<RustToken> _tokens = [];


/// <summary>
///     创建 Rust 语法解析器
/// </summary>
    public RustParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
        _diagnostics = diagnostics;
    }


/// <summary>
///     解析词法单元序列
/// </summary>
    public override RustAstNode parse(IReadOnlyList<RustToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var items = new List<RustAstNode>();

        while (!is_at_end())
        {
            skip_new_lines();

            if (is_at_end())
            {
                break;
            }

            var item = parse_item();
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return new RustCrate(items);
    }

    #region Types

    private RustAstNode parse_type()
    {
        var isReference = false;
        var isMutable = false;

        if (match(RustNodeKind.@operator, "&"))
        {
            isReference = true;
            isMutable = match(RustNodeKind.keyword, "mut");
        }

        if (check(RustNodeKind.delimiter, "("))
        {
            advance();
            var elements = new List<RustAstNode>();

            if (!check(RustNodeKind.delimiter, ")"))
            {
                do
                {
                    elements.Add(parse_type());
                } while (match(RustNodeKind.delimiter, ","));
            }

            consume(RustNodeKind.delimiter, "OR2051", "期望 ')'");
            return new RustTypeNode("tuple", isReference, isMutable);
        }

        if (check(RustNodeKind.delimiter, "["))
        {
            advance();
            var elementType = parse_type();

            if (match(RustNodeKind.@operator, ";"))
            {
                parse_expression();
            }

            consume(RustNodeKind.delimiter, "OR2052", "期望 ']'");
            return new RustTypeNode("array", isReference, isMutable);
        }

        var typeName = consume(RustNodeKind.identifier, "OR2053", "期望类型名").text;

        if (match(RustNodeKind.@operator, "<"))
        {
            advance();
            do
            {
                parse_type();
            } while (match(RustNodeKind.delimiter, ","));

            consume(RustNodeKind.@operator, "OR2054", "期望 '>'");
        }

        return new RustTypeNode(typeName, isReference, isMutable);
    }

    #endregion

    private void synchronize()
    {
        advance();

        while (!is_at_end())
        {
            if (previous().kind == RustNodeKind.@operator && previous().text == ";")
            {
                return;
            }

            if (check(RustNodeKind.keyword))
            {
                var value = peek().text;
                if (value is "fn" or "struct" or "enum" or "impl" or "trait" or "let" or "if" or "while" or "for"
                    or "return" or "use" or "mod")
                {
                    return;
                }
            }

            advance();
        }
    }

    #region Token Access

    private bool is_at_end()
    {
        return peek().kind == RustNodeKind.eof;
    }

    private RustToken peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private RustToken previous()
    {
        return _tokens[_current - 1];
    }

    private RustToken advance()
    {
        if (!is_at_end())
        {
            _current++;
        }

        return previous();
    }

    private bool check(NodeKind kind)
    {
        return !is_at_end() && peek().kind == kind;
    }

    private bool check(NodeKind kind, string value)
    {
        return !is_at_end() && peek().kind == kind && peek().text == value;
    }

    private bool match(NodeKind kind)
    {
        if (check(kind))
        {
            advance();
            return true;
        }

        return false;
    }

    private bool match(NodeKind kind, string value)
    {
        if (check(kind, value))
        {
            advance();
            return true;
        }

        return false;
    }

    private RustToken consume(NodeKind kind, string errorCode, string message)
    {
        if (check(kind))
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
        while (match(RustNodeKind.new_line))
        {
        }
    }

    private RustToken peek_next()
    {
        return _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];
    }

    #endregion

    #region Items

    private RustAstNode? parse_item()
    {
        try
        {
            skip_new_lines();

            if (is_at_end())
            {
                return null;
            }

            if (check(RustNodeKind.keyword, "fn"))
            {
                return parse_function_def();
            }

            if (check(RustNodeKind.keyword, "struct"))
            {
                return parse_struct_def();
            }

            if (check(RustNodeKind.keyword, "enum"))
            {
                return parse_enum_def();
            }

            if (check(RustNodeKind.keyword, "impl"))
            {
                return parse_impl_def();
            }

            if (check(RustNodeKind.keyword, "trait"))
            {
                return parse_trait_def();
            }

            if (check(RustNodeKind.keyword, "type"))
            {
                return parse_type_alias();
            }

            if (check(RustNodeKind.keyword, "use"))
            {
                return parse_use_decl();
            }

            if (check(RustNodeKind.keyword, "mod"))
            {
                return parse_mod_decl();
            }

            if (check(RustNodeKind.keyword, "pub"))
            {
                advance();
                var inner = parse_item();
                return inner;
            }

            return parse_statement();
        }
        catch (ParseException)
        {
            synchronize();
            return null;
        }
    }

    private RustAstNode parse_function_def()
    {
        var startToken = advance();
        var name = consume(RustNodeKind.identifier, "OR2001", "期望函数名").text;
        consume(RustNodeKind.delimiter, "OR2002", "期望 '('");
        var parameters = new List<RustParam>();

        if (!check(RustNodeKind.delimiter, ")"))
        {
            do
            {
                skip_new_lines();
                if (check(RustNodeKind.delimiter, ")"))
                {
                    break;
                }

                var paramName = consume(RustNodeKind.identifier, "OR2003", "期望参数名").text;
                RustAstNode? paramType = null;
                if (match(RustNodeKind.@operator, ":"))
                {
                    paramType = parse_type();
                }

                parameters.Add(new RustParam(paramName, paramType));
            } while (match(RustNodeKind.delimiter, ","));
        }

        consume(RustNodeKind.delimiter, "OR2004", "期望 ')'");

        RustAstNode? returnType = null;
        if (match(RustNodeKind.@operator, "->"))
        {
            returnType = parse_type();
        }

        var body = parse_block_expr();

        return new RustFunctionDef(name, parameters, returnType, body, default);
    }

    private RustAstNode parse_struct_def()
    {
        var startToken = advance();
        var name = consume(RustNodeKind.identifier, "OR2005", "期望结构体名").text;
        var fields = new List<RustFieldDef>();

        if (match(RustNodeKind.delimiter, "{"))
        {
            while (!check(RustNodeKind.delimiter, "}") && !is_at_end())
            {
                skip_new_lines();
                if (check(RustNodeKind.delimiter, "}"))
                {
                    break;
                }

                var isPublic = match(RustNodeKind.keyword, "pub");
                var fieldName = consume(RustNodeKind.identifier, "OR2006", "期望字段名").text;
                consume(RustNodeKind.@operator, "OR2007", "期望 ':'");
                var fieldType = parse_type();

                fields.Add(new RustFieldDef(fieldName, fieldType, isPublic));

                if (!check(RustNodeKind.delimiter, "}"))
                {
                    consume(RustNodeKind.delimiter, "OR2008", "期望 ','");
                }
            }

            consume(RustNodeKind.delimiter, "OR2009", "期望 '}'");
        }
        else if (match(RustNodeKind.delimiter, "("))
        {
            while (!check(RustNodeKind.delimiter, ")") && !is_at_end())
            {
                var fieldType = parse_type();
                fields.Add(new RustFieldDef($"_{fields.Count}", fieldType, false));

                if (!check(RustNodeKind.delimiter, ")"))
                {
                    consume(RustNodeKind.delimiter, "OR2010", "期望 ','");
                }
            }

            consume(RustNodeKind.delimiter, "OR2011", "期望 ')'");
        }

        return new RustStructDef(name, fields, default);
    }

    private RustAstNode parse_enum_def()
    {
        var startToken = advance();
        var name = consume(RustNodeKind.identifier, "OR2012", "期望枚举名").text;
        consume(RustNodeKind.delimiter, "OR2013", "期望 '{'");
        var variants = new List<RustVariantDef>();

        while (!check(RustNodeKind.delimiter, "}") && !is_at_end())
        {
            skip_new_lines();
            if (check(RustNodeKind.delimiter, "}"))
            {
                break;
            }

            var variantName = consume(RustNodeKind.identifier, "OR2014", "期望变体名").text;
            List<RustAstNode> variantFields;
            if (match(RustNodeKind.delimiter, "("))
            {
                advance();
                variantFields = [];

                while (!check(RustNodeKind.delimiter, ")") && !is_at_end())
                {
                    variantFields.Add(parse_type());
                    if (!check(RustNodeKind.delimiter, ")"))
                    {
                        consume(RustNodeKind.delimiter, "OR2015", "期望 ','");
                    }
                }

                consume(RustNodeKind.delimiter, "OR2016", "期望 ')'");
            }
            else if (check(RustNodeKind.delimiter, "{"))
            {
                advance();
                variantFields = [];

                while (!check(RustNodeKind.delimiter, "}") && !is_at_end())
                {
                    var fieldName = consume(RustNodeKind.identifier, "OR2017", "期望字段名").text;
                    consume(RustNodeKind.@operator, "OR2018", "期望 ':'");
                    variantFields.Add(parse_type());
                    if (!check(RustNodeKind.delimiter, "}"))
                    {
                        consume(RustNodeKind.delimiter, "OR2019", "期望 ','");
                    }
                }

                consume(RustNodeKind.delimiter, "OR2020", "期望 '}'");
            }
            else
            {
                variantFields = [];
            }

            variants.Add(new RustVariantDef(variantName, variantFields));

            if (!check(RustNodeKind.delimiter, "}"))
            {
                consume(RustNodeKind.delimiter, "OR2021", "期望 ','");
            }
        }

        consume(RustNodeKind.delimiter, "OR2022", "期望 '}'");
        return new RustEnumDef(name, variants, default);
    }

    private RustAstNode parse_impl_def()
    {
        var startToken = advance();
        RustAstNode? trait = null;

        var firstType = parse_type();

        if (match(RustNodeKind.keyword, "for"))
        {
            trait = firstType;
        }

        var targetType = trait is not null ? parse_type() : firstType;
        consume(RustNodeKind.delimiter, "OR2023", "期望 '{'");

        var members = new List<RustAstNode>();
        while (!check(RustNodeKind.delimiter, "}") && !is_at_end())
        {
            skip_new_lines();
            if (check(RustNodeKind.delimiter, "}"))
            {
                break;
            }

            var member = parse_item();
            if (member is not null)
            {
                members.Add(member);
            }
        }

        consume(RustNodeKind.delimiter, "OR2024", "期望 '}'");
        return new RustImplDef(trait, targetType, members, default);
    }

    private RustAstNode parse_trait_def()
    {
        var startToken = advance();
        var name = consume(RustNodeKind.identifier, "OR2025", "期望 trait 名").text;
        consume(RustNodeKind.delimiter, "OR2026", "期望 '{'");
        var members = new List<RustAstNode>();

        while (!check(RustNodeKind.delimiter, "}") && !is_at_end())
        {
            skip_new_lines();
            if (check(RustNodeKind.delimiter, "}"))
            {
                break;
            }

            var member = parse_item();
            if (member is not null)
            {
                members.Add(member);
            }
        }

        consume(RustNodeKind.delimiter, "OR2027", "期望 '}'");
        return new RustTraitDef(name, members, default);
    }

    private RustAstNode parse_type_alias()
    {
        var startToken = advance();
        var name = consume(RustNodeKind.identifier, "OR2028", "期望类型别名").text;
        consume(RustNodeKind.@operator, "OR2029", "期望 '='");
        var type = parse_type();
        consume(RustNodeKind.@operator, "OR2030", "期望 ';'");

        return new RustTypeAlias(name, type, default);
    }

    private RustAstNode parse_use_decl()
    {
        var startToken = advance();
        var sb = new StringBuilder();

        while (!check(RustNodeKind.@operator, ";") && !is_at_end()) sb.Append(advance().text);

        consume(RustNodeKind.@operator, "OR2031", "期望 ';'");

        string? alias = null;
        var path = sb.ToString();

        var asIndex = path.IndexOf(" as ", StringComparison.Ordinal);
        if (asIndex >= 0)
        {
            alias = path[(asIndex + 4)..].Trim();
            path = path[..asIndex].Trim();
        }

        return new RustUseDecl(path, alias, default);
    }

    private RustAstNode parse_mod_decl()
    {
        var startToken = advance();
        var name = consume(RustNodeKind.identifier, "OR2032", "期望模块名").text;
        RustAstNode? body = null;
        if (match(RustNodeKind.delimiter, "{"))
        {
            body = parse_block_expr();
        }
        else
        {
            consume(RustNodeKind.@operator, "OR2033", "期望 ';'");
        }

        return new RustModDecl(name, body, default);
    }

    #endregion

    #region Statements

    private RustAstNode parse_statement()
    {
        skip_new_lines();

        if (check(RustNodeKind.keyword, "let"))
        {
            return parse_let_stmt();
        }

        if (check(RustNodeKind.keyword, "return"))
        {
            return parse_return_stmt();
        }

        if (check(RustNodeKind.keyword, "break"))
        {
            return parse_break_stmt();
        }

        if (check(RustNodeKind.keyword, "continue"))
        {
            return parse_continue_stmt();
        }

        if (check(RustNodeKind.keyword, "while"))
        {
            return parse_while_stmt();
        }

        if (check(RustNodeKind.keyword, "loop"))
        {
            return parse_loop_stmt();
        }

        if (check(RustNodeKind.keyword, "for"))
        {
            return parse_for_stmt();
        }

        if (check(RustNodeKind.keyword, "if"))
        {
            return parse_if_expr();
        }

        if (check(RustNodeKind.keyword, "match"))
        {
            return parse_match_expr();
        }

        var expr = parse_expression();

        if (match(RustNodeKind.@operator, ";"))
        {
            return new RustExprStmt(expr);
        }

        return expr;
    }

    private RustAstNode parse_let_stmt()
    {
        var startToken = advance();
        var isMutable = match(RustNodeKind.keyword, "mut");
        var pattern = parse_pattern();
        RustAstNode? type = null;

        if (match(RustNodeKind.@operator, ":"))
        {
            type = parse_type();
        }

        RustAstNode? initializer = null;
        if (match(RustNodeKind.@operator, "="))
        {
            initializer = parse_expression();
        }

        consume(RustNodeKind.@operator, "OR2034", "期望 ';'");
        return new RustLetStmt(pattern, type, initializer, isMutable, default);
    }

    private RustAstNode parse_pattern()
    {
        if (check(RustNodeKind.identifier))
        {
            var token = advance();
            return new RustIdentifier(token.text, default);
        }

        if (match(RustNodeKind.delimiter, "("))
        {
            var elements = new List<RustAstNode>();

            while (!check(RustNodeKind.delimiter, ")") && !is_at_end())
            {
                elements.Add(parse_pattern());
                if (!check(RustNodeKind.delimiter, ")"))
                {
                    consume(RustNodeKind.delimiter, "OR2035", "期望 ','");
                }
            }

            consume(RustNodeKind.delimiter, "OR2036", "期望 ')'");
            return new RustTupleExpr(elements);
        }

        var errorToken = peek();
        return new RustIdentifier("_");
    }

    private RustAstNode parse_return_stmt()
    {
        var startToken = advance();
        RustAstNode? value = null;

        if (!check(RustNodeKind.@operator, ";") && !check(RustNodeKind.delimiter, "}"))
        {
            value = parse_expression();
        }

        match(RustNodeKind.@operator, ";");
        return new RustReturnStmt(value, default);
    }

    private RustAstNode parse_break_stmt()
    {
        var startToken = advance();
        RustAstNode? value = null;

        if (!check(RustNodeKind.@operator, ";") && !check(RustNodeKind.delimiter, "}"))
        {
            value = parse_expression();
        }

        match(RustNodeKind.@operator, ";");
        return new RustBreakStmt(value, default);
    }

    private RustAstNode parse_continue_stmt()
    {
        var startToken = advance();
        match(RustNodeKind.@operator, ";");
        return new RustContinueStmt();
    }

    private RustAstNode parse_while_stmt()
    {
        var startToken = advance();
        var condition = parse_expression();
        var body = parse_block_expr();

        return new RustWhileStmt(condition, body, default);
    }

    private RustAstNode parse_loop_stmt()
    {
        var startToken = advance();
        var body = parse_block_expr();

        return new RustLoopStmt(body, default);
    }

    private RustAstNode parse_for_stmt()
    {
        var startToken = advance();
        var pattern = parse_pattern();
        consume(RustNodeKind.keyword, "OR2037", "期望 'in'");
        var iterator = parse_expression();
        var body = parse_block_expr();

        return new RustForStmt(pattern, iterator, body, default);
    }

    #endregion

    #region Expressions

    private RustAstNode parse_expression()
    {
        return parse_assignment();
    }

    private RustAstNode parse_assignment()
    {
        var left = parse_range();

        if (check(RustNodeKind.@operator) &&
            peek().text is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "&=" or "|=" or "^=" or "<<=" or ">>=")
        {
            var op = advance().text;
            var right = parse_assignment();
            return new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode parse_range()
    {
        var left = parse_or();

        if (check(RustNodeKind.@operator, ".."))
        {
            advance();
            var right = parse_or();
            return new RustRange(left, right, false);
        }

        if (check(RustNodeKind.@operator, "..="))
        {
            advance();
            var right = parse_or();
            return new RustRange(left, right, true);
        }

        return left;
    }

    private RustAstNode parse_or()
    {
        var left = parse_and();

        while (match(RustNodeKind.@operator, "||"))
        {
            var op = previous().text;
            var right = parse_and();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode parse_and()
    {
        var left = parse_comparison();

        while (match(RustNodeKind.@operator, "&&"))
        {
            var op = previous().text;
            var right = parse_comparison();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode parse_comparison()
    {
        var left = parse_bit_or();

        while (check(RustNodeKind.@operator) && peek().text is "==" or "!=" or "<" or ">" or "<=" or ">=")
        {
            var op = advance().text;
            var right = parse_bit_or();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode parse_bit_or()
    {
        var left = parse_bit_xor();

        while (match(RustNodeKind.@operator, "|"))
        {
            var right = parse_bit_xor();
            left = new RustBinaryOp(left, "|", right);
        }

        return left;
    }

    private RustAstNode parse_bit_xor()
    {
        var left = parse_bit_and();

        while (match(RustNodeKind.@operator, "^"))
        {
            var right = parse_bit_and();
            left = new RustBinaryOp(left, "^", right);
        }

        return left;
    }

    private RustAstNode parse_bit_and()
    {
        var left = parse_shift();

        while (match(RustNodeKind.@operator, "&"))
        {
            var right = parse_shift();
            left = new RustBinaryOp(left, "&", right);
        }

        return left;
    }

    private RustAstNode parse_shift()
    {
        var left = parse_additive();

        while (check(RustNodeKind.@operator) && peek().text is "<<" or ">>")
        {
            var op = advance().text;
            var right = parse_additive();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode parse_additive()
    {
        var left = parse_multiplicative();

        while (check(RustNodeKind.@operator) && peek().text is "+" or "-")
        {
            var op = advance().text;
            var right = parse_multiplicative();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode parse_multiplicative()
    {
        var left = parse_cast();

        while (check(RustNodeKind.@operator) && peek().text is "*" or "/" or "%")
        {
            var op = advance().text;
            var right = parse_cast();
            left = new RustBinaryOp(left, op, right);
        }

        return left;
    }

    private RustAstNode parse_cast()
    {
        var left = parse_unary();

        if (check(RustNodeKind.keyword, "as"))
        {
            advance();
            var type = parse_type();
            return new RustCast(left, type);
        }

        return left;
    }

    private RustAstNode parse_unary()
    {
        if (check(RustNodeKind.@operator) && peek().text is "-" or "!" or "*" or "&")
        {
            var op = advance().text;
            var operand = parse_unary();
            return new RustUnaryOp(op, operand);
        }

        return parse_postfix();
    }

    private RustAstNode parse_postfix()
    {
        var expr = parse_primary();

        while (true)
        {
            if (match(RustNodeKind.@operator, "."))
            {
                if (check(RustNodeKind.identifier) && peek_next().kind == RustNodeKind.delimiter &&
                    peek_next().text == "(")
                {
                    var method = advance().text;
                    consume(RustNodeKind.delimiter, "OR2038", "期望 '('");
                    var args = new List<RustAstNode>();

                    if (!check(RustNodeKind.delimiter, ")"))
                    {
                        do
                        {
                            args.Add(parse_expression());
                        } while (match(RustNodeKind.delimiter, ","));
                    }

                    consume(RustNodeKind.delimiter, "OR2039", "期望 ')'");
                    expr = new RustMethodCall(expr, method, args);
                }
                else
                {
                    var field = consume(RustNodeKind.identifier, "OR2040", "期望字段名").text;
                    expr = new RustFieldAccess(expr, field);
                }
            }
            else if (match(RustNodeKind.delimiter, "["))
            {
                var index = parse_expression();
                consume(RustNodeKind.delimiter, "OR2041", "期望 ']'");
                expr = new RustIndex(expr, index);
            }
            else if (match(RustNodeKind.delimiter, "("))
            {
                var args = new List<RustAstNode>();

                if (!check(RustNodeKind.delimiter, ")"))
                {
                    do
                    {
                        args.Add(parse_expression());
                    } while (match(RustNodeKind.delimiter, ","));
                }

                consume(RustNodeKind.delimiter, "OR2042", "期望 ')'");
                expr = new RustCall(expr, args);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private RustAstNode parse_primary()
    {
        if (match(RustNodeKind.number))
        {
            var token = previous();
            return new RustLiteral("number", token.text, default);
        }

        if (match(RustNodeKind.@string))
        {
            var token = previous();
            return new RustLiteral("string", token.text, default);
        }

        if (match(RustNodeKind.@char))
        {
            var token = previous();
            return new RustLiteral("char", token.text, default);
        }

        if (check(RustNodeKind.keyword, "true"))
        {
            var token = advance();
            return new RustLiteral("bool", "true", default);
        }

        if (check(RustNodeKind.keyword, "false"))
        {
            var token = advance();
            return new RustLiteral("bool", "false", default);
        }

        if (check(RustNodeKind.identifier))
        {
            var token = advance();
            return new RustIdentifier(token.text, default);
        }

        if (check(RustNodeKind.delimiter, "("))
        {
            advance();
            var expr = parse_expression();
            consume(RustNodeKind.delimiter, "OR2043", "期望 ')'");
            return expr;
        }

        if (check(RustNodeKind.delimiter, "["))
        {
            return parse_array_expr();
        }

        if (check(RustNodeKind.keyword, "if"))
        {
            return parse_if_expr();
        }

        if (check(RustNodeKind.keyword, "match"))
        {
            return parse_match_expr();
        }

        if (check(RustNodeKind.delimiter, "{"))
        {
            return parse_block_expr();
        }

        if (check(RustNodeKind.identifier) && peek().text.StartsWith('!'))
        {
            return parse_macro_call();
        }

        var errorToken = peek();
        _diagnostics?.report_error(
            string.Empty,
            default,
            "OR2044",
            $"意外的标记 '{errorToken.text}'");

        throw new ParseException($"意外的标记 '{errorToken.text}'");
    }

    private RustAstNode parse_if_expr()
    {
        var startToken = advance();
        var condition = parse_expression();
        var thenBranch = parse_block_expr();
        RustAstNode? elseBranch = null;

        if (match(RustNodeKind.keyword, "else"))
        {
            if (check(RustNodeKind.keyword, "if"))
            {
                elseBranch = parse_if_expr();
            }
            else
            {
                elseBranch = parse_block_expr();
            }
        }

        return new RustIfExpr(condition, thenBranch, elseBranch, default);
    }

    private RustAstNode parse_match_expr()
    {
        var startToken = advance();
        var scrutinee = parse_expression();
        consume(RustNodeKind.delimiter, "OR2045", "期望 '{'");

        var arms = new List<RustMatchArm>();
        while (!check(RustNodeKind.delimiter, "}") && !is_at_end())
        {
            skip_new_lines();
            if (check(RustNodeKind.delimiter, "}"))
            {
                break;
            }

            var pattern = parse_pattern();
            match(RustNodeKind.keyword, "if");
            consume(RustNodeKind.@operator, "OR2046", "期望 '=>'");
            var body = parse_expression();
            match(RustNodeKind.@operator, ",");

            arms.Add(new RustMatchArm(pattern, body));
        }

        consume(RustNodeKind.delimiter, "OR2047", "期望 '}'");
        return new RustMatchExpr(scrutinee, arms, default);
    }

    private RustAstNode parse_array_expr()
    {
        var startToken = advance();
        var elements = new List<RustAstNode>();

        if (!check(RustNodeKind.delimiter, "]"))
        {
            do
            {
                elements.Add(parse_expression());
            } while (match(RustNodeKind.delimiter, ","));
        }

        consume(RustNodeKind.delimiter, "OR2048", "期望 ']'");
        return new RustArrayExpr(elements, default);
    }

    private RustAstNode parse_block_expr()
    {
        var startToken = consume(RustNodeKind.delimiter, "OR2049", "期望 '{'");
        var statements = new List<RustAstNode>();

        while (!check(RustNodeKind.delimiter, "}") && !is_at_end())
        {
            skip_new_lines();
            if (check(RustNodeKind.delimiter, "}"))
            {
                break;
            }

            var stmt = parse_statement();
            statements.Add(stmt);
        }

        consume(RustNodeKind.delimiter, "OR2050", "期望 '}'");
        return new RustBlockExpr(statements, default);
    }

    private RustAstNode parse_macro_call()
    {
        var startToken = advance();
        var name = startToken.text;
        var body = parse_expression();

        return new RustMacroCall(name, body, default);
    }

    #endregion
}