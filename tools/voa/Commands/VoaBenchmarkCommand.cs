using System.Diagnostics;
using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.Language.Awsl.Asgard.Compiler;
using Nyar.Language.Valkyrie.Config;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa benchmark 命令：性能基准测试
/// </summary>
[Command("benchmark", "性能基准测试")]
public sealed class VoaBenchmarkCommand : ICommand
{
    /// <summary>
    ///     编译目标
    /// </summary>
    [Argument(0, "编译目标")]
    public string target { get; set; } = "wasm";

    /// <summary>
    ///     生成代码行数
    /// </summary>
    [Option("lines", "生成代码行数")]
    public int lines { get; set; } = 100000;

    /// <summary>
    ///     生成文件数
    /// </summary>
    [Option("files", "生成文件数")]
    public int files { get; set; } = 100;

    /// <summary>
    ///     迭代次数
    /// </summary>
    [Option('i', "iterations", "迭代次数")]
    public int iterations { get; set; } = 3;

    /// <summary>
    ///     执行 benchmark 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), $"voa-bench-{Guid.NewGuid():N}");
        var sourceDir = Path.Combine(tmpDir, "source");
        Directory.CreateDirectory(sourceDir);

        try
        {
            var allTimes = new List<long>();

            for (var iter = 0; iter < iterations; iter++)
            {
                var stopwatch = Stopwatch.StartNew();
                var benchBuilder = new AsgardMultiTargetBuilder();
                var result = benchBuilder.build(new VoaBuildConfig(), sourceDir, Path.Combine(tmpDir, "output"),
                    target, false);
                stopwatch.Stop();

                allTimes.Add(stopwatch.ElapsedMilliseconds);
                Console.WriteLine(
                    $"  迭代 {iter + 1}: {stopwatch.ElapsedMilliseconds}ms — {(result.success ? "通过" : "失败")}");
            }

            Console.WriteLine($"平均: {allTimes.Average():F0}ms / 最快: {allTimes.Min()}ms / 最慢: {allTimes.Max()}ms");
        }
        finally
        {
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
        }

        return Task.FromResult(ExitCode.Success);
    }
}
