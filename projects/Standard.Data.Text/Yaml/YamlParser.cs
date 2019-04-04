using System.Globalization;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;

namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 解析器，将词法单元流解析为 YamlValue 树
/// </summary>
public sealed class YamlParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<YamlToken> _tokens = [];

    private YamlToken _current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];


    /// <summary>
    ///     解析 YAML 文本
    /// </summary>
    /// <param name="source">YAML 源文本。</param>
    /// <param name="diagnostics">诊断收集器。</param>
    /// <returns>解析结果。</returns>
    public ParseResult<YamlValue> parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new YamlLexer();
        _tokens = lexer.tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        var value = parse_document();

        if (_diagnostics is not null && _diagnostics.has_errors)
            return ParseResult<YamlValue>.fail(_diagnostics.messages);

        return ParseResult<YamlValue>.ok(value, _diagnostics?.messages);
    }

    private YamlToken advance()
    {
        var token = _current;
        if (_position < _tokens.Count - 1) _position++;

        return token;
    }

    private bool match(YamlTokenType type)
    {
        if (_current.type == type)
        {
            advance();
            return true;
        }

        return false;
    }

    private YamlValue parse_document()
    {
        if (_current.type == YamlTokenType.document_start) advance();

        var value = parse_block(0);

        if (_current.type == YamlTokenType.document_end) advance();

        return value;
    }

    private YamlValue parse_block(int minIndent)
    {
        skip_newlines();

        if (_current.type == YamlTokenType.dash) return parse_sequence(minIndent);

        if (_current.type == YamlTokenType.key) return parse_mapping(minIndent);

        return parse_inline_value();
    }

    private YamlMapping parse_mapping(int minIndent)
    {
        var properties = new List<(string Key, YamlValue Value)>();

        while (_current.type == YamlTokenType.key && _current.indent >= minIndent)
        {
            var keyToken = advance();
            var key = keyToken.text.Trim();

            skip_newlines();

            if (_current.type == YamlTokenType.indent && _current.indent > keyToken.indent)
            {
                var indent = _current.indent;
                advance();
                skip_newlines();
                var value = parse_block(indent);
                properties.Add((key, value));
            }
            else if ((_current.type == YamlTokenType.key && _current.indent > keyToken.indent) ||
                     (_current.type == YamlTokenType.dash && _current.indent > keyToken.indent))
            {
                var value = parse_block(_current.indent);
                properties.Add((key, value));
            }
            else
            {
                if (_current.type == YamlTokenType.indent) advance();

                var value = parse_inline_value();
                properties.Add((key, value));
            }

            skip_newlines();
        }

        return new YamlMapping([.. properties]);
    }

    private YamlSequence parse_sequence(int minIndent)
    {
        var items = new List<YamlValue>();

        while (_current.type == YamlTokenType.dash && _current.indent >= minIndent)
        {
            var dashToken = advance();
            skip_newlines();

            if (_current.type == YamlTokenType.indent && _current.indent > dashToken.indent)
            {
                var indent = _current.indent;
                advance();
                skip_newlines();
                var item = parse_block(indent);
                items.Add(item);
            }
            else if ((_current.type == YamlTokenType.key && _current.indent > dashToken.indent) ||
                     (_current.type == YamlTokenType.dash && _current.indent > dashToken.indent))
            {
                var item = parse_block(_current.indent);
                items.Add(item);
            }
            else if (_current.type == YamlTokenType.key)
            {
                var item = parse_mapping(dashToken.indent + 2);
                items.Add(item);
            }
            else if (_current.type == YamlTokenType.dash && _current.indent == dashToken.indent)
            {
                items.Add(YamlNull.instance);
            }
            else
            {
                var item = parse_inline_value();
                items.Add(item);
            }

            skip_newlines();
        }

        return new YamlSequence([.. items]);
    }

    private YamlValue parse_inline_value()
    {
        switch (_current.type)
        {
            case YamlTokenType.flow_map_start:
                return parse_flow_mapping();
            case YamlTokenType.flow_seq_start:
                return parse_flow_sequence();
            case YamlTokenType.@null:
                advance();
                return YamlNull.instance;
            case YamlTokenType.boolean:
                return parse_boolean();
            case YamlTokenType.number:
                return parse_number();
            case YamlTokenType.@string:
                var strToken = advance();
                return new YamlString(unquote(strToken.text));
            case YamlTokenType.end_of_file:
                return YamlNull.instance;
            default:
                return YamlNull.instance;
        }
    }

    private YamlBoolean parse_boolean()
    {
        var token = advance();
        var value = token.text.ToLowerInvariant() switch
        {
            "true" or "yes" or "on" => true,
            "false" or "no" or "off" => false,
            _ => false
        };
        return new YamlBoolean(value);
    }

    private YamlNumber parse_number()
    {
        var token = advance();
        var value = double.Parse(token.text, CultureInfo.InvariantCulture);
        return new YamlNumber(value);
    }

    private YamlMapping parse_flow_mapping()
    {
        advance();

        var properties = new List<(string Key, YamlValue Value)>();

        skip_newlines();

        if (_current.type != YamlTokenType.flow_map_end)
        {
            properties.Add(parse_flow_property());

            while (match(YamlTokenType.flow_comma))
            {
                skip_newlines();
                if (_current.type == YamlTokenType.flow_map_end) break;

                properties.Add(parse_flow_property());
            }
        }

        skip_newlines();

        if (_current.type == YamlTokenType.flow_map_end) advance();

        return new YamlMapping([.. properties]);
    }

    private (string Key, YamlValue Value) parse_flow_property()
    {
        var keyToken = _current;
        var key = keyToken.type == YamlTokenType.key
            ? advance().text.Trim()
            : keyToken.type == YamlTokenType.@string
                ? unquote(advance().text)
                : advance().text;

        if (_current.type == YamlTokenType.colon) advance();

        if (_current.type == YamlTokenType.indent) advance();

        skip_newlines();

        var value = parse_inline_value();
        return (key, value);
    }

    private YamlSequence parse_flow_sequence()
    {
        advance();

        var items = new List<YamlValue>();

        skip_newlines();

        if (_current.type != YamlTokenType.flow_seq_end)
        {
            items.Add(parse_inline_value());

            while (match(YamlTokenType.flow_comma))
            {
                skip_newlines();
                if (_current.type == YamlTokenType.flow_seq_end) break;

                items.Add(parse_inline_value());
            }
        }

        skip_newlines();

        if (_current.type == YamlTokenType.flow_seq_end) advance();

        return new YamlSequence([.. items]);
    }

    private void skip_newlines()
    {
        while (_current.type is YamlTokenType.newline or YamlTokenType.comment) advance();
    }

    private static string unquote(string text)
    {
        if (text.Length >= 2)
            if ((text.StartsWith('"') && text.EndsWith('"')) ||
                (text.StartsWith('\'') && text.EndsWith('\'')))
                return text[1..^1];

        return text;
    }
}