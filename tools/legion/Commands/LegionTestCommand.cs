using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion test 命令：运行测试
/// </summary>
[Command("test", "运行测试")]
public sealed class LegionTestCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     测试过滤器
    /// </summary>
    [Option('f', "filter", "测试过滤器")]
    public string? filter { get; set; }

    /// <summary>
    ///     编译目标
    /// </summary>
    [Option('t', "target", "编译目标（逗号分隔，或 all）")]
    public string? target { get; set; }

    /// <summary>
    ///     显式运行器
    /// </summary>
    [Option('r', "runner", "显式运行器")]
    public string? runner { get; set; }

    /// <summary>
    ///     是否输出详细信息
    /// </summary>
    [Option('v', "verbose", "输出详细信息")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 test 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        var targets = LegionHelper.resolve_test_targets(target);

        if (LegionHelper.is_workspace(projectDir))
        {
            var (totalPassed, totalFailed, totalSkipped, allResults) =
                LegionHelper.run_tests_for_workspace_multi_target(projectDir, filter, targets, runner, verbose);

            Console.WriteLine($"测试报告：{totalPassed} 通过, {totalFailed} 失败, {totalSkipped} 跳过");

            var reportDir = Path.Combine(projectDir, "dist", "legion-test");
            LegionHelper.generate_test_report_html(reportDir, Path.GetFileName(projectDir), allResults);
            Console.WriteLine($"HTML 测试报告已生成：{Path.Combine(reportDir, "index.html")}");

            return Task.FromResult(totalFailed > 0 ? ExitCode.Error : ExitCode.Success);
        }

        var memberNameSingle = Path.GetFileName(projectDir);
        Console.WriteLine($"--- {memberNameSingle} ---");
        var (p, f, s, results) =
            LegionHelper.run_tests_for_project_multi_target(projectDir, filter, targets, runner, verbose);
        Console.WriteLine();
        Console.WriteLine($"测试报告：{p} 通过, {f} 失败, {s} 跳过");

        var reportDirSingle = Path.Combine(projectDir, "dist", "legion-test");
        LegionHelper.generate_test_report_html(reportDirSingle, memberNameSingle, results);
        Console.WriteLine($"HTML 测试报告已生成：{Path.Combine(reportDirSingle, "index.html")}");

        return Task.FromResult(f > 0 ? ExitCode.Error : ExitCode.Success);
    }
}
