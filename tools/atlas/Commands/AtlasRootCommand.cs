using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Atlas.CLI.Commands;

/// <summary>
///     atlas 根命令 — 管理后端基础设施 Schema 与部署
/// </summary>
[Command("atlas", "管理后端基础设施 Schema 与部署")]
public sealed class AtlasRootCommand : ICommand
{
    /// <summary>
    ///     dev 子命令：一键启动编译+生成+DB同步+运行+热重载
    /// </summary>
    [Subcommand]
    public AtlasDevCommand? dev { get; set; }

    /// <summary>
    ///     save 子命令：持久化写入 Schema → 数据库
    /// </summary>
    [Subcommand]
    public AtlasSaveCommand? save { get; set; }

    /// <summary>
    ///     load 子命令：持久化读取 数据库 → Schema
    /// </summary>
    [Subcommand]
    public AtlasLoadCommand? load { get; set; }

    /// <summary>
    ///     diff 子命令：对比两个数据库/文件之间的 Schema 差异并预览 SQL
    /// </summary>
    [Subcommand]
    public AtlasDiffCommand? diff { get; set; }

    /// <summary>
    ///     status 子命令：项目状态仪表盘
    /// </summary>
    [Subcommand]
    public AtlasStatusCommand? status { get; set; }

    /// <summary>
    ///     env 子命令：环境管理——查看和切换当前操作环境
    /// </summary>
    [Subcommand]
    public AtlasEnvCommand? env { get; set; }

    /// <summary>
    ///     generate 子命令：根据 Hermes Schema 生成代码和 DDL
    /// </summary>
    [Subcommand]
    public AtlasGenerateCommand? generate { get; set; }

    /// <summary>
    ///     publish 子命令：发布 test → main
    /// </summary>
    [Subcommand]
    public AtlasPublishCommand? publish { get; set; }

    /// <summary>
    ///     deploy 子命令：部署前后端到目标环境
    /// </summary>
    [Subcommand]
    public AtlasDeployCommand? deploy { get; set; }

    /// <summary>
    ///     全局配置文件路径选项
    /// </summary>
    [Option("config", "配置文件路径（默认查找 atlas.config.von）")]
    public string? config { get; set; }

    /// <summary>
    ///     执行根命令（无子命令时显示帮助）
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("使用 'atlas --help' 查看可用命令");
        return Task.FromResult(ExitCode.Success);
    }
}
