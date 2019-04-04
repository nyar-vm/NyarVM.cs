using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Asgard.CLI.Commands;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa 根命令 — VOA Web 应用构建与开发工具
/// </summary>
[Command("voa", "VOA Web 应用构建与开发工具")]
public sealed class VoaRootCommand : ICommand
{
    /// <summary>
    ///     dev 子命令：启动开发服务器
    /// </summary>
    [Subcommand]
    public VoaDevCommand? dev { get; set; }

    /// <summary>
    ///     start 子命令：启动生产服务器
    /// </summary>
    [Subcommand]
    public VoaStartCommand? start { get; set; }

    /// <summary>
    ///     build 子命令：构建项目
    /// </summary>
    [Subcommand]
    public VoaBuildCommand? build { get; set; }

    /// <summary>
    ///     clean 子命令：清理构建产物
    /// </summary>
    [Subcommand]
    public VoaCleanCommand? clean { get; set; }

    /// <summary>
    ///     new 子命令：创建新项目
    /// </summary>
    [Subcommand]
    public VoaNewCommand? new_command { get; set; }

    /// <summary>
    ///     check 子命令：检查项目
    /// </summary>
    [Subcommand]
    public VoaCheckCommand? check { get; set; }

    /// <summary>
    ///     fmt 子命令：格式化代码
    /// </summary>
    [Subcommand]
    public VoaFmtCommand? fmt { get; set; }

    /// <summary>
    ///     test 子命令：运行测试
    /// </summary>
    [Subcommand]
    public VoaTestCommand? test { get; set; }

    /// <summary>
    ///     init 子命令：初始化项目配置
    /// </summary>
    [Subcommand]
    public VoaInitCommand? init { get; set; }

    /// <summary>
    ///     benchmark 子命令：性能基准测试
    /// </summary>
    [Subcommand]
    public VoaBenchmarkCommand? benchmark { get; set; }

    /// <summary>
    ///     coverage 子命令：收集代码覆盖率
    /// </summary>
    [Subcommand]
    public VoaCoverageCommand? coverage { get; set; }

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
        Console.WriteLine("使用 'voa --help' 查看可用命令");
        return Task.FromResult(ExitCode.Success);
    }
}
