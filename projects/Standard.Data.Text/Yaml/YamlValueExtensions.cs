using System.Globalization;
using Std.DataProcess.Serialize;

namespace Std.Data.Text.Yaml;

/// <summary>
///     YamlValue 扩展方法，支持转换为统一 SerdeValue 模型
/// </summary>
public static class YamlValueExtensions
{
    /// <summary>
    ///     将 YamlValue 转换为 SerdeValue
    /// </summary>
    public static SerdeValue to_serde_value(this YamlValue yamlValue)
    {
        return yamlValue switch
        {
            YamlNull => SerdeValue.@null(),
            YamlBoolean b => SerdeValue.boolean(b.value),
            YamlNumber n => convert_yaml_number(n),
            YamlString s => SerdeValue.@string(s.value),
            YamlSequence seq => SerdeValue.array([.. seq.items.Select(to_serde_value)]),
            YamlMapping map => SerdeValue.@object(
                map.properties.ToDictionary(p => p.Key, p => p.Value.to_serde_value())),
            _ => SerdeValue.@null()
        };
    }

    private static SerdeValue convert_yaml_number(YamlNumber number)
    {
        if (number.try_get_long(out var longValue))
            return SerdeValue.integer(longValue.ToString(CultureInfo.InvariantCulture));

        var doubleStr = number.value.ToString(CultureInfo.InvariantCulture);
        if (double.IsNaN(number.value) || double.IsInfinity(number.value)) return SerdeValue.@null();

        return SerdeValue.@decimal(doubleStr);
    }


    /// <summary>
    ///     将 SerdeValue 转换为 YamlValue
    /// </summary>
    public static YamlValue to_yaml_value(this SerdeValue serdeValue)
    {
        return serdeValue.type switch
        {
            SerdeValueType.@null => YamlNull.instance,
            SerdeValueType.boolean => new YamlBoolean(serdeValue.get_boolean()),
            SerdeValueType.integer => new YamlNumber(double.Parse(serdeValue.get_integer_string()!,
                CultureInfo.InvariantCulture)),
            SerdeValueType.@decimal => new YamlNumber(double.Parse(serdeValue.get_decimal_string()!,
                CultureInfo.InvariantCulture)),
            SerdeValueType.@string => new YamlString(serdeValue.get_string()!),
            SerdeValueType.array => new YamlSequence([.. serdeValue.elements!.Select(to_yaml_value)]),
            SerdeValueType.@object => new YamlMapping([
                .. serdeValue.fields!
                    .Select(kv => (kv.Key, kv.Value.to_yaml_value()))
            ]),
            _ => YamlNull.instance
        };
    }
}