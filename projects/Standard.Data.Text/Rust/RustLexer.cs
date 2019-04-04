using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Lexing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Rust;


/// <summary>
///     Rust 语言词法分析器
/// </summary>
public sealed class RustLexer : LexerBase
{
    private new int _line = 1;
    private new int _column = 1;

    private static readonly HashSet<string> _keywords =
    [
        new(StringComparer.Ordinal),
        "as", "async", "await", "break", "const", "continue", "crate", "dyn",
        "else", "enum", "extern", "fn", "for", "if", "impl", "in",
        "let", "loop", "match", "mod", "move", "mut", "pub", "ref",
        "return", "self", "Self", "static", "struct", "super", "trait", "type",
        "unsafe", "use", "where", "while", "yield",
        "true", "false"
    ];

    private static readonly HashSet<string> _operators =
    [
        new(StringComparer.Ordinal),
        "+", "-", "*", "/", "%",
        "==", "!=", ">", "<", ">=", "<=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>",
        "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=",
        ".", "..", "...", "..=",
        "->", "=>", "?", ":", "::", ";",
        "@", "#", "$"
    ];

    public RustLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码转换为词法单元序列
    /// </summary>
    public override IReadOnlyList<GreenLeafNode> tokenize(string source)
    {
        var tokens = tokenize_as_rust_tokens(source);
        var nodes = new List<GreenLeafNode>();

        foreach (var token in tokens)
        {
            nodes.Add(new GreenLeafNode(token.kind, token.text.Length, token.text));
        }

        return nodes;
    }

    /// <summary>
    ///     将源代码转换为 RustToken 序列
    /// </summary>
    public IReadOnlyList<RustToken> tokenize_as_rust_tokens(string source)
    {
        _source = new StringSource(source);
        reset();
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<RustToken>();

        while (!is_at_end())
        {
            skip_whitespace();

            if (is_at_end())
            {
                break;
            }

            var c = peek();
            var line = _line;
            var column = _column;

            if (c is '\n' or '\r')
            {
                advance_new_line();
                tokens.Add(new RustToken(RustNodeKind.new_line, "\n", line, column));
                continue;
            }

            if (c == '/' && peek_next() == '/')
            {
                skip_line_comment();
                continue;
            }

            if (c == '/' && peek_next() == '*')
            {
                scan_block_comment_token(tokens, line, column);
                continue;
            }

            scan_token(tokens, line, column);
        }

        tokens.Add(new RustToken(RustNodeKind.eof, string.Empty, _line, _column));
        return tokens;
    }

    private void scan_token(List<RustToken> tokens, int line, int column)
    {
        var c = peek();

        if (c == '"')
        {
            scan_string_token(tokens, line, column);
            return;
        }

        if (c == '\'')
        {
            scan_char_or_lifetime_token(tokens, line, column);
            return;
        }

        if (char.IsDigit(c))
        {
            scan_number_token(tokens, line, column);
            return;
        }

        if (c == '_' || char.IsLetter(c))
        {
            scan_identifier_or_keyword_token(tokens, line, column);
            return;
        }

        if (is_operator_start(c))
        {
            scan_operator_token(tokens, line, column);
            return;
        }

        if (is_delimiter(c))
        {
            advance();
            tokens.Add(new RustToken(RustNodeKind.delimiter, c.ToString(), line, column));
            return;
        }

        advance();
        _diagnostics?.report_error(
            string.Empty,
            default,
            1001,
            $"意外的字符 '{c}'");
    }

    private void scan_string_token(List<RustToken> tokens, int line, int column)
    {
        advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '"')
        {
            if (peek() == '\\')
            {
                advance();
                if (is_at_end())
                {
                    break;
                }

                var escaped = advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(advance());
            }
        }

        if (!is_at_end())
        {
            advance();
        }

        tokens.Add(new RustToken(RustNodeKind.@string, sb.ToString(), line, column));
    }

    private void scan_char_or_lifetime_token(List<RustToken> tokens, int line, int column)
    {
        advance();

        if (!is_at_end() && peek() != '\'' && !char.IsWhiteSpace(peek()) && peek() != '\\')
        {
            var next = advance();

            if (!is_at_end() && peek() == '\'')
            {
                advance();
                tokens.Add(new RustToken(RustNodeKind.@char, next.ToString(), line, column));
                return;
            }

            var sb = new StringBuilder();
            sb.Append(next);

            while (!is_at_end() && (peek() == '_' || char.IsLetterOrDigit(peek())))
            {
                sb.Append(advance());
            }

            tokens.Add(new RustToken(RustNodeKind.identifier, $"'{sb}", line, column));
            return;
        }

        tokens.Add(new RustToken(RustNodeKind.identifier, "'", line, column));
    }

    private void scan_number_token(List<RustToken> tokens, int line, int column)
    {
        var sb = new StringBuilder();
        var isHex = false;
        var isBinary = false;
        var isOctal = false;

        if (peek() == '0')
        {
            sb.Append(advance());

            if (!is_at_end())
            {
                if (peek() == 'x' || peek() == 'X')
                {
                    sb.Append(advance());
                    isHex = true;
                }
                else if (peek() == 'b' || peek() == 'B')
                {
                    sb.Append(advance());
                    isBinary = true;
                }
                else if (peek() == 'o' || peek() == 'O')
                {
                    sb.Append(advance());
                    isOctal = true;
                }
            }
        }

        if (isHex)
        {
            while (!is_at_end() && (char.IsDigit(peek()) || (peek() >= 'a' && peek() <= 'f') ||
                                  (peek() >= 'A' && peek() <= 'F') || peek() == '_'))
            {
                var c = advance();
                if (c != '_')
                {
                    sb.Append(c);
                }
            }
        }
        else if (isBinary)
        {
            while (!is_at_end() && (peek() == '0' || peek() == '1' || peek() == '_'))
            {
                var c = advance();
                if (c != '_')
                {
                    sb.Append(c);
                }
            }
        }
        else if (isOctal)
        {
            while (!is_at_end() && ((peek() >= '0' && peek() <= '7') || peek() == '_'))
            {
                var c = advance();
                if (c != '_')
                {
                    sb.Append(c);
                }
            }
        }
        else
        {
            while (!is_at_end() && (char.IsDigit(peek()) || peek() == '_'))
            {
                var c = advance();
                if (c != '_')
                {
                    sb.Append(c);
                }
            }
        }

        if (!isHex && !isBinary && !isOctal && !is_at_end() && peek() == '.' && !is_delimiter(peek_next()))
        {
            sb.Append(advance());
            while (!is_at_end() && (char.IsDigit(peek()) || peek() == '_'))
            {
                var c = advance();
                if (c != '_')
                {
                    sb.Append(c);
                }
            }
        }

        if (!isHex && !isBinary && !isOctal && !is_at_end() && (peek() == 'e' || peek() == 'E'))
        {
            sb.Append(advance());
            if (!is_at_end() && (peek() == '+' || peek() == '-'))
            {
                sb.Append(advance());
            }

            while (!is_at_end() && (char.IsDigit(peek()) || peek() == '_'))
            {
                var c = advance();
                if (c != '_')
                {
                    sb.Append(c);
                }
            }
        }

        while (!is_at_end() && (peek() == 'f' || peek() == 'F' || peek() == 'i' || peek() == 'I' || peek() == 'u' ||
                              peek() == 'U'))
        {
            sb.Append(advance());

            if (!is_at_end() && char.IsDigit(peek()))
            {
                while (!is_at_end() && char.IsDigit(peek()))
                {
                    sb.Append(advance());
                }
            }

            break;
        }

        tokens.Add(new RustToken(RustNodeKind.number, sb.ToString(), line, column));
    }

    private void scan_identifier_or_keyword_token(List<RustToken> tokens, int line, int column)
    {
        var sb = new StringBuilder();

        while (!is_at_end() && (peek() == '_' || char.IsLetterOrDigit(peek())))
        {
            sb.Append(advance());
        }

        var text = sb.ToString();
        var kind = _keywords.Contains(text) ? RustNodeKind.keyword : RustNodeKind.identifier;
        tokens.Add(new RustToken(kind, text, line, column));
    }

    private void scan_operator_token(List<RustToken> tokens, int line, int column)
    {
        var sb = new StringBuilder();
        sb.Append(advance());

        while (!is_at_end())
        {
            var candidate = sb.ToString() + peek();

            if (_operators.Contains(candidate))
            {
                sb.Append(advance());
            }
            else
            {
                break;
            }
        }

        tokens.Add(new RustToken(RustNodeKind.@operator, sb.ToString(), line, column));
    }

    private void skip_line_comment()
    {
        while (!is_at_end() && peek() != '\n') advance();
    }

    private void scan_block_comment_token(List<RustToken> tokens, int line, int column)
    {
        var sb = new StringBuilder();

        sb.Append(advance());
        sb.Append(advance());

        var depth = 1;

        while (!is_at_end() && depth > 0)
        {
            if (peek() == '/' && peek_next() == '*')
            {
                sb.Append(advance());
                sb.Append(advance());
                depth++;
            }
            else if (peek() == '*' && peek_next() == '/')
            {
                sb.Append(advance());
                sb.Append(advance());
                depth--;
            }
            else
            {
                sb.Append(advance());
            }
        }

        var value = sb.ToString();
        if (!string.IsNullOrEmpty(value))
        {
            tokens.Add(new RustToken(RustNodeKind.comment, value, line, column));
        }
    }

    private static bool is_operator_start(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' or '.' or '?' or ':' or '@' or '#' or '$' => true,
            _ => false
        };
    }

    private static bool is_delimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ';';
    }

    private void advance_new_line()
    {
        if (peek() == '\r')
        {
            advance();
            _column = 1;
        }

        if (peek() == '\n')
        {
            advance();
            _line++;
            _column = 1;
        }
    }

    private new void skip_whitespace()
    {
        while (!is_at_end())
        {
            var c = peek();
            if (c is ' ' or '\t' or '\f' or '\v')
            {
                advance();
                _column++;
            }
            else if (c is '\n' or '\r')
            {
                advance_new_line();
            }
            else
            {
                break;
            }
        }
    }
}
