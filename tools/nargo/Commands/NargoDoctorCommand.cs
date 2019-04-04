using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo doctor 命令：诊断兼容问题与迁移问题
/// </summary>
[Command("doctor", "诊断兼容问题与迁移问题")]
public sealed class NargoDoctorCommand : ICommand
{
    /// <summary>
    ///     项目目录（默认当前目录）
    /// </summary>
    [Option('d', "dir", "项目目录")]
    public string? directory { get; set; }

    /// <summary>
    ///     执行 doctor 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var directoryPath = directory ?? Directory.GetCurrentDirectory();
        DoctorCommand.execute(directoryPath);
        return Task.FromResult(ExitCode.Success);
    }
}
