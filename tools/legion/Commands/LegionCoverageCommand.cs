using Core.Command;
using Core.Command.Argument;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion coverage 命令：收集语法覆盖率
/// </summary>
[Command("coverage", "收集语法覆盖率")]
public sealed class LegionCoverageCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     执行 coverage 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        return Task.FromResult((ExitCode)LegionHelper.execute_coverage_command(project));
    }
}
