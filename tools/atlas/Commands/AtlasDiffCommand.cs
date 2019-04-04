using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     diff 子命令：对比两个数据库/文件之间的 Schema 差异并预览 SQL
/// </summary>
[Command("diff", "对比两个数据库/文件之间的 Schema 差异并预览 SQL")]
public sealed class AtlasDiffCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     源环境（schema/test/main）
    /// </summary>
    [Option("from", "源环境（schema/test/main）")]
    public string? from { get; set; }

    /// <summary>
    ///     目标环境（schema/test/main）
    /// </summary>
    [Option("to", "目标环境（schema/test/main）")]
    public string? to { get; set; }

    /// <summary>
    ///     数据库连接字符串
    /// </summary>
    [Option('c', "connection-string", "数据库连接字符串")]
    public string? connectionString { get; set; }

    /// <summary>
    ///     数据库提供者
    /// </summary>
    [Option('p', "provider", "数据库提供者")]
    public string? provider { get; set; }

    /// <summary>
    ///     执行 diff 命令
    /// </summary>
    public async Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = await AtlasCommands.diff(
            config,
            from,
            to,
            connectionString,
            provider);

        return result == 0 ? ExitCode.Success : ExitCode.Error;
    }
}
