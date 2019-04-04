using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     load 子命令：持久化读取 数据库 → Schema
/// </summary>
[Command("load", "持久化读取：数据库 → Schema")]
public sealed class AtlasLoadCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     输出文件路径
    /// </summary>
    [Option('o', "output-path", "输出文件路径")]
    public string? outputPath { get; set; }

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
    ///     来源环境
    /// </summary>
    [Option('e', "environment", "来源环境")]
    public string environment { get; set; } = "main";

    /// <summary>
    ///     执行 load 命令
    /// </summary>
    public async Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = await AtlasCommands.load(
            config,
            outputPath,
            connectionString,
            provider,
            environment);

        return result == 0 ? ExitCode.Success : ExitCode.Error;
    }
}
