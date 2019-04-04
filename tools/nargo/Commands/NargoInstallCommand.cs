using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo install 命令：统一依赖安装与锁定
/// </summary>
[Command("install", "统一依赖安装与锁定")]
public sealed class NargoInstallCommand : ICommand
{
    /// <summary>
    ///     要安装的包名（可选，不指定则安装全部依赖）
    /// </summary>
    [Option('p', "package", "要安装的包名")]
    public string? package { get; set; }

    /// <summary>
    ///     是否作为开发依赖
    /// </summary>
    [Option('D', "save-dev", "作为开发依赖安装")]
    public bool save_dev { get; set; }

    /// <summary>
    ///     执行 install 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("nargo install — 依赖安装功能待实现");
        Console.WriteLine("当前请使用 Nyar.PackageManager.DependencyResolver 进行依赖解析");
        return Task.FromResult(ExitCode.Success);
    }
}
