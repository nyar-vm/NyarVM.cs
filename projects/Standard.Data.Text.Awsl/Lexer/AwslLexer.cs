using System.Text;

namespace Std.Data.Text.Awsl.Lexer;

/// <summary>
///     AWSL 词法分析器，将 AWSL 模板源码转换为 GreenLeafNode 序列。
///     支持模板标签、表达式插值、脚本声明、样式定义等 AWSL 语法。
/// </summary>
public sealed class AwslLexer : LexerBase
{
    private static readonly HashSet<string> _keywords = new(StringComparer.Ordinal)
    {
        "let", "const", "micro",
        "if", "else", "for", "foreach", "in",
        "return", "break", "continue",
        "import", "export", "using", "namespace",
        "widget", "component", "plugin",
        "match", "case", "end",
        "struct", "class", "enums", "flags", "union",
        "type"
    };

    private static readonly HashSet<string> _literals = new(StringComparer.Ordinal)
    {
        "true", "false", "null"
    };

    private static readonly HashSet<string> _operators = new(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "%",
        "==", "!=", "<", ">", "<=", ">=",
        "&&", "||", "!",
        "&", "|", "^", "~",
        "=", "+=", "-=", "*=", "/=", "%=",
        "<<", ">>", "<<=", ">>=",
        "=>", "->", "??",
        "++", "--",
        "?.",
        "</", "/>"
    };

    private static readonly HashSet<char> _delimiters = ['(', ')', '[', ']', '{', '}', ','];

    public AwslLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     将 AWSL 源码转换为词法单元序列
    /// </summary>
    /// <param name="source">AWSL 源码文本。</param>
    /// <returns>词法单元列表。</returns>
    public override IReadOnlyList<GreenLeafNode> tokenize(string source)
    {
        _source = new StringSource(source);
        reset();
        _diagnostics ??= new DiagnosticSink();

        var nodes = new List<GreenLeafNode>();

        while (!is_at_end())
        {
            skip_whitespace_and_comments();

            if (is_at_end()) break;

            var node = scan_node();

            if (node is not null) nodes.Add(node);
        }

        nodes.Add(new GreenLeafNode(AwslNodeKind.eof, 0, string.Empty));
        return nodes;
    }

    #region 主扫描方法

    private GreenLeafNode? scan_node()
    {
        var c = peek();

        if (c == '@') return scan_at_prefixed();

        if (c is '"' or '\'' or '`') return scan_string();

        if (char.IsDigit(c)) return scan_number();

        if (c == '_' || char.IsLetter(c)) return scan_identifier_or_keyword();

        if (c == '.')
        {
            var next = peek_next();

            if (char.IsDigit(next)) return scan_number();

            return scan_operator_or_delimiter();
        }

        if (c is ':' or ';') return scan_punctuation();

        if (is_operator_start(c)) return scan_operator_or_delimiter();

        if (_delimiters.Contains(c))
        {
            advance();
            return new GreenLeafNode(AwslNodeKind.delimiter, 1, c.ToString());
        }

        advance();
        _diagnostics?.report_warning(
            default,
            $"意外的字符 '{c}'");

        return null;
    }

    #endregion

    #region 标识符与关键字扫描

    private GreenLeafNode scan_identifier_or_keyword()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && (peek() == '_' || peek() == '-' || char.IsLetterOrDigit(peek()))) sb.Append(advance());

        var text = sb.ToString();

        if (_literals.Contains(text)) return new GreenLeafNode(AwslNodeKind.literal, text.Length, text);

        if (_keywords.Contains(text)) return new GreenLeafNode(AwslNodeKind.keyword, text.Length, text);

        return new GreenLeafNode(AwslNodeKind.identifier, text.Length, text);
    }

    #endregion

    #region @ 前缀扫描

    /// <summary>
    ///     扫描 @click, @bind, @input 等事件绑定或响应式绑定前缀
    /// </summary>
    private GreenLeafNode scan_at_prefixed()
    {
        advance();

        if (is_at_end()) return new GreenLeafNode(AwslNodeKind.@operator, 1, "@");

        var c = peek();

        if (c == '_' || char.IsLetter(c))
        {
            var sb = new StringBuilder();

            while (!is_at_end() && (peek() == '_' || char.IsLetterOrDigit(peek()))) sb.Append(advance());

            var name = sb.ToString();
            return new GreenLeafNode(AwslNodeKind.at_prefix, name.Length + 1, name);
        }

        return new GreenLeafNode(AwslNodeKind.@operator, 1, "@");
    }

    #endregion

    #region 辅助方法

    private static bool is_hex_digit(char c)
    {
        return char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    }

    #endregion

    #region 注释跳过

    private void skip_whitespace_and_comments()
    {
        while (!is_at_end())
        {
            var c = peek();

            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    advance();
                    break;
                case '<':
                    if (peek(1) == '!' && peek(2) == '-' && peek(3) == '-')
                        skip_html_comment();
                    else
                        return;

                    break;
                case '/':
                    if (peek_next() == '/')
                        skip_line_comment();
                    else if (peek_next() == '*')
                        skip_block_comment();
                    else
                        return;

                    break;
                default:
                    return;
            }
        }
    }

    private void skip_html_comment()
    {
        advance();
        advance();
        advance();
        advance();

        while (!is_at_end())
        {
            if (peek() == '-' && peek(1) == '-' && peek(2) == '>')
            {
                advance();
                advance();
                advance();
                return;
            }

            advance();
        }

        _diagnostics?.report_warning(
            default,
            "未闭合的 HTML 注释");
    }

    private void skip_line_comment()
    {
        while (!is_at_end() && peek() != '\n') advance();
    }

    private void skip_block_comment()
    {
        advance();
        advance();

        var depth = 1;

        while (!is_at_end() && depth > 0)
            if (peek() == '/' && peek_next() == '*')
            {
                advance();
                advance();
                depth++;
            }
            else if (peek() == '*' && peek_next() == '/')
            {
                advance();
                advance();
                depth--;
            }
            else
            {
                advance();
            }

        if (depth > 0)
            _diagnostics?.report_warning(
                default,
                "未闭合的块注释");
    }

    #endregion

    #region 字面量扫描

    private GreenLeafNode scan_number()
    {
        var sb = new StringBuilder();

        if (peek() == '0' && (peek_next() == 'x' || peek_next() == 'X'))
        {
            sb.Append(advance());
            sb.Append(advance());

            while (!is_at_end() && is_hex_digit(peek())) sb.Append(advance());

            return new GreenLeafNode(AwslNodeKind.number, sb.Length, sb.ToString());
        }

        while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());

        if (!is_at_end() && peek() == '.' && char.IsDigit(peek_next()))
        {
            sb.Append(advance());

            while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());
        }

        if (!is_at_end() && (peek() == 'f' || peek() == 'F')) sb.Append(advance());

        var value = sb.ToString();
        return new GreenLeafNode(AwslNodeKind.number, value.Length, value);
    }

    private GreenLeafNode scan_string()
    {
        var quote = advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != quote)
            if (peek() == '\\')
            {
                advance();

                if (is_at_end()) break;

                var escaped = advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    '`' => '`',
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(advance());
            }

        if (is_at_end())
            _diagnostics?.report_error(
                default,
                "未闭合的字符串字面量");
        else
            advance();

        var value = sb.ToString();
        return new GreenLeafNode(AwslNodeKind.@string, value.Length, value);
    }

    #endregion

    #region 运算符与标点扫描

    private GreenLeafNode scan_punctuation()
    {
        var c = advance();

        if (c == ':' && peek() == ':')
        {
            advance();
            return new GreenLeafNode(AwslNodeKind.punctuation, 2, "::");
        }

        return new GreenLeafNode(AwslNodeKind.punctuation, 1, c.ToString());
    }

    private static bool is_operator_start(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&' or '|' or '^' or '~' or '?' => true,
            _ => false
        };
    }

    private GreenLeafNode scan_operator_or_delimiter()
    {
        if (peek() == '.')
        {
            advance();
            return new GreenLeafNode(AwslNodeKind.@operator, 1, ".");
        }

        var sb = new StringBuilder();
        sb.Append(advance());

        while (!is_at_end())
        {
            var candidate = sb.ToString() + peek();

            if (_operators.Contains(candidate))
                sb.Append(advance());
            else
                break;
        }

        var op = sb.ToString();

        if (op is ":" or "::" or ";" or ",") return new GreenLeafNode(AwslNodeKind.punctuation, op.Length, op);

        if (op is "/") return new GreenLeafNode(AwslNodeKind.@operator, op.Length, op);

        return new GreenLeafNode(AwslNodeKind.@operator, op.Length, op);
    }

    #endregion
}