using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Valkyrie.CLI.Compiler;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     compile 子命令：编译源文件
/// </summary>
[Command("compile", "编译源文件")]
public sealed class VccCompileCommand : ICommand
{
    /// <summary>
    ///     源文件路径
    /// </summary>
    [Argument(0, "源文件路径")]
    public string file { get; set; } = string.Empty;

    /// <summary>
    ///     编译目标（nyar / wasm / clr / jvm / native）
    /// </summary>
    [Option('t', "target", "编译目标（nyar / wasm / clr / jvm / native）")]
    public string target { get; set; } = "nyar";

    /// <summary>
    ///     输出目录
    /// </summary>
    [Option('o', "output", "输出目录")]
    public string? output { get; set; }

    /// <summary>
    ///     详细输出
    /// </summary>
    [Option('v', "verbose", "详细输出")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 compile 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"错误：源文件不存在 '{file}'");
            return Task.FromResult(ExitCode.Error);
        }

        var outputDir = output ?? Path.Combine(Path.GetDirectoryName(file) ?? ".", "dist");
        var compiler = new VccCompiler();
        var result = compiler.compile(file, target, outputDir, verbose);

        if (!result.success)
        {
            Console.Error.WriteLine($"编译失败：{result.error}");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"编译完成 → {result.output_directory}");

        if (verbose)
        {
            foreach (var outputFile in result.output_files)
            {
                Console.WriteLine($"  产出：{outputFile}");
            }
        }

        return Task.FromResult(ExitCode.Success);
    }
}
