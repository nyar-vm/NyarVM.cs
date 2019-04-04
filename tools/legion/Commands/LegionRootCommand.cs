using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion 根命令 — Legion 构建系统与项目管理 CLI
/// </summary>
[Command("legion", "Legion 构建系统与项目管理 CLI")]
public sealed class LegionRootCommand : ICommand
{
    /// <summary>
    ///     build 子命令：构建项目
    /// </summary>
    [Subcommand]
    public LegionBuildCommand? build { get; set; }

    /// <summary>
    ///     clean 子命令：清理构建产物
    /// </summary>
    [Subcommand]
    public LegionCleanCommand? clean { get; set; }

    /// <summary>
    ///     run 子命令：构建并运行项目
    /// </summary>
    [Subcommand]
    public LegionRunCommand? run { get; set; }

    /// <summary>
    ///     check 子命令：检查项目
    /// </summary>
    [Subcommand]
    public LegionCheckCommand? check { get; set; }

    /// <summary>
    ///     lint 子命令：代码静态分析
    /// </summary>
    [Subcommand]
    public LegionLintCommand? lint { get; set; }

    /// <summary>
    ///     fmt 子命令：格式化代码
    /// </summary>
    [Subcommand]
    public LegionFmtCommand? fmt { get; set; }

    /// <summary>
    ///     doc 子命令：生成文档
    /// </summary>
    [Subcommand]
    public LegionDocCommand? doc { get; set; }

    /// <summary>
    ///     test 子命令：运行测试
    /// </summary>
    [Subcommand]
    public LegionTestCommand? test { get; set; }

    /// <summary>
    ///     benchmark 子命令：性能基准测试
    /// </summary>
    [Subcommand]
    public LegionBenchmarkCommand? benchmark { get; set; }

    /// <summary>
    ///     bench 子命令：benchmark 的缩写
    /// </summary>
    [Subcommand]
    public LegionBenchCommand? bench { get; set; }

    /// <summary>
    ///     coverage 子命令：收集语法覆盖率
    /// </summary>
    [Subcommand]
    public LegionCoverageCommand? coverage { get; set; }

    /// <summary>
    ///     cov 子命令：收集语法覆盖率（别名）
    /// </summary>
    [Subcommand]
    public LegionCovCommand? cov { get; set; }

    /// <summary>
    ///     spy 子命令：内建诊断工具
    /// </summary>
    [Subcommand]
    public LegionSpyCommand? spy { get; set; }

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
        Console.WriteLine("使用 'legion --help' 查看可用命令");
        return Task.FromResult(ExitCode.Success);
    }
}
