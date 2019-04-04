using System.Globalization;
using System.Text;
using Std.Command.Help;

namespace Std.Command;

/// <summary>
///     CLI 帮助文本构建器，用于生成格式化的帮助文本
///     运行时回退实现，当 Source Generator 未参与编译时提供基础帮助文本生成
/// </summary>
public sealed class HelpBuilder
{
    private readonly StringBuilder _sb = new();

    /// <summary>
    ///     获取或设置本地化目标文化，默认使用当前 UI 文化
    /// </summary>
    public CultureInfo culture { get; set; } = CultureInfo.CurrentUICulture;

    /// <summary>
    ///     追加应用名称和版本信息
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="version">版本号</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_header(string appName, string version)
    {
        _sb.AppendLine($"{appName} v{version}");
        _sb.AppendLine();
        return this;
    }

    /// <summary>
    ///     追加描述文本
    /// </summary>
    /// <param name="description">描述文本</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_description(string description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            _sb.AppendLine(description);
            _sb.AppendLine();
        }

        return this;
    }

    /// <summary>
    ///     追加用法说明
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="commandName">命令名称（可选）</param>
    /// <param name="usage">用途字符串</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_usage(string appName, string? commandName, string usage)
    {
        _sb.AppendLine(HelpResources.usage_header.to_string(culture));
        if (commandName is not null)
            _sb.AppendLine($"  {appName} {commandName} {usage}");
        else
            _sb.AppendLine($"  {appName} {usage}");

        _sb.AppendLine();
        return this;
    }

    /// <summary>
    ///     追加命令列表
    /// </summary>
    /// <param name="commands">命令名与描述的键值对集合</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_commands(IReadOnlyDictionary<string, string> commands)
    {
        if (commands.Count > 0)
        {
            _sb.AppendLine(HelpResources.commands_header.to_string(culture));
            var maxLen = commands.Keys.Max(k => k.Length);
            foreach (var (name, help) in commands) _sb.AppendLine($"  {name.PadRight(maxLen + 2)}{help}");

            _sb.AppendLine();
        }

        return this;
    }

    /// <summary>
    ///     追加选项列表
    /// </summary>
    /// <param name="options">选项描述元组集合（短名、长名、描述）</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_options(IReadOnlyList<(char ShortName, string LongName, string Help)> options)
    {
        if (options.Count > 0)
        {
            _sb.AppendLine(HelpResources.options_header.to_string(culture));
            foreach (var (shortName, longName, help) in options)
                _sb.AppendLine($"  -{shortName}, --{longName}".PadRight(24) + help);

            _sb.AppendLine();
        }

        return this;
    }

    /// <summary>
    ///     追加位置参数列表
    /// </summary>
    /// <param name="arguments">参数描述元组集合（名称、描述、是否必需）</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_arguments(IReadOnlyList<(string Name, string Help, bool Required)> arguments)
    {
        if (arguments.Count > 0)
        {
            _sb.AppendLine(HelpResources.arguments_header.to_string(culture));
            foreach (var (name, help, required) in arguments)
            {
                var tag = required
                    ? HelpResources.required_tag.to_string(culture)
                    : HelpResources.optional_tag.to_string(culture);
                _sb.AppendLine($"  <{name}> {tag}".PadRight(28) + help);
            }

            _sb.AppendLine();
        }

        return this;
    }

    /// <summary>
    ///     追加子命令列表
    /// </summary>
    /// <param name="subCommands">子命令描述元组集合（名称、描述、别名）</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_sub_commands(
        IReadOnlyList<(string Name, string? Description, string[]? Aliases)> subCommands)
    {
        if (subCommands.Count > 0)
        {
            _sb.AppendLine(HelpResources.sub_commands_header.to_string(culture));
            var maxLen = subCommands.Max(sc => sc.Name.Length);
            foreach (var (name, description, aliases) in subCommands)
            {
                var aliasText = aliases is not null && aliases.Length > 0
                    ? $" ({string.Join(", ", aliases)})"
                    : string.Empty;
                var desc = description ?? string.Empty;
                _sb.AppendLine($"  {name.PadRight(maxLen + 2)}{desc}{aliasText}");
            }

            _sb.AppendLine();
        }

        return this;
    }

    /// <summary>
    ///     追加环境变量映射列表
    /// </summary>
    /// <param name="envVars">环境变量映射元组集合（选项名称、环境变量名）</param>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder append_environment_variables(IReadOnlyList<(string OptionName, string? EnvVariable)> envVars)
    {
        if (envVars.Count > 0)
        {
            _sb.AppendLine(HelpResources.environment_variables_header.to_string(culture));
            var maxLen = envVars.Max(ev => ev.OptionName.Length);
            foreach (var (optionName, envVariable) in envVars)
            {
                var envText = envVariable ?? string.Empty;
                _sb.AppendLine($"  --{optionName.PadRight(maxLen + 2)}{envText}");
            }

            _sb.AppendLine();
        }

        return this;
    }

    /// <summary>
    ///     返回构建的完整帮助文本
    /// </summary>
    /// <returns>帮助文本字符串</returns>
    public override string ToString()
    {
        return _sb.ToString();
    }

    /// <summary>
    ///     清空所有已构建内容
    /// </summary>
    /// <returns>当前构建器实例，支持链式调用</returns>
    public HelpBuilder clear()
    {
        _sb.Clear();
        return this;
    }
}