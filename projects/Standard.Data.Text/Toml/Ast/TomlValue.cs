namespace Std.Data.Text.Toml.Ast;

public sealed class TomlValue
{
    public TomlValueType type { get; init; }
    public object? raw_value { get; init; }

    public string? as_string => raw_value as string;

    public long? as_integer => raw_value as long?;

    public double? as_float => raw_value as double?;

    public bool? as_boolean => raw_value as bool?;

    public TomlValue[]? as_array => raw_value as TomlValue[];

    public Dictionary<string, TomlValue>? as_inline_table => raw_value as Dictionary<string, TomlValue>;
}