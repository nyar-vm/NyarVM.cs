using System.Globalization;
using Std.Command.Builder;
using Std.Command.Metadata;

namespace Std.Command.Help;

/// <summary>
///     帮助文本生成器，基于命令模型自动生成格式化的帮助信息
///     用于构建器模式（CommandApp.Run + CommandRegistryBuilder）的帮助生成
///     对于属性驱动模式，使用 HelpRenderer
/// </summary>
public static class HelpGenerator
{
    /// <summary>
    ///     为多命令应用生成根帮助文本
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="appDescription">应用描述</param>
    /// <param name="commands">命令列表</param>
    /// <param name="version">版本号</param>
    /// <param name="culture">目标文化，默认使用当前 UI 文化</param>
    /// <returns>格式化的帮助文本</returns>
    public static string generate_root_help(
        string appName,
        string appDescription,
        IReadOnlyList<BuiltCommandModel> commands,
        string? version = null,
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var lines = new List<string>();

        var title = version is not null ? $"{appName} v{version}" : appName;
        lines.Add(title);
        if (!string.IsNullOrEmpty(appDescription)) lines.Add(appDescription);

        lines.Add("");
        lines.Add(HelpResources.usage_header.to_string(culture));
        lines.Add($"  {appName} <command> [options]");

        if (commands.Count > 0)
        {
            lines.Add("");
            lines.Add(HelpResources.commands_header.to_string(culture));
            var maxNameLen = commands.Max(c => c.name.Length);
            foreach (var cmd in commands.OrderBy(c => c.name))
            {
                var desc = string.IsNullOrEmpty(cmd.description) ? "" : cmd.description;
                lines.Add($"  {cmd.name.PadRight(maxNameLen + 2)}{desc}");
            }
        }

        lines.Add("");
        lines.Add(HelpResources.global_options_header.to_string(culture));
        lines.Add($"  -h, --help       {HelpResources.help_option_desc.to_string(culture)}");
        lines.Add($"  -v, --version    {HelpResources.version_option_desc.to_string(culture)}");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    ///     为单个命令生成帮助文本
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="model">命令模型</param>
    /// <param name="commandPath">命令路径（从顶层到当前命令的完整路径）</param>
    /// <param name="culture">目标文化，默认使用当前 UI 文化</param>
    /// <returns>格式化的帮助文本</returns>
    public static string generate_command_help(
        string appName,
        BuiltCommandModel model,
        string[]? commandPath = null,
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        commandPath ??= [model.name];
        var lines = new List<string>();

        var title = string.IsNullOrEmpty(model.description) ? model.name : model.description;
        var fullCommandPath = string.Join(" ", commandPath);
        lines.Add($"{appName} {fullCommandPath} — {title}");

        lines.Add("");

        var usageParts = new List<string> { appName };
        usageParts.AddRange(commandPath);
        if (model.arguments.Count > 0)
            foreach (var arg in model.arguments)
                usageParts.Add(arg.required ? $"<{arg.name}>" : $"[{arg.name}]");

        if (model.options.Count > 0) usageParts.Add(HelpResources.options_hint.to_string(culture));

        if (model.sub_commands.Count > 0) usageParts.Add(HelpResources.sub_commands_hint.to_string(culture));

        lines.Add(HelpResources.usage_header.to_string(culture));
        lines.Add($"  {string.Join(" ", usageParts)}");

        if (model.arguments.Count > 0)
        {
            lines.Add("");
            lines.Add(HelpResources.arguments_header.to_string(culture));
            var maxNameLen = model.arguments.Max(a => a.name.Length);
            foreach (var arg in model.arguments)
            {
                var required = arg.required
                    ? HelpResources.required_tag.to_string(culture)
                    : HelpResources.optional_tag.to_string(culture);
                var defVal = arg.default_value != null
                    ? string.Format(HelpResources.default_value_format.to_string(culture), arg.default_value)
                    : "";
                var desc = string.IsNullOrEmpty(arg.description) ? "" : arg.description;
                lines.Add($"  {arg.name.PadRight(maxNameLen + 2)}{desc} {required}{defVal}");
            }
        }

        if (model.options.Count > 0)
        {
            lines.Add("");
            lines.Add(HelpResources.options_header.to_string(culture));

            var optionLines = new List<(string spec, string desc)>();
            foreach (var opt in model.options)
            {
                var shortPart = opt.short_name.HasValue ? $"-{opt.short_name}, " : "    ";
                var typeHint = opt.is_flag ? "" : $" <{format_type_name(opt.type, culture)}>";
                var spec = $"{shortPart}--{opt.name}{typeHint}";
                var defVal = opt.default_value != null
                    ? string.Format(HelpResources.default_value_parens_format.to_string(culture), opt.default_value)
                    : "";
                var desc = string.IsNullOrEmpty(opt.description) ? "" : opt.description;
                optionLines.Add((spec, $"{desc} {defVal}".Trim()));
            }

            var maxSpecLen = optionLines.Max(l => l.spec.Length);
            foreach (var (spec, desc) in optionLines) lines.Add($"  {spec.PadRight(maxSpecLen + 2)}{desc}");
        }

        if (model.sub_commands.Count > 0)
        {
            lines.Add("");
            lines.Add(HelpResources.sub_commands_header.to_string(culture));
            var maxNameLen = model.sub_commands.Max(s => s.name.Length);
            foreach (var sub in model.sub_commands.OrderBy(s => s.name))
            {
                var desc = string.IsNullOrEmpty(sub.description) ? "" : sub.description;
                lines.Add($"  {sub.name.PadRight(maxNameLen + 2)}{desc}");

                if (sub.sub_commands.Count > 0)
                    foreach (var nested in sub.sub_commands.OrderBy(n => n.name))
                    {
                        var nestedDesc = string.IsNullOrEmpty(nested.description) ? "" : nested.description;
                        lines.Add($"    ├─ {nested.name.PadRight(maxNameLen - 1)}{nestedDesc}");
                    }
            }
        }

        lines.Add("");
        lines.Add(string.Format(HelpResources.run_command_help_hint_format.to_string(culture),
            $"{appName} {fullCommandPath}"));

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    ///     为属性驱动命令模型生成帮助文本
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="appDescription">应用描述</param>
    /// <param name="commands">命令信息列表</param>
    /// <param name="version">版本号</param>
    /// <param name="culture">目标文化，默认使用当前 UI 文化</param>
    /// <returns>格式化的帮助文本</returns>
    public static string generate_root_help_from_command_info(
        string appName,
        string appDescription,
        IEnumerable<CommandInfo> commands,
        string? version = null,
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var lines = new List<string>();

        var title = version is not null ? $"{appName} v{version}" : appName;
        lines.Add(title);
        if (!string.IsNullOrEmpty(appDescription)) lines.Add(appDescription);

        lines.Add("");
        lines.Add(HelpResources.usage_header.to_string(culture));
        lines.Add($"  {appName} <command> [options]");

        var cmdList = commands.ToList();
        if (cmdList.Count > 0)
        {
            lines.Add("");
            lines.Add(HelpResources.commands_header.to_string(culture));
            var maxNameLen = cmdList.Max(c => c.name.Length);
            foreach (var cmd in cmdList.OrderBy(c => c.name))
            {
                var desc = string.IsNullOrEmpty(cmd.description) ? "" : cmd.description;
                lines.Add($"  {cmd.name.PadRight(maxNameLen + 2)}{desc}");
            }
        }

        lines.Add("");
        lines.Add(HelpResources.global_options_header.to_string(culture));
        lines.Add($"  -h, --help       {HelpResources.help_option_desc.to_string(culture)}");
        lines.Add($"  -v, --version    {HelpResources.version_option_desc.to_string(culture)}");

        return string.Join(Environment.NewLine, lines);
    }

    private static string format_type_name(Type type, CultureInfo culture)
    {
        if (type == typeof(string)) return get_localized_type_name("help.type.string", "文本", culture);

        if (type == typeof(int) || type == typeof(long))
            return get_localized_type_name("help.type.integer", "整数", culture);

        if (type == typeof(double) || type == typeof(float))
            return get_localized_type_name("help.type.number", "数值", culture);

        if (type == typeof(bool)) return get_localized_type_name("help.type.flag", "标志", culture);

        if (type.IsEnum)
        {
            var names = Enum.GetNames(type);
            return string.Join("|", names);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return format_type_name(Nullable.GetUnderlyingType(type)!, culture);

        return type.Name.ToLowerInvariant();
    }

    private static string get_localized_type_name(string key, string fallback, CultureInfo culture)
    {
        return Localizer.current?.get_string(key, fallback) ?? fallback;
    }
}