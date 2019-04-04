using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Julia.Lexer;

public sealed class JlLexer
{
    private static readonly HashSet<string> _operators =
    [
        new(StringComparer.Ordinal),
        "+", "-", "*", "/", "//", "^", "%", "\\", "~",
        "==", "!=", "===", "!==", "<", ">", "<=", ">=",
        "=", "+=", "-=", "*=", "/=", "//=", "^=", "%=", "\\=",
        "&&", "||", "!", "<<<", ">>>", "<<", ">>",
        "&", "|",
        "->", "=>", ":", "::", "?", "...", "..",
        ".", "|>", "<|"
    ];

    private static readonly HashSet<char> _delimiters = ['(', ')', '{', '}', '[', ']', ',', ';'];

    private DiagnosticSink? _diagnostics;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private ISource _source = StringSource.empty;

    public JlLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public IReadOnlyList<JlToken> tokenize(string source)
    {
        _source = new StringSource(source);
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<JlToken>();

        while (!is_at_end())
        {
            skip_whitespace_and_comments();
            if (is_at_end())
            {
                break;
            }

            var token = scan_token();
            if (token is not null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new JlToken(JlTokenType.eof, string.Empty, _line, _column));
        return tokens;
    }

    private bool is_at_end() => _position >= _source.length;
    private char peek() => is_at_end() ? '\0' : _source[_position];
    private char peek_next() => peek(1);
    private char peek(int offset) { var i = _position + offset; return i >= _source.length ? '\0' : _source[i]; }

    private char advance()
    {
        var c = _source[_position];
        _position++;
        if (c == '\n') { _line++; _column = 1; } else { _column++; }
        return c;
    }

    private JlToken? scan_token()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = peek();

        if (c == '"') { if (peek_next() == '"' && peek(2) == '"')
            {
                return scan_triple_quoted_string(startLine, startColumn);
            }

            return scan_string(startLine, startColumn); }
        if (c == '\'')
        {
            return scan_char(startLine, startColumn);
        }

        if (c == '`') { if (peek_next() == '`' && peek(2) == '`')
            {
                return scan_triple_command(startLine, startColumn);
            }

            return scan_command(startLine, startColumn); }
        if (char.IsDigit(c))
        {
            return scan_number(startLine, startColumn);
        }

        if (c == '@')
        {
            return scan_macro_name(startLine, startColumn);
        }

        if (c == '_' || char.IsLetter(c) || c > 127)
        {
            return scan_identifier_or_keyword(startLine, startColumn);
        }

        if (is_operator_start(c))
        {
            return scan_operator(startLine, startColumn);
        }

        if (_delimiters.Contains(c)) { advance(); return new JlToken(JlTokenType.delimiter, c.ToString(), startLine, startColumn); }

        advance();
        _diagnostics?.report_error(string.Empty, default, 1001, $"意外的字符 '{c}'");
        return null;
    }

    private JlToken scan_string(int startLine, int startColumn)
    {
        advance();
        var sb = new StringBuilder();
        while (!is_at_end() && peek() != '"')
        {
            if (peek() == '\\') { advance(); if (is_at_end())
                {
                    break;
                }

                var e = advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '"' => '"', '\'' => '\'', '0' => '\0', _ => e }); }
            else
            {
                sb.Append(advance());
            }
        }
        if (is_at_end())
        {
            _diagnostics?.report_error(string.Empty, default, 1002, "未闭合的字符串字面量");
        }
        else
        {
            advance();
        }

        return new JlToken(JlTokenType.@string, sb.ToString(), startLine, startColumn);
    }

    private JlToken scan_triple_quoted_string(int startLine, int startColumn)
    {
        advance(); advance(); advance();
        var sb = new StringBuilder();
        while (!is_at_end())
        {
            if (peek() == '"' && peek_next() == '"' && peek(2) == '"') { advance(); advance(); advance(); break; }
            if (peek() == '\\') { advance(); if (!is_at_end()) { var e = advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '"' => '"', _ => e }); } }
            else
            {
                sb.Append(advance());
            }
        }
        return new JlToken(JlTokenType.@string, sb.ToString(), startLine, startColumn);
    }

    private JlToken scan_char(int startLine, int startColumn)
    {
        advance();
        var sb = new StringBuilder();
        if (!is_at_end() && peek() == '\\') { advance(); if (!is_at_end()) { var e = advance(); sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '\\' => '\\', '\'' => '\'', '0' => '\0', _ => e }); } }
        else if (!is_at_end())
        {
            sb.Append(advance());
        }

        if (!is_at_end() && peek() == '\'')
        {
            advance();
        }
        else
        {
            _diagnostics?.report_error(string.Empty, default, 1003, "未闭合的字符字面量");
        }

        return new JlToken(JlTokenType.@char, sb.ToString(), startLine, startColumn);
    }

    private JlToken scan_command(int startLine, int startColumn)
    {
        advance();
        var sb = new StringBuilder();
        while (!is_at_end() && peek() != '`') sb.Append(advance());
        if (!is_at_end())
        {
            advance();
        }

        return new JlToken(JlTokenType.command_type, sb.ToString(), startLine, startColumn);
    }

    private JlToken scan_triple_command(int startLine, int startColumn)
    {
        advance(); advance(); advance();
        var sb = new StringBuilder();
        while (!is_at_end()) { if (peek() == '`' && peek_next() == '`' && peek(2) == '`') { advance(); advance(); advance(); break; } sb.Append(advance()); }
        return new JlToken(JlTokenType.command_type, sb.ToString(), startLine, startColumn);
    }

    private JlToken scan_number(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        if (peek() == '0' && (peek_next() == 'x' || peek_next() == 'X')) { advance(); advance(); sb.Append("0x"); while (!is_at_end() && is_hex_digit(peek())) sb.Append(advance()); return new JlToken(JlTokenType.number, sb.ToString(), startLine, startColumn); }
        if (peek() == '0' && (peek_next() == 'b' || peek_next() == 'B')) { advance(); advance(); sb.Append("0b"); while (!is_at_end() && is_binary_digit(peek())) sb.Append(advance()); return new JlToken(JlTokenType.number, sb.ToString(), startLine, startColumn); }
        if (peek() == '0' && (peek_next() == 'o' || peek_next() == 'O')) { advance(); advance(); sb.Append("0o"); while (!is_at_end() && is_octal_digit(peek())) sb.Append(advance()); return new JlToken(JlTokenType.number, sb.ToString(), startLine, startColumn); }
        while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());
        if (!is_at_end() && peek() == '.' && char.IsDigit(peek_next())) { sb.Append(advance()); while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance()); }
        if (!is_at_end() && (peek() == 'e' || peek() == 'E')) { sb.Append(advance()); if (!is_at_end() && (peek() == '+' || peek() == '-'))
            {
                sb.Append(advance());
            }

            while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance()); }
        if (!is_at_end() && (peek() == 'i' || peek() == 'f'))
        {
            sb.Append(advance());
        }

        return new JlToken(JlTokenType.number, sb.ToString(), startLine, startColumn);
    }

    private static bool is_hex_digit(char c) => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    private static bool is_octal_digit(char c) => c is >= '0' and <= '7';
    private static bool is_binary_digit(char c) => c is '0' or '1';

    private JlToken scan_macro_name(int startLine, int startColumn)
    {
        advance();
        var sb = new StringBuilder();
        while (!is_at_end() && (peek() == '_' || peek() == '!' || char.IsLetterOrDigit(peek()))) sb.Append(advance());
        return new JlToken(JlTokenType.macro_name, sb.ToString(), startLine, startColumn);
    }

    private JlToken scan_identifier_or_keyword(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && (peek() == '_' || peek() == '!' || char.IsLetterOrDigit(peek()) || peek() > 127)) sb.Append(advance());
        if (!is_at_end() && peek() == '!' && sb.Length > 0)
        {
            sb.Append(advance());
        }

        var text = sb.ToString();
        if (JlKeywords.all.Contains(text))
        {
            return new JlToken(JlTokenType.keyword, text, startLine, startColumn);
        }

        return new JlToken(JlTokenType.identifier, text, startLine, startColumn);
    }

    private JlToken scan_operator(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(advance());
        while (!is_at_end()) { var candidate = sb.ToString() + peek(); if (_operators.Contains(candidate))
            {
                sb.Append(advance());
            }
            else
            {
                break;
            }
        }
        var op = sb.ToString();
        if (op == ":")
        {
            return new JlToken(JlTokenType.punctuation, op, startLine, startColumn);
        }

        return new JlToken(JlTokenType.@operator, op, startLine, startColumn);
    }

    private static bool is_operator_start(char c) => c is '+' or '-' or '*' or '/' or '%' or '^' or '\\' or '=' or '!' or '<' or '>' or '&' or '|' or '~' or '?' or ':' or '.';

    private void skip_whitespace_and_comments()
    {
        while (!is_at_end())
        {
            switch (peek())
            {
                case ' ' or '\t' or '\r' or '\n': advance(); break;
                case '#': if (peek_next() == '=')
                    {
                        skip_block_comment();
                    }
                    else
                    {
                        skip_line_comment();
                    }

                    break;
                default: return;
            }
        }
    }

    private void skip_line_comment() { while (!is_at_end() && peek() != '\n') advance(); }

    private void skip_block_comment()
    {
        advance(); advance();
        var startLine = _line; var startColumn = _column; var depth = 1;
        while (!is_at_end() && depth > 0)
        {
            if (peek() == '#' && peek_next() == '=') { advance(); advance(); depth++; }
            else if (peek() == '=' && peek_next() == '#') { advance(); advance(); depth--; }
            else
            {
                advance();
            }
        }
        if (depth > 0)
        {
            _diagnostics?.report_error(string.Empty, default, 1004, "未闭合的块注释");
        }
    }
}
