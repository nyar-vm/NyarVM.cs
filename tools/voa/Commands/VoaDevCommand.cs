using System.Diagnostics;
using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.Language.Valkyrie.Config;
using Nyar.Types.Targets;
using Valkyrie.Asgard.DevServer;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa dev 命令：启动开发服务器
/// </summary>
[Command("dev", "启动开发服务器（热重载）")]
public sealed class VoaDevCommand : ICommand
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
    public string env { get; set; } = "development";

    /// <summary>
    ///     开发服务器端口
    /// </summary>
    [Option('p', "port", "开发服务器端口")]
    public int? port { get; set; }

    /// <summary>
    ///     开发服务器主机地址
    /// </summary>
    [Option('h', "host", "开发服务器主机地址")]
    public string? host { get; set; }

    /// <summary>
    ///     是否自动打开浏览器
    /// </summary>
    [Option('o', "open", "自动打开浏览器")]
    public bool open { get; set; }

    /// <summary>
    ///     执行 dev 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = VoaHelper.resolve_voa_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'（需包含 voa.von 文件）");
            return Task.FromResult(ExitCode.Error);
        }

        var devPort = port ?? 3000;
        var devHost = host ?? "localhost";
        var url = $"http://{devHost}:{devPort}";

        var configLoader = new VoaConfigLoader();
        var config = configLoader.load(projectDir);
        config.hot_reload.enabled = true;
        config.build.target_mode = TargetMode.dev;

        Console.WriteLine("VOA 开发服务器");
        Console.WriteLine($"  项目：{projectDir}");
        Console.WriteLine($"  环境：{env}");
        Console.WriteLine($"  地址：{url}");
        Console.WriteLine("  热重载：开启");

        Environment.SetEnvironmentVariable("PORT", devPort.ToString());
        Environment.SetEnvironmentVariable("VOA_ENV", env);

        using var devServer = new VoaDevServer(projectDir, devHost, devPort, config);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        if (open)
        {
            VoaHelper.open_browser(url);
        }

        try
        {
            devServer.start(cts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"开发服务器异常：{ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }
        finally
        {
            devServer.stop();
        }

        return Task.FromResult(ExitCode.Success);
    }
}
