using System.Text;

namespace Std.Data.Text.Erlang.Lexer;

public sealed class ErLexer
{
    private static readonly HashSet<string> Operators =
    [
        new(StringComparer.Ordinal),
        "+", "-", "*", "/", "==", "/=", "=<", ">=", "<", ">", "=:=", "=/=",
        "++", "--", "!", "=", "<-", "<<", ">>", "||",
        "->", ":", "::", "..", "#", "@", "?"
    ];

    private static readonly HashSet<char> Delimiters = ['(', ')', '{', '}', '[', ']', ',', '.', ';', '|'];

    private DiagnosticSink? _diagnostics;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private ISource _source = StringSource.Empty;

    public ErLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public IReadOnlyList<ErToken> Tokenize(string source)
    {
        _source = new StringSource(source);
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<ErToken>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();
            if (IsAtEnd())
            {
                break;
            }

            var token = ScanToken();
            if (token is not null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new ErToken(ErTokenType.Eof, string.Empty, _line, _column));
        return tokens;
    }

    private bool IsAtEnd() => _position >= _source.Length;
    private char Peek() => IsAtEnd() ? '\0' : _source[_position];
    private char PeekNext() => Peek(1);
    private char Peek(int offset) { var i = _position + offset; return i >= _source.Length ? '\0' : _source[i]; }

    private char Advance()
    {
        var c = _source[_position];
        _position++;
        if (c == '\n') { _line++; _column = 1; } else { _column++; }
        return c;
    }

    private ErToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = Peek();

        if (c == '"')
        {
            return ScanString(startLine, startColumn);
        }

        if (c == '\'')
        {
            return ScanQuotedAtom(startLine, startColumn);
        }

        if (c == '$')
        {
            return ScanCharLiteral(startLine, startColumn);
        }

        if (char.IsDigit(c))
        {
            return ScanNumber(startLine, startColumn);
        }

        if (char.IsUpper(c) || c == '_')
        {
            return ScanVariable(startLine, startColumn);
        }

        if (char.IsLower(c))
        {
            return ScanAtomOrKeyword(startLine, startColumn);
        }

        if (IsOperatorStart(c))
        {
            return ScanOperator(startLine, startColumn);
        }

        if (Delimiters.Contains(c))
        {
            Advance();
            var value = c.ToString();
            if (c == '.')
            {
                return new ErToken(ErTokenType.Punctuation, value, startLine, startColumn);
            }

            return new ErToken(ErTokenType.Delimiter, value, startLine, startColumn);
        }

        Advance();
        _diagnostics?.AddError(string.Empty, default, "ERL1001", $"意外的字符 '{c}'");
        return null;
    }

    private ErToken ScanString(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\') { Advance(); if (IsAtEnd())
                {
                    break;
                }

                var e = Advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '"' => '"', '\'' => '\'', '0' => '\0', _ => e }); }
            else
            {
                sb.Append(Advance());
            }
        }
        if (IsAtEnd())
        {
            _diagnostics?.AddError(string.Empty, default, "ERL1002", "未闭合的字符串字面量");
        }
        else
        {
            Advance();
        }

        return new ErToken(ErTokenType.String, sb.ToString(), startLine, startColumn);
    }

    private ErToken ScanQuotedAtom(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '\'')
        {
            if (Peek() == '\\') { Advance(); if (IsAtEnd())
                {
                    break;
                }

                var e = Advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '\'' => '\'', _ => e }); }
            else
            {
                sb.Append(Advance());
            }
        }
        if (IsAtEnd())
        {
            _diagnostics?.AddError(string.Empty, default, "ERL1003", "未闭合的原子");
        }
        else
        {
            Advance();
        }

        return new ErToken(ErTokenType.Atom, sb.ToString(), startLine, startColumn);
    }

    private ErToken ScanCharLiteral(int startLine, int startColumn)
    {
        Advance();
        if (IsAtEnd())
        {
            return new ErToken(ErTokenType.Char, string.Empty, startLine, startColumn);
        }

        if (Peek() == '\\')
        {
            Advance();
            if (IsAtEnd())
            {
                return new ErToken(ErTokenType.Char, string.Empty, startLine, startColumn);
            }

            var e = Advance();
            var value = e switch { 'n' => "\n", 'r' => "\r", 't' => "\t", '\\' => "\\", '\'' => "'", '"' => "\"", '0' => "\0", _ => e.ToString() };
            return new ErToken(ErTokenType.Char, value, startLine, startColumn);
        }
        var ch = Advance();
        return new ErToken(ErTokenType.Char, ch.ToString(), startLine, startColumn);
    }

    private ErToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        if (Peek() == '0' && (PeekNext() == 'x' || PeekNext() == 'X')) { Advance(); Advance(); sb.Append("0x"); while (!IsAtEnd() && IsHexDigit(Peek())) sb.Append(Advance()); return new ErToken(ErTokenType.Number, sb.ToString(), startLine, startColumn); }
        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext())) { sb.Append(Advance()); while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E')) { sb.Append(Advance()); if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                sb.Append(Advance());
            }

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        return new ErToken(ErTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    private static bool IsHexDigit(char c) => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';

    private ErToken ScanVariable(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()) || Peek() == '@')) sb.Append(Advance());
        return new ErToken(ErTokenType.Variable, sb.ToString(), startLine, startColumn);
    }

    private ErToken ScanAtomOrKeyword(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || Peek() == '@' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        var text = sb.ToString();
        if (ErKeywords.All.Contains(text))
        {
            return new ErToken(ErTokenType.Keyword, text, startLine, startColumn);
        }

        return new ErToken(ErTokenType.Atom, text, startLine, startColumn);
    }

    private ErToken ScanOperator(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());
        while (!IsAtEnd()) { var candidate = sb.ToString() + Peek(); if (Operators.Contains(candidate))
            {
                sb.Append(Advance());
            }
            else
            {
                break;
            }
        }
        return new ErToken(ErTokenType.Operator, sb.ToString(), startLine, startColumn);
    }

    private static bool IsOperatorStart(char c) => c is '+' or '-' or '*' or '/' or '=' or '!' or '<' or '>' or '|' or ':' or '#' or '@' or '?' or '~';

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            switch (Peek())
            {
                case ' ' or '\t' or '\r' or '\n': Advance(); break;
                case '%': while (!IsAtEnd() && Peek() != '\n') Advance(); break;
                default: return;
            }
        }
    }
}
