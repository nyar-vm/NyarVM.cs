using System.Text;
using Std.DataProcess.Serialize;

namespace Nyar.Language.Von;

/// <summary>
///     Gon 配置格式的值类型（已弃用，请使用 Oak.Data.SerdeValue）
/// </summary>
[Obsolete("请使用 Oak.Data.SerdeValue")]
public sealed class VonValue
{
    private VonValue(SerdeValue inner, string? typeName = null, string? variantName = null)
    {
        this.inner = inner;
        type_name = typeName;
        variant_name = variantName;
    }

    /// <summary>
    ///     内部 SerdeValue
    /// </summary>
    public SerdeValue inner { get; }

    /// <summary>
    ///     值类型
    /// </summary>
    public VonValueType type => (VonValueType)(int)inner.type;

    /// <summary>
    ///     原始值
    /// </summary>
    public object? raw_value => inner.raw_value;

    /// <summary>
    ///     类型名（VON 特有，仅对象类型）
    /// </summary>
    public string? type_name { get; }

    /// <summary>
    ///     变体名（VON 特有，仅对象类型）
    /// </summary>
    public string? variant_name { get; }

    /// <summary>
    ///     对象字段（仅对象类型）
    /// </summary>
    public Dictionary<string, VonValue>? fields =>
        inner.fields?.ToDictionary(kv => kv.Key, kv => new VonValue(kv.Value));

    /// <summary>
    ///     数组元素（仅数组类型）
    /// </summary>
    public List<VonValue>? elements =>
        [.. inner.elements?.Select(e => new VonValue(e))];

    /// <summary>
    ///     创建空值
    /// </summary>
    public static VonValue @null()
    {
        return new VonValue(SerdeValue.@null());
    }

    /// <summary>
    ///     创建布尔值
    /// </summary>
    public static VonValue boolean(bool value)
    {
        return new VonValue(SerdeValue.boolean(value));
    }

    /// <summary>
    ///     创建整数（无范围限制，使用字符串存储）
    /// </summary>
    public static VonValue integer(string value)
    {
        return new VonValue(SerdeValue.integer(value));
    }

    /// <summary>
    ///     创建小数（无范围限制，使用字符串存储）
    /// </summary>
    public static VonValue @decimal(string value)
    {
        return new VonValue(SerdeValue.@decimal(value));
    }

    /// <summary>
    ///     创建字符串
    /// </summary>
    public static VonValue @string(string value)
    {
        return new VonValue(SerdeValue.@string(value));
    }

    /// <summary>
    ///     创建对象
    /// </summary>
    public static VonValue @object(string? typeName, string? variantName, Dictionary<string, VonValue> fields)
    {
        var dataFields = fields.ToDictionary(kv => kv.Key, kv => kv.Value.inner);
        return new VonValue(SerdeValue.@object(dataFields), typeName, variantName);
    }

    /// <summary>
    ///     创建数组
    /// </summary>
    public static VonValue array(List<VonValue> elements)
    {
        var dataElements = elements.Select(e => e.inner).ToList();
        return new VonValue(SerdeValue.array(dataElements));
    }

    /// <summary>
    ///     获取布尔值
    /// </summary>
    public bool get_boolean()
    {
        return inner.get_boolean();
    }

    /// <summary>
    ///     获取整数字符串表示
    /// </summary>
    public string? get_integer_string()
    {
        return inner.get_integer_string();
    }

    /// <summary>
    ///     获取小数字符串表示
    /// </summary>
    public string? get_decimal_string()
    {
        return inner.get_decimal_string();
    }

    /// <summary>
    ///     获取字符串
    /// </summary>
    public string? get_string()
    {
        return inner.get_string();
    }

    /// <summary>
    ///     获取字段值
    /// </summary>
    public VonValue? get_field(string name)
    {
        var field = inner.get_field(name);
        return field is null ? null : new VonValue(field);
    }

    /// <summary>
    ///     获取字段值并转换为指定类型
    /// </summary>
    public T? get_field_as<T>(string name) where T : struct
    {
        return inner.get_field_as<T>(name);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (type == VonValueType.@object && (type_name is not null || variant_name is not null))
        {
            var sb = new StringBuilder();
            if (type_name is not null)
            {
                sb.Append(type_name);
                sb.Append(' ');
            }

            if (variant_name is not null)
            {
                sb.Append(variant_name);
                sb.Append(' ');
            }

            sb.Append(inner);
            return sb.ToString();
        }

        return inner.ToString();
    }

    /// <summary>
    ///     隐式转换为 SerdeValue
    /// </summary>
    public static implicit operator SerdeValue(VonValue value)
    {
        return value.inner;
    }

    /// <summary>
    ///     显式从 SerdeValue 转换
    /// </summary>
    public static explicit operator VonValue(SerdeValue value)
    {
        return new VonValue(value);
    }
}