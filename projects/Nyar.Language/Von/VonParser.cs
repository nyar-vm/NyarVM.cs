using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.DataProcess.Serialize;
using TextReader = Std.Data.Text.Syntax.TextReader;

namespace Nyar.Language.Von;

/// <summary>
///     Gon 配置格式解析器
/// </summary>
public sealed class VonParser : IStringParser<VonValue>, ISerdeFormat
{
    private readonly DiagnosticSink? _diagnostics;
    private TextReader _reader = new(string.Empty);

    public VonParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <inheritdoc />
    public string format_name => "VON";

    /// <inheritdoc />
    public SerdeValue deserialize(string source)
    {
        _reader = new TextReader(source);
        return parse_value_internal();
    }

    /// <inheritdoc />
    public string serialize(SerdeValue value)
    {
        return value.ToString();
    }

    /// <summary>
    ///     解析 Gon 文本（兼容旧接口，保留 TypeName/VariantName）
    /// </summary>
    public VonValue parse(string source)
    {
        _reader = new TextReader(source);
        return parse_gon_value_internal();
    }

    #region SerdeValue 解析（保留 VON 特有元数据）

    private VonValue parse_gon_value_internal()
    {
        skip_whitespace_and_comments();

        if (_reader.is_at_end) return VonValue.@null();

        var c = _reader.peek();

        if (c == '{') return VonValue.@object(null, null, parse_object_fields());

        if (c == '[') return VonValue.array(parse_array_elements());

        if (c == '"') return VonValue.@string(parse_quoted_string());

        if (c == '-' || char.IsDigit(c)) return parse_gon_number();

        if (is_identifier_start(c)) return parse_gon_identifier_value();

        _diagnostics?.report_error(
            default,
            $"意外的字符 '{c}'");

        _reader.advance();
        return VonValue.@null();
    }

    private VonValue parse_gon_identifier_value()
    {
        var identifier = parse_identifier();

        skip_whitespace_and_comments();

        if (identifier == "true") return VonValue.boolean(true);

        if (identifier == "false") return VonValue.boolean(false);

        if (identifier == "null") return VonValue.@null();

        if (identifier is "inf" or "nan")
        {
            _diagnostics?.report_error(
                default,
                $"Von 格式不支持 '{identifier}'，属于语法错误");

            return VonValue.@null();
        }

        // VON 类型标注对象：Person { name: "Bob" }
        if (!_reader.is_at_end && _reader.peek() == '{')
        {
            var fields = parse_object_fields();
            return VonValue.@object(identifier, null, fields);
        }

        // VON 变体对象：Color Red { r: 255 }
        if (!_reader.is_at_end && is_identifier_start(_reader.peek()))
        {
            var variantName = parse_identifier();
            skip_whitespace_and_comments();

            if (!_reader.is_at_end && _reader.peek() == '{')
            {
                var fields = parse_object_fields();
                return VonValue.@object(identifier, variantName, fields);
            }

            _diagnostics?.report_error(
                default,
                $"标识符序列 '{identifier} {variantName}' 不是有效的值");

            return VonValue.@null();
        }

        _diagnostics?.report_error(
            default,
            $"标识符 '{identifier}' 不是有效的值");

        return VonValue.@null();
    }

    private VonValue parse_gon_number()
    {
        var isNegative = false;

        if (_reader.peek() == '-')
        {
            isNegative = true;
            _reader.advance();
        }

        var sb = new StringBuilder();

        while (!_reader.is_at_end && char.IsDigit(_reader.peek())) sb.Append(_reader.advance());

        var hasDecimalPoint = false;
        if (!_reader.is_at_end && _reader.peek() == '.')
        {
            hasDecimalPoint = true;
            sb.Append(_reader.advance());

            while (!_reader.is_at_end && char.IsDigit(_reader.peek())) sb.Append(_reader.advance());
        }

        if (!_reader.is_at_end && (_reader.peek() == 'e' || _reader.peek() == 'E'))
        {
            hasDecimalPoint = true;
            sb.Append(_reader.advance());

            if (!_reader.is_at_end && (_reader.peek() == '+' || _reader.peek() == '-')) sb.Append(_reader.advance());

            while (!_reader.is_at_end && char.IsDigit(_reader.peek())) sb.Append(_reader.advance());
        }

        var numberStr = sb.ToString();
        if (isNegative) numberStr = "-" + numberStr;

        if (hasDecimalPoint) return VonValue.@decimal(numberStr);

        return VonValue.integer(numberStr);
    }

    private Dictionary<string, VonValue> parse_object_fields()
    {
        _reader.advance();

        var fields = new Dictionary<string, VonValue>();

        skip_whitespace_and_comments();

        if (_reader.peek() != '}')
        {
            parse_gon_field(fields);

            while (true)
            {
                skip_whitespace_and_comments();

                if (_reader.peek() != ',') break;

                _reader.advance();
                skip_whitespace_and_comments();

                if (_reader.peek() == '}') break;

                parse_gon_field(fields);
            }
        }

        skip_whitespace_and_comments();

        if (_reader.peek() == '}')
            _reader.advance();
        else
            _diagnostics?.report_error(
                default,
                "期望 '}'");

        return fields;
    }

    private void parse_gon_field(Dictionary<string, VonValue> fields)
    {
        skip_whitespace_and_comments();

        string fieldName;

        if (_reader.peek() == '"')
            fieldName = parse_quoted_string();
        else
            fieldName = parse_identifier();

        skip_whitespace_and_comments();

        if (_reader.peek() == ':')
            _reader.advance();
        else
            _diagnostics?.report_error(
                default,
                $"期望 ':'，但遇到 '{_reader.peek()}'");

        var value = parse_gon_value_internal();
        fields[fieldName] = value;
    }

    private List<VonValue> parse_array_elements()
    {
        _reader.advance();

        var elements = new List<VonValue>();

        skip_whitespace_and_comments();

        if (_reader.peek() != ']')
        {
            elements.Add(parse_gon_value_internal());

            while (true)
            {
                skip_whitespace_and_comments();

                if (_reader.peek() != ',') break;

                _reader.advance();
                skip_whitespace_and_comments();

                if (_reader.peek() == ']') break;

                elements.Add(parse_gon_value_internal());
            }
        }

        skip_whitespace_and_comments();

        if (_reader.peek() == ']')
            _reader.advance();
        else
            _diagnostics?.report_error(
                default,
                "期望 ']'");

        return elements;
    }

    #endregion

    #region SerdeValue 解析（通用接口）

    private SerdeValue parse_value_internal()
    {
        skip_whitespace_and_comments();

        if (_reader.is_at_end) return SerdeValue.@null();

        var c = _reader.peek();

        if (c == '{') return parse_object_internal();

        if (c == '[') return parse_array_internal();

        if (c == '"') return SerdeValue.@string(parse_quoted_string());

        if (c == '-' || char.IsDigit(c)) return parse_number_internal();

        if (is_identifier_start(c)) return parse_identifier_value_internal();

        _diagnostics?.report_error(
            default,
            $"意外的字符 '{c}'");

        _reader.advance();
        return SerdeValue.@null();
    }

    private SerdeValue parse_object_internal()
    {
        _reader.advance();

        var fields = new Dictionary<string, SerdeValue>();

        skip_whitespace_and_comments();

        if (_reader.peek() != '}')
        {
            parse_field_internal(fields);

            while (true)
            {
                skip_whitespace_and_comments();

                if (_reader.peek() != ',') break;

                _reader.advance();
                skip_whitespace_and_comments();

                if (_reader.peek() == '}') break;

                parse_field_internal(fields);
            }
        }

        skip_whitespace_and_comments();

        if (_reader.peek() == '}')
            _reader.advance();
        else
            _diagnostics?.report_error(
                default,
                "期望 '}'");

        return SerdeValue.@object(fields);
    }

    private void parse_field_internal(Dictionary<string, SerdeValue> fields)
    {
        skip_whitespace_and_comments();

        string fieldName;

        if (_reader.peek() == '"')
            fieldName = parse_quoted_string();
        else
            fieldName = parse_identifier();

        skip_whitespace_and_comments();

        if (_reader.peek() == ':')
            _reader.advance();
        else
            _diagnostics?.report_error(
                default,
                $"期望 ':'，但遇到 '{_reader.peek()}'");

        var value = parse_value_internal();
        fields[fieldName] = value;
    }

    private SerdeValue parse_array_internal()
    {
        _reader.advance();

        var elements = new List<SerdeValue>();

        skip_whitespace_and_comments();

        if (_reader.peek() != ']')
        {
            elements.Add(parse_value_internal());

            while (true)
            {
                skip_whitespace_and_comments();

                if (_reader.peek() != ',') break;

                _reader.advance();
                skip_whitespace_and_comments();

                if (_reader.peek() == ']') break;

                elements.Add(parse_value_internal());
            }
        }

        skip_whitespace_and_comments();

        if (_reader.peek() == ']')
            _reader.advance();
        else
            _diagnostics?.report_error(
                default,
                "期望 ']'");

        return SerdeValue.array(elements);
    }

    private SerdeValue parse_number_internal()
    {
        var isNegative = false;

        if (_reader.peek() == '-')
        {
            isNegative = true;
            _reader.advance();
        }

        var sb = new StringBuilder();

        while (!_reader.is_at_end && char.IsDigit(_reader.peek())) sb.Append(_reader.advance());

        var hasDecimalPoint = false;
        if (!_reader.is_at_end && _reader.peek() == '.')
        {
            hasDecimalPoint = true;
            sb.Append(_reader.advance());

            while (!_reader.is_at_end && char.IsDigit(_reader.peek())) sb.Append(_reader.advance());
        }

        if (!_reader.is_at_end && (_reader.peek() == 'e' || _reader.peek() == 'E'))
        {
            hasDecimalPoint = true;
            sb.Append(_reader.advance());

            if (!_reader.is_at_end && (_reader.peek() == '+' || _reader.peek() == '-')) sb.Append(_reader.advance());

            while (!_reader.is_at_end && char.IsDigit(_reader.peek())) sb.Append(_reader.advance());
        }

        var numberStr = sb.ToString();
        if (isNegative) numberStr = "-" + numberStr;

        if (hasDecimalPoint) return SerdeValue.@decimal(numberStr);

        return SerdeValue.integer(numberStr);
    }

    private SerdeValue parse_identifier_value_internal()
    {
        var identifier = parse_identifier();

        skip_whitespace_and_comments();

        if (identifier == "true") return SerdeValue.boolean(true);

        if (identifier == "false") return SerdeValue.boolean(false);

        if (identifier == "null") return SerdeValue.@null();

        if (identifier is "inf" or "nan")
        {
            _diagnostics?.report_error(
                default,
                $"Von 格式不支持 '{identifier}'，属于语法错误");

            return SerdeValue.@null();
        }

        // VON 类型标注对象：Person { name: "Bob" }
        if (!_reader.is_at_end && _reader.peek() == '{') return parse_object_internal();

        // VON 变体对象：Color Red { r: 255 }
        if (!_reader.is_at_end && is_identifier_start(_reader.peek()))
        {
            var nextIdentifier = parse_identifier();
            skip_whitespace_and_comments();

            if (!_reader.is_at_end && _reader.peek() == '{') return parse_object_internal();

            _diagnostics?.report_error(
                default,
                $"标识符序列 '{identifier} {nextIdentifier}' 不是有效的值");

            return SerdeValue.@null();
        }

        _diagnostics?.report_error(
            default,
            $"标识符 '{identifier}' 不是有效的值");

        return SerdeValue.@null();
    }

    #endregion

    #region 通用辅助方法

    private string parse_identifier()
    {
        var sb = new StringBuilder();

        while (!_reader.is_at_end && is_identifier_part(_reader.peek())) sb.Append(_reader.advance());

        return sb.ToString();
    }

    private string parse_quoted_string()
    {
        _reader.advance();

        var sb = new StringBuilder();

        while (!_reader.is_at_end && _reader.peek() != '"')
            if (_reader.peek() == '\\')
            {
                _reader.advance();
                if (_reader.is_at_end) break;

                var escaped = _reader.advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(_reader.advance());
            }

        if (!_reader.is_at_end) _reader.advance();

        return sb.ToString();
    }

    private void skip_whitespace_and_comments()
    {
        while (!_reader.is_at_end)
            if (char.IsWhiteSpace(_reader.peek()))
            {
                _reader.skip_whitespace();
            }
            else if (_reader.peek() == '#')
            {
                if (_reader.peek(1) == '>') return;

                _reader.skip_line_comment();
            }
            else if (_reader.peek() == '<' && _reader.peek(1) == '#')
            {
                _reader.skip_block_comment();
            }
            else
            {
                break;
            }
    }

    private static bool is_identifier_start(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool is_identifier_part(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    #endregion
}