using System.Text;

namespace Std.Data.Text.Prolog.Lexer;

public sealed class PlLexer
{
    private static readonly HashSet<char> Delimiters = ['(', ')', '[', ']', '{', '}', ',', '.', '|'];

    private DiagnosticSink? _diagnostics;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private ISource _source = StringSource.Empty;

    public PlLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public IReadOnlyList<PlToken> Tokenize(string source)
    {
        _source = new StringSource(source);
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<PlToken>();

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

        tokens.Add(new PlToken(PlTokenType.Eof, string.Empty, _line, _column));
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

    private PlToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = Peek();

        if (c == '"' || c == '\'')
        {
            return ScanQuotedAtom(startLine, startColumn);
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
            return ScanAtom(startLine, startColumn);
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
                return new PlToken(PlTokenType.Punctuation, value, startLine, startColumn);
            }

            return new PlToken(PlTokenType.Delimiter, value, startLine, startColumn);
        }

        Advance();
        _diagnostics?.AddError(string.Empty, default, "PLG1001", $"意外的字符 '{c}'");
        return null;
    }

    private PlToken ScanQuotedAtom(int startLine, int startColumn)
    {
        var quote = Advance();
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != quote)
        {
            if (Peek() == '\\') { Advance(); if (IsAtEnd())
                {
                    break;
                }

                var e = Advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '\'' => '\'', '"' => '"', '0' => '\0', _ => e }); }
            else
            {
                sb.Append(Advance());
            }
        }
        if (IsAtEnd())
        {
            _diagnostics?.AddError(string.Empty, default, "PLG1002", "未闭合的引号原子");
        }
        else
        {
            Advance();
        }

        return new PlToken(PlTokenType.String, sb.ToString(), startLine, startColumn);
    }

    private PlToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext())) { sb.Append(Advance()); while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E')) { sb.Append(Advance()); if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                sb.Append(Advance());
            }

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        return new PlToken(PlTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    private PlToken ScanVariable(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        return new PlToken(PlTokenType.Variable, sb.ToString(), startLine, startColumn);
    }

    private PlToken ScanAtom(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        var text = sb.ToString();
        if (PlOperators.All.Contains(text))
        {
            return new PlToken(PlTokenType.Operator, text, startLine, startColumn);
        }

        return new PlToken(PlTokenType.Atom, text, startLine, startColumn);
    }

    private PlToken ScanOperator(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());
        while (!IsAtEnd()) { var candidate = sb.ToString() + Peek(); if (PlOperators.All.Contains(candidate))
            {
                sb.Append(Advance());
            }
            else
            {
                break;
            }
        }
        return new PlToken(PlTokenType.Operator, sb.ToString(), startLine, startColumn);
    }

    private static bool IsOperatorStart(char c) => c is ':' or '?' or '=' or '\\' or '<' or '>' or '+' or '-' or '*' or '/' or '^' or '!' or '~' or ';' or '|' or '@';

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            switch (Peek())
            {
                case ' ' or '\t' or '\r' or '\n': Advance(); break;
                case '%': SkipLineComment(); break;
                case '/': if (PeekNext() == '*')
                    {
                        SkipBlockComment();
                    }
                    else
                    {
                        return;
                    }

                    break;
                default: return;
            }
        }
    }

    private void SkipLineComment() { while (!IsAtEnd() && Peek() != '\n') Advance(); }

    private void SkipBlockComment()
    {
        Advance(); Advance();
        var startLine = _line; var startColumn = _column;
        while (!IsAtEnd()) { if (Peek() == '*' && PeekNext() == '/') { Advance(); Advance(); return; } Advance(); }
        _diagnostics?.AddError(string.Empty, default, "PLG1003", "未闭合的块注释");
    }
}
