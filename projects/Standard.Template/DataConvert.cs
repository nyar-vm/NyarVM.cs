using System.Collections;
using Std.Data.Text.Yaml;

namespace Std.Template;

/// <summary>
///     Oak 数据模型与 Dictionary&lt;string, object&gt; 之间的转换工具
/// </summary>
public static class DataConvert
{
    /// <summary>
    ///     将 YamlValue 转换为 object
    /// </summary>
    public static object? YamlValueToObject(YamlValue value)
    {
        return value switch
        {
            YamlNull => null,
            YamlBoolean b => b.value,
            YamlNumber n => n.try_get_long(out var l) ? l : n.value,
            YamlString s => s.value,
            YamlSequence seq => seq.items.Select(YamlValueToObject).ToList()!,
            YamlMapping map => map.properties.ToDictionary(p => p.Key, p => YamlValueToObject(p.Value)),
            _ => value.ToString()
        };
    }

    /// <summary>
    ///     将 JsonValue 转换为 object
    /// </summary>
    public static object? JsonValueToObject(JsonValue value)
    {
        return value switch
        {
            JsonNull => null,
            JsonBoolean b => b.Value,
            JsonNumber n => n.TryGetLong(out var l) ? l : n.Value,
            JsonString s => s.Value,
            JsonArray arr => arr.Items.Select(JsonValueToObject).ToList()!,
            JsonObject obj => obj.Properties.ToDictionary(p => p.Key, p => JsonValueToObject(p.Value)),
            _ => value.ToString()
        };
    }

    /// <summary>
    ///     将 object 转换为 SerdeValue（用于 JSON 序列化）
    /// </summary>
    public static SerdeValue ObjectToSerdeValue(object? value)
    {
        return value switch
        {
            null => SerdeValue.@null(),
            bool b => SerdeValue.boolean(b),
            int i => SerdeValue.integer(i.ToString()),
            long l => SerdeValue.integer(l.ToString()),
            double d => SerdeValue.@decimal(d.ToString()),
            string s => SerdeValue.@string(s),
            DateTime dt => SerdeValue.@string(dt.ToString("yyyy-MM-dd")),
            Dictionary<string, object> dict => SerdeValue.@object(dict.ToDictionary(
                kvp => kvp.Key,
                kvp => ObjectToSerdeValue(kvp.Value))),
            IList list => SerdeValue.array(list.Cast<object?>().Select(ObjectToSerdeValue).ToList()),
            _ => SerdeValue.@string(value.ToString() ?? "")
        };
    }

    /// <summary>
    ///     解析 YAML 文本为 Dictionary&lt;string, object?&gt;
    /// </summary>
    public static Dictionary<string, object?> YamlToDictionary(string yaml)
    {
        var parser = new YamlParser();
        var result = parser.parse(yaml);
        if (!result.success || result.value is not YamlMapping mapping)
            return new Dictionary<string, object?>();

        return mapping.properties.ToDictionary(p => p.Key, p => YamlValueToObject(p.Value));
    }

    /// <summary>
    ///     解析 JSON 文本为 Dictionary&lt;string, object?&gt;
    /// </summary>
    public static Dictionary<string, object?> JsonToDictionary(string json)
    {
        var parser = new JsonParser();
        var result = parser.Parse(json);
        if (!result.Success || result.Value is not JsonObject obj)
            return new Dictionary<string, object?>();

        return obj.Properties.ToDictionary(p => p.Key, p => JsonValueToObject(p.Value));
    }

    /// <summary>
    ///     将对象列表序列化为 JSON 文本
    /// </summary>
    public static string SerializeJson(List<Dictionary<string, object>> items)
    {
        var elements = items.Select(dict => ObjectToSerdeValue(dict)).ToList();
        var array = SerdeValue.array(elements);
        var format = new JsonFormat();
        return format.Serialize(array);
    }

    /// <summary>
    ///     从 YamlMapping 获取字符串值
    /// </summary>
    public static string GetYamlString(YamlMapping mapping, string key, string defaultValue = "")
    {
        if (!mapping.try_get_value(key, out var value)) return defaultValue;
        return value is YamlString s ? s.value : value.ToString() ?? defaultValue;
    }

    /// <summary>
    ///     从 YamlMapping 获取整数值
    /// </summary>
    public static int GetYamlInt(YamlMapping mapping, string key, int defaultValue = 0)
    {
        if (!mapping.try_get_value(key, out var value)) return defaultValue;
        if (value is YamlNumber n && n.try_get_int(out var i)) return i;
        return defaultValue;
    }

    /// <summary>
    ///     从 YamlMapping 获取布尔值
    /// </summary>
    public static bool GetYamlBool(YamlMapping mapping, string key, bool defaultValue = false)
    {
        if (!mapping.try_get_value(key, out var value)) return defaultValue;
        return value is YamlBoolean b ? b.value : defaultValue;
    }

    /// <summary>
    ///     从 YamlMapping 获取字符串列表
    /// </summary>
    public static List<string> GetYamlStringList(YamlMapping mapping, string key)
    {
        if (!mapping.try_get_value(key, out var value)) return new List<string>();
        if (value is YamlSequence seq)
            return seq.items.OfType<YamlString>().Select(s => s.value).ToList();
        if (value is YamlString s)
            return s.value.Trim('[', ']').Split(',').Select(t => t.Trim().Trim('"', '\''))
                .Where(t => !string.IsNullOrEmpty(t)).ToList();
        return new List<string>();
    }

    /// <summary>
    ///     从 JsonObject 获取字符串值
    /// </summary>
    public static string GetJsonString(JsonObject obj, string key, string defaultValue = "")
    {
        if (!obj.TryGetValue(key, out var value)) return defaultValue;
        return value is JsonString s ? s.Value : defaultValue;
    }

    /// <summary>
    ///     从 JsonObject 获取双精度值
    /// </summary>
    public static double GetJsonDouble(JsonObject obj, string key, double defaultValue = 0)
    {
        if (!obj.TryGetValue(key, out var value)) return defaultValue;
        return value is JsonNumber n ? n.Value : defaultValue;
    }

    /// <summary>
    ///     从 JsonObject 获取整数值
    /// </summary>
    public static int GetJsonInt(JsonObject obj, string key, int defaultValue = 0)
    {
        if (!obj.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonNumber n && n.TryGetInt(out var i)) return i;
        return defaultValue;
    }

    /// <summary>
    ///     从 JsonObject 获取长整数值
    /// </summary>
    public static long GetJsonLong(JsonObject obj, string key, long defaultValue = 0)
    {
        if (!obj.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonNumber n && n.TryGetLong(out var l)) return l;
        return defaultValue;
    }
}