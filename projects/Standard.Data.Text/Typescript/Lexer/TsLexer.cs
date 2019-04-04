using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.Lexer;


/// <summary>
///     TypeScript 词法分析器
/// </summary>
public sealed class TsLexer
{
    private static readonly HashSet<string> _operators =
    [
        new(StringComparer.Ordinal),
        "+", "-", "*", "/", "%", "**",
        "=", "==", "===", "!=", "!==", "<", ">", "<=", ">=",
        "+=", "-=", "*=", "/=", "%=", "**=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>", ">>>",
        "&=", "|=", "^=", "<<=", ">>=", ">>>=",
        "=>", "->", "::", "??", "??=",
        "?.", "?", ":"
    ];

    private static readonly HashSet<char> _delimiters = ['(', ')', '{', '}', '[', ']', ',', ';', '.'];

    private DiagnosticSink? _diagnostics;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private ISource _source = StringSource.empty;

    
/// <summary>
///     创建 TypeScript 词法分析器
/// </summary>
    public TsLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    
/// <summary>
///     将源代码文本转换为词法单元序列
/// </summary>
    public IReadOnlyList<TsToken> tokenize(string source)
    {
        _source = new StringSource(source);
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics ??= new DiagnosticSink();

        skip_hashbang();

        var tokens = new List<TsToken>();

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

        tokens.Add(new TsToken(TsTokenType.eof, string.Empty, _line, _column));
        return tokens;
    }

    private bool is_at_end() => _position >= _source.length;

    private char peek() => is_at_end() ? '\0' : _source[_position];

    private char peek_next() => peek(1);

    private char peek(int offset)
    {
        var index = _position + offset;
        return index >= _source.length ? '\0' : _source[index];
    }

    private char advance()
    {
        var c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private TsToken? scan_token()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = peek();

        if (c is '"' or '\'')
        {
            return scan_string(startLine, startColumn);
        }

        if (c == '`')
        {
            return scan_template_string(startLine, startColumn);
        }

        if (char.IsDigit(c))
        {
            return scan_number(startLine, startColumn);
        }

        if (c == '_' || c == '$' || char.IsLetter(c))
        {
            return scan_identifier_or_keyword(startLine, startColumn);
        }

        if (is_operator_start(c))
        {
            return scan_operator_or_punctuation(startLine, startColumn);
        }

        if (_delimiters.Contains(c))
        {
            advance();
            return new TsToken(TsTokenType.delimiter, c.ToString(), startLine, startColumn);
        }

        advance();
        _diagnostics?.report_error(
            string.Empty,
            default,
            "OAK1001",
            $"意外的字符 '{c}'");

        return null;
    }

    private TsToken scan_string(int startLine, int startColumn)
    {
        var quote = advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != quote)
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
                    '\\' => '\\', '"' => '"', '\'' => '\'',
                    '0' => '\0', _ => escaped
                });
            }
            else
            {
                sb.Append(advance());
            }

        if (is_at_end())
        {
            _diagnostics?.report_error(string.Empty,
                default,
                "OAK1002", "未闭合的字符串字面量");
        }
        else
        {
            advance();
        }

        return new TsToken(TsTokenType.@string, sb.ToString(), startLine, startColumn);
    }

    private TsToken scan_template_string(int startLine, int startColumn)
    {
        advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '`')
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
                    '\\' => '\\', '`' => '`', '$' => '$',
                    '0' => '\0', _ => escaped
                });
            }
            else
            {
                sb.Append(advance());
            }

        if (is_at_end())
        {
            _diagnostics?.report_error(string.Empty,
                default,
                "OAK1002", "未闭合的模板字符串");
        }
        else
        {
            advance();
        }

        return new TsToken(TsTokenType.template_string, sb.ToString(), startLine, startColumn);
    }

    
/// <summary>
///     扫描数字字面量，支持十进制、十六进制、二进制、八进制及 BigInt
/// </summary>
    private TsToken scan_number(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (peek() == '0')
        {
            var next = peek_next();

            if (next is 'x' or 'X')
            {
                advance();
                advance();
                sb.Append("0x");
                while (!is_at_end() && is_hex_digit(peek())) sb.Append(advance());

                if (!is_at_end() && peek() == 'n')
                {
                    sb.Append(advance());
                    return new TsToken(TsTokenType.big_int, sb.ToString(), startLine, startColumn);
                }

                return new TsToken(TsTokenType.number, sb.ToString(), startLine, startColumn);
            }

            if (next is 'b' or 'B')
            {
                advance();
                advance();
                sb.Append("0b");
                while (!is_at_end() && is_binary_digit(peek())) sb.Append(advance());

                if (!is_at_end() && peek() == 'n')
                {
                    sb.Append(advance());
                    return new TsToken(TsTokenType.big_int, sb.ToString(), startLine, startColumn);
                }

                return new TsToken(TsTokenType.number, sb.ToString(), startLine, startColumn);
            }

            if (next is 'o' or 'O')
            {
                advance();
                advance();
                sb.Append("0o");
                while (!is_at_end() && is_octal_digit(peek())) sb.Append(advance());

                if (!is_at_end() && peek() == 'n')
                {
                    sb.Append(advance());
                    return new TsToken(TsTokenType.big_int, sb.ToString(), startLine, startColumn);
                }

                return new TsToken(TsTokenType.number, sb.ToString(), startLine, startColumn);
            }
        }

        while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());

        if (!is_at_end() && peek() == '.' && char.IsDigit(peek_next()))
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

        if (!is_at_end() && (peek() == 'f' || peek() == 'F' || peek() == 'i' || peek() == 'I'
                           || peek() == 'u' || peek() == 'U'))
        {
            sb.Append(advance());
            return new TsToken(TsTokenType.number, sb.ToString(), startLine, startColumn);
        }

        if (!is_at_end() && peek() == 'n')
        {
            sb.Append(advance());
            return new TsToken(TsTokenType.big_int, sb.ToString(), startLine, startColumn);
        }

        return new TsToken(TsTokenType.number, sb.ToString(), startLine, startColumn);
    }

    
/// <summary>
///     判断字符是否为十六进制数字
/// </summary>
    private static bool is_hex_digit(char c) =>
        char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';

    
/// <summary>
///     判断字符是否为二进制数字
/// </summary>
    private static bool is_binary_digit(char c) => c is '0' or '1';

    
/// <summary>
///     判断字符是否为八进制数字
/// </summary>
    private static bool is_octal_digit(char c) => c is >= '0' and <= '7';

    private TsToken scan_identifier_or_keyword(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && (peek() == '_' || peek() == '$' || char.IsLetterOrDigit(peek()))) sb.Append(advance());
        var text = sb.ToString();

        if (TsKeywords.all.Contains(text))
        {
            if (text is "true" or "false" or "null")
            {
                return new TsToken(TsTokenType.literal, text, startLine, startColumn);
            }

            return new TsToken(TsTokenType.keyword, text, startLine, startColumn);
        }

        return new TsToken(TsTokenType.identifier, text, startLine, startColumn);
    }

    private static bool is_operator_start(char c) =>
        c is '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&' or '|' or '^' or '~' or '?' or ':';

    private TsToken scan_operator_or_punctuation(int startLine, int startColumn)
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

        var op = sb.ToString();
        if (op is ":")
        {
            return new TsToken(TsTokenType.punctuation, op, startLine, startColumn);
        }

        return new TsToken(TsTokenType.@operator, op, startLine, startColumn);
    }

    private void skip_whitespace_and_comments()
    {
        while (!is_at_end())
        {
            switch (peek())
            {
                case ' ' or '\t' or '\r' or '\n':
                    advance();
                    break;
                case '/':
                    if (peek_next() == '/')
                    {
                        skip_line_comment();
                    }
                    else if (peek_next() == '*')
                    {
                        skip_block_comment();
                    }
                    else
                    {
                        return;
                    }

                    break;
                default:
                    return;
            }
        }
    }

    private void skip_line_comment()
    {
        while (!is_at_end() && peek() != '\n') advance();
    }

    private void skip_block_comment()
    {
        advance();
        advance();
        var startLine = _line;
        var startColumn = _column;
        var depth = 1;

        while (!is_at_end() && depth > 0)
        {
            if (peek() == '/' && peek_next() == '*') { advance(); advance(); depth++; }
            else if (peek() == '*' && peek_next() == '/') { advance(); advance(); depth--; }
            else
            {
                advance();
            }
        }

        if (depth > 0)
        {
            _diagnostics?.report_error(string.Empty,
                default,
                "OAK1003", "未闭合的块注释");
        }
    }

    
/// <summary>
///     跳过 Hashbang 注释（以 #! 开头的行）
/// </summary>
    private void skip_hashbang()
    {
        if (_source.length >= 2 && _source[0] == '#' && _source[1] == '!')
        {
            while (!is_at_end() && peek() != '\n') advance();
        }
    }
}