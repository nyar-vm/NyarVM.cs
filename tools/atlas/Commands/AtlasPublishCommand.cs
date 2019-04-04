using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     publish 子命令：发布 test → main（diff 预览 + 确认 + save + deploy）
/// </summary>
[Command("publish", "发布 test → main（diff 预览 + 确认 + save + deploy）")]
public sealed class AtlasPublishCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     数据库连接字符串（覆盖配置文件）
    /// </summary>
    [Option('c', "connection-string", "数据库连接字符串（覆盖配置文件）")]
    public string? connectionString { get; set; }

    /// <summary>
    ///     数据库提供者（sqlite/mysql/pgsql）
    /// </summary>
    [Option('p', "provider", "数据库提供者（sqlite/mysql/pgsql）")]
    public string? provider { get; set; }

    /// <summary>
    ///     跳过 diff 确认，直接执行
    /// </summary>
    [Option('y', "yes", "跳过 diff 确认，直接执行")]
    public bool yes { get; set; }

    /// <summary>
    ///     干跑模式，只预览不执行
    /// </summary>
    [Option('d', "dry-run", "干跑模式，只预览不执行")]
    public bool dryRun { get; set; }

    /// <summary>
    ///     发布标签/版本号
    /// </summary>
    [Option('t', "tag", "发布标签/版本号")]
    public string? tag { get; set; }

    /// <summary>
    ///     执行 publish 命令
    /// </summary>
    public async Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = await AtlasCommands.publish(
            config,
            connectionString,
            provider,
            yes,
            dryRun,
            tag);

        return result == 0 ? ExitCode.Success : ExitCode.Error;
    }
}
