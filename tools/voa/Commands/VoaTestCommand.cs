using System.Diagnostics;
using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa test 命令：运行测试
/// </summary>
[Command("test", "运行测试")]
public sealed class VoaTestCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string project { get; set; } = ".";

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
        var projectDir = VoaHelper.resolve_voa_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"正在运行测试：{projectDir}");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = verbose ? "test --verbosity normal" : "test",
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            Console.Error.WriteLine("错误：无法启动测试运行器");
            return Task.FromResult(ExitCode.Error);
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.Error.WriteLine(error);
        }

        Console.WriteLine(process.ExitCode == 0
            ? "✅ 测试全部通过"
            : "❌ 测试失败");

        return Task.FromResult(process.ExitCode == 0 ? ExitCode.Success : ExitCode.Error);
    }
}
