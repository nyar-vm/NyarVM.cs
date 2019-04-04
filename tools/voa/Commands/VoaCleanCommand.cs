using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa clean 命令：清理构建产物
/// </summary>
[Command("clean", "清理构建产物")]
public sealed class VoaCleanCommand : ICommand
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
    ///     执行 clean 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = VoaHelper.resolve_voa_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        var distDir = Path.Combine(projectDir, "dist");
        if (Directory.Exists(distDir))
        {
            Directory.Delete(distDir, true);
            if (verbose)
            {
                Console.WriteLine($"已清理：{distDir}");
            }
        }

        var ssgCache = Path.Combine(projectDir, ".voa_ssg");
        if (Directory.Exists(ssgCache))
        {
            Directory.Delete(ssgCache, true);
            if (verbose)
            {
                Console.WriteLine($"已清理：{ssgCache}");
            }
        }

        Console.WriteLine("清理完成");
        return Task.FromResult(ExitCode.Success);
    }
}
