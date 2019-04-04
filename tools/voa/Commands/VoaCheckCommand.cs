using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using Valkyrie.Asgard;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa check 命令：检查项目
/// </summary>
[Command("check", "检查项目")]
public sealed class VoaCheckCommand : ICommand
{
    /// <summary>
    ///     检查路径（默认当前目录）
    /// </summary>
    [Argument(0, "检查路径")]
    public string? path { get; set; }

    /// <summary>
    ///     执行 check 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var totalErrors = VoaCheckService.check(path);
        Console.WriteLine($"检查完成：{totalErrors} 个错误");
        return Task.FromResult(totalErrors > 0 ? ExitCode.Error : ExitCode.Success);
    }
}
