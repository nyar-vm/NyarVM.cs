namespace Std.Data.Text.Toml.Ast;

public sealed class TomlTable
{
    public Dictionary<string, TomlValue> entries { get; init; } = [];
    public Dictionary<string, TomlTable> tables { get; init; } = [];
    public string name { get; init; } = string.Empty;
    public bool is_array_table { get; init; }
}