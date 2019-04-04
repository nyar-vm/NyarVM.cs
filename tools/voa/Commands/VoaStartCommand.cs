using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa start 命令：启动生产服务器
/// </summary>
[Command("start", "启动生产服务器")]
public sealed class VoaStartCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string project { get; set; } = ".";

    /// <summary>
    ///     运行环境
    /// </summary>
    [Option('e', "env", "运行环境")]
    public string env { get; set; } = "production";

    /// <summary>
    ///     服务器端口
    /// </summary>
    [Option('p', "port", "服务器端口")]
    public int? port_number { get; set; }

    /// <summary>
    ///     服务器主机地址
    /// </summary>
    [Option('h', "host", "服务器主机地址")]
    public string? host_addr { get; set; }

    /// <summary>
    ///     是否启用 SSL
    /// </summary>
    [Option("ssl", "启用 SSL")]
    public bool ssl { get; set; }

    /// <summary>
    ///     缓存策略
    /// </summary>
    [Option("cache", "缓存策略")]
    public string cache { get; set; } = "long";

    /// <summary>
    ///     执行 start 命令
    /// </summary>
    public async Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var exitCode = await StartCommand.execute(project, env, port_number, host_addr, ssl, cache);
        return exitCode == 0 ? ExitCode.Success : ExitCode.Error;
    }
}
