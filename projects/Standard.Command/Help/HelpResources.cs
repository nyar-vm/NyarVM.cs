namespace Std.Command.Help;

/// <summary>
///     HelpRenderer 使用的可本地化字符串资源，内置中英文双语支持
/// </summary>
public static class HelpResources
{
    static HelpResources()
    {
        initialize_built_in_resources();
    }

    #region 可本地化字符串字段

    /// <summary>
    ///     "用法:"
    /// </summary>
    public static readonly LocalizableString usage_header = new() { key = "help.usage", default_value = "用法:" };

    /// <summary>
    ///     "命令:"
    /// </summary>
    public static readonly LocalizableString commands_header = new() { key = "help.commands", default_value = "命令:" };

    /// <summary>
    ///     "子命令:"
    /// </summary>
    public static readonly LocalizableString sub_commands_header =
        new() { key = "help.subcommands", default_value = "子命令:" };

    /// <summary>
    ///     "参数:"
    /// </summary>
    public static readonly LocalizableString arguments_header = new() { key = "help.arguments", default_value = "参数:" };

    /// <summary>
    ///     "选项:"
    /// </summary>
    public static readonly LocalizableString options_header = new() { key = "help.options", default_value = "选项:" };

    /// <summary>
    ///     "全局选项:"
    /// </summary>
    public static readonly LocalizableString global_options_header =
        new() { key = "help.global_options", default_value = "全局选项:" };

    /// <summary>
    ///     "可用命令:"
    /// </summary>
    public static readonly LocalizableString available_commands_header =
        new() { key = "help.available_commands", default_value = "可用命令:" };

    /// <summary>
    ///     "(必填)"
    /// </summary>
    public static readonly LocalizableString required_tag = new() { key = "help.required", default_value = "(必填)" };

    /// <summary>
    ///     "(可选)"
    /// </summary>
    public static readonly LocalizableString optional_tag = new() { key = "help.optional", default_value = "(可选)" };

    /// <summary>
    ///     "，默认值: {0}"
    /// </summary>
    public static readonly LocalizableString default_value_format =
        new() { key = "help.default_value", default_value = "，默认值: {0}" };

    /// <summary>
    ///     "（默认值: {0}）"
    /// </summary>
    public static readonly LocalizableString default_value_parens_format =
        new() { key = "help.default_value_parens", default_value = "（默认值: {0}）" };

    /// <summary>
    ///     " [选项] &lt;参数&gt;"
    /// </summary>
    public static readonly LocalizableString options_args_hint = new()
        { key = "help.options_args_hint", default_value = " [选项] <参数>" };

    /// <summary>
    ///     "&lt;子命令&gt;"
    /// </summary>
    public static readonly LocalizableString sub_commands_hint = new()
        { key = "help.sub_commands_hint", default_value = "<子命令>" };

    /// <summary>
    ///     "[选项]"
    /// </summary>
    public static readonly LocalizableString options_hint = new() { key = "help.options_hint", default_value = "[选项]" };

    /// <summary>
    ///     "显示帮助信息"
    /// </summary>
    public static readonly LocalizableString help_option_desc = new()
        { key = "help.help_option_desc", default_value = "显示帮助信息" };

    /// <summary>
    ///     "显示版本号"
    /// </summary>
    public static readonly LocalizableString version_option_desc =
        new() { key = "help.version_option_desc", default_value = "显示版本号" };

    /// <summary>
    ///     "运行 'app &lt;命令&gt; --help' 查看具体命令的帮助。"
    /// </summary>
    public static readonly LocalizableString run_help_hint = new()
        { key = "help.run_help_hint", default_value = "运行 'app <命令> --help' 查看具体命令的帮助。" };

    /// <summary>
    ///     "使用 '{0} --help' 查看具体命令的帮助。"
    /// </summary>
    public static readonly LocalizableString run_command_help_hint_format = new()
        { key = "help.run_command_help_hint", default_value = "使用 '{0} --help' 查看具体命令的帮助。" };

    /// <summary>
    ///     "环境变量:"
    /// </summary>
    public static readonly LocalizableString environment_variables_header =
        new() { key = "help.environment_variables", default_value = "环境变量:" };

    #endregion

    #region 内置资源初始化

    private static void initialize_built_in_resources()
    {
        ensure_resx_localizer();

        var zhCn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help.usage"] = "用法:",
            ["help.commands"] = "命令:",
            ["help.subcommands"] = "子命令:",
            ["help.arguments"] = "参数:",
            ["help.options"] = "选项:",
            ["help.global_options"] = "全局选项:",
            ["help.available_commands"] = "可用命令:",
            ["help.required"] = "(必填)",
            ["help.optional"] = "(可选)",
            ["help.default_value"] = "，默认值: {0}",
            ["help.default_value_parens"] = "（默认值: {0}）",
            ["help.options_args_hint"] = " [选项] <参数>",
            ["help.sub_commands_hint"] = "<子命令>",
            ["help.options_hint"] = "[选项]",
            ["help.help_option_desc"] = "显示帮助信息",
            ["help.version_option_desc"] = "显示版本号",
            ["help.run_help_hint"] = "运行 'app <命令> --help' 查看具体命令的帮助。",
            ["help.run_command_help_hint"] = "使用 '{0} --help' 查看具体命令的帮助。",
            ["help.environment_variables"] = "环境变量:"
        };

        var enUs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help.usage"] = "Usage:",
            ["help.commands"] = "Commands:",
            ["help.subcommands"] = "Subcommands:",
            ["help.arguments"] = "Arguments:",
            ["help.options"] = "Options:",
            ["help.global_options"] = "Global Options:",
            ["help.available_commands"] = "Available commands:",
            ["help.required"] = "(required)",
            ["help.optional"] = "(optional)",
            ["help.default_value"] = ", default: {0}",
            ["help.default_value_parens"] = " (default: {0})",
            ["help.options_args_hint"] = " [options] <arguments>",
            ["help.sub_commands_hint"] = "<subcommand>",
            ["help.options_hint"] = "[options]",
            ["help.help_option_desc"] = "Show help information",
            ["help.version_option_desc"] = "Show version information",
            ["help.run_help_hint"] = "Run 'app <command> --help' for more information on a command.",
            ["help.run_command_help_hint"] = "Run '{0} --help' for more information on a command.",
            ["help.environment_variables"] = "Environment Variables:"
        };

        if (Localizer.current is ResxLocalizer resx)
        {
            resx.add_resources("zh-CN", zhCn);
            resx.add_resources("en-US", enUs);
        }
    }

    private static void ensure_resx_localizer()
    {
        if (Localizer.current is not ResxLocalizer) Localizer.current = new ResxLocalizer();
    }

    #endregion
}