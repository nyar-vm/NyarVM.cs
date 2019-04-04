using Core.Command;
using Core.Command.Argument;
using ExitCode = Core.Terminal.ExitCode;
using Valkyrie.CLI.Diag;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     diag 子命令：诊断编译管线各阶段
/// </summary>
[Command("diag", "诊断编译管线各阶段")]
public sealed class VccDiagCommand : ICommand
{
    /// <summary>
    ///     要诊断的源文件路径
    /// </summary>
    [Argument(0, "源文件路径")]
    public string? file { get; set; }

    /// <summary>
    ///     执行 diag 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        if (string.IsNullOrEmpty(file))
        {
            Console.Error.WriteLine("需要提供文件路径");
            return Task.FromResult(ExitCode.Error);
        }

        var result = DiagTool.Run(file);
        return Task.FromResult(result == 0 ? ExitCode.Success : ExitCode.Error);
    }
}
