using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     env 子命令：环境管理——查看和切换当前操作环境
/// </summary>
[Command("env", "环境管理——查看和切换当前操作环境")]
public sealed class AtlasEnvCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     子命令：空=显示，use=切换
    /// </summary>
    [Option("sub-command", "子命令：空=显示，use=切换")]
    public string? subCommand { get; set; }

    /// <summary>
    ///     环境名（与 use 子命令搭配）
    /// </summary>
    [Option("environment-name", "环境名（与 use 子命令搭配）")]
    public string? environmentName { get; set; }

    /// <summary>
    ///     执行 env 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = AtlasCommands.env(config, subCommand, environmentName);

        return Task.FromResult(result == 0 ? ExitCode.Success : ExitCode.Error);
    }
}
