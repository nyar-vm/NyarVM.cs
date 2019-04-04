namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 值类型
/// </summary>
public enum YamlValueType
{
    @null,
    boolean,
    number,
    @string,
    sequence,
    mapping
}