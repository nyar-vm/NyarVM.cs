using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     status 子命令：项目状态仪表盘
/// </summary>
[Command("status", "项目状态仪表盘：Schema vs 数据库")]
public sealed class AtlasStatusCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     指定环境（默认全部）
    /// </summary>
    [Option('e', "environment", "指定环境（默认全部）")]
    public string? environment { get; set; }

    /// <summary>
    ///     执行 status 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = AtlasCommands.status(config, environment);

        return Task.FromResult(result == 0 ? ExitCode.Success : ExitCode.Error);
    }
}
