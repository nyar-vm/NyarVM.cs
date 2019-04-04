namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML null 值
/// </summary>
public sealed class YamlNull : YamlValue
{
    public static YamlNull instance { get; } = new();

    public override YamlValueType ValueType => YamlValueType.@null;

    public override string ToString()
    {
        return "null";
    }
}