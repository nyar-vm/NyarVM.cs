namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 布尔值
/// </summary>
public sealed class YamlBoolean : YamlValue
{
    public YamlBoolean(bool value)
    {
        this.value = value;
    }

    public bool value { get; }

    public override YamlValueType ValueType => YamlValueType.boolean;

    public override string ToString()
    {
        return value ? "true" : "false";
    }
}