namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 值，不可变的 YAML 数据模型
/// </summary>
public abstract class YamlValue
{
    /// <summary>
    ///     值类型
    /// </summary>
    public abstract YamlValueType ValueType { get; }

    /// <summary>
    ///     是否为 null
    /// </summary>
    public bool is_null => ValueType == YamlValueType.@null;

    /// <summary>
    ///     是否为布尔值
    /// </summary>
    public bool is_boolean => ValueType == YamlValueType.boolean;

    /// <summary>
    ///     是否为数字
    /// </summary>
    public bool is_number => ValueType == YamlValueType.number;

    /// <summary>
    ///     是否为字符串
    /// </summary>
    public bool is_string => ValueType == YamlValueType.@string;

    /// <summary>
    ///     是否为序列
    /// </summary>
    public bool is_sequence => ValueType == YamlValueType.sequence;

    /// <summary>
    ///     是否为映射
    /// </summary>
    public bool is_mapping => ValueType == YamlValueType.mapping;
}