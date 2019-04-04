namespace Std.Data.Text.Ini;

public sealed class IniSection
{
    public string name { get; init; } = string.Empty;
    public Dictionary<string, string> entries { get; init; } = [];
}