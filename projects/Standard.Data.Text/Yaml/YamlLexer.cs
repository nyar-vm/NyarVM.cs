using System.Globalization;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 词法分析器
/// </summary>
public sealed class YamlLexer
{
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;


    /// <summary>
    ///     执行词法分析
    /// </summary>
    /// <param name="source">YAML 源文本。</param>
    /// <param name="diagnostics">诊断收集器。</param>
    /// <returns>词法单元列表。</returns>
    public IReadOnlyList<YamlToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<YamlToken>();

        while (!is_at_end())
        {
            var token = scan_token();
            if (token.type != YamlTokenType.invalid) tokens.Add(token);
        }

        tokens.Add(new YamlToken(YamlTokenType.end_of_file, string.Empty, _line, _column));
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

    private YamlToken scan_token()
    {
        if (peek() == '\n')
        {
            advance();
            return scan_line_start();
        }

        if (peek() == '\r')
        {
            advance();
            if (peek() == '\n') advance();

            return scan_line_start();
        }

        if (char.IsWhiteSpace(peek())) return scan_indentation();

        if (peek() == '#') return scan_comment();

        if (peek() == '-' && peek_next() == '-' && peek_at(2) == '-') return scan_document_start();

        if (peek() == '.' && peek_next() == '.' && peek_at(2) == '.') return scan_document_end();

        if (peek() == '-' && _column == 1) return scan_dash();

        if (peek() == '{')
        {
            var line = _line;
            var column = _column;
            advance();
            return new YamlToken(YamlTokenType.flow_map_start, "{", line, column);
        }

        if (peek() == '}')
        {
            var line = _line;
            var column = _column;
            advance();
            return new YamlToken(YamlTokenType.flow_map_end, "}", line, column);
        }

        if (peek() == '[')
        {
            var line = _line;
            var column = _column;
            advance();
            return new YamlToken(YamlTokenType.flow_seq_start, "[", line, column);
        }

        if (peek() == ']')
        {
            var line = _line;
            var column = _column;
            advance();
            return new YamlToken(YamlTokenType.flow_seq_end, "]", line, column);
        }

        if (peek() == ',')
        {
            var line = _line;
            var column = _column;
            advance();
            return new YamlToken(YamlTokenType.flow_comma, ",", line, column);
        }

        if (peek() == '!' && _column <= 2) return scan_tag();

        if (peek() == '&') return scan_anchor();

        if (peek() == '*') return scan_alias();

        return scan_value();
    }

    private char peek_at(int offset)
    {
        var index = _position + offset;
        return index >= _source.Length ? '\0' : _source[index];
    }

    private YamlToken scan_line_start()
    {
        var indent = 0;

        while (!is_at_end() && (peek() == ' ' || peek() == '\t'))
        {
            indent++;
            advance();
        }

        if (is_at_end() || peek() == '\n' || peek() == '\r')
            return new YamlToken(YamlTokenType.newline, string.Empty, _line, 1, indent);

        if (peek() == '#') return scan_comment();

        if (peek() == '-' && peek_next() == '-' && peek_at(2) == '-') return scan_document_start();

        if (peek() == '.' && peek_next() == '.' && peek_at(2) == '.') return scan_document_end();

        if (peek() == '-' && (peek_next() == ' ' || peek_next() == '\t'))
        {
            var line = _line;
            var column = _column;
            advance();
            return new YamlToken(YamlTokenType.dash, "-", line, column, indent);
        }

        return scan_key_or_value(indent);
    }

    private YamlToken scan_indentation()
    {
        var line = _line;
        var column = _column;
        var indent = 0;

        while (!is_at_end() && (peek() == ' ' || peek() == '\t'))
        {
            indent++;
            advance();
        }

        return new YamlToken(YamlTokenType.indent, string.Empty, line, column, indent);
    }

    private YamlToken scan_comment()
    {
        var line = _line;
        var column = _column;
        advance();

        var start = _position;
        while (!is_at_end() && peek() != '\n' && peek() != '\r') advance();

        var text = _source[start.._position];
        return new YamlToken(YamlTokenType.comment, text, line, column);
    }

    private YamlToken scan_document_start()
    {
        var line = _line;
        var column = _column;
        advance();
        advance();
        advance();
        return new YamlToken(YamlTokenType.document_start, "---", line, column);
    }

    private YamlToken scan_document_end()
    {
        var line = _line;
        var column = _column;
        advance();
        advance();
        advance();
        return new YamlToken(YamlTokenType.document_end, "...", line, column);
    }

    private YamlToken scan_dash()
    {
        var line = _line;
        var column = _column;

        if (peek_next() == ' ' || peek_next() == '\t')
        {
            advance();
            return new YamlToken(YamlTokenType.dash, "-", line, column);
        }

        return scan_value();
    }

    private YamlToken scan_tag()
    {
        var line = _line;
        var column = _column;
        advance();

        var start = _position;
        while (!is_at_end() && !char.IsWhiteSpace(peek()) && peek() != ':' && peek() != ',') advance();

        var text = _source[(start - 1).._position];
        return new YamlToken(YamlTokenType.tag, text, line, column);
    }

    private YamlToken scan_anchor()
    {
        var line = _line;
        var column = _column;
        advance();

        var start = _position;
        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '_' || peek() == '-')) advance();

        var text = _source[(start - 1).._position];
        return new YamlToken(YamlTokenType.anchor, text, line, column);
    }

    private YamlToken scan_alias()
    {
        var line = _line;
        var column = _column;
        advance();

        var start = _position;
        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '_' || peek() == '-')) advance();

        var text = _source[(start - 1).._position];
        return new YamlToken(YamlTokenType.alias, text, line, column);
    }

    private YamlToken scan_key_or_value(int indent)
    {
        var line = _line;
        var column = _column;
        var start = _position;

        while (!is_at_end() && peek() != '\n' && peek() != '\r' && peek() != ':' && peek() != '#') advance();

        if (peek() == ':')
        {
            var text = _source[start.._position].TrimEnd();
            advance();
            return new YamlToken(YamlTokenType.key, text, line, column, indent);
        }

        return classify_value(_source[start.._position].TrimEnd(), line, column, indent);
    }

    private YamlToken scan_value()
    {
        var line = _line;
        var column = _column;
        var start = _position;

        while (!is_at_end() && peek() != '\n' && peek() != '\r' && peek() != '#' && peek() != ',' && peek() != '}' &&
               peek() != ']')
        {
            if (peek() == ':' && (peek_next() == ' ' || peek_next() == '\t' || peek_next() == '\n' ||
                                  peek_next() == '\r' || _position + 1 >= _source.Length))
            {
                var keyText = _source[start.._position].TrimEnd();
                advance();
                return new YamlToken(YamlTokenType.key, keyText, line, column);
            }

            advance();
        }

        var text = _source[start.._position].TrimEnd();
        return classify_value(text, line, column);
    }

    private YamlToken classify_value(string text, int line, int column, int indent = 0)
    {
        if (string.IsNullOrEmpty(text)) return new YamlToken(YamlTokenType.@null, text, line, column, indent);

        if (text is "null" or "~" or "Null" or "NULL")
            return new YamlToken(YamlTokenType.@null, text, line, column, indent);

        if (text is "true" or "True" or "TRUE" or "false" or "False" or "FALSE")
            return new YamlToken(YamlTokenType.boolean, text, line, column, indent);

        if (text is "yes" or "Yes" or "YES" or "no" or "No" or "NO" or "on" or "On" or "ON" or "off" or "Off" or "OFF")
            return new YamlToken(YamlTokenType.boolean, text, line, column, indent);

        if (double.TryParse(text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out _))
            return new YamlToken(YamlTokenType.number, text, line, column, indent);

        if (text.StartsWith('"') || text.StartsWith('\''))
            return new YamlToken(YamlTokenType.@string, text, line, column, indent);

        return new YamlToken(YamlTokenType.@string, text, line, column, indent);
    }
}