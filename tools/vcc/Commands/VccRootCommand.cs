using Core.Command;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     vcc 根命令 — Valkyrie 编译器命令行工具
/// </summary>
[Command("vcc", "Valkyrie 编译器命令行工具")]
public sealed class VccRootCommand : ICommand
{
    /// <summary>
    ///     disasm 子命令：反汇编 .nyar 字节码
    /// </summary>
    [Subcommand]
    public VccDisasmCommand? disasm { get; set; }

    /// <summary>
    ///     diag 子命令：诊断编译管线各阶段
    /// </summary>
    [Subcommand]
    public VccDiagCommand? diag { get; set; }

    /// <summary>
    ///     compile 子命令：编译源文件
    /// </summary>
    [Subcommand]
    public VccCompileCommand? compile { get; set; }

    /// <summary>
    ///     run 子命令：编译并运行源文件
    /// </summary>
    [Subcommand]
    public VccRunCommand? run { get; set; }

    /// <summary>
    ///     test 子命令：编译并运行测试
    /// </summary>
    [Subcommand]
    public VccTestCommand? test { get; set; }

    /// <summary>
    ///     repl 子命令：启动交互式 REPL
    /// </summary>
    [Subcommand]
    public VccReplCommand? repl { get; set; }

    /// <summary>
    ///     format 子命令：格式化源代码
    /// </summary>
    [Subcommand]
    public VccFormatCommand? format { get; set; }

    /// <summary>
    ///     check 子命令：类型检查源文件
    /// </summary>
    [Subcommand]
    public VccCheckCommand? check { get; set; }

    /// <summary>
    ///     执行根命令（无子命令时显示帮助）
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("使用 'vcc --help' 查看可用命令");
        return Task.FromResult(ExitCode.Success);
    }
}
