using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     dev 子命令：一键启动编译+生成+DB同步+运行+热重载
/// </summary>
[Command("dev", "开发模式：一键启动编译+生成+DB同步+运行+热重载")]
public sealed class AtlasDevCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     监听 Schema 文件变化，自动重新编译和同步
    /// </summary>
    [Option('w', "watch", "监听 Schema 文件变化，自动重新编译和同步")]
    public bool watch { get; set; }

    /// <summary>
    ///     目标环境
    /// </summary>
    [Option('e', "environment", "目标环境")]
    public string environment { get; set; } = "test";

    /// <summary>
    ///     应用端口
    /// </summary>
    [Option('p', "port", "应用端口")]
    public int port { get; set; } = 8080;

    /// <summary>
    ///     跳过代码生成
    /// </summary>
    [Option("no-generate", "跳过代码生成")]
    public bool noGenerate { get; set; }

    /// <summary>
    ///     跳过数据库同步
    /// </summary>
    [Option("no-save", "跳过数据库同步")]
    public bool noSave { get; set; }

    /// <summary>
    ///     执行 dev 命令
    /// </summary>
    public async Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = await AtlasCommands.dev(
            config,
            watch,
            environment,
            port,
            noGenerate,
            noSave);

        return result == 0 ? ExitCode.Success : ExitCode.Error;
    }
}
