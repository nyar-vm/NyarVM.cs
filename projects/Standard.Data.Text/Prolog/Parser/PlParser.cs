using Std.Data.Text.Prolog.AST;
using Std.Data.Text.Prolog.Lexer;

namespace Std.Data.Text.Prolog.Parser;

public sealed class PlParser : IParser<IReadOnlyList<PlToken>, PlAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<PlToken> _tokens = [];

    public PlParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public PlAstNode Parse(IReadOnlyList<PlToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var clauses = new List<PlAstNode>();

        while (!IsAtEnd())
        {
            var clause = ParseClause();
            if (clause is not null)
            {
                clauses.Add(clause);
            }
        }

        return new PlProgram(clauses);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().type == PlTokenType.Eof;
    }

    private PlToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private PlToken Previous()
    {
        return _tokens[_current - 1];
    }

    private PlToken Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool Check(PlTokenType type)
    {
        return !IsAtEnd() && Peek().type == type;
    }

    private bool Check(PlTokenType type, string value)
    {
        return !IsAtEnd() && Peek().type == type && Peek().Value == value;
    }

    private bool Match(PlTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(PlTokenType type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private PlToken Consume(PlTokenType type, string errorCode, string message)
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

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().type == PlTokenType.Punctuation && Previous().Value == ".")
            {
                return;
            }

            Advance();
        }
    }

    #endregion

    #region Clauses

    private PlAstNode? ParseClause()
    {
        try
        {
            if (Check(PlTokenType.Operator, ":-"))
            {
                Advance();
                var args = new List<PlAstNode>();
                if (!Check(PlTokenType.Punctuation, "."))
                {
                    args.Add(ParseTerm());
                    while (Match(PlTokenType.Delimiter, ",")) args.Add(ParseTerm());
                }

                Consume(PlTokenType.Punctuation, "PLG2001", "期望 '.'");
                return new PlDirective("directive", args);
            }

            if (Check(PlTokenType.Operator, "?-"))
            {
                Advance();
                var goals = new List<PlAstNode>();
                goals.Add(ParseTerm());
                while (Match(PlTokenType.Delimiter, ",")) goals.Add(ParseTerm());

                Consume(PlTokenType.Punctuation, "PLG2002", "期望 '.'");
                return new PlQuery(goals);
            }

            var head = ParseTerm();

            if (Match(PlTokenType.Punctuation, "."))
            {
                return new PlFact(head);
            }

            if (Match(PlTokenType.Operator, ":-"))
            {
                var body = new List<PlAstNode>();
                body.Add(ParseTerm());
                while (Match(PlTokenType.Delimiter, ",")) body.Add(ParseTerm());

                Consume(PlTokenType.Punctuation, "PLG2003", "期望 '.'");
                return new PlRule(head, body);
            }

            if (Match(PlTokenType.Operator, "-->"))
            {
                var body = new List<PlAstNode>();
                body.Add(ParseTerm());
                while (Match(PlTokenType.Delimiter, ",")) body.Add(ParseTerm());

                Consume(PlTokenType.Punctuation, "PLG2004", "期望 '.'");
                return new PlRule(head, body);
            }

            Consume(PlTokenType.Punctuation, "PLG2005", "期望 '.' 或 ':-'");
            return new PlFact(head);
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    #endregion

    #region Terms

    private PlAstNode ParseTerm()
    {
        return ParseOperatorTerm(0);
    }

    private PlAstNode ParseOperatorTerm(int minPrecedence)
    {
        var left = ParsePrimaryTerm();

        while (Check(PlTokenType.Operator) && GetPrecedence(Peek().Value) >= minPrecedence)
        {
            var op = Advance().Value;
            var precedence = GetPrecedence(op);
            var right = ParseOperatorTerm(precedence + 1);
            left = new PlBinaryOp(left, op, right);
        }

        if (Match(PlTokenType.Operator, "->"))
        {
            var thenBranch = ParseTerm();
            Consume(PlTokenType.Operator, "PLG2006", "期望 ';'");
            var elseBranch = ParseTerm();
            left = new PlIfThenElse(left, thenBranch, elseBranch);
        }

        return left;
    }

    private static int GetPrecedence(string op)
    {
        return op switch
        {
            ";" => 100,
            "->" => 200,
            "," => 300,
            "=" => 400,
            "\\=" => 400,
            "==" => 400,
            "\\==" => 400,
            "@<" => 400,
            "@>" => 400,
            "@=<" => 400,
            "@>=" => 400,
            "is" => 400,
            "=.." => 400,
            "<" => 400,
            ">" => 400,
            "=<" => 400,
            ">=" => 400,
            ":" => 500,
            "+" => 600,
            "-" => 600,
            "*" => 700,
            "/" => 700,
            "//" => 700,
            "mod" => 700,
            "rem" => 700,
            "**" => 800,
            "^" => 800,
            "\\" => 900,
            _ => 0
        };
    }

    private PlAstNode ParsePrimaryTerm()
    {
        if (Check(PlTokenType.Operator, "\\+"))
        {
            Advance();
            var goal = ParsePrimaryTerm();
            return new PlNot(goal);
        }

        if (Check(PlTokenType.Operator, "!"))
        {
            Advance();
            return new PlCut();
        }

        if (Check(PlTokenType.Atom, "fail"))
        {
            Advance();
            return new PlFail();
        }

        if (Check(PlTokenType.Atom, "true"))
        {
            Advance();
            return new PlTrue();
        }

        if (Check(PlTokenType.Number))
        {
            var token = Advance();
            return new PlNumber(Peek().Value);
        }

        if (Check(PlTokenType.String))
        {
            var token = Advance();
            return new PlString(Peek().Value);
        }

        if (Check(PlTokenType.Variable))
        {
            var token = Advance();
            return new PlVariable(Peek().Value);
        }

        if (Check(PlTokenType.Atom))
        {
            var name = Advance().Value;

            if (Match(PlTokenType.Delimiter, "("))
            {
                var args = new List<PlAstNode>();
                if (!Check(PlTokenType.Delimiter, ")"))
                {
                    args.Add(ParseTerm());
                    while (Match(PlTokenType.Delimiter, ",")) args.Add(ParseTerm());
                }

                Consume(PlTokenType.Delimiter, "PLG2007", "期望 ')'");
                return new PlCompound(name, args);
            }

            return new PlAtom(name);
        }

        if (Check(PlTokenType.Delimiter, "("))
        {
            Advance();
            var expr = ParseTerm();
            Consume(PlTokenType.Delimiter, "PLG2008", "期望 ')'");
            return expr;
        }

        if (Check(PlTokenType.Delimiter, "["))
        {
            return ParseList();
        }

        if (Check(PlTokenType.Delimiter, "{"))
        {
            return ParseTuple();
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            "PLG2009",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    private PlList ParseList()
    {
        Consume(PlTokenType.Delimiter, "PLG2010", "期望 '['");

        var elements = new List<PlAstNode>();

        if (!Check(PlTokenType.Delimiter, "]"))
        {
            elements.Add(ParseTerm());

            while (Match(PlTokenType.Delimiter, ",")) elements.Add(ParseTerm());
        }

        PlAstNode? tail = null;
        if (Match(PlTokenType.Operator, "|"))
        {
            tail = ParseTerm();
        }

        Consume(PlTokenType.Delimiter, "PLG2011", "期望 ']'");
        return new PlList(elements, tail);
    }

    private PlTuple ParseTuple()
    {
        Consume(PlTokenType.Delimiter, "PLG2012", "期望 '{'");

        var elements = new List<PlAstNode>();

        if (!Check(PlTokenType.Delimiter, "}"))
        {
            elements.Add(ParseTerm());

            while (Match(PlTokenType.Delimiter, ",")) elements.Add(ParseTerm());
        }

        Consume(PlTokenType.Delimiter, "PLG2013", "期望 '}'");
        return new PlTuple(elements);
    }

    #endregion
}
