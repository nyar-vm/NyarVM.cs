using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;

namespace Std.Data.Text.Protobuf;

public sealed class ProtoParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<ProtoToken> _tokens = [];

    private ProtoToken _current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    public ParseResult<ProtoFile> parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new ProtoLexer();
        _tokens = lexer.tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        string? syntax = null;
        string? package = null;
        var imports = new List<string>();
        var options = new List<ProtoOption>();
        var messages = new List<ProtoMessage>();
        var enums = new List<ProtoEnum>();
        var services = new List<ProtoService>();

        while (!is_at_end())
            if (check(ProtoTokenType.syntax))
                syntax = parse_syntax();
            else if (check(ProtoTokenType.package))
                package = parse_package();
            else if (check(ProtoTokenType.import))
                imports.Add(parse_import());
            else if (check(ProtoTokenType.option))
                options.Add(parse_option());
            else if (check(ProtoTokenType.message))
                messages.Add(parse_message());
            else if (check(ProtoTokenType.@enum))
                enums.Add(parse_enum());
            else if (check(ProtoTokenType.service))
                services.Add(parse_service());
            else
                advance();

        var file = new ProtoFile(syntax, package, imports, options, messages, enums, services);

        if (_diagnostics is not null && _diagnostics.has_errors)
            return ParseResult<ProtoFile>.fail(_diagnostics.messages);

        return ParseResult<ProtoFile>.ok(file, _diagnostics?.messages);
    }

    private string parse_syntax()
    {
        consume(ProtoTokenType.syntax, 101, "期望 'syntax'");
        consume(ProtoTokenType.equals, 102, "期望 '='");
        var value = consume(ProtoTokenType.string_literal, 103, "期望字符串").text;
        consume(ProtoTokenType.semicolon, 104, "期望 ';'");
        return value;
    }

    private string parse_package()
    {
        consume(ProtoTokenType.package, 105, "期望 'package'");
        var name = parse_full_name();
        consume(ProtoTokenType.semicolon, 106, "期望 ';'");
        return name;
    }

    private string parse_import()
    {
        consume(ProtoTokenType.import, 107, "期望 'import'");
        if (check(ProtoTokenType.identifier) && _current.text is "public" or "weak") advance();

        var path = consume(ProtoTokenType.string_literal, 108, "期望字符串").text;
        consume(ProtoTokenType.semicolon, 109, "期望 ';'");
        return path;
    }

    private ProtoOption parse_option()
    {
        consume(ProtoTokenType.option, 110, "期望 'option'");
        var name = parse_full_name();
        consume(ProtoTokenType.equals, 111, "期望 '='");
        var value = parse_constant_value();
        consume(ProtoTokenType.semicolon, 112, "期望 ';'");
        return new ProtoOption(name, value);
    }

    private ProtoMessage parse_message()
    {
        consume(ProtoTokenType.message, 113, "期望 'message'");
        var name = consume(ProtoTokenType.identifier, 114, "期望消息名称").text;
        consume(ProtoTokenType.left_brace, 115, "期望 '{'");

        var fields = new List<ProtoField>();
        var nestedMessages = new List<ProtoMessage>();
        var nestedEnums = new List<ProtoEnum>();
        var oneofs = new List<ProtoOneof>();
        var mapFields = new List<ProtoMapField>();
        var reserved = new List<ProtoReserved>();
        var options = new List<ProtoOption>();

        while (!check(ProtoTokenType.right_brace) && !is_at_end())
            if (check(ProtoTokenType.message))
                nestedMessages.Add(parse_message());
            else if (check(ProtoTokenType.@enum))
                nestedEnums.Add(parse_enum());
            else if (check(ProtoTokenType.oneof))
                oneofs.Add(ParseOneof());
            else if (check(ProtoTokenType.map))
                mapFields.Add(parse_map_field());
            else if (check(ProtoTokenType.reserved))
                reserved.Add(parse_reserved());
            else if (check(ProtoTokenType.option))
                options.Add(parse_option());
            else if (check(ProtoTokenType.repeated) || check(ProtoTokenType.optional) || check(ProtoTokenType.required))
                fields.Add(parse_field());
            else if (check(ProtoTokenType.identifier) || check(ProtoTokenType.dot))
                fields.Add(parse_field_no_label());
            else
                advance();

        consume(ProtoTokenType.right_brace, 116, "期望 '}'");

        return new ProtoMessage(name, fields, nestedMessages, nestedEnums, oneofs, mapFields, reserved, options);
    }

    private ProtoField parse_field()
    {
        var label = advance().text;
        var type = parse_type_name();
        var name = consume(ProtoTokenType.identifier, 117, "期望字段名称").text;
        consume(ProtoTokenType.equals, 118, "期望 '='");
        var number = parse_field_number();
        var options = parse_field_options();
        consume(ProtoTokenType.semicolon, 119, "期望 ';'");
        return new ProtoField(label, type, name, number, options);
    }

    private ProtoField parse_field_no_label()
    {
        var type = parse_type_name();
        var name = consume(ProtoTokenType.identifier, 120, "期望字段名称").text;
        consume(ProtoTokenType.equals, 121, "期望 '='");
        var number = parse_field_number();
        var options = parse_field_options();
        consume(ProtoTokenType.semicolon, 122, "期望 ';'");
        return new ProtoField("", type, name, number, options);
    }

    private ProtoMapField parse_map_field()
    {
        consume(ProtoTokenType.map, 123, "期望 'map'");
        consume(ProtoTokenType.lt, 124, "期望 '<'");
        var keyType = parse_type_name();
        consume(ProtoTokenType.comma, 125, "期望 ','");
        var valueType = parse_type_name();
        consume(ProtoTokenType.gt, 126, "期望 '>'");
        var name = consume(ProtoTokenType.identifier, 127, "期望字段名称").text;
        consume(ProtoTokenType.equals, 128, "期望 '='");
        var number = parse_field_number();
        consume(ProtoTokenType.semicolon, 129, "期望 ';'");
        return new ProtoMapField(keyType, valueType, name, number);
    }

    private ProtoOneof ParseOneof()
    {
        consume(ProtoTokenType.oneof, 130, "期望 'oneof'");
        var name = consume(ProtoTokenType.identifier, 131, "期望名称").text;
        consume(ProtoTokenType.left_brace, 132, "期望 '{'");

        var fields = new List<ProtoField>();
        while (!check(ProtoTokenType.right_brace) && !is_at_end())
        {
            var type = parse_type_name();
            var fieldName = consume(ProtoTokenType.identifier, 133, "期望字段名称").text;
            consume(ProtoTokenType.equals, 134, "期望 '='");
            var number = parse_field_number();
            consume(ProtoTokenType.semicolon, 135, "期望 ';'");
            fields.Add(new ProtoField("", type, fieldName, number, []));
        }

        consume(ProtoTokenType.right_brace, 136, "期望 '}'");
        return new ProtoOneof(name, fields);
    }

    private ProtoReserved parse_reserved()
    {
        consume(ProtoTokenType.reserved, 137, "期望 'reserved'");

        var names = new List<string>();
        var ranges = new List<(int Start, int End)>();

        if (check(ProtoTokenType.string_literal))
        {
            names.Add(advance().text);
            while (match(ProtoTokenType.comma))
                if (check(ProtoTokenType.string_literal))
                    names.Add(advance().text);
                else
                    break;
        }
        else
        {
            ranges.Add(parse_range());
            while (match(ProtoTokenType.comma)) ranges.Add(parse_range());
        }

        consume(ProtoTokenType.semicolon, 138, "期望 ';'");
        return new ProtoReserved(names, ranges);
    }

    private (int Start, int End) parse_range()
    {
        var start = parse_field_number();
        if (match(ProtoTokenType.identifier) && _current.text == "to")
        {
            advance();
            var end = parse_field_number();
            return (start, end);
        }

        return (start, start);
    }

    private ProtoEnum parse_enum()
    {
        consume(ProtoTokenType.@enum, 139, "期望 'enum'");
        var name = consume(ProtoTokenType.identifier, 140, "期望枚举名称").text;
        consume(ProtoTokenType.left_brace, 141, "期望 '{'");

        var values = new List<ProtoEnumValue>();
        while (!check(ProtoTokenType.right_brace) && !is_at_end())
        {
            if (check(ProtoTokenType.option))
            {
                parse_option();
                continue;
            }

            if (check(ProtoTokenType.reserved))
            {
                parse_reserved();
                continue;
            }

            var valueName = consume(ProtoTokenType.identifier, 142, "期望枚举值名称").text;
            consume(ProtoTokenType.equals, 143, "期望 '='");
            var number = parse_field_number();
            consume(ProtoTokenType.semicolon, 144, "期望 ';'");
            values.Add(new ProtoEnumValue(valueName, number));
        }

        consume(ProtoTokenType.right_brace, 145, "期望 '}'");
        return new ProtoEnum(name, values);
    }

    private ProtoService parse_service()
    {
        consume(ProtoTokenType.service, 146, "期望 'service'");
        var name = consume(ProtoTokenType.identifier, 147, "期望服务名称").text;
        consume(ProtoTokenType.left_brace, 148, "期望 '{'");

        var methods = new List<ProtoRpc>();
        while (!check(ProtoTokenType.right_brace) && !is_at_end())
        {
            if (check(ProtoTokenType.option))
            {
                parse_option();
                continue;
            }

            if (check(ProtoTokenType.rpc))
                methods.Add(parse_rpc());
            else
                advance();
        }

        consume(ProtoTokenType.right_brace, 149, "期望 '}'");
        return new ProtoService(name, methods);
    }

    private ProtoRpc parse_rpc()
    {
        consume(ProtoTokenType.rpc, 150, "期望 'rpc'");
        var name = consume(ProtoTokenType.identifier, 151, "期望方法名称").text;
        consume(ProtoTokenType.left_paren, 152, "期望 '('");

        var inputStream = match(ProtoTokenType.stream);
        var inputType = parse_type_name();
        consume(ProtoTokenType.right_paren, 153, "期望 ')'");

        consume(ProtoTokenType.returns, 154, "期望 'returns'");
        consume(ProtoTokenType.left_paren, 155, "期望 '('");

        var outputStream = match(ProtoTokenType.stream);
        var outputType = parse_type_name();
        consume(ProtoTokenType.right_paren, 156, "期望 ')'");

        if (match(ProtoTokenType.semicolon))
        {
        }
        else if (match(ProtoTokenType.left_brace))
        {
            while (!check(ProtoTokenType.right_brace) && !is_at_end()) advance();
            consume(ProtoTokenType.right_brace, 157, "期望 '}'");
        }

        return new ProtoRpc(name, inputType, inputStream, outputType, outputStream);
    }

    private string parse_type_name()
    {
        return parse_full_name();
    }

    private string parse_full_name()
    {
        var sb = new StringBuilder();

        while (match(ProtoTokenType.dot)) sb.Append('.');

        sb.Append(consume(ProtoTokenType.identifier, 158, "期望标识符").text);

        while (check(ProtoTokenType.dot))
        {
            sb.Append(advance().text);
            sb.Append(consume(ProtoTokenType.identifier, 159, "期望标识符").text);
        }

        return sb.ToString();
    }

    private int parse_field_number()
    {
        var token = consume(ProtoTokenType.int_literal, 160, "期望字段编号");
        return int.TryParse(token.text, out var value) ? value : 0;
    }

    private IReadOnlyList<ProtoOption> parse_field_options()
    {
        var options = new List<ProtoOption>();

        if (!match(ProtoTokenType.left_bracket)) return options;

        options.Add(parse_field_option());
        while (match(ProtoTokenType.comma)) options.Add(parse_field_option());

        consume(ProtoTokenType.right_bracket, 161, "期望 ']'");
        return options;
    }

    private ProtoOption parse_field_option()
    {
        var name = parse_full_name();
        consume(ProtoTokenType.equals, 162, "期望 '='");
        var value = parse_constant_value();
        return new ProtoOption(name, value);
    }

    private string parse_constant_value()
    {
        if (check(ProtoTokenType.string_literal)) return $"\"{advance().text}\"";

        if (check(ProtoTokenType.int_literal)) return advance().text;

        if (check(ProtoTokenType.float_literal)) return advance().text;

        if (check(ProtoTokenType.bool_literal)) return advance().text;

        if (check(ProtoTokenType.identifier)) return parse_full_name();

        _diagnostics?.report_error(string.Empty, default,
            163, $"期望常量值，实际遇到 {_current.type}");
        return advance().text;
    }

    private bool is_at_end()
    {
        return _current.type == ProtoTokenType.end_of_file;
    }

    private bool check(ProtoTokenType type)
    {
        return _current.type == type;
    }

    private bool match(ProtoTokenType type)
    {
        if (_current.type != type) return false;

        advance();
        return true;
    }

    private ProtoToken advance()
    {
        var token = _current;
        if (_position < _tokens.Count - 1) _position++;

        return token;
    }

    private ProtoToken consume(ProtoTokenType type, int? errorCode, string message)
    {
        if (_current.type == type) return advance();

        _diagnostics?.report_error(string.Empty, default,
            errorCode, $"{message}，实际遇到 {_current.type}");
        return _current;
    }
}
