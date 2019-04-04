namespace Sonic.Data.Generator.Command;

/// <summary>
///     子命令集合信息。
/// </summary>
internal readonly struct SubCommandInfo
{
    public readonly string property_name;
    public readonly string enum_full_name;
    public readonly string? default_command;
    public readonly List<SubCommandEntry> entries;

    public SubCommandInfo(
        string propertyName, string enumFullName,
        string? defaultCommand, List<SubCommandEntry> entries)
    {
        property_name = propertyName;
        enum_full_name = enumFullName;
        default_command = defaultCommand;
        this.entries = entries;
    }
}