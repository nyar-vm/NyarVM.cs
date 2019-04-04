using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo dev 命令：启动开发会话
/// </summary>
[Command("dev", "启动开发会话")]
public sealed class NargoDevCommand : ICommand
{
    /// <summary>
    ///     开发服务器端口
    /// </summary>
    [Option('p', "port", "开发服务器端口")]
    public int port { get; set; } = 3000;

    /// <summary>
    ///     是否打开浏览器
    /// </summary>
    [Option('o', "open", "自动打开浏览器")]
    public bool open_browser { get; set; }

    /// <summary>
    ///     执行 dev 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("nargo dev — 开发服务器功能待实现");
        Console.WriteLine("当前请使用 Valkyrie.Asgard.DevServer.VoaDevServer 进行开发");
        return Task.FromResult(ExitCode.Success);
    }
}
