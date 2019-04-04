using System.Text;

namespace Std.Data.Text.OCaml.Lexer;

public sealed class OcLexer
{
    private static readonly HashSet<string> Operators =
    [
        new(StringComparer.Ordinal),
        "->", "<-", "::", "|", "||", "&&", ";;", ";",
        "=", "<>", "<", ">", "<=", ">=", "==", "!=",
        "+", "-", "*", "/", "%", "**", "^",
        "@", "!", "?", "~", ":=", "|>",
        ">>", "<<", ">>>", "<<<",
        "+.", "-.", "*.", "/.", "**.", "=.", "<>.", "<.", ">.", "<=.", ">=."
    ];

    private static readonly HashSet<char> Delimiters = ['(', ')', '{', '}', '[', ']', ',', '.'];

    private DiagnosticSink? _diagnostics;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private ISource _source = StringSource.Empty;

    public OcLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public IReadOnlyList<OcToken> Tokenize(string source)
    {
        _source = new StringSource(source);
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<OcToken>();

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

        tokens.Add(new OcToken(OcTokenType.Eof, string.Empty, _line, _column));
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

    private OcToken? ScanToken()
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
            return ScanChar(startLine, startColumn);
        }

        if (char.IsDigit(c))
        {
            return ScanNumber(startLine, startColumn);
        }

        if (c == '_' || char.IsLetter(c))
        {
            return ScanIdentifierOrKeyword(startLine, startColumn);
        }

        if (IsOperatorStart(c))
        {
            return ScanOperator(startLine, startColumn);
        }

        if (Delimiters.Contains(c))
        {
            if (c == '(' && PeekNext() == '*')
            {
                return ScanBlockCommentAsToken(startLine, startColumn);
            }

            Advance();
            return new OcToken(OcTokenType.Delimiter, c.ToString(), startLine, startColumn);
        }

        Advance();
        _diagnostics?.AddError(string.Empty, default, "OCM1001", $"意外的字符 '{c}'");
        return null;
    }

    private OcToken ScanString(int startLine, int startColumn)
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
            _diagnostics?.AddError(string.Empty, default, "OCM1002", "未闭合的字符串字面量");
        }
        else
        {
            Advance();
        }

        return new OcToken(OcTokenType.String, sb.ToString(), startLine, startColumn);
    }

    private OcToken ScanChar(int startLine, int startColumn)
    {
        Advance();
        var sb = new StringBuilder();
        if (!IsAtEnd() && Peek() == '\\') { Advance(); if (!IsAtEnd()) { var e = Advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '\'' => '\'', '0' => '\0', _ => e }); } }
        else if (!IsAtEnd())
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == '\'')
        {
            Advance();
        }
        else
        {
            _diagnostics?.AddError(string.Empty, default, "OCM1003", "未闭合的字符字面量");
        }

        return new OcToken(OcTokenType.Char, sb.ToString(), startLine, startColumn);
    }

    private OcToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        if (Peek() == '0' && (PeekNext() == 'x' || PeekNext() == 'X')) { Advance(); Advance(); sb.Append("0x"); while (!IsAtEnd() && IsHexDigit(Peek())) sb.Append(Advance()); return new OcToken(OcTokenType.Number, sb.ToString(), startLine, startColumn); }
        if (Peek() == '0' && (PeekNext() == 'o' || PeekNext() == 'O')) { Advance(); Advance(); sb.Append("0o"); while (!IsAtEnd() && IsOctalDigit(Peek())) sb.Append(Advance()); return new OcToken(OcTokenType.Number, sb.ToString(), startLine, startColumn); }
        if (Peek() == '0' && (PeekNext() == 'b' || PeekNext() == 'B')) { Advance(); Advance(); sb.Append("0b"); while (!IsAtEnd() && IsBinaryDigit(Peek())) sb.Append(Advance()); return new OcToken(OcTokenType.Number, sb.ToString(), startLine, startColumn); }
        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext())) { sb.Append(Advance()); while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E')) { sb.Append(Advance()); if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                sb.Append(Advance());
            }

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance()); }
        if (!IsAtEnd() && (Peek() == 'l' || Peek() == 'L' || Peek() == 'n'))
        {
            sb.Append(Advance());
        }

        return new OcToken(OcTokenType.Number, sb.ToString(), startLine, startColumn);
    }

    private static bool IsHexDigit(char c) => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    private static bool IsOctalDigit(char c) => c is >= '0' and <= '7';
    private static bool IsBinaryDigit(char c) => c is '0' or '1';

    private OcToken ScanIdentifierOrKeyword(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        if (char.IsUpper(Peek()))
        {
            while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
            if (!IsAtEnd() && Peek() == '.')
            {
                return new OcToken(OcTokenType.ModuleName, sb.ToString(), startLine, startColumn);
            }

            var text = sb.ToString();
            if (OcKeywords.All.Contains(text))
            {
                return new OcToken(OcTokenType.Keyword, text, startLine, startColumn);
            }

            return new OcToken(OcTokenType.ModuleName, text, startLine, startColumn);
        }
        while (!IsAtEnd() && (Peek() == '_' || Peek() == '\'' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        var id = sb.ToString();
        if (OcKeywords.All.Contains(id))
        {
            return new OcToken(OcTokenType.Keyword, id, startLine, startColumn);
        }

        return new OcToken(OcTokenType.Identifier, id, startLine, startColumn);
    }

    private OcToken ScanOperator(int startLine, int startColumn)
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
        return new OcToken(OcTokenType.Operator, sb.ToString(), startLine, startColumn);
    }

    private OcToken ScanBlockCommentAsToken(int startLine, int startColumn)
    {
        Advance(); Advance();
        var depth = 1;
        var sb = new StringBuilder("(*");
        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '(' && PeekNext() == '*') { Advance(); Advance(); depth++; sb.Append("(*"); }
            else if (Peek() == '*' && PeekNext() == ')') { Advance(); Advance(); depth--; sb.Append("*)"); }
            else
            {
                sb.Append(Advance());
            }
        }
        if (depth > 0)
        {
            _diagnostics?.AddError(string.Empty, default, "OCM1004", "未闭合的块注释");
        }

        return new OcToken(OcTokenType.Comment, sb.ToString(), startLine, startColumn);
    }

    private static bool IsOperatorStart(char c) => c is '!' or '=' or '<' or '>' or '|' or '&' or '+' or '-' or '*' or '/' or '%' or '^' or '@' or '~' or '?' or ':';

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            switch (Peek())
            {
                case ' ' or '\t' or '\r' or '\n': Advance(); break;
                default: return;
            }
        }
    }
}
