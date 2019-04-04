using Core.Command;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legend.CLI.Commands;

/// <summary>
///     legend 根命令 — 多语言统一运行时 CLI
/// </summary>
[Command("legend", "多语言统一运行时 CLI")]
public sealed class LegendRootCommand : ICommand
{
    /// <summary>
    ///     eval 子命令：执行代码片段
    /// </summary>
    [Subcommand]
    public EvalCommand? eval { get; set; }

    /// <summary>
    ///     run 子命令：运行脚本文件
    /// </summary>
    [Subcommand]
    public RunCommand? run { get; set; }

    /// <summary>
    ///     build 子命令：编译脚本文件到指定目标
    /// </summary>
    [Subcommand]
    public BuildCommand? build { get; set; }

    /// <summary>
    ///     list 子命令：列出支持的语言和目标
    /// </summary>
    [Subcommand]
    public ListCommand? list { get; set; }

    /// <summary>
    ///     执行根命令（无子命令时显示帮助）
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("使用 'legend --help' 查看可用命令");
        return Task.FromResult(ExitCode.Success);
    }
}
