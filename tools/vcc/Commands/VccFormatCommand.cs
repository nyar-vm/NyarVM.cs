using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     format 子命令：格式化源代码
/// </summary>
[Command("format", "格式化源代码")]
public sealed class VccFormatCommand : ICommand
{
    /// <summary>
    ///     目标路径（文件或目录）
    /// </summary>
    [Argument(0, "目标路径（文件或目录）")]
    public string? path { get; set; }

    /// <summary>
    ///     检查模式（不修改文件，仅报告格式问题）
    /// </summary>
    [Option('c', "check", "检查模式（不修改文件，仅报告格式问题）")]
    public bool check { get; set; }

    /// <summary>
    ///     详细输出
    /// </summary>
    [Option('v', "verbose", "详细输出")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 format 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.Error.WriteLine("format 命令暂不可用：Valkyrie.Formatter 正在迁移到新版 AST。");
        return Task.FromResult(ExitCode.Error);
    }
}
