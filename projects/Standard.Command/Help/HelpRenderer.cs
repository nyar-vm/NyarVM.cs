using System.Globalization;
using System.Text;
using Std.Command.Builder;
using Std.Command.Metadata;

namespace Std.Command.Help;

/// <summary>
///     帮助文本渲染器，从命令定义自动生成帮助文档，支持多语言本地化
///     用于属性驱动模式（ICommand + CommandAttribute）的帮助生成
///     对于构建器模式，使用 HelpGenerator
/// </summary>
public sealed class HelpRenderer
{
    /// <summary>
    ///     终端最大宽度
    /// </summary>
    public int max_width { get; set; } = 120;

    /// <summary>
    ///     缩进宽度
    /// </summary>
    public int indent_size { get; set; } = 2;

    /// <summary>
    ///     是否显示默认值
    /// </summary>
    public bool show_default_values { get; set; } = true;

    /// <summary>
    ///     为单个命令生成帮助文本
    /// </summary>
    /// <param name="command">命令信息</param>
    /// <param name="culture">目标文化</param>
    public string render(CommandInfo command, CultureInfo culture)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', indent_size);

        sb.AppendLine($"{command.name} — {command.description}");
        sb.AppendLine();

        sb.AppendLine(HelpResources.usage_header.to_string(culture));
        sb.Append(indent);
        sb.Append("app ");
        sb.Append(command.name);
        sb.AppendLine(HelpResources.options_args_hint.to_string(culture));
        sb.AppendLine();

        if (command.sub_commands.Count > 0)
        {
            sb.AppendLine(HelpResources.sub_commands_header.to_string(culture));
            var maxSubLen = command.sub_commands.Max(s => s.name.Length) + indent_size;
            foreach (var sub in command.sub_commands)
            {
                sb.Append(indent);
                sb.Append(sub.name.PadRight(maxSubLen));
                sb.AppendLine(sub.description);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     为全部命令生成总览帮助
    /// </summary>
    /// <param name="commands">命令集合</param>
    /// <param name="culture">目标文化</param>
    public string render_all(IEnumerable<CommandInfo> commands, CultureInfo culture)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', indent_size);

        sb.AppendLine(HelpResources.available_commands_header.to_string(culture));
        var cmdList = commands.ToList();
        var maxCmdLen = cmdList.Max(c => c.name.Length) + indent_size;

        foreach (var cmd in cmdList)
        {
            sb.Append(indent);
            sb.Append(cmd.name.PadRight(maxCmdLen));
            sb.AppendLine(cmd.description);
        }

        sb.AppendLine();
        sb.AppendLine(HelpResources.run_help_hint.to_string(culture));
        return sb.ToString();
    }

    /// <summary>
    ///     为构建器模式命令模型生成帮助文本
    /// </summary>
    /// <param name="model">构建器命令模型</param>
    /// <param name="culture">区域设置</param>
    internal string render(BuiltCommandModel model, CultureInfo culture)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', indent_size);

        sb.AppendLine($"{model.name} — {model.description}");
        sb.AppendLine();

        sb.AppendLine(HelpResources.usage_header.to_string(culture));
        sb.Append(indent);
        sb.Append("app ");
        sb.Append(model.name);
        sb.AppendLine(HelpResources.options_args_hint.to_string(culture));
        sb.AppendLine();

        if (model.arguments.Count > 0)
        {
            sb.AppendLine(HelpResources.arguments_header.to_string(culture));
            var maxNameLen = model.arguments.Max(a => a.name.Length) + indent_size + 4;
            foreach (var arg in model.arguments)
            {
                var required = arg.required
                    ? HelpResources.required_tag.to_string(culture)
                    : HelpResources.optional_tag.to_string(culture);
                sb.Append(indent);
                sb.Append($"<{arg.name}>".PadRight(maxNameLen));
                sb.Append(arg.description);
                sb.Append(' ');
                sb.Append(required);

                if (show_default_values && arg.default_value != null)
                    sb.Append(string.Format(HelpResources.default_value_format.to_string(culture), arg.default_value));

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        if (model.options.Count > 0)
        {
            sb.AppendLine(HelpResources.options_header.to_string(culture));
            var maxOptLen = model.options.Max(o =>
            {
                var len = $"--{o.name}".Length;
                if (o.short_name.HasValue) len += 4;

                return len;
            }) + indent_size + 2;

            foreach (var opt in model.options)
            {
                sb.Append(indent);
                var shortPart = opt.short_name.HasValue ? $"-{opt.short_name}, " : "    ";
                var optStr = $"{shortPart}--{opt.name}";
                sb.Append(optStr.PadRight(maxOptLen));
                sb.Append(opt.description);

                if (show_default_values && opt.default_value != null)
                    sb.Append(string.Format(HelpResources.default_value_parens_format.to_string(culture),
                        opt.default_value));

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        if (model.sub_commands.Count > 0)
        {
            sb.AppendLine(HelpResources.sub_commands_header.to_string(culture));
            var maxSubLen = model.sub_commands.Max(s => s.name.Length) + indent_size;
            foreach (var sub in model.sub_commands)
            {
                sb.Append(indent);
                sb.Append(sub.name.PadRight(maxSubLen));
                sb.AppendLine(sub.description);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}