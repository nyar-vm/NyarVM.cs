namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 字符串值
/// </summary>
public sealed class YamlString : YamlValue
{
    public YamlString(string value)
    {
        this.value = value;
    }

    public string value { get; }

    public override YamlValueType ValueType => YamlValueType.@string;

    public override string ToString()
    {
        return value;
    }
}