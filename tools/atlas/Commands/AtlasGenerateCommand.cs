using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     generate 子命令：根据 Hermes Schema 生成代码和 DDL
/// </summary>
[Command("generate", "根据 Hermes Schema 生成代码和 DDL")]
public sealed class AtlasGenerateCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     Schema 文件或目录路径（覆盖配置文件）
    /// </summary>
    [Option('s', "schema-path", "Schema 文件或目录路径（覆盖配置文件）")]
    public string? schemaPath { get; set; }

    /// <summary>
    ///     输出目录路径（覆盖配置文件）
    /// </summary>
    [Option('o', "output-path", "输出目录路径（覆盖配置文件）")]
    public string? outputPath { get; set; }

    /// <summary>
    ///     生成代码的命名空间（覆盖配置文件）
    /// </summary>
    [Option('n', "namespace", "生成代码的命名空间（覆盖配置文件）")]
    public string? @namespace { get; set; }

    /// <summary>
    ///     干跑模式，只预览不写入
    /// </summary>
    [Option('d', "dry-run", "干跑模式，只预览不写入")]
    public bool dryRun { get; set; }

    /// <summary>
    ///     执行 generate 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = AtlasCommands.generate(
            config,
            schemaPath,
            outputPath,
            @namespace,
            dryRun);

        return Task.FromResult(result == 0 ? ExitCode.Success : ExitCode.Error);
    }
}
