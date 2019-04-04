using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion run 命令：构建并运行项目
/// </summary>
[Command("run", "构建并运行项目")]
public sealed class LegionRunCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     编译目标
    /// </summary>
    [Option('t', "target", "编译目标")]
    public string? target { get; set; }

    /// <summary>
    ///     入口函数名
    /// </summary>
    [Option('f', "function", "入口函数名")]
    public string? function { get; set; }

    /// <summary>
    ///     传递给入口函数的参数（逗号分隔）
    /// </summary>
    [Option('a', "arg", "传递给入口函数的参数（逗号分隔）")]
    public string? args { get; set; }

    /// <summary>
    ///     是否输出详细信息
    /// </summary>
    [Option('v', "verbose", "输出详细信息")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 run 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        return Task.FromResult(
            LegionHelper.execute_run_command(projectDir, target, function, args, verbose));
    }
}
