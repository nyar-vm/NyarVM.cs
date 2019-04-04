using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo publish 命令：发布到 npm / jsr 注册表
/// </summary>
[Command("publish", "发布到 npm / jsr 注册表")]
public sealed class NargoPublishCommand : ICommand
{
    /// <summary>
    ///     发布目标注册表（npm / jsr）
    /// </summary>
    [Option('r', "registry", "目标注册表：npm / jsr")]
    public string registry { get; set; } = "npm";

    /// <summary>
    ///     是否试运行（不实际发布）
    /// </summary>
    [Option("dry-run", "试运行，不实际发布")]
    public bool dry_run { get; set; }

    /// <summary>
    ///     执行 publish 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("nargo publish — 发布功能待实现");
        Console.WriteLine("当前请使用 Nyar.PackageRegistry.NpmRegistry 进行 npm 发布");
        Console.WriteLine("当前请使用 Nyar.PackageRegistry.JsrRegistry 进行 jsr 发布");
        return Task.FromResult(ExitCode.Success);
    }
}
