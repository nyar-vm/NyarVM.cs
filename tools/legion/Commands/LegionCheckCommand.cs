using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Legion.CLI.Compiler;
using Std.Data.Text.Diagnostics;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion check 命令：检查项目
/// </summary>
[Command("check", "检查项目")]
public sealed class LegionCheckCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     检查目标
    /// </summary>
    [Option('t', "target", "检查目标")]
    public string? target { get; set; }

    /// <summary>
    ///     是否输出详细信息
    /// </summary>
    [Option('v', "verbose", "输出详细信息")]
    public bool verbose { get; set; }

    /// <summary>
    ///     诊断输出格式：<c>pretty</c> / <c>short</c> / <c>detail</c> / <c>json</c>
    /// </summary>
    [Option("format", "诊断输出格式：pretty / short / detail / json")]
    public string? format { get; set; }

    /// <summary>
    ///     最小诊断级别：<c>fatal</c> / <c>error</c> / <c>warning</c> / <c>info</c> / <c>hint</c> / <c>debug</c> / <c>trace</c>
    /// </summary>
    [Option("level", "最小诊断级别：fatal / error / warning / info / hint / debug / trace")]
    public string? level { get; set; }

    /// <summary>
    ///     诊断颜色模式：<c>auto</c> / <c>always</c> / <c>never</c>
    /// </summary>
    [Option("color", "诊断颜色模式：auto / always / never")]
    public string? color { get; set; }

    /// <summary>
    ///     执行 check 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        if (!LegionHelper.try_resolve_diagnostic_options(
                projectDir,
                format,
                level,
                color,
                null,
                out var diagnosticOptions,
                out var diagnosticError))
        {
            Console.Error.WriteLine($"错误：{diagnosticError}");
            return Task.FromResult(ExitCode.Error);
        }

        return Task.FromResult(
            LegionHelper.execute_check_command(projectDir, target, verbose, diagnosticOptions));
    }
}
