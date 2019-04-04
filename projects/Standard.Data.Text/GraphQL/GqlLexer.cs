using System.Text;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.GraphQL;

public sealed class GqlLexer
{
    private static readonly Dictionary<string, GqlTokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["query"] = GqlTokenType.query,
        ["mutation"] = GqlTokenType.mutation,
        ["subscription"] = GqlTokenType.subscription,
        ["fragment"] = GqlTokenType.fragment,
        ["on"] = GqlTokenType.on,
        ["type"] = GqlTokenType.type,
        ["input"] = GqlTokenType.input,
        ["interface"] = GqlTokenType.@interface,
        ["enum"] = GqlTokenType.@enum,
        ["union"] = GqlTokenType.union,
        ["scalar"] = GqlTokenType.scalar,
        ["schema"] = GqlTokenType.schema,
        ["extend"] = GqlTokenType.extend,
        ["directive"] = GqlTokenType.directive,
        ["repeatable"] = GqlTokenType.repeatable,
        ["implements"] = GqlTokenType.implements,
        ["null"] = GqlTokenType.@null,
        ["true"] = GqlTokenType.@true,
        ["false"] = GqlTokenType.@false
    };

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<GqlToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<GqlToken>();

        while (!is_at_end())
        {
            skip_ignored();

            if (is_at_end()) break;

            var token = scan_token();
            if (token.type != GqlTokenType.invalid) tokens.Add(token);
        }

        tokens.Add(new GqlToken(GqlTokenType.end_of_file, string.Empty, _line, _column));
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

    private void skip_ignored()
    {
        while (!is_at_end())
        {
            var c = peek();

            if (char.IsWhiteSpace(c) || c == ',' || c == '\n' || c == '\r' || c == '\t')
                advance();
            else if (c == '#')
                while (!is_at_end() && peek() != '\n')
                    advance();
            else
                break;
        }
    }

    private GqlToken scan_block_string(int line, int column)
    {
        advance();
        advance();
        advance();

        var sb = new StringBuilder();

        while (!is_at_end())
        {
            if (peek() == '"' && peek_next() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
            {
                advance();
                advance();
                advance();
                return new GqlToken(GqlTokenType.string_value, sb.ToString(), line, column);
            }

            if (peek() == '\\' && peek_next() == '"' && _position + 2 < _source.Length &&
                _source[_position + 3] < _source.Length)
                if (_source[_position + 2] == '"' && _source[_position + 3] == '"')
                {
                    advance();
                    advance();
                    advance();
                    advance();
                    sb.Append("\"\"\"");
                    continue;
                }

            sb.Append(advance());
        }

        return new GqlToken(GqlTokenType.string_value, sb.ToString(), line, column);
    }

    private GqlToken scan_token()
    {
        var line = _line;
        var column = _column;
        var c = peek();

        switch (c)
        {
            case '{':
                advance();
                return new GqlToken(GqlTokenType.left_brace, "{", line, column);
            case '}':
                advance();
                return new GqlToken(GqlTokenType.right_brace, "}", line, column);
            case '(':
                advance();
                return new GqlToken(GqlTokenType.left_paren, "(", line, column);
            case ')':
                advance();
                return new GqlToken(GqlTokenType.right_paren, ")", line, column);
            case '[':
                advance();
                return new GqlToken(GqlTokenType.left_bracket, "[", line, column);
            case ']':
                advance();
                return new GqlToken(GqlTokenType.right_bracket, "]", line, column);
            case '!':
                advance();
                return new GqlToken(GqlTokenType.exclamation, "!", line, column);
            case '$':
                advance();
                return new GqlToken(GqlTokenType.dollar, "$", line, column);
            case '@':
                advance();
                return new GqlToken(GqlTokenType.at, "@", line, column);
            case '&':
                advance();
                return new GqlToken(GqlTokenType.ampersand, "&", line, column);
            case '|':
                advance();
                return new GqlToken(GqlTokenType.pipe, "|", line, column);
            case '=':
                advance();
                return new GqlToken(GqlTokenType.equals, "=", line, column);
            case ':':
                advance();
                return new GqlToken(GqlTokenType.colon, ":", line, column);
            case '.':
                if (peek_next() == '.' && _position + 2 < _source.Length && _source[_position + 2] == '.')
                {
                    advance();
                    advance();
                    advance();
                    return new GqlToken(GqlTokenType.spread, "...", line, column);
                }

                _diagnostics?.report_error(string.Empty, default,
                    1, "意外的字符 '.'");
                advance();
                return new GqlToken(GqlTokenType.invalid, ".", line, column);
            case '"':
                if (peek_next() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
                    return scan_block_string(line, column);

                return scan_string(line, column);
            default:
            {
                if (c == '-' || char.IsDigit(c)) return scan_number(line, column);

                if (c == '_' || char.IsLetter(c)) return scan_name(line, column);

                _diagnostics?.report_error(string.Empty, default,
                    2, $"意外的字符 '{c}'");
                advance();
                return new GqlToken(GqlTokenType.invalid, c.ToString(), line, column);
            }
        }
    }

    private GqlToken scan_string(int line, int column)
    {
        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '"')
            if (peek() == '\\')
            {
                advance();
                if (is_at_end()) break;

                var escaped = advance();
                sb.Append(escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    '/' => '/',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(advance());
            }

        if (!is_at_end()) advance();

        return new GqlToken(GqlTokenType.string_value, sb.ToString(), line, column);
    }

    private GqlToken scan_number(int line, int column)
    {
        var start = _position;

        if (peek() == '-') advance();

        while (!is_at_end() && char.IsDigit(peek())) advance();

        var isFloat = false;

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

        var text = _source[start.._position];
        return new GqlToken(isFloat ? GqlTokenType.float_value : GqlTokenType.int_value, text, line, column);
    }

    private GqlToken scan_name(int line, int column)
    {
        var sb = new StringBuilder();

        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '_')) sb.Append(advance());

        var text = sb.ToString();

        if (_keywords.TryGetValue(text, out var keywordType)) return new GqlToken(keywordType, text, line, column);

        return new GqlToken(GqlTokenType.name, text, line, column);
    }
}
