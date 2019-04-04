using System.Diagnostics;
using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa coverage 命令：收集代码覆盖率
/// </summary>
[Command("coverage", "收集代码覆盖率")]
public sealed class VoaCoverageCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string project { get; set; } = ".";

    /// <summary>
    ///     输出格式
    /// </summary>
    [Option('o', "output", "输出格式")]
    public string output { get; set; } = "html";

    /// <summary>
    ///     是否输出详细信息
    /// </summary>
    [Option('v', "verbose", "输出详细信息")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 coverage 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = VoaHelper.resolve_voa_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"正在收集覆盖率：{projectDir}");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "test --collect:\"XPlat Code Coverage\"",
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            Console.Error.WriteLine("错误：无法启动覆盖率收集");
            return Task.FromResult(ExitCode.Error);
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine("测试运行失败，无法生成覆盖率报告");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"覆盖率报告格式：{output}");
        Console.WriteLine("覆盖率收集完成");

        return Task.FromResult(ExitCode.Success);
    }
}
