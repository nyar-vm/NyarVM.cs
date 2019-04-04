using Core.Command;
using Core.Command.Argument;
using ExitCode = Core.Terminal.ExitCode;
using Valkyrie.CLI.Disasm;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     disasm 子命令：反汇编 .nyar 字节码
/// </summary>
[Command("disasm", "反汇编 .nyar 字节码")]
public sealed class VccDisasmCommand : ICommand
{
    /// <summary>
    ///     要反汇编的 .nyar 文件路径
    /// </summary>
    [Argument(0, ".nyar 文件路径")]
    public string? file { get; set; }

    /// <summary>
    ///     执行 disasm 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        if (string.IsNullOrEmpty(file))
        {
            Console.Error.WriteLine("需要提供 .nyar 文件路径");
            return Task.FromResult(ExitCode.Error);
        }

        var result = DisasmTool.Run(file);
        return Task.FromResult(result == 0 ? ExitCode.Success : ExitCode.Error);
    }
}
