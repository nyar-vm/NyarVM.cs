using System.Text;
using Std.Data.Text.Diagnostics;
using Std.DataProcess.Serialize;

namespace Std.Data.Text.Toml;

public sealed class TomlFormat : ISerdeFormat
{
    private readonly TomlParser _parser = new();

    public string format_name => "TOML";

    public SerdeValue deserialize(string source)
    {
        var diagnostics = new DiagnosticSink();
        var result = _parser.parse(source, diagnostics);
        return result.root.to_serde_value();
    }

    public string serialize(SerdeValue value)
    {
        return serialize_toml_value(value);
    }

    private static string serialize_toml_value(SerdeValue value, string? key = null, int indent = 0)
    {
        var prefix = new string(' ', indent * 2);

        switch (value.type)
        {
            case SerdeValueType.@null:
                return key is not null ? $"{prefix}{key} = \"\"" : "";

            case SerdeValueType.boolean:
                return key is not null
                    ? $"{prefix}{key} = {value.get_boolean().ToString().ToLowerInvariant()}"
                    : value.get_boolean().ToString().ToLowerInvariant();

            case SerdeValueType.integer:
                return key is not null ? $"{prefix}{key} = {value.get_integer_string()}" : value.get_integer_string()!;

            case SerdeValueType.@decimal:
                return key is not null ? $"{prefix}{key} = {value.get_decimal_string()}" : value.get_decimal_string()!;

            case SerdeValueType.@string:
                return key is not null ? $"{prefix}{key} = \"{value.get_string()}\"" : $"\"{value.get_string()}\"";

            case SerdeValueType.array:
            {
                var sb = new StringBuilder();

                if (key is not null) sb.AppendLine($"{prefix}[[{key}]]");

                foreach (var item in value.elements ?? []) sb.AppendLine(serialize_toml_value(item, null, indent));

                return sb.ToString();
            }

            case SerdeValueType.@object:
            {
                var sb = new StringBuilder();

                if (key is not null) sb.AppendLine($"{prefix}[{key}]");

                foreach (var (fieldKey, fieldValue) in value.fields ?? [])
                    if (fieldValue.type == SerdeValueType.@object)
                    {
                        var nestedKey = key is not null ? $"{key}.{fieldKey}" : fieldKey;
                        sb.AppendLine();
                        sb.Append(serialize_toml_value(fieldValue, nestedKey, indent));
                    }
                    else if (fieldValue.type == SerdeValueType.array &&
                             fieldValue.elements?.Any(e => e.type == SerdeValueType.@object) == true)
                    {
                        var nestedKey = key is not null ? $"{key}.{fieldKey}" : fieldKey;

                        foreach (var item in fieldValue.elements)
                        {
                            sb.AppendLine();
                            sb.Append(serialize_toml_value(item, nestedKey, indent));
                        }
                    }
                    else
                    {
                        sb.AppendLine(serialize_toml_value(fieldValue, fieldKey, indent + 1));
                    }

                return sb.ToString();
            }

            default:
                return "";
        }
    }
}