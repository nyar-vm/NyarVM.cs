using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo build 命令：统一构建
/// </summary>
[Command("build", "统一构建")]
public sealed class NargoBuildCommand : ICommand
{
    /// <summary>
    ///     构建输出目录
    /// </summary>
    [Option('o', "output", "构建输出目录")]
    public string output { get; set; } = "dist";

    /// <summary>
    ///     构建目标模式（development / production）
    /// </summary>
    [Option('m', "mode", "构建模式：development / production")]
    public string mode { get; set; } = "production";

    /// <summary>
    ///     执行 build 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("nargo build — 构建功能待实现");
        Console.WriteLine("当前请使用 Nyar.PackageManager.Build.BuildOrchestrator 进行构建");
        return Task.FromResult(ExitCode.Success);
    }
}
