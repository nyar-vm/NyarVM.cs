namespace Std.Data.Text.Lua;

/// <summary>
///     Lua 词法分析器。
/// </summary>
public sealed class LuaLexer
{
    private int _position;
    private string _source;


    /// <summary>
    ///     初始化 <see cref="LuaLexer" /> 的新实例。
    /// </summary>
    public LuaLexer()
    {
        _source = string.Empty;
        _position = 0;
        line = 1;
        column = 1;
    }


    /// <summary>
    ///     当前行号。
    /// </summary>
    public int line { get; private set; }


    /// <summary>
    ///     当前列号。
    /// </summary>
    public int column { get; private set; }


    /// <summary>
    ///     设置源文本。
    /// </summary>
    public void set_source(string source)
    {
        _source = source;
        _position = 0;
        line = 1;
        column = 1;
    }


    /// <summary>
    ///     读取下一个 Token。
    /// </summary>
    public (LuaTokenType Type, string Text, int Line, int Column) next_token()
    {
        skip_whitespace();

        if (_position >= _source.Length) return (LuaTokenType.end_of_file, string.Empty, line, column);

        var ch = _source[_position];
        var startLine = line;
        var startCol = column;

        if (ch == '-' && peek(1) == '-') return read_comment(startLine, startCol);

        if (ch is '"' or '\'') return read_short_string(startLine, startCol);

        if (ch == '[' && (peek(1) == '[' || peek(1) == '=')) return read_long_string(startLine, startCol);

        if (char.IsDigit(ch) || (ch == '.' && peek(1) != '.' && char.IsDigit(peek(1))))
            return read_number(startLine, startCol);

        if (is_name_start(ch)) return read_name(startLine, startCol);

        return read_symbol(startLine, startCol);
    }

    private void skip_whitespace()
    {
        while (_position < _source.Length)
        {
            var ch = _source[_position];

            if (ch is ' ' or '\t' or '\r')
                advance();
            else if (ch == '\n')
                advance();
            else
                break;
        }
    }

    private (LuaTokenType, string, int, int) read_comment(int line, int col)
    {
        advance();
        advance();

        if (_position < _source.Length && _source[_position] == '[')
        {
            var level = count_long_brackets();

            if (level >= 0) return read_long_comment_body(level, line, col);
        }

        var start = _position;

        while (_position < _source.Length && _source[_position] != '\n') advance();

        return (LuaTokenType.comment, _source[start.._position], line, col);
    }

    private (LuaTokenType, string, int, int) read_long_comment_body(int level, int line, int col)
    {
        var start = _position - 2;
        skip_long_string_body(level);

        return (LuaTokenType.long_comment, _source[start.._position], line, col);
    }

    private (LuaTokenType, string, int, int) read_short_string(int line, int col)
    {
        var quote = _source[_position];
        advance();
        var start = _position;

        while (_position < _source.Length)
        {
            var ch = _source[_position];

            if (ch == '\\')
            {
                advance();

                if (_position < _source.Length) advance();

                continue;
            }

            if (ch == quote) break;

            if (ch == '\n') break;

            advance();
        }

        var text = _source[start.._position];

        if (_position < _source.Length && _source[_position] == quote) advance();

        return (LuaTokenType.@string, text, line, col);
    }

    private (LuaTokenType, string, int, int) read_long_string(int line, int col)
    {
        var start = _position;
        var level = count_long_brackets();

        if (level < 0) return (LuaTokenType.left_bracket, "[", line, col);

        skip_long_string_body(level);

        return (LuaTokenType.long_string, _source[start.._position], line, col);
    }

    private int count_long_brackets()
    {
        if (_position >= _source.Length || _source[_position] != '[') return -1;

        var pos = _position + 1;
        var level = 0;

        while (pos < _source.Length && _source[pos] == '=')
        {
            level++;
            pos++;
        }

        if (pos < _source.Length && _source[pos] == '[')
        {
            _position = pos + 1;
            return level;
        }

        return -1;
    }

    private void skip_long_string_body(int level)
    {
        while (_position < _source.Length)
        {
            if (_source[_position] == ']')
            {
                var pos = _position + 1;
                var eqCount = 0;

                while (pos < _source.Length && _source[pos] == '=' && eqCount < level)
                {
                    eqCount++;
                    pos++;
                }

                if (eqCount == level && pos < _source.Length && _source[pos] == ']')
                {
                    _position = pos + 1;
                    return;
                }
            }

            if (_source[_position] == '\n')
            {
                line++;
                column = 1;
            }

            _position++;
            column++;
        }
    }

    private (LuaTokenType, string, int, int) read_number(int line, int col)
    {
        var start = _position;
        var isFloat = false;

        if (_source[_position] == '0' && _position + 1 < _source.Length)
        {
            var next = char.ToLower(_source[_position + 1]);

            if (next == 'x')
            {
                advance();
                advance();

                while (_position < _source.Length && is_hex_digit(_source[_position])) advance();

                return (LuaTokenType.integer, _source[start.._position], line, col);
            }

            if (next == 'b')
            {
                advance();
                advance();

                while (_position < _source.Length && (_source[_position] == '0' || _source[_position] == '1'))
                    advance();

                return (LuaTokenType.integer, _source[start.._position], line, col);
            }
        }

        while (_position < _source.Length && char.IsDigit(_source[_position])) advance();

        if (_position < _source.Length && _source[_position] == '.')
            if (_position + 1 < _source.Length && char.IsDigit(_source[_position + 1]))
            {
                isFloat = true;
                advance();

                while (_position < _source.Length && char.IsDigit(_source[_position])) advance();
            }

        if (_position < _source.Length &&
            (char.ToLower(_source[_position]) == 'e' || char.ToLower(_source[_position]) == 'p'))
        {
            isFloat = true;
            advance();

            if (_position < _source.Length && (_source[_position] == '+' || _source[_position] == '-')) advance();

            while (_position < _source.Length && char.IsDigit(_source[_position])) advance();
        }

        return (isFloat ? LuaTokenType.@float : LuaTokenType.integer, _source[start.._position], line, col);
    }

    private (LuaTokenType, string, int, int) read_name(int line, int col)
    {
        var start = _position;

        while (_position < _source.Length && is_name_char(_source[_position])) advance();

        var text = _source[start.._position];
        var type = classify_keyword(text);

        return (type, text, line, col);
    }

    private (LuaTokenType, string, int, int) read_symbol(int line, int col)
    {
        var ch = _source[_position];

        var type = ch switch
        {
            '+' => LuaTokenType.plus,
            '*' => LuaTokenType.star,
            '/' => LuaTokenType.slash,
            '%' => LuaTokenType.percent,
            '^' => LuaTokenType.caret,
            '#' => LuaTokenType.hash,
            '(' => LuaTokenType.left_paren,
            ')' => LuaTokenType.right_paren,
            '{' => LuaTokenType.left_brace,
            '}' => LuaTokenType.right_brace,
            ']' => LuaTokenType.right_bracket,
            ';' => LuaTokenType.semicolon,
            ',' => LuaTokenType.comma,
            _ => LuaTokenType.end_of_file
        };

        if (ch == '=' && peek(1) == '=')
        {
            advance();
            advance();
            return (LuaTokenType.equal_equal, "==", line, col);
        }

        if (ch == '~' && peek(1) == '=')
        {
            advance();
            advance();
            return (LuaTokenType.tilde_equal, "~=", line, col);
        }

        if (ch == '<' && peek(1) == '=')
        {
            advance();
            advance();
            return (LuaTokenType.less_equal, "<=", line, col);
        }

        if (ch == '>' && peek(1) == '=')
        {
            advance();
            advance();
            return (LuaTokenType.greater_equal, ">=", line, col);
        }

        if (ch == '<')
        {
            advance();
            return (LuaTokenType.less, "<", line, col);
        }

        if (ch == '>')
        {
            advance();
            return (LuaTokenType.greater, ">", line, col);
        }

        if (ch == '=')
        {
            advance();
            return (LuaTokenType.equal, "=", line, col);
        }

        if (ch == '-')
        {
            advance();
            return (LuaTokenType.minus, "-", line, col);
        }

        if (ch == '.' && peek(1) == '.' && peek(2) == '.')
        {
            advance();
            advance();
            advance();
            return (LuaTokenType.dots, "...", line, col);
        }

        if (ch == '.' && peek(1) == '.')
        {
            advance();
            advance();
            return (LuaTokenType.concat, "..", line, col);
        }

        if (ch == '.')
        {
            advance();
            return (LuaTokenType.dot, ".", line, col);
        }

        if (ch == ':' && peek(1) == ':')
        {
            advance();
            advance();
            return (LuaTokenType.double_colon, "::", line, col);
        }

        if (ch == ':')
        {
            advance();
            return (LuaTokenType.colon, ":", line, col);
        }

        if (ch == '[')
        {
            advance();
            return (LuaTokenType.left_bracket, "[", line, col);
        }

        advance();

        return (type, ch.ToString(), line, col);
    }

    private static LuaTokenType classify_keyword(string text)
    {
        return text switch
        {
            "and" => LuaTokenType.and,
            "break" => LuaTokenType.@break,
            "do" => LuaTokenType.@do,
            "else" => LuaTokenType.@else,
            "elseif" => LuaTokenType.else_if,
            "end" => LuaTokenType.end,
            "false" => LuaTokenType.@false,
            "for" => LuaTokenType.@for,
            "function" => LuaTokenType.function,
            "goto" => LuaTokenType.@goto,
            "if" => LuaTokenType.@if,
            "in" => LuaTokenType.@in,
            "local" => LuaTokenType.local,
            "nil" => LuaTokenType.nil,
            "not" => LuaTokenType.not,
            "or" => LuaTokenType.or,
            "repeat" => LuaTokenType.repeat,
            "return" => LuaTokenType.@return,
            "then" => LuaTokenType.then,
            "true" => LuaTokenType.@true,
            "until" => LuaTokenType.until,
            "while" => LuaTokenType.@while,
            _ => LuaTokenType.name
        };
    }

    private char peek(int offset)
    {
        var pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private void advance()
    {
        if (_position < _source.Length)
        {
            if (_source[_position] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }

            _position++;
        }
    }

    private static bool is_name_start(char ch)
    {
        return char.IsLetter(ch) || ch == '_';
    }

    private static bool is_name_char(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static bool is_hex_digit(char ch)
    {
        return char.IsDigit(ch) || ch is >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }
}