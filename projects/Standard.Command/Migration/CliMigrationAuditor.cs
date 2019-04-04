namespace Std.Command.Migration;

/// <summary>
///     CLI 工具迁移审计器，扫描组织中的所有 CLI 工具并生成迁移状态报告
/// </summary>
public static class CliMigrationAuditor
{
    /// <summary>
    ///     生成全公司 CLI 工具 Iris 采用率审计报告
    /// </summary>
    public static MigrationAuditReport generate_report()
    {
        var entries = get_all_cli_tools();

        return new MigrationAuditReport
        {
            entries = entries,
            generated_at = DateTimeOffset.UtcNow,
            total_cli_tools = entries.Count(e => e.status != MigrationStatus.exempt),
            migrated_count = entries.Count(e => e.status == MigrationStatus.migrated),
            in_progress_count = entries.Count(e => e.status == MigrationStatus.in_progress),
            planned_count = entries.Count(e => e.status == MigrationStatus.planned),
            pending_count = entries.Count(e => e.status == MigrationStatus.pending)
        };
    }

    /// <summary>
    ///     获取全公司所有 CLI 工具列表
    /// </summary>
    public static IReadOnlyList<CliToolAuditEntry> get_all_cli_tools()
    {
        return new List<CliToolAuditEntry>
        {
            new()
            {
                tool_name = "Valkyrie CLI",
                project_path = "Valkyrie.cs/projects/Valkyrie",
                current_framework = "System.CommandLine",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "GGScript/GGShader 编译器主入口，管道编排控制台，命令结构复杂"
            },
            new()
            {
                tool_name = "VOA ToolChain (Asgard)",
                project_path = "Valkyrie.cs/projects/Valkyrie.ToolChains/Asgard",
                current_framework = "System.CommandLine",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "Valkyrie 工具链之一，与 Valkyrie CLI 共享代码模式"
            },
            new()
            {
                tool_name = "Valkyrie ToolChains (Legion)",
                project_path = "Valkyrie.cs/projects/Valkyrie.ToolChains/Legion",
                current_framework = "System.CommandLine",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "Valkyrie 工具链，集中管理多个子命令"
            },
            new()
            {
                tool_name = "Hermes CLI",
                project_path = "Sonic.Standard.Hermes/projects/Hermes.CLI",
                current_framework = "McMaster.Extensions.CommandLineUtils",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "Hermes 数据库迁移工具主命令行"
            },
            new()
            {
                tool_name = "Hermes Atlas CLI",
                project_path = "Sonic.Standard.Hermes/projects/Hermes.Atlas.CLI",
                current_framework = "McMaster.Extensions.CommandLineUtils",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "Hermes Atlas 子模块命令行"
            },
            new()
            {
                tool_name = "Hermes Tools",
                project_path = "Sonic.Standard.Hermes/projects/Hermes.Tools",
                current_framework = "System.CommandLine",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "Hermes 辅助工具集"
            },
            new()
            {
                tool_name = "Atlas CLI",
                project_path = "Sonic.Standard.Atlas/projects/Atlas.CLI",
                current_framework = "McMaster.Extensions.CommandLineUtils",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "Atlas 包管理命令行工具"
            },
            new()
            {
                tool_name = "OlympOS Control Console",
                project_path = "OlympOS/projects/Control/OlympOS.Control.Console",
                current_framework = "System.CommandLine",
                target_framework = "net11.0",
                status = MigrationStatus.planned,
                notes = "OlympOS 分布式控制系统控制台"
            },
            new()
            {
                tool_name = "OlympOS BuildKit",
                project_path = "OlympOS/tools/OlympOS.BuildKit",
                current_framework = "原始手动解析",
                target_framework = "net7.0",
                status = MigrationStatus.pending,
                notes = "构建工具，net7.0，需要先升级目标框架"
            },
            new()
            {
                tool_name = "OlympOS Tools",
                project_path = "OlympOS/tools/OlympOS.Tools",
                current_framework = "原始手动解析",
                target_framework = "net7.0",
                status = MigrationStatus.pending,
                notes = "运维工具集，net7.0，需要先升级目标框架"
            },
            new()
            {
                tool_name = "OlympOS Clean",
                project_path = "OlympOS/tools/OlympOS.Clean",
                current_framework = "原始手动解析",
                target_framework = "net7.0",
                status = MigrationStatus.pending,
                notes = "清理工具，net7.0，需要先升级目标框架"
            },
            new()
            {
                tool_name = "OlympOS DevSimulator",
                project_path = "OlympOS/tools/OlympOS.DevSimulator",
                current_framework = "原始手动解析",
                target_framework = "-",
                status = MigrationStatus.pending,
                notes = "开发模拟器"
            },
            new()
            {
                tool_name = "Nyar Language Examples",
                project_path = "NyarVM.cs/examples/",
                current_framework = "原始手动解析",
                target_framework = "net9.0",
                status = MigrationStatus.exempt,
                notes = "示例项目，非生产工具，不需要强制迁移"
            }
        };
    }
}