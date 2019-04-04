using System.Globalization;
using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Toml.Ast;

namespace Std.Data.Text.Toml;

public sealed class TomlParser
{
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public TomlParseResult parse(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var root = new TomlTable();
        var currentTable = root;
        var tables = new Dictionary<string, TomlTable> { [""] = root };

        while (!is_at_end())
        {
            skip_whitespace_and_newlines();

            if (is_at_end()) break;

            if (peek() == '#')
            {
                skip_comment();
                continue;
            }

            if (peek() == '\n' || peek() == '\r')
            {
                skip_newline();
                continue;
            }

            if (peek() == '[')
            {
                currentTable = parse_table_header(root, tables);
                continue;
            }

            parse_key_value(currentTable);
        }

        return new TomlParseResult { root = root, diagnostics = _diagnostics?.messages ?? [] };
    }

    #region 表头解析

    private TomlTable parse_table_header(TomlTable root, Dictionary<string, TomlTable> tables)
    {
        var isArrayTable = peek() == '[' && peek_next() == '[';

        if (isArrayTable)
        {
            advance();
            advance();
        }
        else
        {
            advance();
        }

        skip_whitespace();

        var key = parse_key();

        skip_whitespace();

        if (isArrayTable)
        {
            if (peek() == ']' && peek_next() == ']')
            {
                advance();
                advance();
            }
            else
            {
                _diagnostics?.report_error(
                    string.Empty,
                    default,
                    "TOML002",
                    "期望 ']]'");
            }
        }
        else
        {
            if (peek() == ']')
                advance();
            else
                _diagnostics?.report_error(
                    string.Empty,
                    default,
                    "TOML003",
                    "期望 ']'");
        }

        expect_newline_or_end();

        var parts = key.Split('.');
        var current = root;

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();

            if (i == parts.Length - 1)
            {
                var table = new TomlTable { name = key, is_array_table = isArrayTable };

                if (current.tables.TryAdd(part, table))
                {
                }
                else if (isArrayTable)
                {
                    current.tables[part] = table;
                }

                tables[key] = table;
                current = table;
            }
            else
            {
                if (!current.tables.TryGetValue(part, out var child))
                {
                    child = new TomlTable { name = string.Join('.', parts[..(i + 1)]) };
                    current.tables[part] = child;
                }

                current = child;
            }
        }

        return current;
    }

    #endregion

    #region 数组解析

    private TomlValue parse_array()
    {
        advance();
        skip_whitespace_and_newlines();

        var items = new List<TomlValue>();

        while (!is_at_end() && peek() != ']')
        {
            skip_whitespace_and_newlines();

            if (peek() == '#')
            {
                skip_comment();
                skip_whitespace_and_newlines();
                continue;
            }

            if (peek() == ']') break;

            items.Add(parse_value());

            skip_whitespace_and_newlines();

            if (peek() == '#')
            {
                skip_comment();
                skip_whitespace_and_newlines();
            }

            if (peek() == ',')
            {
                advance();
                skip_whitespace_and_newlines();
                continue;
            }

            if (peek() == '#')
            {
                skip_comment();
                skip_whitespace_and_newlines();
            }
        }

        if (!is_at_end()) advance();

        return new TomlValue { type = TomlValueType.array, raw_value = items.ToArray() };
    }

    #endregion

    #region 内联表解析

    private TomlValue parse_inline_table()
    {
        advance();
        skip_whitespace();

        var entries = new Dictionary<string, TomlValue>();

        while (!is_at_end() && peek() != '}')
        {
            skip_whitespace();

            if (peek() == '}') break;

            var key = parse_key();

            skip_whitespace();

            if (peek() != '=')
            {
                _diagnostics?.report_error(
                    string.Empty,
                    default,
                    "TOML006",
                    "内联表中期望 '='");
                break;
            }

            advance();
            skip_whitespace();

            var value = parse_value();
            entries[key] = value;

            skip_whitespace();

            if (peek() == ',')
            {
                advance();
                skip_whitespace();
            }
        }

        if (!is_at_end()) advance();

        return new TomlValue { type = TomlValueType.inline_table, raw_value = entries };
    }

    #endregion

    #region 基础工具

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

    private void skip_whitespace()
    {
        while (!is_at_end() && peek() is ' ' or '\t') advance();
    }

    private void skip_whitespace_and_newlines()
    {
        while (!is_at_end() && peek() is ' ' or '\t' or '\n' or '\r') advance();
    }

    private void skip_newline()
    {
        if (peek() == '\r') advance();

        if (peek() == '\n') advance();
    }

    private void skip_comment()
    {
        while (!is_at_end() && peek() != '\n') advance();
    }

    private void expect_newline_or_end()
    {
        skip_whitespace();

        if (is_at_end()) return;

        if (peek() == '#')
        {
            skip_comment();
            return;
        }

        if (peek() == '\n' || peek() == '\r')
        {
            skip_newline();
            return;
        }

        _diagnostics?.report_warning(
            string.Empty,
            default,
            "TOML001",
            "期望换行或文件结尾");
    }

    #endregion

    #region 键值对解析

    private void parse_key_value(TomlTable table)
    {
        var key = parse_key();

        skip_whitespace();

        if (peek() != '=')
        {
            _diagnostics?.report_error(
                string.Empty,
                default,
                "TOML004",
                "期望 '='");
            return;
        }

        advance();
        skip_whitespace();

        var value = parse_value();

        table.entries[key] = value;

        expect_newline_or_end();
    }

    private string parse_key()
    {
        if (peek() == '"') return parse_quoted_key('"');

        if (peek() == '\'') return parse_quoted_key('\'');

        var start = _position;

        while (!is_at_end() && peek() is not ('=' or '.' or ' ' or '\t' or '\n' or '\r' or '#' or '[')) advance();

        return _source[start.._position].Trim();
    }

    private string parse_quoted_key(char quote)
    {
        advance();

        var start = _position;

        while (!is_at_end() && peek() != quote)
        {
            if (peek() == '\\') advance();

            advance();
        }

        var content = _source[start.._position];

        if (!is_at_end()) advance();

        return content;
    }

    #endregion

    #region 值解析

    private TomlValue parse_value()
    {
        var c = peek();

        if (c == '"') return parse_basic_string();

        if (c == '\'') return parse_literal_string();

        if (c == '[') return parse_array();

        if (c == '{') return parse_inline_table();

        if (char.IsDigit(c) || c == '-' || c == '+' || c == 'i' || c == 'n') return parse_scalar_value();

        _diagnostics?.report_error(
            string.Empty,
            default,
            "TOML005",
            $"意外的字符 '{c}'");

        advance();
        return new TomlValue { type = TomlValueType.@string, raw_value = string.Empty };
    }

    private TomlValue parse_scalar_value()
    {
        var start = _position;

        if (peek() == 't' && _position + 3 < _source.Length && _source[_position..(_position + 4)] == "true")
        {
            _position += 4;
            _column += 4;
            return new TomlValue { type = TomlValueType.boolean, raw_value = true };
        }

        if (peek() == 'f' && _position + 4 < _source.Length && _source[_position..(_position + 5)] == "false")
        {
            _position += 5;
            _column += 5;
            return new TomlValue { type = TomlValueType.boolean, raw_value = false };
        }

        while (!is_at_end() && peek() is not (' ' or '\t' or '\n' or '\r' or '#' or ',' or ']' or '}')) advance();

        var text = _source[start.._position];

        if (text.Contains(':') || text.Contains('T') || text.StartsWith("inf", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("nan", StringComparison.OrdinalIgnoreCase))
        {
            if (text is "inf" or "+inf" or "-inf" or "infinity" or "+infinity" or "-infinity")
                return new TomlValue
                {
                    type = TomlValueType.@float,
                    raw_value = text.StartsWith('-') ? double.NegativeInfinity : double.PositiveInfinity
                };

            if (text is "nan" or "+nan" or "-nan")
                return new TomlValue { type = TomlValueType.@float, raw_value = double.NaN };

            return new TomlValue { type = TomlValueType.date_time, raw_value = text };
        }

        if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
            if (double.TryParse(text.Replace("_", ""), NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var floatVal))
                return new TomlValue { type = TomlValueType.@float, raw_value = floatVal };

        var intText = text.Replace("_", "");

        if (intText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            if (long.TryParse(intText[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexVal))
                return new TomlValue { type = TomlValueType.integer, raw_value = hexVal };

        if (intText.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            try
            {
                var octVal = Convert.ToInt64(intText[2..], 8);
                return new TomlValue { type = TomlValueType.integer, raw_value = octVal };
            }
            catch (FormatException)
            {
                // 回退到字符串
            }

        if (intText.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            try
            {
                var binVal = Convert.ToInt64(intText[2..], 2);
                return new TomlValue { type = TomlValueType.integer, raw_value = binVal };
            }
            catch (FormatException)
            {
                // 回退到字符串
            }

        if (long.TryParse(intText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
            return new TomlValue { type = TomlValueType.integer, raw_value = intVal };

        return new TomlValue { type = TomlValueType.@string, raw_value = text };
    }

    #endregion

    #region 字符串解析

    private TomlValue parse_basic_string()
    {
        if (peek() == '"' && peek_next() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
            return parse_multiline_basic_string();

        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '"')
        {
            if (peek() == '\\')
            {
                advance();
                sb.Append(parse_escape_sequence());
                continue;
            }

            sb.Append(advance());
        }

        if (!is_at_end()) advance();

        return new TomlValue { type = TomlValueType.@string, raw_value = sb.ToString() };
    }

    private TomlValue parse_multiline_basic_string()
    {
        advance();
        advance();
        advance();

        if (peek() == '\n') advance();

        if (peek() == '\r')
        {
            advance();
            if (peek() == '\n') advance();
        }

        var sb = new StringBuilder();

        while (!is_at_end())
        {
            if (peek() == '"' && peek_next() == '"' && _position + 2 < _source.Length && _source[_position + 2] == '"')
            {
                advance();
                advance();
                advance();
                break;
            }

            if (peek() == '\\')
            {
                if (peek_next() == '\n' || peek_next() == '\r')
                {
                    advance();

                    if (peek() == '\r') advance();

                    if (peek() == '\n') advance();

                    skip_whitespace();
                    continue;
                }

                advance();
                sb.Append(parse_escape_sequence());
                continue;
            }

            sb.Append(advance());
        }

        return new TomlValue { type = TomlValueType.@string, raw_value = sb.ToString() };
    }

    private TomlValue parse_literal_string()
    {
        if (peek() == '\'' && peek_next() == '\'' && _position + 2 < _source.Length && _source[_position + 2] == '\'')
            return parse_multiline_literal_string();

        advance();

        var start = _position;

        while (!is_at_end() && peek() != '\'') advance();

        var content = _source[start.._position];

        if (!is_at_end()) advance();

        return new TomlValue { type = TomlValueType.@string, raw_value = content };
    }

    private TomlValue parse_multiline_literal_string()
    {
        advance();
        advance();
        advance();

        if (peek() == '\n') advance();

        if (peek() == '\r')
        {
            advance();
            if (peek() == '\n') advance();
        }

        var sb = new StringBuilder();

        while (!is_at_end())
        {
            if (peek() == '\'' && peek_next() == '\'' && _position + 2 < _source.Length &&
                _source[_position + 2] == '\'')
            {
                advance();
                advance();
                advance();
                break;
            }

            sb.Append(advance());
        }

        return new TomlValue { type = TomlValueType.@string, raw_value = sb.ToString() };
    }

    private char parse_escape_sequence()
    {
        if (is_at_end()) return '\\';

        var c = advance();

        return c switch
        {
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            '\\' => '\\',
            '"' => '"',
            'u' => parse_unicode_escape(4),
            'U' => parse_unicode_escape(8),
            _ => c
        };
    }

    private char parse_unicode_escape(int digits)
    {
        var hex = new char[digits];

        for (var i = 0; i < digits; i++)
        {
            if (is_at_end()) return '\0';

            hex[i] = advance();
        }

        var code = int.Parse(new string(hex), NumberStyles.HexNumber);
        return (char)code;
    }

    #endregion
}