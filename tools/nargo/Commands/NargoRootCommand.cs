using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nargo.Cli.Commands;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli;

/// <summary>
///     nargo 根命令 — Node / Web 工程统一支配工具
/// </summary>
[Command("nargo", "Node / Web 工程统一支配工具")]
public sealed class NargoRootCommand : ICommand
{
    /// <summary>
    ///     init 子命令：初始化 nargo 原生项目
    /// </summary>
    [Subcommand]
    public NargoInitCommand? init { get; set; }

    /// <summary>
    ///     import 子命令：导入现有 npm / pnpm 项目
    /// </summary>
    [Subcommand]
    public NargoImportCommand? import { get; set; }

    /// <summary>
    ///     install 子命令：统一依赖安装与锁定
    /// </summary>
    [Subcommand]
    public NargoInstallCommand? install { get; set; }

    /// <summary>
    ///     dev 子命令：启动开发会话
    /// </summary>
    [Subcommand]
    public NargoDevCommand? dev { get; set; }

    /// <summary>
    ///     build 子命令：统一构建
    /// </summary>
    [Subcommand]
    public NargoBuildCommand? build { get; set; }

    /// <summary>
    ///     graph 子命令：显示工程图、依赖图、入口图
    /// </summary>
    [Subcommand]
    public NargoGraphCommand? graph { get; set; }

    /// <summary>
    ///     doctor 子命令：诊断兼容问题与迁移问题
    /// </summary>
    [Subcommand]
    public NargoDoctorCommand? doctor { get; set; }

    /// <summary>
    ///     publish 子命令：发布到 npm / jsr 注册表
    /// </summary>
    [Subcommand]
    public NargoPublishCommand? publish { get; set; }

    /// <summary>
    ///     全局详细输出选项
    /// </summary>
    [Option('v', "verbose", "启用详细输出")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行根命令（无子命令时显示帮助）
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        // 无子命令时由 CommandApp 层已处理帮助输出
        // 此处仅作为兜底
        Console.WriteLine("使用 'nargo --help' 查看可用命令");
        return Task.FromResult(ExitCode.Success);
    }
}
