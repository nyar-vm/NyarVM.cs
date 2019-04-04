using Core.Command;
using ExitCode = Core.Terminal.ExitCode;
using Valkyrie.CLI.Repl;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     repl 子命令：启动交互式 REPL
/// </summary>
[Command("repl", "启动交互式 REPL")]
public sealed class VccReplCommand : ICommand
{
    /// <summary>
    ///     执行 repl 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var repl = new VccRepl();
        repl.run();
        return Task.FromResult(ExitCode.Success);
    }
}
