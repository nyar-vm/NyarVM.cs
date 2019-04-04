using System.Text;
using Std.Command.Builder;
using Std.Command.Metadata;

namespace Std.Command.Completion;

/// <summary>
///     Shell 自动补全脚本生成器，支持 Bash/Zsh/PowerShell 三种 Shell
/// </summary>
public static class CompletionScriptGenerator
{
    /// <summary>
    ///     支持的 Shell 类型
    /// </summary>
    public static readonly string[] supported_shells = ["bash", "zsh", "powershell", "fish"];

    /// <summary>
    ///     为目标 Shell 生成补全脚本
    /// </summary>
    /// <param name="shell">Shell 类型（bash/zsh/powershell）</param>
    /// <param name="appName">应用命令名称</param>
    /// <param name="commands">要生成补全的命令列表</param>
    /// <returns>补全脚本文本</returns>
    public static string generate(string shell, string appName, params CommandInfo[] commands)
    {
        return shell.ToLowerInvariant() switch
        {
            "bash" => generate_bash(appName, commands),
            "zsh" => generate_zsh(appName, commands),
            "powershell" => generate_power_shell(appName, commands),
            "fish" => generate_fish(appName, commands),
            _ => throw new ArgumentException($"不支持的 Shell 类型: {shell}。支持: {string.Join(", ", supported_shells)}")
        };
    }

    /// <summary>
    ///     根据 BuiltCommandModel 生成补全脚本（内部使用）
    /// </summary>
    internal static string generate_for_commands(string shell, string appName,
        IReadOnlyList<BuiltCommandModel> commands)
    {
        var infos = commands.Select(convert_to_info).ToArray();
        return generate(shell, appName, infos);
    }

    internal static CommandInfo convert_to_info(BuiltCommandModel model)
    {
        var subCommands = model.sub_commands.Select(convert_to_info).ToList();

        var options = model.options.Select(o => new OptionInfo
        {
            long_name = o.name,
            short_name = o.short_name,
            description = o.description,
            is_flag = o.is_flag,
            aliases = o.aliases
        }).ToList();

        var arguments = model.arguments.Select(a => new ArgumentInfo
        {
            name = a.name,
            description = a.description,
            required = a.required
        }).ToList();

        return new CommandInfo
        {
            name = model.name,
            description = model.description,
            command_type = model.handler_type ?? typeof(object),
            sub_commands = subCommands,
            options = options,
            arguments = arguments
        };
    }

    #region Bash

    private static string generate_bash(string appName, CommandInfo[] commands)
    {
        var sb = new StringBuilder();
        var funcName = $"_{appName}";
        var maxDepth = calculate_max_depth(commands);

        sb.AppendLine($"# {appName} Bash 自动补全脚本");
        sb.AppendLine($"# 安装方式: source <({appName} completion bash)");
        sb.AppendLine();
        sb.AppendLine($"{funcName}()");
        sb.AppendLine("{");
        sb.AppendLine("    local cur prev words cword");
        sb.AppendLine("    _init_completion -s || return");
        sb.AppendLine();
        sb.AppendLine("    local i=1");
        sb.AppendLine("    local cmd_path=()");
        sb.AppendLine();
        sb.AppendLine("    # 跳过选项，构建命令路径");
        sb.AppendLine("    while [[ $i -lt $cword ]]; do");
        sb.AppendLine("        if [[ \"${words[i]}\" != -* ]]; then");
        sb.AppendLine("            cmd_path+=(\"${words[i]}\")");
        sb.AppendLine("        fi");
        sb.AppendLine("        ((i++))");
        sb.AppendLine("    done");
        sb.AppendLine();
        sb.AppendLine("    local cmd_depth=${#cmd_path[@]}");
        sb.AppendLine();
        sb.AppendLine("    case $cmd_depth in");

        sb.AppendLine("        0)");
        sb.AppendLine(
            $"            COMPREPLY=($(compgen -W \"{get_sub_command_names_bash(commands)}\" -- \"${{cur}}\"))");
        sb.AppendLine("            return");
        sb.AppendLine("            ;;");

        for (var depth = 1; depth <= maxDepth; depth++)
        {
            sb.AppendLine($"        {depth})");
            build_bash_command_match(commands, 0, depth, sb);
            sb.AppendLine("            ;;");
        }

        sb.AppendLine("    esac");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"complete -F {funcName} {appName}");

        return sb.ToString();
    }

    private static void build_bash_command_match(CommandInfo[] commands, int currentDepth, int targetDepth,
        StringBuilder sb)
    {
        if (currentDepth == targetDepth)
        {
            var completions = new List<string>();
            completions.Add(get_sub_command_names_bash(commands));
            var optNames = commands.SelectMany(c => c.options).ToList();
            if (optNames.Count > 0) completions.Add(get_option_names_bash(optNames));

            sb.AppendLine($"            COMPREPLY=($(compgen -W \"{string.Join(" ", completions)}\" -- \"${{cur}}\"))");
            return;
        }

        sb.AppendLine($"            case \"${{cmd_path[{currentDepth}]}}\" in");
        foreach (var cmd in commands)
        {
            sb.AppendLine($"                {cmd.name})");
            if (cmd.sub_commands.Count > 0)
            {
                build_bash_command_match([.. cmd.sub_commands], currentDepth + 1, targetDepth, sb);
            }
            else
            {
                var completions = new List<string>();
                var optNames = get_option_names_bash(cmd.options);
                if (optNames.Length > 0) completions.Add(optNames);

                if (completions.Count > 0)
                    sb.AppendLine(
                        $"                    COMPREPLY=($(compgen -W \"{string.Join(" ", completions)}\" -- \"${{cur}}\"))");
            }

            sb.AppendLine("                    ;;");
        }

        sb.AppendLine("            esac");
    }

    private static string get_option_names_bash(IReadOnlyList<OptionInfo> options)
    {
        var names = new List<string>();
        foreach (var opt in options)
        {
            names.Add($"--{opt.long_name}");
            if (opt.short_name.HasValue) names.Add($"-{opt.short_name}");

            foreach (var alias in opt.aliases) names.Add($"--{alias}");
        }

        return string.Join(" ", names);
    }

    private static string get_sub_command_names_bash(IReadOnlyList<CommandInfo> commands)
    {
        return string.Join(" ", commands.Select(c => c.name));
    }

    #endregion

    #region Zsh

    private static string generate_zsh(string appName, CommandInfo[] commands)
    {
        var sb = new StringBuilder();
        var funcName = $"_{appName}";

        sb.AppendLine($"#compdef {appName}");
        sb.AppendLine();
        sb.AppendLine($"# {appName} Zsh 自动补全脚本");
        sb.AppendLine($"# 安装方式: 放入 $fpath 目录或 source <({appName} completion zsh)");
        sb.AppendLine();
        sb.AppendLine($"{funcName}()");
        sb.AppendLine("{");
        sb.AppendLine("    local -a commands");
        sb.Append(zsh_commands_array(commands, 0));
        sb.AppendLine("    _describe 'command' commands");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"{funcName}");

        return sb.ToString();
    }

    private static string zsh_commands_array(CommandInfo[] commands, int indent)
    {
        var sb = new StringBuilder();
        var pad = new string(' ', indent + 4);

        foreach (var cmd in commands)
        {
            var desc = string.IsNullOrEmpty(cmd.description) ? cmd.name : cmd.description.Replace("'", @"'\''");
            sb.AppendLine($"{pad}'{cmd.name}:{desc}'");

            if (cmd.options.Count > 0)
                foreach (var opt in cmd.options)
                {
                    var optDesc = string.IsNullOrEmpty(opt.description)
                        ? opt.long_name
                        : opt.description.Replace("'", @"'\''");
                    var optName = opt.short_name.HasValue
                        ? $"(-{opt.short_name} --{opt.long_name})"
                        : $"--{opt.long_name}";
                    sb.AppendLine($"{pad}'{optName}[{optDesc}]'");
                }

            if (cmd.sub_commands.Count > 0) sb.Append(zsh_commands_array([.. cmd.sub_commands], indent));
        }

        return sb.ToString();
    }

    #endregion

    #region PowerShell

    private static string generate_power_shell(string appName, CommandInfo[] commands)
    {
        var sb = new StringBuilder();
        var maxDepth = calculate_max_depth(commands);

        sb.AppendLine($"# {appName} PowerShell 自动补全脚本");
        sb.AppendLine($"# 安装方式: .\\{appName}.ps1 或放入 $PROFILE");
        sb.AppendLine();
        sb.AppendLine($"Register-ArgumentCompleter -Native -CommandName {appName} -ScriptBlock {{");
        sb.AppendLine("    param($wordToComplete, $commandAst, $cursorPosition)");
        sb.AppendLine();
        sb.AppendLine("    $tokens = $commandAst.CommandElements | ForEach-Object { $_.Value }");
        sb.AppendLine("    $tokenCount = $tokens.Count");
        sb.AppendLine();
        sb.AppendLine("    # 构建命令路径（跳过选项）");
        sb.AppendLine("    $cmdPath = @()");
        sb.AppendLine("    for ($i = 1; $i -lt $tokenCount; $i++) {");
        sb.AppendLine("        if ($tokens[$i] -notmatch '^-') {");
        sb.AppendLine("            $cmdPath += $tokens[$i]");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    $depth = $cmdPath.Count");
        sb.AppendLine();
        sb.AppendLine("    if ($depth -eq 0) {");
        var rootNames = string.Join(", ", commands.Select(c => $"'{c.name}'"));
        sb.AppendLine($"        @({rootNames}) | Where-Object {{ $_ -like \"$wordToComplete*\" }}");
        sb.AppendLine("        return");
        sb.AppendLine("    }");
        sb.AppendLine();

        for (var depth = 1; depth <= maxDepth; depth++)
        {
            sb.AppendLine($"    if ($depth -eq {depth}) {{");
            build_ps_command_match(commands, 0, depth, sb);
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void build_ps_command_match(CommandInfo[] commands, int currentDepth, int targetDepth,
        StringBuilder sb)
    {
        if (currentDepth == targetDepth)
        {
            var completions = new List<string>();
            completions.AddRange(commands.Select(c => $"'{c.name}'"));
            var allOpts = commands.SelectMany(c => c.options).ToList();
            foreach (var opt in allOpts)
            {
                completions.Add($"'--{opt.long_name}'");
                if (opt.short_name.HasValue) completions.Add($"'-{opt.short_name}'");
            }

            sb.AppendLine(
                $"        @({string.Join(", ", completions)}) | Where-Object {{ $_ -like \"$wordToComplete*\" }}");
            return;
        }

        sb.AppendLine($"        switch ($cmdPath[{currentDepth}]) {{");
        foreach (var cmd in commands)
        {
            sb.AppendLine($"            '{cmd.name}' {{");
            if (cmd.sub_commands.Count > 0)
            {
                build_ps_command_match([.. cmd.sub_commands], currentDepth + 1, targetDepth, sb);
            }
            else
            {
                var completions = new List<string>();
                foreach (var opt in cmd.options)
                {
                    completions.Add($"'--{opt.long_name}'");
                    if (opt.short_name.HasValue) completions.Add($"'-{opt.short_name}'");
                }

                if (completions.Count > 0)
                    sb.AppendLine(
                        $"                @({string.Join(", ", completions)}) | Where-Object {{ $_ -like \"$wordToComplete*\" }}");
            }

            sb.AppendLine("            }");
        }

        sb.AppendLine("        }");
    }

    #endregion

    #region Fish

    private static string generate_fish(string appName, CommandInfo[] commands)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {appName} Fish 自动补全脚本");
        sb.AppendLine($"# 安装方式: 放入 ~/.config/fish/completions/{appName}.fish");
        sb.AppendLine();

        build_fish_completions(sb, appName, commands, string.Empty);

        return sb.ToString();
    }

    private static void build_fish_completions(StringBuilder sb, string appName,
        IReadOnlyList<CommandInfo> commands, string parentCondition)
    {
        var isRoot = string.IsNullOrEmpty(parentCondition);

        if (isRoot)
        {
            var commandNames = string.Join(" ", commands.Select(c => c.name));
            sb.AppendLine(
                $"complete -c {appName} -n 'not __fish_seen_subcommand_from {commandNames}' -f -a '{commandNames}'");
            sb.AppendLine();
        }

        foreach (var cmd in commands)
        {
            var condition = isRoot
                ? $"__fish_seen_subcommand_from {cmd.name}"
                : $"{parentCondition}; and __fish_seen_subcommand_from {cmd.name}";

            foreach (var opt in cmd.options)
                if (opt.is_flag)
                {
                    sb.Append($"complete -c {appName} -n '{condition}'");
                    sb.Append($" -l {opt.long_name}");
                    if (opt.short_name.HasValue) sb.Append($" -s {opt.short_name}");

                    if (!string.IsNullOrEmpty(opt.description))
                        sb.Append($" -d '{escape_fish_string(opt.description)}'");

                    sb.AppendLine();
                }
                else
                {
                    sb.Append($"complete -c {appName} -n '{condition}'");
                    sb.Append($" -l {opt.long_name}");
                    if (opt.short_name.HasValue) sb.Append($" -s {opt.short_name}");

                    sb.Append(" -r");
                    if (!string.IsNullOrEmpty(opt.description))
                        sb.Append($" -d '{escape_fish_string(opt.description)}'");

                    sb.AppendLine();
                }

            if (cmd.sub_commands.Count > 0)
            {
                var subNames = string.Join(" ", cmd.sub_commands.Select(s => s.name));
                sb.AppendLine($"complete -c {appName} -n '{condition}' -f -a '{subNames}'");

                build_fish_completions(sb, appName, cmd.sub_commands, condition);
            }

            sb.AppendLine();
        }
    }

    private static string escape_fish_string(string value)
    {
        return value.Replace("'", @"\'");
    }

    /// <summary>
    ///     计算命令树的最大深度
    /// </summary>
    private static int calculate_max_depth(IReadOnlyList<CommandInfo> commands)
    {
        if (commands.Count == 0) return 0;

        var maxChildDepth = 0;
        foreach (var cmd in commands)
            if (cmd.sub_commands.Count > 0)
            {
                var childDepth = calculate_max_depth(cmd.sub_commands);
                if (childDepth > maxChildDepth) maxChildDepth = childDepth;
            }

        return 1 + maxChildDepth;
    }

    #endregion
}