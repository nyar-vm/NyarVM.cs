using Std.Data.Text.Erlang.AST;
using Std.Data.Text.Erlang.Lexer;

namespace Std.Data.Text.Erlang.Parser;

public sealed class ErParser : IParser<IReadOnlyList<ErToken>, ErAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<ErToken> _tokens = [];

    public ErParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public ErAstNode Parse(IReadOnlyList<ErToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var attributes = new List<ErAstNode>();
        var functions = new List<ErFunction>();
        var moduleName = "unknown";

        while (!IsAtEnd())
        {
            if (Check(ErTokenType.Operator, "-"))
            {
                var attr = ParseAttribute();
                if (attr is ErAttribute a && a.Name == "module" && a.Arguments.Count > 0)
                {
                    if (a.Arguments[0] is ErAtom atom)
                    {
                        moduleName = atom.Name;
                    }
                }

                attributes.Add(attr);
            }
            else if (Check(ErTokenType.Atom))
            {
                var func = ParseFunction();
                if (func is not null)
                {
                    functions.Add(func);
                }
            }
            else
            {
                Advance();
            }
        }

        return new ErModule(moduleName, attributes, functions);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().type == ErTokenType.Eof;
    }

    private ErToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private ErToken Previous()
    {
        return _tokens[_current - 1];
    }

    private ErToken Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool Check(ErTokenType type)
    {
        return !IsAtEnd() && Peek().type == type;
    }

    private bool Check(ErTokenType type, string value)
    {
        return !IsAtEnd() && Peek().type == type && Peek().Value == value;
    }

    private bool Match(ErTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(ErTokenType type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private ErToken Consume(ErTokenType type, string errorCode, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().type == ErTokenType.Punctuation && Previous().Value == ".")
            {
                return;
            }

            Advance();
        }
    }

    #endregion

    #region Attributes

    private ErAttribute ParseAttribute()
    {
        Consume(ErTokenType.Operator, "ERL2001", "期望 '-'");
        var name = Consume(ErTokenType.Atom, "ERL2002", "期望属性名").Value;

        var args = new List<ErAstNode>();

        if (Match(ErTokenType.Delimiter, "("))
        {
            if (!Check(ErTokenType.Delimiter, ")"))
            {
                args.Add(ParseExpression());

                while (Match(ErTokenType.Delimiter, ",")) args.Add(ParseExpression());
            }

            Consume(ErTokenType.Delimiter, "ERL2003", "期望 ')'");
        }

        Consume(ErTokenType.Punctuation, "ERL2004", "期望 '.'");
        return new ErAttribute(name, args);
    }

    #endregion

    #region Functions

    private ErFunction? ParseFunction()
    {
        try
        {
            var name = Consume(ErTokenType.Atom, "ERL2005", "期望函数名").Value;

            Consume(ErTokenType.Delimiter, "ERL2006", "期望 '('");
            var paramCount = 0;

            if (!Check(ErTokenType.Delimiter, ")"))
            {
                paramCount++;
                while (Match(ErTokenType.Delimiter, ",")) paramCount++;
            }

            Consume(ErTokenType.Delimiter, "ERL2007", "期望 ')'");

            var clauses = new List<ErClause>();

            if (Match(ErTokenType.Operator, "->"))
            {
                var body = ParseExpression();
                Consume(ErTokenType.Punctuation, "ERL2008", "期望 '.'");
                clauses.Add(new ErClause([], [], body));
            }
            else if (Check(ErTokenType.Delimiter, "{") || Check(ErTokenType.Keyword, "when"))
            {
                var clause = ParseClauseBody();
                Consume(ErTokenType.Punctuation, "ERL2009", "期望 '.'");
                clauses.Add(clause);
            }

            while (Check(ErTokenType.Atom, name))
            {
                Advance();

                Consume(ErTokenType.Delimiter, "ERL2010", "期望 '('");
                var patterns = new List<ErAstNode>();

                if (!Check(ErTokenType.Delimiter, ")"))
                {
                    patterns.Add(ParsePattern());
                    while (Match(ErTokenType.Delimiter, ",")) patterns.Add(ParsePattern());
                }

                Consume(ErTokenType.Delimiter, "ERL2011", "期望 ')'");

                var guards = new List<ErAstNode>();
                if (Match(ErTokenType.Keyword, "when"))
                {
                    guards.Add(ParseExpression());
                    while (Match(ErTokenType.Operator, ",")) guards.Add(ParseExpression());
                }

                Consume(ErTokenType.Operator, "ERL2012", "期望 '->'");
                var body = ParseExpression();
                Consume(ErTokenType.Punctuation, "ERL2013", "期望 '.'");

                clauses.Add(new ErClause(patterns, guards, body));
            }

            return new ErFunction(name, paramCount, clauses);
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private ErClause ParseClauseBody()
    {
        var guards = new List<ErAstNode>();
        if (Match(ErTokenType.Keyword, "when"))
        {
            guards.Add(ParseExpression());
            while (Match(ErTokenType.Operator, ",")) guards.Add(ParseExpression());
        }

        Consume(ErTokenType.Operator, "ERL2014", "期望 '->'");
        var body = ParseExpression();

        return new ErClause([], guards, body);
    }

    #endregion

    #region Expressions

    private ErAstNode ParseExpression()
    {
        return ParseMatch();
    }

    private ErAstNode ParseMatch()
    {
        var expr = ParseOrelse();

        if (Match(ErTokenType.Operator, "="))
        {
            var right = ParseMatch();
            return new ErMatch(expr, right);
        }

        return expr;
    }

    private ErAstNode ParseOrelse()
    {
        var left = ParseAndalso();

        while (Match(ErTokenType.Keyword, "orelse"))
        {
            var right = ParseAndalso();
            left = new ErBinaryOp(left, "orelse", right);
        }

        return left;
    }

    private ErAstNode ParseAndalso()
    {
        var left = ParseComparison();

        while (Match(ErTokenType.Keyword, "andalso"))
        {
            var right = ParseComparison();
            left = new ErBinaryOp(left, "andalso", right);
        }

        return left;
    }

    private ErAstNode ParseComparison()
    {
        var left = ParseListOp();

        while (Check(ErTokenType.Operator) &&
               Peek().Value is "==" or "/=" or "=<" or ">=" or "<" or ">" or "=:=" or "=/=")
        {
            var op = Advance().Value;
            var right = ParseListOp();
            left = new ErBinaryOp(left, op, right);
        }

        return left;
    }

    private ErAstNode ParseListOp()
    {
        var left = ParseAdditive();

        while (Check(ErTokenType.Operator) && Peek().Value is "++" or "--")
        {
            var op = Advance().Value;
            var right = ParseAdditive();
            left = new ErBinaryOp(left, op, right);
        }

        return left;
    }

    private ErAstNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Check(ErTokenType.Operator) && Peek().Value is "+" or "-")
        {
            var op = Advance().Value;
            var right = ParseMultiplicative();
            left = new ErBinaryOp(left, op, right);
        }

        return left;
    }

    private ErAstNode ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Check(ErTokenType.Operator) && Peek().Value is "*" or "/" or "div" or "rem" or "band" or "bor" or "bxor")
        {
            var op = Advance().Value;
            var right = ParseUnary();
            left = new ErBinaryOp(left, op, right);
        }

        return left;
    }

    private ErAstNode ParseUnary()
    {
        if (Check(ErTokenType.Operator, "-"))
        {
            Advance();
            var operand = ParseUnary();
            return new ErUnaryOp("-", operand);
        }

        if (Check(ErTokenType.Keyword, "not") || Check(ErTokenType.Operator, "bnot"))
        {
            var op = Advance().Value;
            var operand = ParseUnary();
            return new ErUnaryOp(op, operand);
        }

        return ParsePostfix();
    }

    private ErAstNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Check(ErTokenType.Delimiter, "("))
            {
                Advance();
                var args = new List<ErAstNode>();

                if (!Check(ErTokenType.Delimiter, ")"))
                {
                    args.Add(ParseExpression());
                    while (Match(ErTokenType.Delimiter, ",")) args.Add(ParseExpression());
                }

                Consume(ErTokenType.Delimiter, "ERL2015", "期望 ')'");

                if (expr is ErAtom atom)
                {
                    expr = new ErCall(atom, args);
                }
                else
                {
                    expr = new ErCall(expr, args);
                }
            }
            else if (Match(ErTokenType.Operator, ":"))
            {
                var funcName = Consume(ErTokenType.Atom, "ERL2016", "期望函数名").Value;

                if (Match(ErTokenType.Delimiter, "("))
                {
                    var args = new List<ErAstNode>();

                    if (!Check(ErTokenType.Delimiter, ")"))
                    {
                        args.Add(ParseExpression());
                        while (Match(ErTokenType.Delimiter, ",")) args.Add(ParseExpression());
                    }

                    Consume(ErTokenType.Delimiter, "ERL2017", "期望 ')'");

                    if (expr is ErAtom modAtom)
                    {
                        expr = new ErRemoteCall(modAtom.Name, funcName, args);
                    }
                    else
                    {
                        expr = new ErRemoteCall("unknown", funcName, args);
                    }
                }
                else
                {
                    if (expr is ErAtom modAtom)
                    {
                        expr = new ErFun(modAtom.Name, funcName, 0);
                    }
                    else
                    {
                        expr = new ErFun(null, funcName, 0);
                    }
                }
            }
            else if (Check(ErTokenType.Operator, "!"))
            {
                Advance();
                var message = ParseExpression();
                expr = new ErSend(expr, message);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private ErAstNode ParsePrimary()
    {
        if (Check(ErTokenType.Number))
        {
            var token = Advance();
            return new ErNumber(token.Value);
        }

        if (Check(ErTokenType.String))
        {
            var token = Advance();
            return new ErString(token.Value);
        }

        if (Check(ErTokenType.Char))
        {
            var token = Advance();
            return new ErChar(token.Value);
        }

        if (Check(ErTokenType.Atom))
        {
            var token = Advance();
            return new ErAtom(token.Value);
        }

        if (Check(ErTokenType.Variable))
        {
            var token = Advance();
            return new ErVariable(token.Value);
        }

        if (Check(ErTokenType.Delimiter, "{"))
        {
            return ParseTuple();
        }

        if (Check(ErTokenType.Delimiter, "["))
        {
            return ParseList();
        }

        if (Check(ErTokenType.Delimiter, "<<"))
        {
            return ParseBinary();
        }

        if (Check(ErTokenType.Operator, "#"))
        {
            return ParseRecord();
        }

        if (Check(ErTokenType.Keyword, "fun"))
        {
            return ParseFunExpr();
        }

        if (Check(ErTokenType.Keyword, "case"))
        {
            return ParseCaseExpr();
        }

        if (Check(ErTokenType.Keyword, "if"))
        {
            return ParseIfExpr();
        }

        if (Check(ErTokenType.Keyword, "receive"))
        {
            return ParseReceiveExpr();
        }

        if (Check(ErTokenType.Keyword, "begin"))
        {
            return ParseBlockExpr();
        }

        if (Check(ErTokenType.Keyword, "try"))
        {
            return ParseTryExpr();
        }

        if (Match(ErTokenType.Delimiter, "("))
        {
            var expr = ParseExpression();
            Consume(ErTokenType.Delimiter, "ERL2018", "期望 ')'");
            return expr;
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            "ERL2019",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    private ErTuple ParseTuple()
    {
        Consume(ErTokenType.Delimiter, "ERL2020", "期望 '{'");
        var elements = new List<ErAstNode>();

        if (!Check(ErTokenType.Delimiter, "}"))
        {
            elements.Add(ParseExpression());
            while (Match(ErTokenType.Delimiter, ",")) elements.Add(ParseExpression());
        }

        Consume(ErTokenType.Delimiter, "ERL2021", "期望 '}'");
        return new ErTuple(elements);
    }

    private ErList ParseList()
    {
        Consume(ErTokenType.Delimiter, "ERL2022", "期望 '['");
        var elements = new List<ErAstNode>();

        if (!Check(ErTokenType.Delimiter, "]"))
        {
            elements.Add(ParseExpression());
            while (Match(ErTokenType.Delimiter, ",")) elements.Add(ParseExpression());
        }

        ErAstNode? tail = null;
        if (Match(ErTokenType.Operator, "|"))
        {
            tail = ParseExpression();
        }

        Consume(ErTokenType.Delimiter, "ERL2023", "期望 ']'");
        return new ErList(elements, tail);
    }

    private ErBinary ParseBinary()
    {
        Consume(ErTokenType.Operator, "ERL2024", "期望 '<<'");
        var segments = new List<ErAstNode>();

        if (!Check(ErTokenType.Operator, ">>"))
        {
            segments.Add(ParseExpression());
            while (Match(ErTokenType.Delimiter, ",")) segments.Add(ParseExpression());
        }

        Consume(ErTokenType.Operator, "ERL2025", "期望 '>>'");
        return new ErBinary(segments);
    }

    private ErAstNode ParseRecord()
    {
        Consume(ErTokenType.Operator, "ERL2026", "期望 '#'");
        var name = Consume(ErTokenType.Atom, "ERL2027", "期望记录名").Value;

        if (Match(ErTokenType.Delimiter, "{"))
        {
            var fields = new List<(string Field, ErAstNode Value)>();

            if (!Check(ErTokenType.Delimiter, "}"))
            {
                var fieldName = Consume(ErTokenType.Atom, "ERL2028", "期望字段名").Value;
                Consume(ErTokenType.Operator, "ERL2029", "期望 '='");
                var value = ParseExpression();
                fields.Add((fieldName, value));

                while (Match(ErTokenType.Delimiter, ","))
                {
                    fieldName = Consume(ErTokenType.Atom, "ERL2030", "期望字段名").Value;
                    Consume(ErTokenType.Operator, "ERL2031", "期望 '='");
                    value = ParseExpression();
                    fields.Add((fieldName, value));
                }
            }

            Consume(ErTokenType.Delimiter, "ERL2032", "期望 '}'");
            return new ErRecord(name, fields);
        }

        return new ErAtom($"#{name}");
    }

    private ErAstNode ParseFunExpr()
    {
        Consume(ErTokenType.Keyword, "ERL2033", "期望 'fun'");

        if (Check(ErTokenType.Atom) && PeekNextIsColon())
        {
            var module = Advance().Value;
            Advance();
            var name = Consume(ErTokenType.Atom, "ERL2034", "期望函数名").Value;
            Consume(ErTokenType.Delimiter, "ERL2035", "期望 '/'");
            var arity = Consume(ErTokenType.Number, "ERL2036", "期望元数").Value;
            return new ErFun(module, name, int.Parse(arity));
        }

        if (Check(ErTokenType.Atom))
        {
            var name = Advance().Value;
            if (Match(ErTokenType.Delimiter, "/"))
            {
                var arity = Consume(ErTokenType.Number, "ERL2037", "期望元数").Value;
                return new ErFun(null, name, int.Parse(arity));
            }
        }

        var clauses = new List<ErClause>();

        if (Match(ErTokenType.Delimiter, "("))
        {
            var patterns = new List<ErAstNode>();

            if (!Check(ErTokenType.Delimiter, ")"))
            {
                patterns.Add(ParsePattern());
                while (Match(ErTokenType.Delimiter, ",")) patterns.Add(ParsePattern());
            }

            Consume(ErTokenType.Delimiter, "ERL2038", "期望 ')'");

            var guards = new List<ErAstNode>();
            if (Match(ErTokenType.Keyword, "when"))
            {
                guards.Add(ParseExpression());
                while (Match(ErTokenType.Operator, ",")) guards.Add(ParseExpression());
            }

            Consume(ErTokenType.Operator, "ERL2039", "期望 '->'");
            var body = ParseExpression();
            clauses.Add(new ErClause(patterns, guards, body));

            while (Match(ErTokenType.Punctuation, ";"))
            {
                Consume(ErTokenType.Delimiter, "ERL2040", "期望 '('");
                patterns = [];

                if (!Check(ErTokenType.Delimiter, ")"))
                {
                    patterns.Add(ParsePattern());
                    while (Match(ErTokenType.Delimiter, ",")) patterns.Add(ParsePattern());
                }

                Consume(ErTokenType.Delimiter, "ERL2041", "期望 ')'");

                guards = [];
                if (Match(ErTokenType.Keyword, "when"))
                {
                    guards.Add(ParseExpression());
                    while (Match(ErTokenType.Operator, ",")) guards.Add(ParseExpression());
                }

                Consume(ErTokenType.Operator, "ERL2042", "期望 '->'");
                body = ParseExpression();
                clauses.Add(new ErClause(patterns, guards, body));
            }

            ConsumeKeyword("end", "ERL2043", "期望 'end'");
        }

        return new ErLambda(clauses);
    }

    private ErCase ParseCaseExpr()
    {
        ConsumeKeyword("case", "ERL2044", "期望 'case' 关键字");
        var expression = ParseExpression();
        ConsumeKeyword("of", "ERL2045", "期望 'of' 关键字");

        var clauses = new List<ErClause>();

        while (!Check(ErTokenType.Keyword, "end") && !IsAtEnd())
        {
            var pattern = ParsePattern();

            var guards = new List<ErAstNode>();
            if (Match(ErTokenType.Keyword, "when"))
            {
                guards.Add(ParseExpression());
                while (Match(ErTokenType.Operator, ",")) guards.Add(ParseExpression());
            }

            Consume(ErTokenType.Operator, "ERL2046", "期望 '->'");
            var body = ParseExpression();
            clauses.Add(new ErClause([pattern], guards, body));

            Match(ErTokenType.Punctuation, ";");
        }

        ConsumeKeyword("end", "ERL2047", "期望 'end'");
        return new ErCase(expression, clauses);
    }

    private ErIf ParseIfExpr()
    {
        ConsumeKeyword("if", "ERL2048", "期望 'if' 关键字");

        var clauses = new List<ErClause>();

        while (!Check(ErTokenType.Keyword, "end") && !IsAtEnd())
        {
            var guard = ParseExpression();

            Consume(ErTokenType.Operator, "ERL2049", "期望 '->'");
            var body = ParseExpression();
            clauses.Add(new ErClause([], [guard], body));

            Match(ErTokenType.Punctuation, ";");
        }

        ConsumeKeyword("end", "ERL2050", "期望 'end'");
        return new ErIf(clauses);
    }

    private ErReceive ParseReceiveExpr()
    {
        ConsumeKeyword("receive", "ERL2051", "期望 'receive' 关键字");

        var clauses = new List<ErClause>();

        while (!Check(ErTokenType.Keyword, "after") && !Check(ErTokenType.Keyword, "end") && !IsAtEnd())
        {
            var pattern = ParsePattern();

            var guards = new List<ErAstNode>();
            if (Match(ErTokenType.Keyword, "when"))
            {
                guards.Add(ParseExpression());
                while (Match(ErTokenType.Operator, ",")) guards.Add(ParseExpression());
            }

            Consume(ErTokenType.Operator, "ERL2052", "期望 '->'");
            var body = ParseExpression();
            clauses.Add(new ErClause([pattern], guards, body));

            Match(ErTokenType.Punctuation, ";");
        }

        ErAstNode? afterExpr = null;
        if (Match(ErTokenType.Keyword, "after"))
        {
            var timeout = ParseExpression();
            Consume(ErTokenType.Operator, "ERL2053", "期望 '->'");
            var body = ParseExpression();
            afterExpr = new ErReceiveAfter(timeout, body);
        }

        ConsumeKeyword("end", "ERL2054", "期望 'end'");
        return new ErReceive(clauses, afterExpr);
    }

    private ErBlock ParseBlockExpr()
    {
        ConsumeKeyword("begin", "ERL2055", "期望 'begin' 关键字");

        var expressions = new List<ErAstNode>();
        expressions.Add(ParseExpression());

        while (Match(ErTokenType.Punctuation, ",")) expressions.Add(ParseExpression());

        ConsumeKeyword("end", "ERL2056", "期望 'end'");
        return new ErBlock(expressions);
    }

    private ErTry ParseTryExpr()
    {
        ConsumeKeyword("try", "ERL2057", "期望 'try' 关键字");
        var expression = ParseExpression();

        var catchClauses = new List<ErClause>();
        ErAstNode? afterExpr = null;

        if (Match(ErTokenType.Keyword, "catch"))
        {
            while (!Check(ErTokenType.Keyword, "after") && !Check(ErTokenType.Keyword, "end") && !IsAtEnd())
            {
                var pattern = ParsePattern();

                var guards = new List<ErAstNode>();
                if (Match(ErTokenType.Keyword, "when"))
                {
                    guards.Add(ParseExpression());
                    while (Match(ErTokenType.Operator, ",")) guards.Add(ParseExpression());
                }

                Consume(ErTokenType.Operator, "ERL2058", "期望 '->'");
                var body = ParseExpression();
                catchClauses.Add(new ErClause([pattern], guards, body));

                Match(ErTokenType.Punctuation, ";");
            }
        }

        if (Match(ErTokenType.Keyword, "after"))
        {
            afterExpr = ParseExpression();
        }

        ConsumeKeyword("end", "ERL2059", "期望 'end'");
        return new ErTry(expression, catchClauses, afterExpr);
    }

    private bool PeekNextIsColon()
    {
        if (_current + 1 >= _tokens.Count)
        {
            return false;
        }

        return _tokens[_current + 1].type == ErTokenType.Operator && _tokens[_current + 1].Value == ":";
    }

    private ErToken ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(ErTokenType.Keyword, keyword))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            errorCode,
            message);

        throw new ParseException(message);
    }

    #endregion

    #region Patterns

    private ErAstNode ParsePattern()
    {
        if (Check(ErTokenType.Variable) && Peek().Value == "_")
        {
            Advance();
            return new ErWildCard();
        }

        if (Check(ErTokenType.Number))
        {
            var token = Advance();
            return new ErNumber(token.Value);
        }

        if (Check(ErTokenType.String))
        {
            var token = Advance();
            return new ErString(token.Value);
        }

        if (Check(ErTokenType.Char))
        {
            var token = Advance();
            return new ErChar(token.Value);
        }

        if (Check(ErTokenType.Atom))
        {
            var name = Advance().Value;

            if (Match(ErTokenType.Delimiter, "("))
            {
                var args = new List<ErAstNode>();

                if (!Check(ErTokenType.Delimiter, ")"))
                {
                    args.Add(ParsePattern());
                    while (Match(ErTokenType.Delimiter, ",")) args.Add(ParsePattern());
                }

                Consume(ErTokenType.Delimiter, "ERL2060", "期望 ')'");
                return new ErCall(new ErAtom(name), args);
            }

            return new ErAtom(name);
        }

        if (Check(ErTokenType.Variable))
        {
            var token = Advance();
            return new ErVariable(token.Value);
        }

        if (Check(ErTokenType.Delimiter, "{"))
        {
            return ParseTuplePattern();
        }

        if (Check(ErTokenType.Delimiter, "["))
        {
            return ParseListPattern();
        }

        if (Match(ErTokenType.Delimiter, "("))
        {
            var pattern = ParsePattern();
            Consume(ErTokenType.Delimiter, "ERL2061", "期望 ')'");
            return pattern;
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            "ERL2062",
            $"期望模式，遇到 '{errorToken.Value}'");

        throw new ParseException($"期望模式，遇到 '{errorToken.Value}'");
    }

    private ErTuple ParseTuplePattern()
    {
        Consume(ErTokenType.Delimiter, "ERL2063", "期望 '{'");
        var elements = new List<ErAstNode>();

        if (!Check(ErTokenType.Delimiter, "}"))
        {
            elements.Add(ParsePattern());
            while (Match(ErTokenType.Delimiter, ",")) elements.Add(ParsePattern());
        }

        Consume(ErTokenType.Delimiter, "ERL2064", "期望 '}'");
        return new ErTuple(elements);
    }

    private ErAstNode ParseListPattern()
    {
        Consume(ErTokenType.Delimiter, "ERL2065", "期望 '['");
        var elements = new List<ErAstNode>();

        if (!Check(ErTokenType.Delimiter, "]"))
        {
            elements.Add(ParsePattern());
            while (Match(ErTokenType.Delimiter, ",")) elements.Add(ParsePattern());
        }

        ErAstNode? tail = null;
        if (Match(ErTokenType.Operator, "|"))
        {
            tail = ParsePattern();
        }

        Consume(ErTokenType.Delimiter, "ERL2066", "期望 ']'");
        return new ErList(elements, tail);
    }

    #endregion
}
