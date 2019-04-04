namespace Sonic.Data.Generator.Command;

/// <summary>
///     位置参数信息。
/// </summary>
internal readonly struct ArgumentInfo
{
    public readonly string property_name;
    public readonly string property_type;
    public readonly int position;
    public readonly string? description;
    public readonly object? default_value;
    public readonly bool required;
    public readonly string value_name;
    public readonly string? resource_key;

    public ArgumentInfo(
        string propertyName, string propertyType, int position,
        string? description, object? defaultValue, bool required,
        string valueName, string? resourceKey
    )
    {
        property_name = propertyName;
        property_type = propertyType;
        this.position = position;
        this.description = description;
        default_value = defaultValue;
        this.required = required;
        value_name = valueName;
        resource_key = resourceKey;
    }
}