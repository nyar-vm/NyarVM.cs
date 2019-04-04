using System.Globalization;
using Std.Data.Text.Toml.Ast;
using Std.DataProcess.Serialize;

namespace Std.Data.Text.Toml;

public static class TomlValueExtensions
{
    public static SerdeValue to_serde_value(this TomlValue tomlValue)
    {
        return tomlValue.type switch
        {
            TomlValueType.@string => SerdeValue.@string((string)tomlValue.raw_value!),
            TomlValueType.integer => SerdeValue.integer(
                ((long)tomlValue.raw_value!).ToString(CultureInfo.InvariantCulture)),
            TomlValueType.@float => convert_float((double)tomlValue.raw_value!),
            TomlValueType.boolean => SerdeValue.boolean((bool)tomlValue.raw_value!),
            TomlValueType.date_time or TomlValueType.date or TomlValueType.time => SerdeValue.@string(
                tomlValue.raw_value?.ToString() ?? ""),
            TomlValueType.array =>
                SerdeValue.array([.. ((TomlValue[])tomlValue.raw_value!).Select(to_serde_value)]),
            TomlValueType.inline_table => convert_inline_table((Dictionary<string, TomlValue>)tomlValue.raw_value!),
            _ => SerdeValue.@null()
        };
    }

    private static SerdeValue convert_float(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return SerdeValue.@null();

        return SerdeValue.@decimal(value.ToString(CultureInfo.InvariantCulture));
    }

    private static SerdeValue convert_inline_table(Dictionary<string, TomlValue> table)
    {
        return SerdeValue.@object(table.ToDictionary(kv => kv.Key, kv => kv.Value.to_serde_value()));
    }

    public static SerdeValue to_serde_value(this TomlTable table)
    {
        var fields = new Dictionary<string, SerdeValue>();

        foreach (var (key, value) in table.entries) fields[key] = value.to_serde_value();

        foreach (var (key, childTable) in table.tables) fields[key] = childTable.to_serde_value();

        return SerdeValue.@object(fields);
    }

    public static TomlValue to_toml_value(this SerdeValue serdeValue)
    {
        return serdeValue.type switch
        {
            SerdeValueType.@null => new TomlValue { type = TomlValueType.@string, raw_value = "" },
            SerdeValueType.boolean => new TomlValue
                { type = TomlValueType.boolean, raw_value = serdeValue.get_boolean() },
            SerdeValueType.integer => new TomlValue
            {
                type = TomlValueType.integer,
                raw_value = long.Parse(serdeValue.get_integer_string()!, CultureInfo.InvariantCulture)
            },
            SerdeValueType.@decimal => new TomlValue
            {
                type = TomlValueType.@float,
                raw_value = double.Parse(serdeValue.get_decimal_string()!, CultureInfo.InvariantCulture)
            },
            SerdeValueType.@string => new TomlValue
                { type = TomlValueType.@string, raw_value = serdeValue.get_string()! },
            SerdeValueType.array => new TomlValue
                { type = TomlValueType.array, raw_value = serdeValue.elements!.Select(to_toml_value).ToArray() },
            SerdeValueType.@object => new TomlValue
            {
                type = TomlValueType.inline_table,
                raw_value = serdeValue.fields!.ToDictionary(kv => kv.Key, kv => kv.Value.to_toml_value())
            },
            _ => new TomlValue { type = TomlValueType.@string, raw_value = "" }
        };
    }
}