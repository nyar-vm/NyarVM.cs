using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nargo.Cli.ProjectModel;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo init 命令：初始化 nargo 原生项目
/// </summary>
[Command("init", "初始化 nargo 原生项目")]
public sealed class NargoInitCommand : ICommand
{
    /// <summary>
    ///     工程目标类型
    /// </summary>
    [Argument(0, "工程目标类型：web-app / web-lib / node-app / node-lib / ssr-app / worker / cli")]
    public string target { get; set; } = "web-app";

    /// <summary>
    ///     项目目录（默认当前目录）
    /// </summary>
    [Option('d', "dir", "项目目录")]
    public string? directory { get; set; }

    /// <summary>
    ///     执行 init 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var targetKind = parse_target_kind(target);
        var directoryPath = directory ?? Directory.GetCurrentDirectory();
        InitCommand.execute(directoryPath, targetKind);
        return Task.FromResult(ExitCode.Success);
    }

    /// <summary>
    ///     将字符串解析为 NargoTargetKind
    /// </summary>
    private static NargoTargetKind parse_target_kind(string value)
    {
        return value switch
        {
            "web-app" => NargoTargetKind.BrowserApp,
            "web-lib" => NargoTargetKind.BrowserLib,
            "node-app" => NargoTargetKind.NodeApp,
            "node-lib" => NargoTargetKind.NodeLib,
            "ssr-app" => NargoTargetKind.SsrApp,
            "worker" => NargoTargetKind.Worker,
            "cli" => NargoTargetKind.Cli,
            _ => NargoTargetKind.BrowserApp
        };
    }
}
