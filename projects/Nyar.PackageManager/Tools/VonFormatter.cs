using System.Text;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Tools;

public static class VonFormatter
{
    public static string format(SerdeValue value, int indent = 0)
    {
        var sb = new StringBuilder();
        format_value(sb, value, indent);
        return sb.ToString();
    }

    private static void format_value(StringBuilder sb, SerdeValue value, int indent)
    {
        switch (value.type)
        {
            case SerdeValueType.@null:
                sb.Append("null");
                break;
            case SerdeValueType.boolean:
                sb.Append(value.get_boolean() ? "true" : "false");
                break;
            case SerdeValueType.integer:
                sb.Append(value.get_integer_string() ?? "0");
                break;
            case SerdeValueType.@decimal:
                sb.Append(value.get_decimal_string() ?? "0");
                break;
            case SerdeValueType.@string:
                sb.Append('"');
                sb.Append(escape_string(value.get_string() ?? ""));
                sb.Append('"');
                break;
            case SerdeValueType.array:
                format_array(sb, value, indent);
                break;
            case SerdeValueType.@object:
                format_object(sb, value, indent);
                break;
        }
    }

    private static void format_object(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.fields is null || value.fields.Count == 0)
        {
            sb.Append("{}");
            return;
        }

        var indentStr = new string(' ', indent * 4);
        var innerIndentStr = new string(' ', (indent + 1) * 4);

        sb.AppendLine("{");

        var fields = value.fields.ToList();
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            sb.Append(innerIndentStr);
            sb.Append(needs_quoting(field.Key) ? $"\"{escape_string(field.Key)}\"" : field.Key);
            sb.Append(": ");

            if (field.Value.type is SerdeValueType.@object or SerdeValueType.array)
                format_value(sb, field.Value, indent + 1);
            else
                format_value(sb, field.Value, 0);

            if (i < fields.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.Append(indentStr);
        sb.Append('}');
    }

    private static void format_array(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.elements is null || value.elements.Count == 0)
        {
            sb.Append("[]");
            return;
        }

        var allSimple = value.elements.All(e =>
            e.type != SerdeValueType.@object && e.type != SerdeValueType.array);

        if (allSimple)
        {
            sb.Append("[ ");
            for (var i = 0; i < value.elements.Count; i++)
            {
                format_value(sb, value.elements[i], 0);
                if (i < value.elements.Count - 1) sb.Append(", ");
            }

            sb.Append(" ]");
        }
        else
        {
            var indentStr = new string(' ', indent * 4);
            var innerIndentStr = new string(' ', (indent + 1) * 4);

            sb.AppendLine("[");
            for (var i = 0; i < value.elements.Count; i++)
            {
                sb.Append(innerIndentStr);
                format_value(sb, value.elements[i], indent + 1);
                if (i < value.elements.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            sb.Append(indentStr);
            sb.Append(']');
        }
    }

    private static bool needs_quoting(string key)
    {
        if (string.IsNullOrEmpty(key)) return true;

        if (!char.IsLetter(key[0]) && key[0] != '_') return true;

        foreach (var c in key)
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                return true;

        if (key is "true" or "false" or "null") return true;

        return false;
    }

    private static string escape_string(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }

        return sb.ToString();
    }
}