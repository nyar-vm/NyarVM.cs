using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;

namespace Std.Data.Text.GraphQL;

public sealed class GqlParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<GqlToken> _tokens = [];

    private GqlToken _current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    public ParseResult<GqlSchema> parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new GqlLexer();
        _tokens = lexer.tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        var typeDefs = new List<GqlTypeDefinition>();
        var directiveDefs = new List<GqlDirectiveDefinition>();

        while (!is_at_end())
            if (check(GqlTokenType.type) || check(GqlTokenType.input) ||
                check(GqlTokenType.@interface) || check(GqlTokenType.@enum) ||
                check(GqlTokenType.union) || check(GqlTokenType.scalar))
            {
                var typeDef = parse_type_definition();
                if (typeDef is not null) typeDefs.Add(typeDef);
            }
            else if (check(GqlTokenType.directive))
            {
                var dirDef = parse_directive_definition();
                if (dirDef is not null) directiveDefs.Add(dirDef);
            }
            else if (check(GqlTokenType.schema))
            {
                advance();
                consume(GqlTokenType.left_brace, 101, "期望 '{'");
                while (!check(GqlTokenType.right_brace) && !is_at_end()) advance();
                consume(GqlTokenType.right_brace, 102, "期望 '}'");
            }
            else if (check(GqlTokenType.extend))
            {
                advance();
                if (!is_at_end()) advance();
            }
            else
            {
                advance();
            }

        var schema = new GqlSchema(typeDefs, directiveDefs);

        if (_diagnostics is not null && _diagnostics.has_errors)
            return ParseResult<GqlSchema>.fail(_diagnostics.messages);

        return ParseResult<GqlSchema>.ok(schema, _diagnostics?.messages);
    }

    private GqlTypeDefinition? parse_type_definition()
    {
        var kind = advance().text;
        var name = consume_name(103, "期望类型名称");

        var implements = new List<string>();
        if (match(GqlTokenType.implements))
        {
            implements.Add(consume_name(104, "期望接口名称"));
            while (match(GqlTokenType.ampersand)) implements.Add(consume_name(105, "期望接口名称"));
        }

        var directives = parse_directives();

        if (!check(GqlTokenType.left_brace)) return new GqlTypeDefinition(name, kind, [], implements, directives);

        consume(GqlTokenType.left_brace, 106, "期望 '{'");

        var fields = new List<GqlFieldDefinition>();
        while (!check(GqlTokenType.right_brace) && !is_at_end()) fields.Add(parse_field_definition());

        consume(GqlTokenType.right_brace, 107, "期望 '}'");

        return new GqlTypeDefinition(name, kind, fields, implements, directives);
    }

    private GqlFieldDefinition parse_field_definition()
    {
        var name = consume_name(108, "期望字段名称");

        var arguments = new List<GqlInputValueDefinition>();
        if (match(GqlTokenType.left_paren))
        {
            arguments.Add(parse_input_value_definition());
            while (match(GqlTokenType.comma)) arguments.Add(parse_input_value_definition());
            consume(GqlTokenType.right_paren, 109, "期望 ')'");
        }

        consume(GqlTokenType.colon, 110, "期望 ':'");
        var type = parse_type_ref();
        var directives = parse_directives();

        return new GqlFieldDefinition(name, arguments, type, directives);
    }

    private GqlInputValueDefinition parse_input_value_definition()
    {
        var name = consume_name(111, "期望参数名称");
        consume(GqlTokenType.colon, 112, "期望 ':'");
        var type = parse_type_ref();

        GqlValue? defaultValue = null;
        if (match(GqlTokenType.equals)) defaultValue = parse_value();

        var directives = parse_directives();

        return new GqlInputValueDefinition(name, type, defaultValue, directives);
    }

    private GqlDirectiveDefinition? parse_directive_definition()
    {
        consume(GqlTokenType.directive, 113, "期望 'directive'");
        consume(GqlTokenType.at, 114, "期望 '@'");
        var name = consume_name(115, "期望指令名称");

        var arguments = new List<GqlInputValueDefinition>();
        if (match(GqlTokenType.left_paren))
        {
            arguments.Add(parse_input_value_definition());
            while (match(GqlTokenType.comma)) arguments.Add(parse_input_value_definition());
            consume(GqlTokenType.right_paren, 116, "期望 ')'");
        }

        var repeatable = match(GqlTokenType.repeatable);

        consume(GqlTokenType.on, 117, "期望 'on'");

        var locations = new List<GqlDirectiveLocation> { parse_directive_location() };
        while (match(GqlTokenType.pipe)) locations.Add(parse_directive_location());

        return new GqlDirectiveDefinition(name, arguments, locations, repeatable);
    }

    private GqlDirectiveLocation parse_directive_location()
    {
        var name = advance().text;
        return name.ToUpperInvariant() switch
        {
            "QUERY" => GqlDirectiveLocation.query,
            "MUTATION" => GqlDirectiveLocation.mutation,
            "SUBSCRIPTION" => GqlDirectiveLocation.subscription,
            "FIELD" => GqlDirectiveLocation.field,
            "FRAGMENT_DEFINITION" => GqlDirectiveLocation.fragment_definition,
            "FRAGMENT_SPREAD" => GqlDirectiveLocation.fragment_spread,
            "INLINE_FRAGMENT" => GqlDirectiveLocation.inline_fragment,
            "VARIABLE_DEFINITION" => GqlDirectiveLocation.variable_definition,
            "SCHEMA" => GqlDirectiveLocation.schema,
            "SCALAR" => GqlDirectiveLocation.scalar,
            "OBJECT" => GqlDirectiveLocation.@object,
            "FIELD_DEFINITION" => GqlDirectiveLocation.field_definition,
            "ARGUMENT_DEFINITION" => GqlDirectiveLocation.argument_definition,
            "INTERFACE" => GqlDirectiveLocation.@interface,
            "UNION" => GqlDirectiveLocation.union,
            "ENUM" => GqlDirectiveLocation.@enum,
            "ENUM_VALUE" => GqlDirectiveLocation.enum_value,
            "INPUT_OBJECT" => GqlDirectiveLocation.input_object,
            "INPUT_FIELD_DEFINITION" => GqlDirectiveLocation.input_field_definition,
            _ => GqlDirectiveLocation.field
        };
    }

    private GqlTypeRef parse_type_ref()
    {
        GqlTypeRef inner;

        if (match(GqlTokenType.left_bracket))
        {
            var elementType = parse_type_ref();
            consume(GqlTokenType.right_bracket, 118, "期望 ']'");
            inner = new GqlListType(elementType);
        }
        else
        {
            var name = consume_name(119, "期望类型名称");
            inner = new GqlNamedType(name);
        }

        if (match(GqlTokenType.exclamation)) return new GqlNonNullType(inner);

        return inner;
    }

    private IReadOnlyList<GqlDirective> parse_directives()
    {
        var directives = new List<GqlDirective>();

        while (check(GqlTokenType.at))
        {
            advance();
            var name = consume_name(120, "期望指令名称");

            var args = new List<(string Name, GqlValue Value)>();
            if (match(GqlTokenType.left_paren))
            {
                args.Add(parse_argument());
                while (match(GqlTokenType.comma)) args.Add(parse_argument());
                consume(GqlTokenType.right_paren, 121, "期望 ')'");
            }

            directives.Add(new GqlDirective(name, args));
        }

        return directives;
    }

    private (string Name, GqlValue Value) parse_argument()
    {
        var name = consume_name(122, "期望参数名称");
        consume(GqlTokenType.colon, 123, "期望 ':'");
        var value = parse_value();
        return (name, value);
    }

    private GqlValue parse_value()
    {
        if (check(GqlTokenType.int_value)) return new GqlIntValue(advance().text);

        if (check(GqlTokenType.float_value)) return new GqlFloatValue(advance().text);

        if (check(GqlTokenType.string_value)) return new GqlStringValue(advance().text);

        if (check(GqlTokenType.@true))
        {
            advance();
            return new GqlBooleanValue(true);
        }

        if (check(GqlTokenType.@false))
        {
            advance();
            return new GqlBooleanValue(false);
        }

        if (check(GqlTokenType.@null))
        {
            advance();
            return GqlNullValue.instance;
        }

        if (check(GqlTokenType.name)) return new GqlEnumValue(advance().text);

        if (check(GqlTokenType.left_bracket))
        {
            advance();
            var values = new List<GqlValue>();
            if (!check(GqlTokenType.right_bracket))
            {
                values.Add(parse_value());
                while (match(GqlTokenType.comma)) values.Add(parse_value());
            }

            consume(GqlTokenType.right_bracket, 124, "期望 ']'");
            return new GqlListValue(values);
        }

        if (check(GqlTokenType.left_brace))
        {
            advance();
            var fields = new List<(string Name, GqlValue Value)>();
            if (!check(GqlTokenType.right_brace))
            {
                fields.Add(parse_object_field());
                while (match(GqlTokenType.comma)) fields.Add(parse_object_field());
            }

            consume(GqlTokenType.right_brace, 125, "期望 '}'");
            return new GqlObjectValue(fields);
        }

        _diagnostics?.report_error(string.Empty, default,
            126, $"意外的词法单元：{_current.type}");
        advance();
        return GqlNullValue.instance;
    }

    private (string Name, GqlValue Value) parse_object_field()
    {
        var name = consume_name(127, "期望字段名称");
        consume(GqlTokenType.colon, 128, "期望 ':'");
        var value = parse_value();
        return (name, value);
    }

    private bool is_at_end()
    {
        return _current.type == GqlTokenType.end_of_file;
    }

    private bool check(GqlTokenType type)
    {
        return _current.type == type;
    }

    private bool match(GqlTokenType type)
    {
        if (_current.type != type) return false;

        advance();
        return true;
    }

    private GqlToken advance()
    {
        var token = _current;
        if (_position < _tokens.Count - 1) _position++;

        return token;
    }

    private GqlToken consume(GqlTokenType type, int? errorCode, string message)
    {
        if (_current.type == type) return advance();

        _diagnostics?.report_error(string.Empty, default,
            errorCode, $"{message}，实际遇到 {_current.type}");
        return _current;
    }

    private string consume_name(int? errorCode, string message)
    {
        if (_current.type == GqlTokenType.name) return advance().text;

        _diagnostics?.report_error(string.Empty, default,
            errorCode, $"{message}，实际遇到 {_current.type}");
        return advance().text;
    }
}
