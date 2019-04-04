using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Hermes.CLI.Commands;

/// <summary>
///     generate 子命令：根据 Hermes Schema 生成代码
/// </summary>
[Command("generate", "根据 Hermes Schema 生成代码")]
internal sealed class GenerateCommand : ICommand
{
    /// <summary>
    ///     Schema 文件或目录路径
    /// </summary>
    [Option('s', "schema", "Schema 文件或目录路径")]
    public string? schema { get; set; }

    /// <summary>
    ///     输出目录路径
    /// </summary>
    [Option('o', "output", "输出目录路径")]
    public string output { get; set; } = "./generated";

    /// <summary>
    ///     命名空间
    /// </summary>
    [Option('n', "namespace", "命名空间")]
    public string? @namespace { get; set; }

    /// <summary>
    ///     生成目标（csharp/typescript/go/java/rust/sql/ggscript/rpc/all）
    /// </summary>
    [Option('t', "target", "生成目标 (csharp/typescript/go/java/rust/sql/ggscript/rpc/all)")]
    public string target { get; set; } = "all";

    /// <summary>
    ///     干跑模式，只预览不写入
    /// </summary>
    [Option('d', "dry-run", "干跑模式，只预览不写入")]
    public bool dry_run { get; set; }

    /// <summary>
    ///     执行 generate 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        HermesCommands.generate(schema, output, @namespace, target, dry_run);
        return Task.FromResult(ExitCode.Success);
    }
}
