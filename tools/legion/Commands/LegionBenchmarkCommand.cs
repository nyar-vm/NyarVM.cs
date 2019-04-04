using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion benchmark 命令：性能基准测试
/// </summary>
[Command("benchmark", "性能基准测试")]
public sealed class LegionBenchmarkCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     运行次数
    /// </summary>
    [Option('n', "runs", "运行次数")]
    public int runs { get; set; } = 3;

    /// <summary>
    ///     编译目标
    /// </summary>
    [Option('t', "target", "编译目标")]
    public string? target { get; set; }

    /// <summary>
    ///     是否输出详细信息
    /// </summary>
    [Option('v', "verbose", "输出详细信息")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 benchmark 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        return Task.FromResult((ExitCode)LegionHelper.execute_benchmark_command(project, runs, target, verbose));
    }
}
