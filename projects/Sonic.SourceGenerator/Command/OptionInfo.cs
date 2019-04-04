namespace Sonic.Data.Generator.Command;

/// <summary>
///     命名选项信息。
/// </summary>
internal readonly struct OptionInfo
{
    public readonly string property_name;
    public readonly string property_type;
    public readonly string long_name;
    public readonly char short_name;
    public readonly string? description;
    public readonly object? default_value;
    public readonly bool required;
    public readonly string value_name;
    public readonly bool takes_value;
    public readonly string? env;
    public readonly bool hide;
    public readonly string? resource_key;

    public OptionInfo(
        string propertyName, string propertyType, string longName,
        char shortName, string? description, object? defaultValue,
        bool required, string valueName, bool takesValue,
        string? env, bool hide, string? resourceKey)
    {
        property_name = propertyName;
        property_type = propertyType;
        long_name = longName;
        short_name = shortName;
        this.description = description;
        default_value = defaultValue;
        this.required = required;
        value_name = valueName;
        takes_value = takesValue;
        this.env = env;
        this.hide = hide;
        resource_key = resourceKey;
    }
}