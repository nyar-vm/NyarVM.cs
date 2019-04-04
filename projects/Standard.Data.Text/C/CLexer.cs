using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Lexing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;


/// <summary>
///     C 语言词法分析器
/// </summary>
public sealed class CLexer : LexerBase
{
    private static readonly HashSet<string> _keywords =
    [
        new(StringComparer.Ordinal),
        "auto", "break", "case", "char", "const", "continue", "default", "do",
        "double", "else", "enum", "extern", "float", "for", "goto", "if",
        "inline", "int", "long", "register", "restrict", "return", "short", "signed",
        "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void",
        "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex",
        "_Generic", "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local"
    ];

    private static readonly HashSet<string> _operators =
    [
        new(StringComparer.Ordinal),
        "+", "-", "*", "/", "%", "++", "--",
        "==", "!=", ">", "<", ">=", "<=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>",
        "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=",
        ".", "->", "...",
        "?", ":"
    ];

    /// <summary>
    ///     创建 C 语言词法分析器
    /// </summary>
    public CLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }


/// <summary>
///     将源代码转换为词法单元序列（GreenLeafNode）
/// </summary>
    public override IReadOnlyList<GreenLeafNode> tokenize(string source)
    {
        var ctokens = tokenize_as_c_tokens(source);
        return [.. ctokens.Select(t => new GreenLeafNode(t.kind, t.text.Length, t.text))];
    }


/// <summary>
///     将源代码转换为 C 词法单元序列
/// </summary>
    public IReadOnlyList<CToken> tokenize_as_c_tokens(string source)
    {
        _source = new StringSource(source);
        reset();
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<CToken>();

        while (!is_at_end())
        {
            skip_whitespace();

            if (is_at_end())
            {
                break;
            }

            var c = peek();

            if (c is '\n' or '\r')
            {
                advance_new_line();
                tokens.Add(new CToken(CNodeKind.new_line, "\n"));
                continue;
            }

            if (c == '#')
            {
                var pp = scan_preprocessor_text();
                if (pp is not null)
                {
                    tokens.Add(new CToken(CNodeKind.preprocessor, pp));
                }

                continue;
            }

            if (c == '/' && peek_next() == '/')
            {
                skip_line_comment();
                continue;
            }

            if (c == '/' && peek_next() == '*')
            {
                var comment = scan_block_comment_text();
                if (comment is not null)
                {
                    tokens.Add(new CToken(CNodeKind.comment, comment));
                }

                continue;
            }

            var (kind, text) = scan_token_parts();

            if (kind is not null)
            {
                tokens.Add(new CToken(kind.Value, text));
            }
        }

        tokens.Add(new CToken(CNodeKind.eof, ""));
        return tokens;
    }

    private (NodeKind? Kind, string Text) scan_token_parts()
    {
        var c = peek();

        if (c is '"')
        {
            return (CNodeKind.@string, scan_string_text());
        }

        if (c is '\'')
        {
            return (CNodeKind.@char, scan_char_text());
        }

        if (char.IsDigit(c))
        {
            return (CNodeKind.number, scan_number_text());
        }

        if (c == '_' || char.IsLetter(c))
        {
            return scan_identifier_or_keyword_text();
        }

        if (is_operator_start(c))
        {
            return (CNodeKind.@operator, scan_operator_text());
        }

        if (is_delimiter(c)) { advance(); return (CNodeKind.delimiter, c.ToString()); }
        advance();
        _diagnostics?.report_error(
            string.Empty,
            default,
            1001,
            $"意外的字符 '{c}'");

        return (null, "");
    }

    private string scan_string_text()
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
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '"' => '"', '0' => '\0',
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

        return sb.ToString();
    }

    private string scan_char_text()
    {
        advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '\'')
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
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '\'' => '\'', '0' => '\0',
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

        return sb.ToString();
    }

    private string scan_number_text()
    {
        var sb = new StringBuilder();
        var isHex = false;
        var isBinary = false;

        if (peek() == '0')
        {
            sb.Append(advance());
            if (!is_at_end() && (peek() == 'x' || peek() == 'X')) { sb.Append(advance()); isHex = true; }
            else if (!is_at_end() && (peek() == 'b' || peek() == 'B')) { sb.Append(advance()); isBinary = true; }
        }

        if (isHex)
        {
            while (!is_at_end() && (char.IsDigit(peek()) || (peek() >= 'a' && peek() <= 'f') || (peek() >= 'A' && peek() <= 'F'))) sb.Append(advance());
        }
        else if (isBinary)
        {
            while (!is_at_end() && (peek() == '0' || peek() == '1')) sb.Append(advance());
        }
        else
        {
            while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());
        }

        if (!isHex && !isBinary && !is_at_end() && peek() == '.' && char.IsDigit(peek_next()))
        {
            sb.Append(advance());
            while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());
        }

        if (!is_at_end() && (peek() == 'e' || peek() == 'E'))
        {
            sb.Append(advance());
            if (!is_at_end() && (peek() == '+' || peek() == '-'))
            {
                sb.Append(advance());
            }

            while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());
        }

        while (!is_at_end() && (peek() == 'u' || peek() == 'U' || peek() == 'l' || peek() == 'L' || peek() == 'f' || peek() == 'F')) sb.Append(advance());

        return sb.ToString();
    }

    private (NodeKind Kind, string Text) scan_identifier_or_keyword_text()
    {
        var sb = new StringBuilder();
        while (!is_at_end() && (peek() == '_' || char.IsLetterOrDigit(peek()))) sb.Append(advance());
        var text = sb.ToString();
        return _keywords.Contains(text) ? (Keyword: CNodeKind.keyword, text) : (Identifier: CNodeKind.identifier, text);
    }

    private string scan_operator_text()
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
        return sb.ToString();
    }

    private string? scan_preprocessor_text()
    {
        var sb = new StringBuilder();
        while (!is_at_end() && peek() != '\n') sb.Append(advance());
        var value = sb.ToString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private void skip_line_comment()
    {
        while (!is_at_end() && peek() != '\n') advance();
    }

    private string? scan_block_comment_text()
    {
        var sb = new StringBuilder();
        sb.Append(advance());
        sb.Append(advance());
        while (!is_at_end() && !(peek() == '*' && peek_next() == '/')) sb.Append(advance());
        if (!is_at_end()) { sb.Append(advance()); sb.Append(advance()); }
        var value = sb.ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool is_operator_start(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' or '.' or '?' or ':' => true,
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
        }

        if (peek() == '\n')
        {
            advance();
        }
    }

    private new void skip_whitespace()
    {
        while (!is_at_end() && (peek() == ' ' || peek() == '\t' || peek() == '\f' || peek() == '\v')) advance();
    }
}
