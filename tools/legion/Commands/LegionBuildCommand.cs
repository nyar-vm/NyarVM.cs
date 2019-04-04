using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion build 命令：构建项目
/// </summary>
[Command("build", "构建项目")]
public sealed class LegionBuildCommand : ICommand
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
    ///     输出目录
    /// </summary>
    [Option('o', "output", "输出目录")]
    public string? output { get; set; }

    /// <summary>
    ///     是否输出详细信息
    /// </summary>
    [Option('v', "verbose", "输出详细信息")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 build 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        var legionsPath = Path.Combine(projectDir, "legions.von");
        if (File.Exists(legionsPath))
        {
            return Task.FromResult((ExitCode)LegionHelper.build_workspace(projectDir, target, output, verbose));
        }

        return Task.FromResult((ExitCode)LegionHelper.build_single_project(projectDir, target, output, verbose));
    }
}
