using System.Text;
using Std.Data.Text.Diagnostics;
using Std.DataProcess.Serialize;

namespace Std.Data.Text.Ini;

public sealed class IniFormat : ISerdeFormat
{
    private readonly IniParser _parser = new();

    public string format_name => "INI";

    public SerdeValue deserialize(string source)
    {
        var diagnostics = new DiagnosticSink();
        var result = _parser.parse(source, diagnostics);
        return result.to_serde_value();
    }

    public string serialize(SerdeValue value)
    {
        return serialize_ini(value);
    }

    private static string serialize_ini(SerdeValue value)
    {
        var sb = new StringBuilder();

        if (value.type != SerdeValueType.@object) return "";

        foreach (var (key, fieldValue) in value.fields ?? [])
            switch (fieldValue.type)
            {
                case SerdeValueType.@object:
                    sb.AppendLine();
                    sb.AppendLine($"[{key}]");

                    foreach (var (entryKey, entryValue) in fieldValue.fields ?? [])
                        sb.AppendLine($"{entryKey} = {serialize_ini_value(entryValue)}");

                    break;

                default:
                    sb.AppendLine($"{key} = {serialize_ini_value(fieldValue)}");
                    break;
            }

        return sb.ToString();
    }

    private static string serialize_ini_value(SerdeValue value)
    {
        return value.type switch
        {
            SerdeValueType.boolean => value.get_boolean().ToString().ToLowerInvariant(),
            SerdeValueType.integer => value.get_integer_string() ?? "",
            SerdeValueType.@decimal => value.get_decimal_string() ?? "",
            SerdeValueType.@string => value.get_string() ?? "",
            SerdeValueType.@null => "",
            _ => value.ToString() ?? ""
        };
    }
}