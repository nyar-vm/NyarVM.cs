using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     deploy 子命令：部署前后端到目标环境
/// </summary>
[Command("deploy", "部署前后端到目标环境")]
public sealed class AtlasDeployCommand : ICommand
{
    /// <summary>
    ///     配置文件路径（默认查找 atlas.config.von）
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     目标环境（staging/production）
    /// </summary>
    [Option('e', "environment", "目标环境（staging/production）")]
    public string environment { get; set; } = "staging";

    /// <summary>
    ///     版本号（默认自动生成 YYYYMMDD.BUILD）
    /// </summary>
    [Option('v', "version", "版本号（默认自动生成 YYYYMMDD.BUILD）")]
    public string? version { get; set; }

    /// <summary>
    ///     前端项目路径
    /// </summary>
    [Option('f', "frontend-path", "前端项目路径")]
    public string? frontendPath { get; set; }

    /// <summary>
    ///     后端项目路径
    /// </summary>
    [Option('b', "backend-path", "后端项目路径")]
    public string? backendPath { get; set; }

    /// <summary>
    ///     跳过前端部署
    /// </summary>
    [Option("skip-frontend", "跳过前端部署")]
    public bool skipFrontend { get; set; }

    /// <summary>
    ///     跳过后端部署
    /// </summary>
    [Option("skip-backend", "跳过后端部署")]
    public bool skipBackend { get; set; }

    /// <summary>
    ///     干跑模式，只预览不执行
    /// </summary>
    [Option('d', "dry-run", "干跑模式，只预览不执行")]
    public bool dryRun { get; set; }

    /// <summary>
    ///     健康检查 URL
    /// </summary>
    [Option("health-check-url", "健康检查 URL")]
    public string? healthCheckUrl { get; set; }

    /// <summary>
    ///     执行 deploy 命令
    /// </summary>
    public async Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var result = await AtlasCommands.deploy(
            config,
            environment,
            version,
            frontendPath,
            backendPath,
            skipFrontend,
            skipBackend,
            dryRun,
            healthCheckUrl);

        return result == 0 ? ExitCode.Success : ExitCode.Error;
    }
}
