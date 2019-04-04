using Core.Command;
using Core.Command.Argument;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion cov 命令：收集语法覆盖率（coverage 的别名）
/// </summary>
[Command("cov", "收集语法覆盖率（coverage 别名）")]
public sealed class LegionCovCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     执行 cov 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        return Task.FromResult((ExitCode)LegionHelper.execute_coverage_command(project));
    }
}
