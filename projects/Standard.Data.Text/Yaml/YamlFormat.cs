using Std.DataProcess.Serialize;

namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 格式序列化/反序列化器，实现统一 ISerdeFormat 接口
/// </summary>
public sealed class YamlFormat : ISerdeFormat
{
    private readonly YamlParser _parser = new();


    /// <inheritdoc />
    public string format_name => "YAML";


    /// <inheritdoc />
    public SerdeValue deserialize(string source)
    {
        var result = _parser.parse(source);

        if (result is { success: true, value: not null }) return result.value.to_serde_value();

        return SerdeValue.@null();
    }


    /// <inheritdoc />
    public string serialize(SerdeValue value)
    {
        return value.to_yaml_value().ToString();
    }
}