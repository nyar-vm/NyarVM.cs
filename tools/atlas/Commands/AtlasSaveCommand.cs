using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     save 子命令：持久化写入 Schema → 数据库
/// </summary>
[Command("save", "持久化写入：Schema → 数据库")]
public sealed class AtlasSaveCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     Schema 文件或目录路径
    /// </summary>
    [Option('s', "schema-path", "Schema 文件或目录路径")]
    public string? schemaPath { get; set; }

    /// <summary>
    ///     数据库连接字符串
    /// </summary>
    [Option('c', "connection-string", "数据库连接字符串")]
    public string? connectionString { get; set; }

    /// <summary>
    ///     数据库提供者（sqlite/mysql/pgsql）
    /// </summary>
    [Option('p', "provider", "数据库提供者（sqlite/mysql/pgsql）")]
    public string? provider { get; set; }

    /// <summary>
    ///     目标环境（test/main）
    /// </summary>
    [Option('e', "environment", "目标环境（test/main）")]
    public string environment { get; set; } = "test";

    /// <summary>
    ///     干跑模式，不实际执行
    /// </summary>
    [Option('d', "dry-run", "干跑模式，不实际执行")]
    public bool dryRun { get; set; }

    /// <summary>
    ///     强制执行，跳过确认提示和 SQL 错误
    /// </summary>
    [Option('f', "force", "强制执行，跳过确认提示和 SQL 错误")]
    public bool force { get; set; }

    /// <summary>
    ///     执行 save 命令
    /// </summary>
    public async Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = await AtlasCommands.save(
            config,
            schemaPath,
            connectionString,
            provider,
            environment,
            dryRun,
            force);

        return result == 0 ? ExitCode.Success : ExitCode.Error;
    }
}
