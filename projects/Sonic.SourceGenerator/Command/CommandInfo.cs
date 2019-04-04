namespace Sonic.Data.Generator.Command;

/// <summary>
///     需要生成解析器的命令类型信息。
/// </summary>
internal readonly struct CommandInfo
{
    public readonly string type_name;
    public readonly string fully_qualified_name;
    public readonly string? namespace_name;
    public readonly string name;
    public readonly string? description;
    public readonly string? long_description;
    public readonly string[]? aliases;
    public readonly bool hide;
    public readonly string? version;
    public readonly string? env_prefix;
    public readonly string? resource_key;
    public readonly List<ArgumentInfo> arguments;
    public readonly List<OptionInfo> options;
    public readonly SubCommandInfo? sub_command;
    public readonly List<string> after_parse_methods;

    public CommandInfo(
        string typeName, string fullyQualifiedName, string? namespaceName,
        string name, string? description, string? longDescription, string[]? aliases,
        bool hide, string? version, string? envPrefix, string? resourceKey,
        List<ArgumentInfo> arguments, List<OptionInfo> options,
        SubCommandInfo? subCommand, List<string> afterParseMethods
    )
    {
        type_name = typeName;
        fully_qualified_name = fullyQualifiedName;
        namespace_name = namespaceName;
        this.name = name;
        this.description = description;
        long_description = longDescription;
        this.aliases = aliases;
        this.hide = hide;
        this.version = version;
        env_prefix = envPrefix;
        resource_key = resourceKey;
        this.arguments = arguments;
        this.options = options;
        sub_command = subCommand;
        after_parse_methods = afterParseMethods;
    }
}