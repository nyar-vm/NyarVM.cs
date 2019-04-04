using System.Text;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Protobuf;

public sealed class ProtoLexer
{
    private static readonly Dictionary<string, ProtoTokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["syntax"] = ProtoTokenType.syntax,
        ["package"] = ProtoTokenType.package,
        ["import"] = ProtoTokenType.import,
        ["option"] = ProtoTokenType.option,
        ["message"] = ProtoTokenType.message,
        ["enum"] = ProtoTokenType.@enum,
        ["service"] = ProtoTokenType.service,
        ["rpc"] = ProtoTokenType.rpc,
        ["returns"] = ProtoTokenType.returns,
        ["stream"] = ProtoTokenType.stream,
        ["repeated"] = ProtoTokenType.repeated,
        ["optional"] = ProtoTokenType.optional,
        ["required"] = ProtoTokenType.required,
        ["oneof"] = ProtoTokenType.oneof,
        ["map"] = ProtoTokenType.map,
        ["reserved"] = ProtoTokenType.reserved,
        ["extensions"] = ProtoTokenType.extensions,
        ["extend"] = ProtoTokenType.extend,
        ["group"] = ProtoTokenType.group,
        ["true"] = ProtoTokenType.bool_literal,
        ["false"] = ProtoTokenType.bool_literal
    };

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<ProtoToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<ProtoToken>();

        while (!is_at_end())
        {
            skip_whitespace_and_comments();

            if (is_at_end()) break;

            var token = scan_token();
            if (token.type != ProtoTokenType.invalid) tokens.Add(token);
        }

        tokens.Add(new ProtoToken(ProtoTokenType.end_of_file, string.Empty, _line, _column));
        return tokens;
    }

    private bool is_at_end()
    {
        return _position >= _source.Length;
    }

    private char peek()
    {
        return is_at_end() ? '\0' : _source[_position];
    }

    private char peek_next()
    {
        return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
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

    private void skip_whitespace_and_comments()
    {
        while (!is_at_end())
        {
            var c = peek();

            if (char.IsWhiteSpace(c))
            {
                advance();
            }
            else if (c == '/' && peek_next() == '/')
            {
                while (!is_at_end() && peek() != '\n') advance();
            }
            else if (c == '/' && peek_next() == '*')
            {
                advance();
                advance();
                while (!is_at_end())
                {
                    if (peek() == '*' && peek_next() == '/')
                    {
                        advance();
                        advance();
                        break;
                    }

                    advance();
                }
            }
            else
            {
                break;
            }
        }
    }

    private ProtoToken scan_token()
    {
        var line = _line;
        var column = _column;
        var c = peek();

        switch (c)
        {
            case '{':
                advance();
                return new ProtoToken(ProtoTokenType.left_brace, "{", line, column);
            case '}':
                advance();
                return new ProtoToken(ProtoTokenType.right_brace, "}", line, column);
            case '[':
                advance();
                return new ProtoToken(ProtoTokenType.left_bracket, "[", line, column);
            case ']':
                advance();
                return new ProtoToken(ProtoTokenType.right_bracket, "]", line, column);
            case '(':
                advance();
                return new ProtoToken(ProtoTokenType.left_paren, "(", line, column);
            case ')':
                advance();
                return new ProtoToken(ProtoTokenType.right_paren, ")", line, column);
            case ';':
                advance();
                return new ProtoToken(ProtoTokenType.semicolon, ";", line, column);
            case ':':
                advance();
                return new ProtoToken(ProtoTokenType.colon, ":", line, column);
            case ',':
                advance();
                return new ProtoToken(ProtoTokenType.comma, ",", line, column);
            case '.':
                advance();
                return new ProtoToken(ProtoTokenType.dot, ".", line, column);
            case '=':
                advance();
                return new ProtoToken(ProtoTokenType.equals, "=", line, column);
            case '<':
                advance();
                return new ProtoToken(ProtoTokenType.lt, "<", line, column);
            case '>':
                advance();
                return new ProtoToken(ProtoTokenType.gt, ">", line, column);
            case '"':
                return scan_string(line, column);
            case '\'':
                return scan_single_quote_string(line, column);
            default:
            {
                if (c == '-' || c == '+' || (c == '.' && char.IsDigit(peek_next())) || char.IsDigit(c))
                    return scan_number(line, column);

                if (c == '_' || char.IsLetter(c)) return scan_identifier(line, column);

                _diagnostics?.report_error(string.Empty, default,
                    1, $"意外的字符 '{c}'");
                advance();
                return new ProtoToken(ProtoTokenType.invalid, c.ToString(), line, column);
            }
        }
    }

    private ProtoToken scan_string(int line, int column)
    {
        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '"')
            if (peek() == '\\')
            {
                advance();
                if (!is_at_end()) sb.Append(advance());
            }
            else
            {
                sb.Append(advance());
            }

        if (!is_at_end()) advance();

        return new ProtoToken(ProtoTokenType.string_literal, sb.ToString(), line, column);
    }

    private ProtoToken scan_single_quote_string(int line, int column)
    {
        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '\'')
            if (peek() == '\\')
            {
                advance();
                if (!is_at_end()) sb.Append(advance());
            }
            else
            {
                sb.Append(advance());
            }

        if (!is_at_end()) advance();

        return new ProtoToken(ProtoTokenType.string_literal, sb.ToString(), line, column);
    }

    private ProtoToken scan_number(int line, int column)
    {
        var start = _position;
        var isFloat = false;

        if (peek() == '-' || peek() == '+') advance();

        if (peek() == '0' && (peek_next() == 'x' || peek_next() == 'X'))
        {
            advance();
            advance();
            while (!is_at_end() && is_hex_digit(peek())) advance();
        }
        else
        {
            while (!is_at_end() && char.IsDigit(peek())) advance();

            if (!is_at_end() && peek() == '.')
            {
                isFloat = true;
                advance();
                while (!is_at_end() && char.IsDigit(peek())) advance();
            }

            if (!is_at_end() && (peek() == 'e' || peek() == 'E'))
            {
                isFloat = true;
                advance();
                if (!is_at_end() && (peek() == '+' || peek() == '-')) advance();

                while (!is_at_end() && char.IsDigit(peek())) advance();
            }
        }

        var text = _source[start.._position];
        return new ProtoToken(isFloat ? ProtoTokenType.float_literal : ProtoTokenType.int_literal, text, line, column);
    }

    private ProtoToken scan_identifier(int line, int column)
    {
        var sb = new StringBuilder();

        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '_')) sb.Append(advance());

        var text = sb.ToString();

        if (_keywords.TryGetValue(text, out var keywordType)) return new ProtoToken(keywordType, text, line, column);

        return new ProtoToken(ProtoTokenType.identifier, text, line, column);
    }

    private static bool is_hex_digit(char c)
    {
        return char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    }
}
