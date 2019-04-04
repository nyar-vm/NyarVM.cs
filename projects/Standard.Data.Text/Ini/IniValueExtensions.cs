using System.Globalization;
using Std.DataProcess.Serialize;

namespace Std.Data.Text.Ini;

public static class IniValueExtensions
{
    public static SerdeValue to_serde_value(this IniParseResult result)
    {
        var fields = new Dictionary<string, SerdeValue>();

        foreach (var (key, value) in result.global_entries) fields[key] = classify_value(value);

        foreach (var section in result.sections)
        {
            var sectionFields = new Dictionary<string, SerdeValue>();

            foreach (var (key, value) in section.entries) sectionFields[key] = classify_value(value);

            fields[section.name] = SerdeValue.@object(sectionFields);
        }

        return SerdeValue.@object(fields);
    }

    private static SerdeValue classify_value(string value)
    {
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) return SerdeValue.boolean(true);

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return SerdeValue.boolean(false);

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
            return SerdeValue.integer(intVal.ToString(CultureInfo.InvariantCulture));

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
            return SerdeValue.@decimal(floatVal.ToString(CultureInfo.InvariantCulture));

        return SerdeValue.@string(value);
    }

    public static IniParseResult to_ini_parse_result(this SerdeValue serdeValue)
    {
        var globalEntries = new Dictionary<string, string>();
        var sections = new List<IniSection>();

        foreach (var (key, value) in serdeValue.fields ?? [])
            if (value.type == SerdeValueType.@object)
            {
                var section = new IniSection { name = key };

                foreach (var (entryKey, entryValue) in value.fields ?? [])
                    section.entries[entryKey] = convert_to_string(entryValue);

                sections.Add(section);
            }
            else
            {
                globalEntries[key] = convert_to_string(value);
            }

        return new IniParseResult
        {
            global_entries = globalEntries,
            sections = sections
        };
    }

    private static string convert_to_string(SerdeValue value)
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