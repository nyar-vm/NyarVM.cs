using System.Numerics;

namespace Std.DataProcess.Serialize;

/// <summary>
///     Serde 使用的中性字面量树节点。
///     它只承载跨格式共享的 literal shape，不直接代表某个 DSL 的专属语义或 `Valkyrie` value。
/// </summary>
public sealed class SerdeValue
{
    private SerdeValue(SerdeValueType type, object? rawValue = null,
        Dictionary<string, SerdeValue>? fields = null, List<SerdeValue>? elements = null)
    {
        this.type = type;
        raw_value = rawValue;
        this.fields = fields;
        this.elements = elements;
    }

    /// <summary>
    ///     值类型
    /// </summary>
    public SerdeValueType type { get; }

    /// <summary>
    ///     原始值
    /// </summary>
    public object? raw_value { get; }

    /// <summary>
    ///     对象字段（仅对象类型）
    /// </summary>
    public Dictionary<string, SerdeValue>? fields { get; }

    /// <summary>
    ///     数组元素（仅数组类型）
    /// </summary>
    public List<SerdeValue>? elements { get; }

    /// <summary>
    ///     创建空值
    /// </summary>
    public static SerdeValue @null()
    {
        return new SerdeValue(SerdeValueType.@null);
    }

    /// <summary>
    ///     创建布尔值
    /// </summary>
    public static SerdeValue boolean(bool value)
    {
        return new SerdeValue(SerdeValueType.boolean, value);
    }

    /// <summary>
    ///     创建整数（无范围限制，使用字符串存储）
    /// </summary>
    public static SerdeValue integer(string value)
    {
        return new SerdeValue(SerdeValueType.integer, value);
    }

    /// <summary>
    ///     创建小数（无范围限制，使用字符串存储）
    /// </summary>
    public static SerdeValue @decimal(string value)
    {
        return new SerdeValue(SerdeValueType.@decimal, value);
    }

    /// <summary>
    ///     创建字符串
    /// </summary>
    public static SerdeValue @string(string value)
    {
        return new SerdeValue(SerdeValueType.@string, value);
    }

    /// <summary>
    ///     创建对象
    /// </summary>
    public static SerdeValue @object(Dictionary<string, SerdeValue> fields)
    {
        return new SerdeValue(SerdeValueType.@object, fields: fields);
    }

    /// <summary>
    ///     创建数组
    /// </summary>
    public static SerdeValue array(List<SerdeValue> elements)
    {
        return new SerdeValue(SerdeValueType.array, elements: elements);
    }

    /// <summary>
    ///     获取布尔值
    /// </summary>
    public bool get_boolean()
    {
        return type == SerdeValueType.boolean && (bool)(raw_value ?? false);
    }

    /// <summary>
    ///     获取整数字符串表示
    /// </summary>
    public string? get_integer_string()
    {
        return type == SerdeValueType.integer ? (string?)raw_value : null;
    }

    /// <summary>
    ///     获取小数字符串表示
    /// </summary>
    public string? get_decimal_string()
    {
        return type == SerdeValueType.@decimal ? (string?)raw_value : null;
    }

    /// <summary>
    ///     获取字符串
    /// </summary>
    public string? get_string()
    {
        return type == SerdeValueType.@string ? (string?)raw_value : null;
    }

    /// <summary>
    ///     获取字段值
    /// </summary>
    public SerdeValue? get_field(string name)
    {
        if (fields is not null && fields.TryGetValue(name, out var value)) return value;

        return null;
    }

    /// <summary>
    ///     获取字段值并转换为指定类型
    /// </summary>
    public T? get_field_as<T>(string name) where T : struct
    {
        var field = get_field(name);
        if (field is null) return null;

        return field.type switch
        {
            SerdeValueType.integer => convert_integer<T>(field.get_integer_string()),
            SerdeValueType.@decimal => convert_decimal<T>(field.get_decimal_string()),
            SerdeValueType.boolean => (T)(object)field.get_boolean(),
            _ => null
        };
    }

    private static T? convert_integer<T>(string? value) where T : struct
    {
        if (value is null) return null;

        if (typeof(T) == typeof(long)) return (T)(object)long.Parse(value);

        if (typeof(T) == typeof(int)) return (T)(object)int.Parse(value);

        if (typeof(T) == typeof(BigInteger)) return (T)(object)BigInteger.Parse(value);

        return null;
    }

    private static T? convert_decimal<T>(string? value) where T : struct
    {
        if (value is null) return null;

        if (typeof(T) == typeof(decimal)) return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);

        if (typeof(T) == typeof(double)) return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);

        return null;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return type switch
        {
            SerdeValueType.@null => "null",
            SerdeValueType.boolean => get_boolean() ? "true" : "false",
            SerdeValueType.integer => format_integer_or_null(),
            SerdeValueType.@decimal => format_decimal_or_null(),
            SerdeValueType.@string => $"\"{get_string()}\"",
            SerdeValueType.@object => format_object(),
            SerdeValueType.array => format_array(),
            _ => "unknown"
        };
    }

    private string format_integer_or_null()
    {
        var value = get_integer_string();
        if (string.IsNullOrEmpty(value)) return "null";

        if (value == "-") return "null";

        return value;
    }

    private string format_decimal_or_null()
    {
        var value = get_decimal_string();
        if (string.IsNullOrEmpty(value)) return "null";

        if (value is "-" or "." or "-.") return "null";

        return value;
    }

    private string format_object()
    {
        var sb = new StringBuilder();
        sb.Append("{ ");

        if (fields is not null)
        {
            var first = true;
            foreach (var (key, value) in fields)
            {
                if (!first) sb.Append(", ");

                sb.Append(key);
                sb.Append(": ");
                sb.Append(value);
                first = false;
            }
        }

        sb.Append(" }");
        return sb.ToString();
    }

    private string format_array()
    {
        var sb = new StringBuilder();
        sb.Append("[ ");

        if (elements is not null)
        {
            var first = true;
            foreach (var element in elements)
            {
                if (!first) sb.Append(", ");

                sb.Append(element);
                first = false;
            }
        }

        sb.Append(" ]");
        return sb.ToString();
    }
}
