namespace Sonic.Data.Generator.Command;

/// <summary>
///     子命令条目信息。
/// </summary>
internal readonly struct SubCommandEntry
{
    public readonly string enum_member_name;
    public readonly string name;
    public readonly string? description;
    public readonly string[]? aliases;
    public readonly bool hide;
    public readonly string? command_key;

    public SubCommandEntry(
        string enumMemberName, string name, string? description,
        string[]? aliases, bool hide, string? commandKey)
    {
        enum_member_name = enumMemberName;
        this.name = name;
        this.description = description;
        this.aliases = aliases;
        this.hide = hide;
        command_key = commandKey;
    }
}